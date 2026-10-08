using UnityEngine;

public class ParkingSlotAllocator : MonoBehaviour
{
    [SerializeField]
    private ParkingSlot[] _slots;

    public ParkingSlot AssignEmpty()
    {
        for(int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                Debug.LogError($"[ParkingSlotAllocator] AssignEmpty: _solt[{i}]가 null입니다");
                continue;
            }
            if (!_slots[i].IsOccupied)
            {
                _slots[i].IsOccupied = true;
                return _slots[i];
            }
        }
        return null; //AssignEmpty() 호출할 때 null로 오면 만차
    }
    public void ResetSlots()
    {
        foreach(var slot in _slots)
        {
            if (slot == null)
            {
                Debug.LogError($"[ParkingSlotAllocator] ResetSlots: {slot}가 null입니다");
                continue;
            }
            slot.ResetSlot();
        }
    }
}
