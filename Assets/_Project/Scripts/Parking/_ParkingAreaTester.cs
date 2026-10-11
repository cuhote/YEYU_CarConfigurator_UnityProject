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

    private bool _upPressed = false;
    private bool _downPressed = false;

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
            if (keyboard.digit8Key.wasPressedThisFrame) ValetTest();
        }

        if (keyboard.digit9Key.wasPressedThisFrame) SmartKeyEnter();

        // 방향키 위/아래 - 누르고 있는 동안 Press, 뗄 때 Release
        if (_remoteParking != null)
        {
            if (keyboard.upArrowKey.isPressed)
            {
                if (!_upPressed)
                {
                    _upPressed = true;
                    _remoteParking.Press(1);
                }
            }
            else if (_upPressed)
            {
                _upPressed = false;
                _remoteParking.Release();
            }

            if (keyboard.downArrowKey.isPressed)
            {
                if (!_downPressed)
                {
                    _downPressed = true;
                    _remoteParking.Press(-1);
                }
            }
            else if (_downPressed)
            {
                _downPressed = false;
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
        _testCar.ResetPose(_testSlot.GetEntryPose());
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
        Debug.Log("[_ParkingAreaTester] 스마트키 장면 진입: FixedView ON, RemoteParking ON", this);
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
}
