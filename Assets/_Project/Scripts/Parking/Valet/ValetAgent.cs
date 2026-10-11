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

            if (Physics.Raycast(carPos, carForward, _stopDistance, LayerMask.GetMask("Vehicle")))
            {
                _car.Brake(5f);
                Debug.Log($"[ValetAgent] 앞 차량 감지, 정지");
            }
        }
    }

    public IEnumerator Go()
    {
        if (_car == null)
        {
            Debug.LogError("[ValetAgent] Go: _car가 null입니다");
            yield break;
        }
        if (_allocator == null)
        {
            Debug.LogError("[ValetAgent] Go: _allocator가 null입니다");
            yield break;
        }

        _assignedSlot = _allocator.AssignEmpty();
        if (_assignedSlot == null)
        {
            State = AgentState.Full;
            Debug.Log($"[ValetAgent] State: {State}");
            yield break;
        }

        // 경로 계산 및 예약 시도
        Vector3[] globalPath = CalculateGlobalPath(_assignedSlot);
        List<Vector2Int> pathCells = GetPathCells(globalPath);
        Vector3[] reverseRoute = _assignedSlot.GetReverseRoute();
        List<Vector2Int> reverseCells = GetPathCells(reverseRoute);

        pathCells.AddRange(reverseCells);

        // 예약 시도
        while (!TryReserve(pathCells))
        {
            State = AgentState.Waiting;
            Debug.Log($"[ValetAgent] State: {State}");
            yield return new WaitForSeconds(_retryInterval);
        }

        State = AgentState.Moving;
        Debug.Log($"[ValetAgent] State: {State}");
        yield return _car.Follow(globalPath, 10f, false);

        Debug.Log("[ValetAgent] 전역 이동 완료");

        Pose entryPose = _assignedSlot.GetEntryPose();
        _car.ResetPose(entryPose);
        Debug.Log("[ValetAgent] 진입 자세 세팅 완료");

        yield return new WaitForFixedUpdate();
        _car.Brake(3f);
        yield return new WaitForSeconds(0.5f);

        if (reverseRoute == null || reverseRoute.Length < 2)
        {
            Debug.LogError("[ValetAgent] Go: 후진 경로가 없거나 점이 부족합니다");
            yield break;
        }

        State = AgentState.Reversing;
        Debug.Log($"[ValetAgent] State: {State}");
        yield return _car.Follow(reverseRoute, 5f, true);

        Vector3 routeDir = (reverseRoute[reverseRoute.Length - 2] - reverseRoute[reverseRoute.Length - 1]).normalized;
        Quaternion parkingRot = Quaternion.LookRotation(routeDir, Vector3.up);

        _car.ResetPose(new Pose(_assignedSlot.transform.position, parkingRot));
        _car.Brake(3f);
        Debug.Log("[ValetAgent] 후진 완료, 주차 위치에 스냅");

        State = AgentState.Parked;
        Debug.Log($"[ValetAgent] State: {State}");

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
            Debug.Log($"[ValetAgent] TryReserve: 예약 OFF, 예약 없이 진행");
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

        return new Vector3[]
        {
            carPos,
            new Vector3(carPos.x, carPos.y, _laneZ),
            new Vector3(entryPos.x, entryPos.y, _laneZ),
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
