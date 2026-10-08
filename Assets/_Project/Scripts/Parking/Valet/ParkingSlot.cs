using UnityEngine;

public class ParkingSlot : MonoBehaviour
{
    [SerializeField]
    private int _id;
    [SerializeField]
    private bool _initiallyOccupied; //처음부터 차가 있는 칸인지
    [SerializeField]
    private Transform _entryPoint; //진입준비지점 (후진 시작 위치,방향)
    [SerializeField]
    private Transform[] _reverseRoute; //후진 웨이포인트(D.3)

    public bool IsOccupied { get; set; }
    public int Id => _id;
    public Transform EntryPoint => _entryPoint;
    public Transform[] ReverseRoute => _reverseRoute;

    public void ResetSlot() //Allocator에서 호출
    {
        IsOccupied = _initiallyOccupied;
    }
    private void Awake()
    {
        ResetSlot();
    }
}
