using UnityEngine;
using System.Collections.Generic;

public class ReservationTable : MonoBehaviour
{
    [SerializeField] private float _slotTime = 0.5f;
    [SerializeField] private GridMap _map;

    private bool _isOn = false;
    private Dictionary<(Vector2Int cell, int timeSlot), int> _reservations = new Dictionary<(Vector2Int, int), int>();
    private HashSet<Vector2Int> _outOfBoundsCells = new HashSet<Vector2Int>();

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

        // 유효한 셀들에 대해 점유 확인
        int timeSlot = Mathf.FloorToInt(startTime / _slotTime);
        foreach (Vector2Int cell in validCells)
        {
            if (_reservations.TryGetValue((cell, timeSlot), out int occupantId))
            {
                Debug.Log($"[ReservationTable] TryReserve 실패: 셀 {cell} 타임슬롯 {timeSlot}은 차량 {occupantId}가 점유 중");
                return false;
            }
        }

        // 예약 기록
        foreach (Vector2Int cell in validCells)
        {
            _reservations[(cell, timeSlot)] = carId;
        }

        int excludedCount = cells.Count - validCells.Count;
        string excludeMsg = excludedCount > 0 ? $", {excludedCount}개 범위 밖 제외" : "";
        Debug.Log($"[ReservationTable] TryReserve 성공: 차량 {carId}, {validCells.Count}개 셀 예약, 시작시간 {startTime:F2}s{excludeMsg}");
        return true;
    }

    public void Release(int carId, Vector2Int cell)
    {
        List<(Vector2Int, int)> keysToRemove = new List<(Vector2Int, int)>();

        foreach (var kvp in _reservations)
        {
            if (kvp.Key.cell == cell && kvp.Value == carId)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _reservations.Remove(key);
        }

        if (keysToRemove.Count > 0)
        {
            Debug.Log($"[ReservationTable] Release: 차량 {carId}, 셀 {cell}");
        }
    }

    public void Clear()
    {
        _reservations.Clear();
        _outOfBoundsCells.Clear();
        Debug.Log("[ReservationTable] Clear: 모든 예약 및 범위 밖 셀 기록 초기화");
    }
}
