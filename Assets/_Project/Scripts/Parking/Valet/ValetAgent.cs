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
    private const float _waitWarnSeconds = 15f;
    private const float _tailMargin = 1.0f;   // 뒤차축에서 차 꽁무니까지 여유 (m)
    private HashSet<Vector2Int> _usedCells = new HashSet<Vector2Int>();   // 지금 점유 중인 셀 전체
    private List<Vector2Int> _forwardCells = new List<Vector2Int>();      // 전진 경로 셀 (지나가는 순서)
    private HashSet<Vector2Int> _reverseCells = new HashSet<Vector2Int>(); // 후진 구간 셀 (Parked까지 유지)
    private int _passedCount = 0;   // _forwardCells에서 이미 지나간 개수
    private bool _blocked = false;                // 앞차 때문에 멈춘 상태인지

    //레이캐스트 : 선->박스
    private const float _noseOffset = 3.75f;  //뒤차축~차 코까지 (m)
    private const float _detectHalfWidth = 2.3f; //감지 상자 반폭 : 차 폭 + @
    private static readonly Collider[] _detectHits = new Collider[8];

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
            // 레이캐스트를 박스로 바꿔서 필요없음
            // Vector3 carPos = _car.transform.position;
            // Vector3 carForward = _car.transform.forward;

            // int vehicleLayer = LayerMask.GetMask("Vehicle");

            //if (Physics.Raycast(carPos, carForward, out RaycastHit hit, _stopDistance, vehicleLayer) && hit.collider.gameObject != gameObject)
            // 충돌 후 다시 출발하기 위해 bolockedNow에 담음
            //bool blockedNow = Physics.Raycast(carPos, carForward, out RaycastHit hit, _stopDistance, vehicleLayer) && hit.collider.gameObject != gameObject;
            // 레이캐스트를 박스로(IsFrontBlocked)
            bool blockedNow = IsFrontBlocked(out Collider blocker);
            if (blockedNow && !_blocked) //감지됨 & 막힘상태false
            {
                // 자신이 아닌 다른 차량만 감지 (IsTrigger 제외)
                // if (hit.collider.gameObject != gameObject && !hit.collider.isTrigger)
                _blocked = true;
                _car.Brake(5f);
                Debug.Log($"[ValetAgent #{_carId}] 앞 차량({blocker.name}) 감지, 정지");
            }
            else if(!blockedNow && _blocked){
                _blocked = false;
                _car.SetSpeed(10f);
                Debug.Log($"[ValetAgent #{_carId}] 앞이 비어 다시 출발");
            }
        }

        // 지나간 전진 셀 반납
        if (State == AgentState.Moving)
        {
            ReleasePassedCells();
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

        // 남은 셀(후진 구간) Release
        ReleaseAllCells();
    }

    // 차 코 앞 _stopDistance 구간에 다른 차가 있는지 본다
    private bool IsFrontBlocked(out Collider blocker){
        Transform t = _car.transform;
        Vector3 halfExtents = new Vector3(_detectHalfWidth, 0.5f, _stopDistance * 0.5f);
        Vector3 center = t.position + t.forward * (_noseOffset + _stopDistance * 0.5f) + Vector3.up * 0.7f;

        int count =  Physics.OverlapBoxNonAlloc(center, halfExtents, _detectHits, t.rotation, LayerMask.GetMask("Vehicle"), QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            // 내 차의 콜라이더는 제외
            if (_detectHits[i].transform.IsChildOf(t))
                continue;

            blocker = _detectHits[i];
            return true;
        }

        blocker = null;
        return false;
    }

    // 차 꽁무니가 있는 셀보다 앞 순서의 전진 셀을 반납한다
    private void ReleasePassedCells()
    {
        if (_car == null || _map == null || _forwardCells.Count == 0)
            return;

        Vector3 tailPos = _car.transform.position - _car.transform.forward * _tailMargin;
        Vector2Int tailCell = _map.WorldToCell(tailPos);

        int index = _forwardCells.IndexOf(tailCell, _passedCount);
        if (index > _passedCount)
            ReleaseForwardCells(index);
    }

    // _forwardCells의 endIndex 앞까지 반납. 후진 구간과 겹치는 셀은 남겨 둔다
    private void ReleaseForwardCells(int endIndex)
    {
        for (int i = _passedCount; i < endIndex; i++)
        {
            Vector2Int cell = _forwardCells[i];
            if (_reverseCells.Contains(cell))
                continue;

            if (_table != null)
                _table.Release(_carId, cell);
            _usedCells.Remove(cell);
        }
        _passedCount = endIndex;
    }

    // 점유 중인 셀을 전부 반납한다
    private void ReleaseAllCells()
    {
        foreach (Vector2Int cell in _usedCells)
        {
            if (_table != null)
                _table.Release(_carId, cell);
        }
        _usedCells.Clear();
        _forwardCells.Clear();
        _reverseCells.Clear();
        _passedCount = 0;
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

        // 다시 출발하는 경우 이전에 잡고 있던 셀부터 반납
        ReleaseAllCells();

        _assignedSlot = _allocator.AssignEmpty();
        if (_assignedSlot == null)
        {
            State = AgentState.Full;
            Debug.Log($"[ValetAgent #{_carId}] State: {State}");
            yield break;
        }

        // 경로 계산 및 예약 시도
        Vector3[] globalPath = CalculateGlobalPath(_assignedSlot);
        List<Vector2Int> forwardCells = GetPathCells(globalPath);
        Vector3[] reverseRoute = _assignedSlot.GetReverseRoute();
        List<Vector2Int> reverseCells = GetPathCells(reverseRoute);

        List<Vector2Int> pathCells = new List<Vector2Int>(forwardCells);
        pathCells.AddRange(reverseCells);

        // 예약 시도
        bool waitingLogged = false;
        bool waitWarned = false;
        float waitStartTime = Time.time;
        while (!TryReserve(pathCells))
        {
            State = AgentState.Waiting;
            if (!waitingLogged)
            {
                Debug.Log($"[ValetAgent #{_carId}] State: {State}");
                waitingLogged = true;
            }
            if (!waitWarned && Time.time - waitStartTime > _waitWarnSeconds)
            {
                Debug.LogWarning($"[ValetAgent #{_carId}] {_waitWarnSeconds}초 넘게 Waiting 중 (다른 차가 셀을 반납하지 않음)");
                waitWarned = true;
            }
            yield return new WaitForSeconds(_retryInterval);
        }

        _forwardCells = forwardCells;
        _reverseCells = new HashSet<Vector2Int>(reverseCells);
        _passedCount = 0;

        State = AgentState.Moving;
        Debug.Log($"[ValetAgent #{_carId}] State: {State}");
        yield return _car.Follow(globalPath, 10f, false);

        Debug.Log($"[ValetAgent #{_carId}] 전역 이동 완료");

        // 전진이 끝났으니 남은 전진 셀 반납 (후진 구간 셀은 유지)
        ReleaseForwardCells(_forwardCells.Count);

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

        // 점과 점 사이도 반 칸 간격으로 찍어서 지나가는 셀을 빠짐없이 모은다
        float step = _map.CellSize * 0.5f;
        for (int i = 0; i < path.Length; i++)
        {
            int samples = 1;
            if (i > 0)
                samples = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(path[i - 1], path[i]) / step));

            for (int s = 1; s <= samples; s++)
            {
                Vector3 point = (i == 0) ? path[0] : Vector3.Lerp(path[i - 1], path[i], (float)s / samples);
                Vector2Int cell = _map.WorldToCell(point);
                if (!cells.Contains(cell))
                    cells.Add(cell);
            }
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
        ReleaseAllCells();
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
