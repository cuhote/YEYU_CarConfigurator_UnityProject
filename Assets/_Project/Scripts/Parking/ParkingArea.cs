
using UnityEngine;

public class ParkingArea : AreaBase
{
    [SerializeField]
    private GridMap _map;
    [SerializeField]
    private ReservationTable _tableA;
    [SerializeField]
    private ReservationTable _tableB;
    [SerializeField]
    private ParkingSlotAllocator[] _slots;
    // 비워 두면 시나리오 없이 영역만 켠다 (_ParkingAreaTester로 수동 테스트할 때)
    [SerializeField]
    private ParkingScenario _scenario;
    [SerializeField]
    private CarController[] _cars;
    [SerializeField]
    private Transform[] _valetStarts;
    [SerializeField]
    private Transform[] _smartKeyStarts;
    public const int SceneValet = 0;
    public const int SceneSmartKey = 1;



    private CarConfig _config;

    // 영역에 들어오면 시나리오를 첫 장면부터 시작한다 (ModeManager가 ResetArea 다음에 부른다)
    public override void OnEnter()
    {
        base.OnEnter();
        if (_scenario != null)
            _scenario.StartFirstScene(_config);
    }

    public override void OnExit()
    {
        if (_scenario != null)
            _scenario.StopScenario();
        base.OnExit();
    }

    public override void ResetArea(CarConfig config)
    {
        _config = config;

        for (int i=0;i<_cars.Length;i++)
        {
            if (_cars[i] == null)
            {
                Debug.LogError($"[ParkingArea] ResetArea: _cars[{i}]가 비어 있음 (Inspector 연결 확인)");
                continue;
            }
            _cars[i].ResetPose(GetStartPose(SceneValet, i));
        }

        for(int i=0;i<_slots.Length;i++)
        {
            if(_slots[i] == null)
            {
                Debug.LogError($"[ParkingArea] ResetArea: _slots[{i}]가 비어 있음 (Inspector 연결 확인)");
                continue;
            }
            _slots[i].ResetSlots();
        }

        // 예약 테이블 초기화
        if (_tableA != null)
        {
            _tableA.SetOn(false);
            _tableA.Clear();
        }
        else
        {
            Debug.LogError("[ParkingArea] ResetArea: _tableA가 null입니다");
        }

        if (_tableB != null)
        {
            bool valetParkingEnabled = config != null ? config.ValetParking : true;
            _tableB.SetOn(valetParkingEnabled);
            _tableB.Clear();
        }
        else
        {
            Debug.LogError("[ParkingArea] ResetArea: _tableB가 null입니다");
        }
    }

    public Pose GetStartPose(int scene , int car)
    {
        Transform[] starts;
        if (scene == SceneValet) starts = _valetStarts;
        else if(scene == SceneSmartKey) starts = _smartKeyStarts;
        else
        {
            Debug.LogError($"[ParkingArea] GetStartPose: scene은 0 또는 1이어야 함 (받은 값: {scene})");
            return new Pose(Vector3.zero, Quaternion.identity); //(0,0,0,0) 쿼터니언 기본값 (0,0,0,1). identity
        }

        if (car < 0 || car >= starts.Length || starts[car] == null)
        {
            Debug.LogError($"[ParkingArea] GetStartPose: scene {scene}의 car {car} 시작 위치가 없음 (범위 또는 Inspector 연결 확인)");
            return default;
        }

        Transform t = starts[car];
        Vector3 pos = t.position;
        pos.y = 0f;
        return new Pose(pos, t.rotation);


    }
}
