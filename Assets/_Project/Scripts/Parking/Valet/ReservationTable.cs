using UnityEngine;
using System.Collections.Generic;

public class ReservationTable : MonoBehaviour
{
    [SerializeField] private float _slotTime = 0.5f;
    [SerializeField] private GridMap _map;

    private bool _isOn = false;
    private Dictionary<(Vector2Int cell, int timeSlot), int> _reservations = new Dictionary<(Vector2Int, int), int>();

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

        // 예약 불가능 확인
        foreach (Vector2Int cell in cells)
        {
            if (_map != null && !_map.Contains(cell))
            {
                Debug.LogError($"[ReservationTable] TryReserve: 셀 {cell}이 범위를 벗어남");
                return false;
            }

            int timeSlot = Mathf.FloorToInt(startTime / _slotTime);
            if (_reservations.TryGetValue((cell, timeSlot), out int occupantId))
            {
                Debug.Log($"[ReservationTable] TryReserve 실패: 셀 {cell} 타임슬롯 {timeSlot}은 차량 {occupantId}가 점유 중");
                return false;
            }
        }

        // 예약 기록
        foreach (Vector2Int cell in cells)
        {
            int timeSlot = Mathf.FloorToInt(startTime / _slotTime);
            _reservations[(cell, timeSlot)] = carId;
        }

        Debug.Log($"[ReservationTable] TryReserve 성공: 차량 {carId}, {cells.Count}개 셀, 시작시간 {startTime:F2}s");
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
        Debug.Log("[ReservationTable] Clear: 모든 예약 초기화");
    }
}
