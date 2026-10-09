using UnityEngine;
using UnityEngine.InputSystem;

// CameraSwitcher 동작 확인용 테스트 스크립트.
// 영역(주행/주차)에 들어간 뒤 그 영역의 좌·우 카메라로 Bind하는 흐름을 버튼으로 흉내 낸다.
// (Button OnClick은 enum 인자를 받지 못해서 Switch(CameraView)를 직접 연결할 수 없다)
public class _CameraSwitcherTester : MonoBehaviour
{
    [SerializeField]
    private CameraSwitcher _switcher;
    [SerializeField]
    private AreaBase _drivingArea;
    [SerializeField]
    private AreaBase _parkingArea;
    // 씬에 ModeManager가 있으면 넣는다. 비워 두면 이 스크립트가 영역을 직접 켜고 끈다.
    [SerializeField]
    private ModeManager _modeManager;

    public void EnterDriving()
    {
        if (_modeManager != null)
            _modeManager.StartDriving();
        else
            ActivateOnly(_drivingArea, _parkingArea);

        BindArea(_drivingArea);
    }

    public void EnterParking()
    {
        if (_modeManager != null)
            _modeManager.StartParking();
        else
            ActivateOnly(_parkingArea, _drivingArea);

        BindArea(_parkingArea);
    }

    public void ShowDriver()
    {
        _switcher.Switch(CameraView.Driver);
        LogView("Switch(Driver)");
    }

    public void ShowBirdEye()
    {
        _switcher.Switch(CameraView.BirdEye);
        LogView("Switch(BirdEye)");
    }

    // 누를 때마다 고정 시점을 켜고 끈다
    public void ToggleFixed()
    {
        bool turnOn = _switcher.CurrentView != CameraView.Fixed;
        _switcher.SetFixedView(turnOn);
        LogView($"SetFixedView({turnOn})");
    }

    public void ShowRightDriver()
    {
        _switcher.SetRightDriverView();
        LogView("SetRightDriverView()");
    }

    // 숫자 키로 테스트한다. (이 프로젝트는 새 Input System만 켜져 있어 OnGUI 버튼은 클릭을 받지 못한다)
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || _switcher == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame) EnterDriving();
        if (keyboard.digit2Key.wasPressedThisFrame) EnterParking();
        if (keyboard.digit3Key.wasPressedThisFrame) ShowDriver();
        if (keyboard.digit4Key.wasPressedThisFrame) ShowBirdEye();
        if (keyboard.digit5Key.wasPressedThisFrame) ToggleFixed();
        if (keyboard.digit6Key.wasPressedThisFrame) ShowRightDriver();
    }

    // 게임 뷰 왼쪽 위에 키 안내와 현재 시점을 표시한다 (표시 전용)
    private void OnGUI()
    {
        if (_switcher == null)
            return;

        // 게임 뷰 해상도와 상관없이 같은 크기로 보이도록 화면 높이에 맞춰 확대한다
        float scale = Screen.height / 360f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        GUILayout.BeginArea(new Rect(8, 8, 170, 150), GUI.skin.box);
        GUILayout.Label($"CurrentView: {_switcher.CurrentView}");
        GUILayout.Label("1 주행 진입   2 주차 진입");
        GUILayout.Label("3 Driver   4 Bird");
        GUILayout.Label("5 Fixed 켜기/끄기");
        GUILayout.Label("6 오른쪽만 Driver");
        GUILayout.EndArea();
    }

    // ModeManager 없이 테스트할 때: 대상 영역만 켜고 나머지는 끈다
    private void ActivateOnly(AreaBase target, AreaBase other)
    {
        if (other != null)
        {
            other.OnExit();
            if (other.Root != null)
                other.Root.SetActive(false);
        }

        if (target == null)
            return;

        if (target.Root != null)
            target.Root.SetActive(true);
        target.OnEnter();
    }

    private void BindArea(AreaBase area)
    {
        if (_switcher == null || area == null)
        {
            Debug.LogError("[CameraSwitcherTester] Switcher 또는 영역이 비어 있음", this);
            return;
        }

        _switcher.Bind(area.LeftCam, area.RightCam);
        LogView($"Bind({area.name})");
    }

    private void LogView(string action)
    {
        Debug.Log($"[CameraSwitcherTester] {action} → CurrentView: {_switcher.CurrentView}", this);
    }
}
