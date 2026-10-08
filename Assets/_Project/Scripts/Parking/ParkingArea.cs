
using UnityEngine;

public class ParkingArea : AreaBase
{
    [SerializeField]
    private GridMap _map;
    //[SerializeField]
    //private ResevervationTable _tableA;
    //[SerializeField]
    //private ResevervationTable _tableB;
    [SerializeField]
    private ParkingSlotAllocator _slots;
    //[SerializeField]
    //private ParkingScenario _scenario;
    [SerializeField]
    private CarController[] _cars;
    [SerializeField]
    private Transform[] _valetStarts;
    [SerializeField]
    private Transform[] _smartKeyStarts;
    public const int SceneValet = 0;
    public const int SceneSmartKey = 1;



    public override void ResetArea(CarConfig config)
    {
        for (int i=0;i<_cars.Length;i++)
        {
            if (_cars[i] == null)
            {
                Debug.LogError($"[ParkingArea] ResetArea: _cars[{i}]가 비어 있음 (Inspector 연결 확인)");
                continue;
            }
            _cars[i].ResetPose(GetStartPose(SceneValet, i));
        }

        //V TODO 슬롯 초기화(ResetSlots)
        if(_slots == null)
        {
            Debug.LogError("[ParkingArea] ResetArea: _slots가 비어 있음 (Inspector 연결 확인)");
        }
        else
        {
            _slots.ResetSlots();
        }

        //TODO 예약 테이블
        //config 나중에
    }

    public Pose GetStartPose(int scene , int car)
    {
        Transform[] starts;
        if (scene == SceneValet) starts = _valetStarts;
        else if(scene == SceneSmartKey) starts = _smartKeyStarts;
        else
        {
            Debug.LogError($"[ParkingArea] GetStartPose: scene은 0 또는 1이어야 함 (받은 값: {scene})");
            return default;
        }

        if (car < 0 || car >= starts.Length || starts[car] == null)
        {
            Debug.LogError($"[ParkingArea] GetStartPose: scene {scene}의 car {car} 시작 위치가 없음 (범위 또는 Inspector 연결 확인)");
            return default;
        }

        Transform t = starts[car];
        return new Pose(t.position, t.rotation);


    }
}
