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

        State = AgentState.Reversing;
        Debug.Log($"[ValetAgent #{_carId}] State: {State}");
        yield return _car.Follow(reverseRoute, 5f, true);

        Vector3 routeDir = (reverseRoute[reverseRoute.Length - 2] - reverseRoute[reverseRoute.Length - 1]).normalized;
        Quaternion parkingRot = Quaternion.LookRotation(routeDir, Vector3.up);

        _car.ResetPose(new Pose(_assignedSlot.transform.position, parkingRot));
        _car.Brake(3f);
        Debug.Log($"[ValetAgent #{_carId}] 후진 완료, 주차 위치에 스냅");

        State = AgentState.Parked;
        Debug.Log($"[ValetAgent #{_carId}] State: {State}");

        // 사용된 셀 Release
        foreach (Vector2Int cell in _usedCells)
        {
            if (_table != null)
                _table.Release(_carId, cell);
        }
        _usedCells.Clear();
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
