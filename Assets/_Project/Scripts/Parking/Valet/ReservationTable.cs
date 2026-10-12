using UnityEngine;
using System.Collections.Generic;

public class ReservationTable : MonoBehaviour
{
    [SerializeField] private GridMap _map;

    private bool _isOn = false;
    // 셀 → 점유 중인 차량 id. 시간 구분 없이 Release될 때까지 유지된다
    private Dictionary<Vector2Int, int> _reservations = new Dictionary<Vector2Int, int>();
    private HashSet<Vector2Int> _outOfBoundsCells = new HashSet<Vector2Int>();
    // 차량별로 마지막에 기록한 실패 원인 (재시도마다 같은 로그가 반복되지 않게)
    private Dictionary<int, (Vector2Int cell, int occupantId)> _lastBlocked = new Dictionary<int, (Vector2Int, int)>();

    public bool IsOn => _isOn;

    private void Start()
    {
        if (_map == null)
        {
            Debug.LogError("[ReservationTable] GridMap이 null입니다");
        }
    }

    public void SetOn(bool on)
    {
        _isOn = on;
        Debug.Log($"[ReservationTable] SetOn({on})");
    }

    // startTime은 쓰지 않는다 (셀 단위 점유). 호출하는 쪽을 바꾸지 않으려고 남겨 둠
    public bool TryReserve(int carId, List<Vector2Int> cells, float startTime)
    {
        if (!_isOn)
        {
            Debug.Log($"[ReservationTable] TryReserve: IsOn=false, 예약 없이 통과");
            return true;
        }

        List<Vector2Int> validCells = new List<Vector2Int>();

        // 범위 확인 및 유효한 셀만 수집
        foreach (Vector2Int cell in cells)
        {
            if (_map != null && !_map.Contains(cell))
            {
                // 범위 밖 에러 - 한 번만 출력
                if (!_outOfBoundsCells.Contains(cell))
                {
                    _outOfBoundsCells.Add(cell);
                    Vector3 cellWorldPos = _map.CellToWorld(cell);
                    Vector3 mapOrigin = _map.transform.position;
                    Debug.LogError($"[ReservationTable] TryReserve 범위 밖: " +
                        $"(a) 셀={cell}, " +
                        $"(b) 차량ID={carId}, " +
                        $"(c) 월드좌표={cellWorldPos}, " +
                        $"(d) 맵원점={mapOrigin}");
                }
                continue; // 이 셀은 제외하고 계속
            }

            validCells.Add(cell);
        }

        // 하나라도 다른 차가 점유 중이면 아무것도 기록하지 않고 실패
        foreach (Vector2Int cell in validCells)
        {
            if (_reservations.TryGetValue(cell, out int occupantId) && occupantId != carId)
            {
                bool sameAsLast = _lastBlocked.TryGetValue(carId, out var last)
                    && last.cell == cell && last.occupantId == occupantId;
                if (!sameAsLast)
                {
                    _lastBlocked[carId] = (cell, occupantId);
                    Debug.Log($"[ReservationTable] TryReserve 실패: 차량 {carId}, 셀 {cell}은 차량 {occupantId}가 점유 중");
                }
                return false;
            }
        }

        // 모두 비었거나 내 것: 전부 내 id로 기록
        foreach (Vector2Int cell in validCells)
        {
            _reservations[cell] = carId;
        }
        _lastBlocked.Remove(carId);

        int excludedCount = cells.Count - validCells.Count;
        string excludeMsg = excludedCount > 0 ? $", {excludedCount}개 범위 밖 제외" : "";
        Debug.Log($"[ReservationTable] TryReserve 성공: 차량 {carId}, {validCells.Count}개 셀 예약{excludeMsg}");
        return true;
    }

    // 내 것일 때만 지운다
    public void Release(int carId, Vector2Int cell)
    {
        if (_reservations.TryGetValue(cell, out int occupantId) && occupantId == carId)
        {
            _reservations.Remove(cell);
            Debug.Log($"[ReservationTable] Release: 차량 {carId}, 셀 {cell}");
        }
    }

    public void Clear()
    {
        _reservations.Clear();
        _outOfBoundsCells.Clear();
        _lastBlocked.Clear();
        Debug.Log("[ReservationTable] Clear: 모든 예약 및 범위 밖 셀 기록 초기화");
    }

    // currentTime은 쓰지 않는다 (셀 단위 점유)
    public List<Vector2Int> GetReservedCells(int carId, float currentTime)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        foreach (var kvp in _reservations)
        {
            if (kvp.Value == carId)
            {
                cells.Add(kvp.Key);
            }
        }

        return cells;
    }
}
