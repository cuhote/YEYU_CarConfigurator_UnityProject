using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private NavigationBar _navigationBar;
    [Header("화면별로 켜고 끌 것")]
    [SerializeField] private GameObject _exteriorPanel; // 외관 선택 화면. 아직 없으면 비워 둔다
    [SerializeField] private GameObject _featurePanel;
    [SerializeField] private GameObject _sceneButtons;  // 주행·주차 화면의 운전석/버드뷰 버튼 묶음

    [Header("시점 탭")]
    [SerializeField] private CameraSwitcher _cameraSwitcher;
    [SerializeField] private Button _driverTab;
    [SerializeField] private Button _birdTab;
    [SerializeField] private TMP_Text _driverLabel;
    [SerializeField] private TMP_Text _birdLabel;
    [SerializeField] private Color _accentColor = new Color32(0x28, 0x66, 0xF2, 0xFF);
    [SerializeField] private Color _tabIdleColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color _tabActiveTextColor = Color.white;
    [SerializeField] private Color _tabIdleTextColor = new Color32(0xC9, 0xCD, 0xD6, 0xFF);

    private ModeManager _modeManager;
    private ScreenHistory _screenHistory;

    private void Awake()
    {
        _modeManager = FindObjectOfType<ModeManager>();
        _screenHistory = FindObjectOfType<ScreenHistory>();

        if (_modeManager == null)
        {
            Debug.LogError("[UIManager] ModeManager를 찾을 수 없습니다");
        }

        if (_screenHistory == null)
        {
            Debug.LogError("[UIManager] ScreenHistory를 찾을 수 없습니다");
        }

        if (_navigationBar == null)
        {
            Debug.LogWarning("[UIManager] NavigationBar가 비어 있습니다");
        }
    }
    public void Start()
    {
        ShowExterior();
    }

    private void ShowOnly(ScreenHistory.ScreenId screen) //지금 화면에 해당하는 것만 킴 
    {
        bool scene = screen == ScreenHistory.ScreenId.DrivingScene || screen == ScreenHistory.ScreenId.ParkingScene;

        if (_exteriorPanel != null) _exteriorPanel.SetActive(screen == ScreenHistory.ScreenId.Exterior);
        if (_featurePanel != null) _featurePanel.SetActive(screen == ScreenHistory.ScreenId.Feature);
        if (_sceneButtons != null) _sceneButtons.SetActive(scene);
    }

    public void ShowExterior()
    {
        if (_exteriorPanel == null)
        {
            ShowFeature();
            return;
        }
        if (_modeManager == null)
        {
            Debug.LogError("[UIManager] ShowExterior: ModeManager가 null입니다");
            return;
        }

        _modeManager.SwitchTo(AppMode.Configurator);
        _screenHistory?.Push(ScreenHistory.ScreenId.Exterior);

        // 외관 선택 화면에서는 NavigationBar 숨김
        if (_navigationBar != null)
            _navigationBar.Hide();
        
        ShowOnly(ScreenHistory.ScreenId.Exterior);
    }

    public void ShowFeature()
    {
        if (_modeManager == null)
        {
            Debug.LogError("[UIManager] ShowFeature: ModeManager가 null입니다");
            return;
        }

        _modeManager.SwitchTo(AppMode.Configurator);
        _screenHistory?.Push(ScreenHistory.ScreenId.Feature);

        // Feature는 외관 선택 화면이므로 NavigationBar 숨김
        if (_navigationBar != null)
            _navigationBar.Hide();
        
        ShowOnly(ScreenHistory.ScreenId.Feature);
    }

    public void ShowDrivingScene()
    {
        if (_modeManager == null)
        {
            Debug.LogError("[UIManager] ShowDrivingScene: ModeManager가 null입니다");
            return;
        }

        _modeManager.SwitchTo(AppMode.Driving);
        _screenHistory?.Push(ScreenHistory.ScreenId.DrivingScene);

        // 주행 시나리오에서는 NavigationBar 표시
        if (_navigationBar != null)
            _navigationBar.Show();

         ShowOnly(ScreenHistory.ScreenId.DrivingScene);
    }

    public void ShowParkingScene()
    {
        if (_modeManager == null)
        {
            Debug.LogError("[UIManager] ShowParkingScene: ModeManager가 null입니다");
            return;
        }

        _modeManager.SwitchTo(AppMode.Parking);
        _screenHistory?.Push(ScreenHistory.ScreenId.ParkingScene);

        // 주차 시나리오에서는 NavigationBar 표시
        if (_navigationBar != null)
            _navigationBar.Show();

        ShowOnly(ScreenHistory.ScreenId.ParkingScene);
    }

    private void Update()
    {
        if (_cameraSwitcher == null) return;
        PaintTab(_driverTab, _driverLabel, _cameraSwitcher.CurrentView == CameraView.Driver);
        PaintTab(_birdTab, _birdLabel, _cameraSwitcher.CurrentView == CameraView.BirdEye);
    }

    private void PaintTab(Button tab, TMP_Text label, bool active)
    {
        if (tab != null) tab.targetGraphic.color = active ? _accentColor : _tabIdleColor;
        if (label != null) label.color = active ? _tabActiveTextColor : _tabIdleTextColor;
    }

}
