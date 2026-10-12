using UnityEngine;
using UnityEngine.InputSystem;

// ParkingArea 동작 확인용 테스트 스크립트.
// 영역 진입(화면 분할, 조명) → 차량을 움직임 → ResetArea로 시작 위치 복귀를 숫자 키로 확인한다.
public class _ParkingAreaTester : MonoBehaviour
{
    [SerializeField]
    private ParkingArea _area;
    // 넣으면 영역 진입 때 좌·우 카메라를 Bind한다. 비워 두면 카메라는 그대로 둔다.
    [SerializeField]
    private CameraSwitcher _switcher;
    // ResetArea에 넘길 설정. 비워 둬도 된다.
    [SerializeField]
    private CarConfig _config;
    [SerializeField]
    private float _testSpeedKmh = 5f;
    [SerializeField]
    private ParkingSlotAllocator _allocator;

    private CarController[] _cars;

    //Valet 후진 테스트
    [SerializeField]
    private ParkingSlot _testSlot;
    [SerializeField]
    private CarController _testCar;

    //Valet 에이전트 테스트
    [SerializeField]
    private ValetAgent _valetAgent;

    //스마트키 원격 주차 테스트
    [SerializeField]
    private RemoteParking _remoteParking;

    //발렛 에이전트 배열 테스트
    [SerializeField]
    private ValetAgent[] _valetAgents;
    [SerializeField]
    private ReservationTable _tableB;

    // 스마트키 장면에서 시야를 가려 숨길 오브젝트 (예: Car_NPC2). 영역 진입·ResetArea 때 다시 보인다
    [SerializeField]
    private GameObject[] _hideInSmartKey;

    // 발렛 장면에서 숨길 오브젝트 (스마트키 차와 그 옆 차). 스마트키 진입(9번) 때 다시 보인다
    [SerializeField]
    private GameObject[] _hideInValet;

    private void Start()
    {
        if (_area == null)
        {
            Debug.LogError("[_ParkingAreaTester] Area가 비어 있음", this);
            return;
        }

        _cars = _area.GetComponentsInChildren<CarController>(true);
        Debug.Log($"[_ParkingAreaTester] 차량 {_cars.Length}대 찾음", this);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (_area != null && _cars != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) EnterArea();
            if (keyboard.digit2Key.wasPressedThisFrame) MoveCars();
            if (keyboard.digit3Key.wasPressedThisFrame) StopCars();
            if (keyboard.digit4Key.wasPressedThisFrame) ResetArea();
            if (keyboard.digit5Key.wasPressedThisFrame) LogStartPoses();
            if (keyboard.digit6Key.wasPressedThisFrame) AssignSlot();
            if (keyboard.digit7Key.wasPressedThisFrame) ReverseTest();
        }

        if (keyboard.digit9Key.wasPressedThisFrame) SmartKeyEnter();

        if (keyboard.digit0Key.wasPressedThisFrame) ToggleTableB();

        if (keyboard.digit8Key.wasPressedThisFrame) StartAllAgents();

        // 방향키 위/아래 - 누르는 동안 매 프레임 Press, 떼는 순간 Release
        if (_remoteParking != null)
        {
            if (keyboard.upArrowKey.isPressed)
            {
                _remoteParking.Press(1);
            }
            if (keyboard.upArrowKey.wasReleasedThisFrame)
            {
                _remoteParking.Release();
            }

            if (keyboard.downArrowKey.isPressed)
            {
                _remoteParking.Press(-1);
            }
            if (keyboard.downArrowKey.wasReleasedThisFrame)
            {
                _remoteParking.Release();
            }

            if (keyboard.enterKey.wasPressedThisFrame) SmartKeyBoard();
        }
    }

    // 영역에 들어갈 때 ModeManager가 하는 일을 흉내 낸다
    private void EnterArea()
    {
        if (_area.Root != null)
            _area.Root.SetActive(true);

        SetSmartKeyHidden(false);
        _area.ResetArea(_config);
        _area.OnEnter();

        if (_switcher != null)
            _switcher.Bind(_area.LeftCam, _area.RightCam);

        Debug.Log("[_ParkingAreaTester] 영역 진입: ResetArea → OnEnter", this);
    }

    private void MoveCars()
    {
        foreach (CarController car in _cars)
            car.SetSpeed(_testSpeedKmh);

        Debug.Log($"[_ParkingAreaTester] 차량 {_cars.Length}대 {_testSpeedKmh}km/h로 전진", this);
    }

    private void StopCars()
    {
        foreach (CarController car in _cars)
            car.Brake(3f);

        Debug.Log("[_ParkingAreaTester] 차량 정지", this);
    }

    private void ResetArea()
    {
        SetSmartKeyHidden(false);
        _area.ResetArea(_config);
        Debug.Log("[_ParkingAreaTester] ResetArea 호출: 차량이 시작 위치로 돌아갔는지 확인", this);
    }

    // 발렛 장면(0)의 차량별 시작 위치를 콘솔에 찍는다
    private void LogStartPoses()
    {
        for (int i = 0; i < _cars.Length; i++)
        {
            Pose pose = _area.GetStartPose(0, i);
            Debug.Log($"[_ParkingAreaTester] 발렛 시작 위치 {i}: 위치 {pose.position}, 방향 {pose.rotation.eulerAngles.y:F0}도", this);
        }
    }

    // 게임 뷰 왼쪽 위에 키 안내를 표시한다 (표시 전용)
    private void OnGUI()
    {
        float scale = Screen.height / 360f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        GUILayout.BeginArea(new Rect(8, 8, 210, 185), GUI.skin.box);
        GUILayout.Label("ParkingArea 테스트");
        GUILayout.Label("1 영역 진입   4 ResetArea");
        GUILayout.Label("2 차량 전진   3 차량 정지");
        GUILayout.Label("5 시작 위치 출력");
        GUILayout.Label("6 주차면 배정   7 후진 테스트");
        GUILayout.Label("8 발렛 에이전트");
        GUILayout.Label("9 스마트키 진입");
        GUILayout.Label("↑/↓ 누르는 동안 이동");
        GUILayout.Label("Enter 탑승");
        GUILayout.EndArea();
    }
    // 빈 주차면을 하나 배정받아 Id를 출력. null이면 만차
    private void AssignSlot()
    {
        if (_allocator == null)
        {
            Debug.LogError("[_ParkingAreaTester] Allocator가 비어 있음", this);
            return;
        }

        ParkingSlot slot = _allocator.AssignEmpty();
        if (slot == null)
            Debug.Log("[_ParkingAreaTester] 만차 (AssignEmpty가 null 반환)", this);
        else
            Debug.Log($"[_ParkingAreaTester] 배정된 주차면 Id: {slot.Id}", this);
    }
    
    //후진 테스트
    private void ReverseTest()
    {
        if (_testSlot == null || _testCar == null)
        {
            Debug.LogError("[_ParkingAreaTester] testSlot 또는 testCar가 비어 있음", this);
            return;
        }

        Vector3[] route = _testSlot.GetReverseRoute();
        _testCar.StopAllCoroutines();
        _testCar.ResetPose(_testSlot.GetReverseStartPose());
        _testCar.StartCoroutine(_testCar.Follow(route, 5f, true));
        Debug.Log($"[_ParkingAreaTester] 후진 시작: 점 {route.Length}개, 목표 {_testSlot.transform.position}", this);
    }

    //발렛 에이전트 테스트
    private void ValetTest()
    {
        if (_valetAgent == null)
        {
            Debug.LogError("[_ParkingAreaTester] ValetAgent가 비어 있음", this);
            return;
        }

        _valetAgent.StartCoroutine(_valetAgent.Go());
        Debug.Log("[_ParkingAreaTester] ValetAgent.Go() 시작", this);
    }

    //스마트키 원격 주차 진입
    private void SmartKeyEnter()
    {
        if (_switcher == null || _remoteParking == null)
        {
            Debug.LogError("[_ParkingAreaTester] CameraSwitcher 또는 RemoteParking이 비어 있음", this);
            return;
        }

        _remoteParking.SetActive(true);
        _switcher.SetFixedView(true);
        SetSmartKeyHidden(true);
        Debug.Log("[_ParkingAreaTester] 스마트키 장면 진입: FixedView ON, RemoteParking ON", this);
    }

    // 스마트키 장면에서 가리는 오브젝트를 숨기거나 다시 보이게 한다
    // hidden이 true면 스마트키 장면(발렛 차 일부를 숨김), false면 발렛 장면(스마트키 쪽 차를 숨김)
    private void SetSmartKeyHidden(bool hidden)
    {
        SetObjectsActive(_hideInSmartKey, !hidden);
        SetObjectsActive(_hideInValet, hidden);
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }

    //스마트키 탑승
    private void SmartKeyBoard()
    {
        if (_remoteParking == null)
        {
            Debug.LogError("[_ParkingAreaTester] RemoteParking이 비어 있음", this);
            return;
        }

        _remoteParking.Board();
        Debug.Log("[_ParkingAreaTester] 탑승 시도", this);
    }

    //모든 발렛 에이전트 시작
    private void StartAllAgents()
    {
        if (_valetAgents == null || _valetAgents.Length == 0)
        {
            Debug.LogError("[_ParkingAreaTester] _valetAgents가 비어 있음", this);
            return;
        }

        // 먼저 모든 차가 서 있는 자리를 잡아 둔다. 안 그러면 먼저 출발한 차가 아직 서 있는 차의 자리를 예약해 버린다
        foreach (ValetAgent agent in _valetAgents)
        {
            if (agent != null)
            {
                agent.ResetAgent();
                agent.HoldPosition();
            }
        }

        foreach (ValetAgent agent in _valetAgents)
        {
            if (agent != null)
            {
                agent.StartGo();
            }
        }

        Debug.Log($"[_ParkingAreaTester] {_valetAgents.Length}개 발렛 에이전트 시작", this);
    }

    //예약 테이블 B 토글
    private void ToggleTableB()
    {
        if (_tableB == null)
        {
            Debug.LogError("[_ParkingAreaTester] _tableB가 비어 있음", this);
            return;
        }

        bool newState = !_tableB.IsOn;
        _tableB.SetOn(newState);
        Debug.Log($"[_ParkingAreaTester] ReservationTable B: {newState}", this);
    }
}
