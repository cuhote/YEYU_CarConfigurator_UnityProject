using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ValetAgent : MonoBehaviour
{
    public enum AgentState { Idle, Waiting, Moving, Reversing, Parked, Full }

    [SerializeField] private CarController _car;
    [SerializeField] private ParkingSlotAllocator _allocator;
    [SerializeField] private ReservationTable _table;
    [SerializeField] private GridMap _map;
    [SerializeField] private int _carId = 0;
    [SerializeField] private float _laneZ = 7.05f;
    [SerializeField] private float _stopDistance = 4f;

    public AgentState State { get; private set; } = AgentState.Idle;

    private Coroutine _currentCoroutine;
    private ParkingSlot _assignedSlot;
    private float _waitTimer = 0f;
    private const float _retryInterval = 0.2f;
    private HashSet<Vector2Int> _usedCells = new HashSet<Vector2Int>();

    // 후진 직접 제어용
    private float _reverseStartTime;
    private int _reverseClosestIndex = 0;
    private Vector3[] _reverseRoute;
    private Vector3 _reverseEndPos;
    private Quaternion _parkingRot;

    private void Update()
    {
        if (State == AgentState.Waiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0)
            {
                _waitTimer = _retryInterval;
                // 재시도는 코루틴에서 처리
            }
        }

        // IsOn == false일 때 앞 차량 감지
        if ((_table == null || !_table.IsOn) && State == AgentState.Moving)
        {
            Vector3 carPos = _car.transform.position;
            Vector3 carForward = _car.transform.forward;

            int vehicleLayer = LayerMask.GetMask("Vehicle");
            if (Physics.Raycast(carPos, carForward, out RaycastHit hit, _stopDistance, vehicleLayer))
            {
                // 자신이 아닌 다른 차량만 감지 (IsTrigger 제외)
                if (hit.collider.gameObject != gameObject && !hit.collider.isTrigger)
                {
                    _car.Brake(5f);
                    Debug.Log($"[ValetAgent #{_carId}] 앞 차량({hit.collider.name}) 감지, 정지");
                }
            }
        }

        // 후진 직접 제어
        if (State == AgentState.Reversing && _car != null && _reverseRoute != null)
        {
            float elapsedTime = Time.time - _reverseStartTime;

            // 15초 안전장치
            if (elapsedTime > 15f)
            {
                FinishReverse("시간 초과(15초)");
                return;
            }

            Rigidbody rb = _car.GetComponent<Rigidbody>();
            if (rb == null)
                return;

            Vector3 rbPos = rb.position;

            // (1) 가장 가까운 점의 인덱스 구하기 (이전 인덱스보다 작아지지 않게)
            float minDist = float.MaxValue;
            int closestIndex = _reverseClosestIndex;

            for (int i = _reverseClosestIndex; i < _reverseRoute.Length; i++)
            {
                float dist = Vector3.Distance(rbPos, _reverseRoute[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestIndex = i;
                }
            }

            _reverseClosestIndex = closestIndex;

            // 마지막 점까지 거리
            float distToEnd = Vector3.Distance(rbPos, _reverseEndPos);

            // (4) 종료 조건: 가장 가까운 인덱스가 마지막 점이거나 거리 0.5m 이하
            if (closestIndex == _reverseRoute.Length - 1 || distToEnd <= 0.5f)
            {
                FinishReverse("도착");
                return;
            }

            // (2) 가장 가까운 인덱스에서 1.0m 이상 앞선 첫 점을 목표로
            int targetIndex = closestIndex;
            for (int i = closestIndex + 1; i < _reverseRoute.Length; i++)
            {
                float dist = Vector3.Distance(rbPos, _reverseRoute[i]);
                if (dist >= 1.0f)
                {
                    targetIndex = i;
                    break;
                }
            }

            // 목표가 없으면 마지막 점 사용
            if (targetIndex == closestIndex && closestIndex < _reverseRoute.Length - 1)
            {
                targetIndex = _reverseRoute.Length - 1;
            }

            // (3) 후진 조향 계산 (Follow와 같은 방식)
            Vector3 toTarget = _reverseRoute[targetIndex] - rbPos;
            toTarget.y = 0f;

            Vector3 heading = -_car.transform.forward;  // 후진이므로 반대 방향
            heading.y = 0f;
            float angle = Vector3.SignedAngle(heading, toTarget, Vector3.up);

            _car.SetSteer(-angle);  // Follow와 동일한 반대 부호 처리
            _car.SetSpeed(-5f);     // 후진 (음수)
        }
    }

    private void FinishReverse(string reason)
    {
        _car.Brake(5f);

        Vector3 snapPos = new Vector3(_reverseEndPos.x, _car.transform.position.y, _reverseEndPos.z);
        _car.ResetPose(new Pose(snapPos, _parkingRot));

        State = AgentState.Parked;
        Debug.Log($"[ValetAgent #{_carId}] 후진 종료: {reason}, 주차 위치에 스냅");
        Debug.Log($"[ValetAgent #{_carId}] State: {State}");

        // 사용된 셀 Release
        foreach (Vector2Int cell in _usedCells)
        {
            if (_table != null)
                _table.Release(_carId, cell);
        }
        _usedCells.Clear();
    }

    public IEnumerator Go()
    {
        if (_car == null)
        {
            Debug.LogError($"[ValetAgent #{_carId}] Go: _car가 null입니다");
            yield break;
        }
        if (_allocator == null)
        {
            Debug.LogError($"[ValetAgent #{_carId}] Go: _allocator가 null입니다");
            yield break;
        }

        _assignedSlot = _allocator.AssignEmpty();
        if (_assignedSlot == null)
        {
            State = AgentState.Full;
            Debug.Log($"[ValetAgent #{_carId}] State: {State}");
            yield break;
        }

        // 경로 계산 및 예약 시도
        Vector3[] globalPath = CalculateGlobalPath(_assignedSlot);
        List<Vector2Int> pathCells = GetPathCells(globalPath);
        Vector3[] reverseRoute = _assignedSlot.GetReverseRoute();
        List<Vector2Int> reverseCells = GetPathCells(reverseRoute);

        pathCells.AddRange(reverseCells);

        // 예약 시도
        bool waitingLogged = false;
        while (!TryReserve(pathCells))
        {
            State = AgentState.Waiting;
            if (!waitingLogged)
            {
                Debug.Log($"[ValetAgent #{_carId}] State: {State}");
                waitingLogged = true;
            }
            yield return new WaitForSeconds(_retryInterval);
        }

        State = AgentState.Moving;
        Debug.Log($"[ValetAgent #{_carId}] State: {State}");
        yield return _car.Follow(globalPath, 10f, false);

        Debug.Log($"[ValetAgent #{_carId}] 전역 이동 완료");

        Pose entryPose = _assignedSlot.GetEntryPose();
        _car.ResetPose(entryPose);
        Debug.Log($"[ValetAgent #{_carId}] 진입 자세 세팅 완료");

        yield return new WaitForFixedUpdate();
        _car.Brake(3f);
        yield return new WaitForSeconds(0.5f);

        if (reverseRoute == null || reverseRoute.Length < 2)
        {
            Debug.LogError($"[ValetAgent #{_carId}] Go: 후진 경로가 없거나 점이 부족합니다");
            yield break;
        }

        // 후진 직접 제어 준비
        Vector3 routeDir = (reverseRoute[reverseRoute.Length - 2] - reverseRoute[reverseRoute.Length - 1]).normalized;
        _parkingRot = Quaternion.LookRotation(routeDir, Vector3.up);
        _reverseRoute = reverseRoute;
        _reverseEndPos = reverseRoute[reverseRoute.Length - 1];
        _reverseStartTime = Time.time;
        _reverseClosestIndex = 0;

        State = AgentState.Reversing;
        Debug.Log($"[ValetAgent #{_carId}] State: {State}");

        // Update에서 후진 로직이 처리될 때까지 기다리기
        while (State == AgentState.Reversing)
        {
            yield return null;
        }
    }

    private bool TryReserve(List<Vector2Int> cells)
    {
        if (_table == null || !_table.IsOn)
        {
            Debug.Log($"[ValetAgent #{_carId}] TryReserve: 예약 OFF, 예약 없이 진행");
            return true;
        }

        if (_table.TryReserve(_carId, cells, Time.time))
        {
            _usedCells = new HashSet<Vector2Int>(cells);
            return true;
        }

        return false;
    }

    private List<Vector2Int> GetPathCells(Vector3[] path)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        if (_map == null)
            return cells;

        foreach (Vector3 point in path)
        {
            Vector2Int cell = _map.WorldToCell(point);
            if (!cells.Contains(cell))
                cells.Add(cell);
        }

        return cells;
    }

    private Vector3[] CalculateGlobalPath(ParkingSlot slot)
    {
        Vector3 carPos = _car.transform.position;
        Vector3 entryPos = slot.GetEntryPose().position;

        Transform lotTransform = _map.transform.parent;
        float laneWorldZ = _laneZ;
        if (lotTransform != null)
        {
            Vector3 laneWorldPoint = lotTransform.TransformPoint(new Vector3(0, 0, _laneZ));
            laneWorldZ = laneWorldPoint.z;
        }

        return new Vector3[]
        {
            carPos,
            new Vector3(carPos.x, carPos.y, laneWorldZ),
            new Vector3(entryPos.x, entryPos.y, laneWorldZ),
            entryPos
        };
    }

    public void ResetAgent()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }

        State = AgentState.Idle;
        _assignedSlot = null;
        _usedCells.Clear();
        _reverseRoute = null;

        if (_car != null)
        {
            _car.Brake(3f);
        }
    }

    public void StartGo()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }
        _currentCoroutine = StartCoroutine(Go());
    }
}
