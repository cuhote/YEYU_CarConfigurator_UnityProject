using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private NavigationBar _navigationBar;
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

    public void ShowExterior()
    {
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
    }
}
