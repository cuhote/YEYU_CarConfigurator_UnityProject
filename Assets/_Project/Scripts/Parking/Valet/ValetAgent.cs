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
    public bool IsBlocked => _blocked;   // 앞차를 감지해 멈춘 상태인지 (라벨 표시용)

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
    private HashSet<Vector2Int> _standingCells = new HashSet<Vector2Int>(); // 서 있는 동안 차체가 차지한 셀
    private bool _blocked = false;                // 앞차 때문에 멈춘 상태인지

    // 예약 OFF에서 마주 보고 멈췄을 때 양보하기 (뒤로 물러났다가 상대가 주차를 마치면 다시 출발)
    private const float _yieldDelay = 3f;         // 서로 막고 선 채 이 시간이 지나면 한쪽이 양보한다 (초)
    private const float _yieldDistance = 6f;      // 뒤로 물러나는 최대 거리 (m)
    private const float _yieldMaxWait = 40f;      // 양보한 채 기다리는 최대 시간 (초)
    private float _blockedSince;
    private bool _yielding = false;
    private bool _yieldBacking = false;
    private ValetAgent _yieldTo;
    private Vector3 _yieldFrom;
    private float _yieldBackDistance;
    private float _yieldStartTime;
    private Vector3 _goStartPos;

    //레이캐스트 : 선->박스
    private const float _noseOffset = 3.75f;  //뒤차축~차 코까지 (m)
    private const float _detectHalfWidth = 2.3f; //감지 상자 반폭 : 차 폭 + @
    private static readonly Collider[] _detectHits = new Collider[8];

    // 후진 전 꺾기
    private const float _swingSpeedKmh = 5f;
    private const float _swingBrakeMarginDeg = 2.5f;   // 제동하는 동안 더 도는 각도만큼 일찍 멈춘다

    // 진입 준비 지점 접근
    private const float _forwardSpeedKmh = 10f;
    private const float _approachLength = 4f;      // 진입 준비 지점 앞 이 거리부터 진입선에 맞춰 들어간다 (m)
    private const float _approachLookAhead = 2.5f; // 진입선 위에서 겨냥하는 점까지의 거리 (m)
    private const float _approachDecel = 5f;       // 진입 준비 지점에 설 때 감속도 (m/s²)
    private const float _cornerRoom = 6f;          // ㄱ자 모서리 뒤에 자세를 바로잡는 데 필요한 직선 (m)

    // 후진 직접 제어용
    private float _reverseStartTime;
    private int _reverseClosestIndex = 0;
    private Vector3[] _reverseRoute;
    private Vector3 _reverseEndPos;
    private Quaternion _parkingRot;
    private const float _wheelBase = 2.883f;
    private bool _reverseStopping = false;          // 도착 지점 앞에서 제동을 건 뒤 멈추기를 기다리는 중
    private const float _reverseDecel = 5f;         // 도착 지점에 설 때 감속도 (m/s²)
    private const float _reverseLookAhead = 1.0f;   // 후진 중 겨냥하는 점까지의 거리 (m)
    private const float _snapMaxOffset = 0.3f;      // 도착 오차가 이보다 작을 때만 주차 위치에 맞춘다 (m)
    private const float _snapMaxAngle = 10f;        // 도착 방향 오차가 이보다 작을 때만 맞춘다 (도)

    // 주차 자세 바로잡기 (앞으로 뺐다가 다시 후진)
    private const float _correctOffset = 0.15f;     // 옆으로 이보다 어긋나면 바로잡는다 (m)
    private const float _correctAngle = 5f;         // 방향이 이보다 어긋나면 바로잡는다 (도)
    private const float _correctDistance = 4f;      // 앞으로 빼는 거리 (m)
    private const int _maxCorrections = 2;          // 바로잡기 최대 횟수
    private int _correctionCount = 0;
    private bool _needsCorrection = false;

    private void Start()
    {
        // 차 머리 위 상태 라벨 연결
        if (_car != null && GetComponent<VehicleStatusLabel>() == null)
            gameObject.AddComponent<VehicleStatusLabel>().Bind(this, _car.transform);
    }

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
            if (_yielding)
            {
                UpdateYield();
            }
            else
            {
                bool blockedNow = IsFrontBlocked(out Collider blocker);
                if (blockedNow && !_blocked) //감지됨 & 막힘상태false
                {
                    _blocked = true;
                    _blockedSince = Time.time;
                    _car.Brake(5f);
                    Debug.Log($"[ValetAgent #{_carId}] 앞 차량({blocker.name}) 감지, 정지");
                }
                else if (!blockedNow && _blocked)
                {
                    _blocked = false;
                    _car.SetSpeed(_forwardSpeedKmh);
                    Debug.Log($"[ValetAgent #{_carId}] 앞이 비어 다시 출발");
                }
                else if (blockedNow && _blocked && Time.time - _blockedSince > _yieldDelay)
                {
                    TryStartYield(blocker);
                }
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

            // 제동을 건 뒤에는 완전히 멈출 때까지 기다렸다가 끝낸다
            if (_reverseStopping)
            {
                if (Mathf.Abs(_car.SpeedKmh) < 0.1f)
                    FinishReverse("도착");
                return;
            }

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

            // 주차했을 때 차가 보는 방향 기준으로, 도착 지점까지 남은 거리
            Vector3 parkDir = _parkingRot * Vector3.forward;
            Vector3 fromEnd = rbPos - _reverseEndPos;
            fromEnd.y = 0f;
            float remaining = Vector3.Dot(fromEnd, parkDir);

            // (4) 종료 조건: 지금 속도에서 멈추는 데 필요한 거리만큼 남으면 제동.
            // 도착 지점에서 멀리 벗어난 차가 깊이만 맞았다고 멈추지 않게 거리도 본다
            float speedMps = Mathf.Abs(_car.SpeedKmh) / 3.6f;
            float brakeDistance = speedMps * speedMps / (2f * _reverseDecel);
            if (remaining <= brakeDistance + 0.02f && fromEnd.magnitude < 2f)
            {
                _reverseStopping = true;
                _car.SetSteer(0f);
                _car.Brake(_reverseDecel);
                return;
            }

            // (2) 가장 가까운 인덱스에서 _reverseLookAhead 이상 앞선 첫 점을 목표로
            Vector3 targetPoint = Vector3.zero;
            bool hasTarget = false;
            for (int i = closestIndex + 1; i < _reverseRoute.Length; i++)
            {
                if (Vector3.Distance(rbPos, _reverseRoute[i]) >= _reverseLookAhead)
                {
                    targetPoint = _reverseRoute[i];
                    hasTarget = true;
                    break;
                }
            }

            // 남은 점이 가까우면 주차 방향 직선을 도착 지점 너머로 늘린 점을 겨냥한다.
            // 도착 지점 자체를 겨냥하면 가까워질수록 방향이 흔들린다
            if (!hasTarget)
                targetPoint = _reverseEndPos + parkDir * (remaining - _reverseLookAhead);

            // (3) 후진 조향 계산 (Follow와 같은 방식)
            Vector3 toTarget = targetPoint - rbPos;
            toTarget.y = 0f;

            Vector3 heading = -_car.transform.forward;  // 후진이므로 반대 방향
            heading.y = 0f;
            float angle = Vector3.SignedAngle(heading, toTarget, Vector3.up);

            // 지나는 각(angle) -> 핸들 각도로 변경
            float targetDist = Mathf.Max(toTarget.magnitude, 0.1f);
            float steer = Mathf.Atan(2f * _wheelBase * Mathf.Sin(angle * Mathf.Deg2Rad) / targetDist) * Mathf.Rad2Deg;

            _car.SetSteer(-steer);  // 후진 (음수)
            _car.SetSpeed(-5f);     // 후진 (음수)
        }
    }

    private void FinishReverse(string reason)
    {
        _car.Brake(_reverseDecel);
        _car.SetSteer(0f);
        _reverseStopping = false;

        Vector3 parkDir = _parkingRot * Vector3.forward;
        Vector3 offset = _car.transform.position - _reverseEndPos;
        offset.y = 0f;
        float sideOffset = Mathf.Abs(Vector3.Dot(offset, Vector3.Cross(Vector3.up, parkDir)));
        float yawError = Mathf.Abs(Mathf.DeltaAngle(_car.transform.eulerAngles.y, _parkingRot.eulerAngles.y));
        Debug.Log($"[ValetAgent #{_carId}] 후진 종료: {reason}, 위치 오차 {offset.magnitude:F2}m (옆 {sideOffset:F2}m), 방향 오차 {yawError:F1}도");

        // 비뚤게 들어왔으면 앞으로 뺐다가 다시 후진해서 바로잡는다 (Go 코루틴이 이어서 처리)
        bool crooked = sideOffset > _correctOffset || yawError > _correctAngle;
        if (crooked && _correctionCount < _maxCorrections)
        {
            _correctionCount++;
            _needsCorrection = true;
            _reverseRoute = null;   // 바로잡는 동안 Update의 후진 제어를 멈춘다
            Debug.Log($"[ValetAgent #{_carId}] 자세 바로잡기 {_correctionCount}회차 시작");
            return;
        }

        // 오차가 작을 때만 주차 위치에 맞춘다. 크면 순간이동처럼 보이므로 그대로 둔다
        if (offset.magnitude <= _snapMaxOffset && yawError <= _snapMaxAngle)
        {
            Vector3 snapPos = new Vector3(_reverseEndPos.x, _car.transform.position.y, _reverseEndPos.z);
            _car.ResetPose(new Pose(snapPos, _parkingRot));
        }
        else
        {
            Debug.LogWarning($"[ValetAgent #{_carId}] 주차 위치에서 많이 어긋남 (진입·꺾기 오차 확인)");
        }

        State = AgentState.Parked;
        Debug.Log($"[ValetAgent #{_carId}] State: {State}");

        // 남은 셀(후진 구간) Release
        ReleaseAllCells();
    }

    // 서로 막고 선 상대가 있으면, 번호가 작은 차가 양보한다
    private void TryStartYield(Collider blocker)
    {
        ValetAgent other = blocker.GetComponentInParent<ValetAgent>();

        // 상대도 나 때문에(또는 다른 차 때문에) 멈춰 선 경우에만 양보가 필요하다.
        // 상대가 움직이는 중이면 지나갈 때까지 기다리면 된다
        if (other == null || other == this || other.State != AgentState.Moving || !other._blocked || other._yielding)
            return;
        if (_carId > other._carId)
            return;   // 번호가 큰 차가 먼저 간다

        _yielding = true;
        _yieldBacking = true;
        _yieldTo = other;
        _yieldFrom = _car.transform.position;
        _yieldStartTime = Time.time;
        // 출발한 자리보다 더 뒤로는 물러나지 않는다 (벽이나 뒤차를 보지 않으므로)
        _yieldBackDistance = Mathf.Min(_yieldDistance, Vector3.Distance(_yieldFrom, _goStartPos));

        _car.SetSpeed(-_swingSpeedKmh);
        Debug.Log($"[ValetAgent #{_carId}] 차량 #{other._carId}에게 양보: {_yieldBackDistance:F1}m 후진");
    }

    // 양보 중: 뒤로 물러난 뒤, 상대가 주차를 마칠 때까지 기다렸다가 다시 출발한다
    private void UpdateYield()
    {
        if (_yieldBacking)
        {
            float backed = Vector3.Distance(_car.transform.position, _yieldFrom);
            if (backed >= _yieldBackDistance - 0.2f)
            {
                _yieldBacking = false;
                _car.Brake(5f);
            }
            return;
        }

        bool otherDone = _yieldTo == null
            || _yieldTo.State == AgentState.Parked
            || _yieldTo.State == AgentState.Full
            || _yieldTo.State == AgentState.Idle;
        bool waitedTooLong = Time.time - _yieldStartTime > _yieldMaxWait;

        if (otherDone || waitedTooLong)
        {
            _yielding = false;
            _yieldTo = null;
            _blocked = false;
            _car.SetSpeed(_forwardSpeedKmh);
            Debug.Log($"[ValetAgent #{_carId}] 양보 끝, 다시 출발");
        }
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
        _standingCells.Clear();
        _forwardCells.Clear();
        _reverseCells.Clear();
        _passedCount = 0;
    }

    // 지금 서 있는 자리의 셀을 잡아 둔다. 출발 전이나 대기 중에 다른 차가 이 차를 지나가지 않게 한다.
    // 여러 대를 함께 출발시킬 때는 모든 차의 HoldPosition()을 먼저 부른 뒤 StartGo()를 부른다
    public void HoldPosition()
    {
        if (_table == null || !_table.IsOn || _map == null || _car == null)
            return;

        Transform t = _car.transform;
        Vector3 forward = t.forward;
        forward.y = 0f;
        forward.Normalize();

        List<Vector2Int> bodyCells = new List<Vector2Int>();
        AddBodyCells(bodyCells, t.position, forward);

        foreach (Vector2Int cell in bodyCells)
        {
            // 다른 차가 이미 잡은 셀은 건너뛴다 (한 셀에는 한 대만 기록된다)
            if (_table.TryHold(_carId, cell))
            {
                _standingCells.Add(cell);
                _usedCells.Add(cell);
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

        // 다시 출발하는 경우 이전에 잡고 있던 셀부터 반납
        ReleaseAllCells();
        _blocked = false;
        _yielding = false;
        _goStartPos = _car.transform.position;

        _assignedSlot = _allocator.AssignEmpty();
        if (_assignedSlot == null)
        {
            State = AgentState.Full;
            Debug.Log($"[ValetAgent #{_carId}] State: {State}");
            HoldPosition();   // 만차로 서 있는 자리도 다른 차가 지나가지 않게
            yield break;
        }

        // 경로 계산 및 예약 시도
        Pose entryPose = _assignedSlot.GetEntryPose();
        Vector3[] globalPath = CalculateGlobalPath(entryPose);
        // 예약 셀은 진입 준비 지점까지 포함해서 계산한다
        List<Vector3> forwardPoints = new List<Vector3>(globalPath) { entryPose.position };
        List<Vector2Int> forwardCells = GetPathCells(forwardPoints.ToArray(), false);
        Vector3[] reverseRoute = _assignedSlot.GetReverseRoute();
        List<Vector2Int> reverseCells = GetPathCells(reverseRoute, true);
        // 꺾기 구간도 후진 구간처럼 Parked까지 잡아 둔다
        reverseCells.AddRange(GetPathCells(_assignedSlot.GetSwingRoute(), false));

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
                HoldPosition();   // 기다리는 동안 서 있는 자리를 잡아 둔다
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
        if (globalPath.Length > 1)
            yield return _car.Follow(globalPath, _forwardSpeedKmh, false);

        // 순간이동 없이 진입선을 따라 들어가 진입 준비 지점에 방향을 맞춰 선다
        yield return ApproachEntry(entryPose);

        Debug.Log($"[ValetAgent #{_carId}] 전역 이동 완료");

        // 전진이 끝났으니 남은 전진 셀 반납 (후진 구간 셀은 유지)
        ReleaseForwardCells(_forwardCells.Count);

        // 많이 어긋났을 때만 진입 자세로 옮긴다 (출발 위치가 진입 지점에 너무 가깝거나 비스듬한 경우)
        Vector3 entryOffset = _car.transform.position - entryPose.position;
        entryOffset.y = 0f;
        float entryYawError = Mathf.Abs(Mathf.DeltaAngle(_car.transform.eulerAngles.y, entryPose.rotation.eulerAngles.y));
        Debug.Log($"[ValetAgent #{_carId}] 진입 준비 지점 도착: 위치 오차 {entryOffset.magnitude:F2}m, 방향 오차 {entryYawError:F1}도");
        // 방향 오차는 꺾기가 목표 방향까지 돌면서 흡수하므로 넉넉하게 본다
        if (entryOffset.magnitude > 1f || entryYawError > 45f)
        {
            Debug.LogWarning($"[ValetAgent #{_carId}] 진입 자세가 많이 어긋나 강제로 맞춤 (출발 위치 확인)");
            _car.ResetPose(entryPose);
            yield return new WaitForFixedUpdate();
        }

        _car.Brake(3f);
        yield return new WaitForSeconds(0.5f);

        // 통로에서 주차칸 반대쪽으로 꺾어 비스듬히 선다 (꺾기 각도가 0이면 건너뜀)
        Pose reverseStartPose = _assignedSlot.GetReverseStartPose();
        float swingDeg = Vector3.SignedAngle(entryPose.forward, reverseStartPose.forward, Vector3.up);
        if (Mathf.Abs(swingDeg) > 1f)
        {
            yield return Swing(reverseStartPose.rotation.eulerAngles.y, Mathf.Sign(swingDeg), _assignedSlot.SwingSteerDeg);
            Debug.Log($"[ValetAgent #{_carId}] 꺾기 완료 ({swingDeg:F0}도)");
            yield return new WaitForSeconds(0.5f);
        }

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
        _reverseStopping = false;
        _correctionCount = 0;
        _needsCorrection = false;

        State = AgentState.Reversing;
        Debug.Log($"[ValetAgent #{_carId}] State: {State}");

        // Update에서 후진 로직이 처리될 때까지 기다리기
        while (State == AgentState.Reversing)
        {
            if (_needsCorrection)
            {
                _needsCorrection = false;
                yield return new WaitForSeconds(0.3f);
                yield return PullForward();
                yield return new WaitForSeconds(0.3f);

                // 주차 방향 직선을 따라 다시 후진
                _reverseRoute = BuildStraightReverseRoute();
                _reverseStartTime = Time.time;
                _reverseClosestIndex = 0;
                _reverseStopping = false;
            }

            yield return null;
        }
    }

    // 주차 방향 직선에 붙으면서 _correctDistance만큼 앞으로 뺀다
    private IEnumerator PullForward()
    {
        Vector3 parkDir = _parkingRot * Vector3.forward;

        while (true)
        {
            Vector3 carPos = _car.transform.position;
            Vector3 fromEnd = carPos - _reverseEndPos;
            fromEnd.y = 0f;
            float along = Vector3.Dot(fromEnd, parkDir);   // 주차 위치에서 앞으로 나온 거리

            float speedMps = Mathf.Abs(_car.SpeedKmh) / 3.6f;
            float brakeDistance = speedMps * speedMps / (2f * _reverseDecel);
            if (_correctDistance - along <= brakeDistance + 0.02f)
                break;

            // 직선 위, 차보다 조금 앞의 점을 겨냥
            Vector3 toTarget = _reverseEndPos + parkDir * (along + _approachLookAhead) - carPos;
            toTarget.y = 0f;
            Vector3 heading = _car.transform.forward;
            heading.y = 0f;
            float angle = Vector3.SignedAngle(heading, toTarget, Vector3.up);
            float steer = Mathf.Atan(2f * _wheelBase * Mathf.Sin(angle * Mathf.Deg2Rad) / Mathf.Max(toTarget.magnitude, 0.1f)) * Mathf.Rad2Deg;

            _car.SetSteer(steer);
            _car.SetSpeed(_swingSpeedKmh);
            yield return new WaitForFixedUpdate();
        }

        _car.SetSteer(0f);
        _car.Brake(_reverseDecel);
        while (Mathf.Abs(_car.SpeedKmh) > 0.1f)
            yield return new WaitForFixedUpdate();
    }

    // 지금 위치에서 주차 위치까지, 주차 방향 직선 위의 점들 (0.5m 간격)
    private Vector3[] BuildStraightReverseRoute()
    {
        Vector3 parkDir = _parkingRot * Vector3.forward;
        Vector3 fromEnd = _car.transform.position - _reverseEndPos;
        fromEnd.y = 0f;
        float along = Mathf.Max(Vector3.Dot(fromEnd, parkDir), 1f);

        int count = Mathf.Max(2, Mathf.CeilToInt(along / 0.5f) + 1);
        Vector3[] route = new Vector3[count];
        for (int i = 0; i < count; i++)
            route[i] = _reverseEndPos + parkDir * (along * (1f - (float)i / (count - 1)));
        return route;
    }

    // 진입선(진입 준비 지점을 지나는 통로 방향 직선)을 따라가며 진입 준비 지점에서 멈춘다
    private IEnumerator ApproachEntry(Pose entryPose)
    {
        Vector3 lineDir = entryPose.forward;
        lineDir.y = 0f;
        lineDir.Normalize();

        while (true)
        {
            Vector3 carPos = _car.transform.position;
            Vector3 fromEntry = carPos - entryPose.position;
            fromEntry.y = 0f;
            float along = Vector3.Dot(fromEntry, lineDir);   // 음수면 진입 준비 지점보다 앞
            float remaining = -along;

            // 지금 속도에서 멈추는 데 필요한 거리만큼 남으면 제동
            float speedMps = Mathf.Abs(_car.SpeedKmh) / 3.6f;
            float brakeDistance = speedMps * speedMps / (2f * _approachDecel);
            if (remaining <= brakeDistance + 0.02f)
                break;

            // 진입선 위, 차보다 조금 앞의 점을 겨냥해 선에 붙는다
            Vector3 toTarget = entryPose.position + lineDir * (along + _approachLookAhead) - carPos;
            toTarget.y = 0f;
            Vector3 heading = _car.transform.forward;
            heading.y = 0f;
            _car.SetSteer(Vector3.SignedAngle(heading, toTarget, Vector3.up));

            // 앞이 막혀 멈춘 동안에는 속도를 넣지 않는다
            if (!_blocked)
                _car.SetSpeed(remaining > 3f ? _forwardSpeedKmh : _swingSpeedKmh);

            yield return new WaitForFixedUpdate();
        }

        _car.SetSteer(0f);
        _car.Brake(_approachDecel);
        while (Mathf.Abs(_car.SpeedKmh) > 0.1f)
            yield return new WaitForFixedUpdate();
    }

    // 조향을 고정한 채 전진해서 차 방향이 targetYaw가 될 때까지 돈다 (direction: +1 오른쪽, -1 왼쪽)
    private IEnumerator Swing(float targetYaw, float direction, float steerDeg)
    {
        _car.SetSteer(direction * steerDeg);
        while (Mathf.DeltaAngle(_car.transform.eulerAngles.y, targetYaw) * direction > _swingBrakeMarginDeg)
        {
            // 앞이 막혀 멈춘 동안에는 속도를 넣지 않는다
            if (!_blocked)
                _car.SetSpeed(_swingSpeedKmh);
            yield return new WaitForFixedUpdate();
        }

        _car.SetSteer(0f);
        _car.Brake(5f);
        while (Mathf.Abs(_car.SpeedKmh) > 0.1f)
            yield return new WaitForFixedUpdate();
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

            // 서 있던 자리 중 경로에 들지 않는 셀은 반납
            foreach (Vector2Int cell in _standingCells)
            {
                if (!_usedCells.Contains(cell))
                    _table.Release(_carId, cell);
            }
            _standingCells.Clear();
            return true;
        }

        return false;
    }

    // 경로를 따라갈 때 차체가 지나는 셀을 모은다.
    // 경로 점은 뒤차축 위치라서, 차 코(앞 3.75m)와 꽁무니(뒤 1m)가 걸치는 셀도 함께 넣는다.
    // reversing이면 차는 진행 방향의 반대쪽을 본다
    private List<Vector2Int> GetPathCells(Vector3[] path, bool reversing)
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

            // 이 구간에서 차가 보는 방향 (첫 점은 다음 점 쪽)
            Vector3 travel = Vector3.zero;
            if (i > 0) travel = path[i] - path[i - 1];
            else if (path.Length > 1) travel = path[1] - path[0];
            travel.y = 0f;
            Vector3 bodyDir = travel.sqrMagnitude > 0.0001f ? travel.normalized : Vector3.zero;
            if (reversing) bodyDir = -bodyDir;

            for (int s = 1; s <= samples; s++)
            {
                Vector3 point = (i == 0) ? path[0] : Vector3.Lerp(path[i - 1], path[i], (float)s / samples);
                if (bodyDir == Vector3.zero)
                    AddCell(cells, point);
                else
                    AddBodyCells(cells, point, bodyDir);
            }
        }

        return cells;
    }

    // 뒤차축이 point에 있고 bodyDir을 볼 때 차체가 걸치는 셀: 뒤차축, 코까지 반 칸 간격, 꽁무니
    private void AddBodyCells(List<Vector2Int> cells, Vector3 point, Vector3 bodyDir)
    {
        float step = _map.CellSize * 0.5f;

        AddCell(cells, point);
        for (float d = step; d < _noseOffset; d += step)
            AddCell(cells, point + bodyDir * d);
        AddCell(cells, point + bodyDir * _noseOffset);
        AddCell(cells, point - bodyDir * _tailMargin);
    }

    private void AddCell(List<Vector2Int> cells, Vector3 point)
    {
        Vector2Int cell = _map.WorldToCell(point);
        if (!cells.Contains(cell))
            cells.Add(cell);
    }

    // 출발 위치 → 진입선 → 진입 준비 지점 앞(_approachLength)까지의 경로.
    // 마지막 구간은 ApproachEntry가 맡는다. 점이 하나뿐이면 전부 ApproachEntry가 맡는다
    private Vector3[] CalculateGlobalPath(Pose entryPose)
    {
        Vector3 carPos = _car.transform.position;

        Vector3 lineDir = entryPose.forward;
        lineDir.y = 0f;
        lineDir.Normalize();

        Vector3 fromEntry = carPos - entryPose.position;
        fromEntry.y = 0f;
        float along = Vector3.Dot(fromEntry, lineDir);   // 음수면 진입 준비 지점보다 앞

        List<Vector3> path = new List<Vector3> { carPos };

        Vector3 corner = entryPose.position + lineDir * along;            // 진입선 위, 차 바로 옆
        Vector3 preEntry = entryPose.position - lineDir * _approachLength;

        // 진입선에서 떨어져 있고 모서리 뒤에 직선이 충분할 때만 ㄱ자로 간다.
        // 그 밖에는 ApproachEntry가 처음부터 진입선에 붙어 들어간다
        bool offLine = Vector3.Distance(carPos, corner) > 1f;
        bool hasRoom = along < -_approachLength && Vector3.Distance(corner, preEntry) >= _cornerRoom;
        if (offLine && hasRoom)
        {
            path.Add(corner);
            path.Add(preEntry);
        }

        return path.ToArray();
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
        _blocked = false;
        _yielding = false;
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
