using UnityEngine;
using UnityEngine.UI;

public class NavigationBar : MonoBehaviour
{
    [SerializeField] public Button _backButton;
    [SerializeField] public Button _homeButton;
    private ScreenHistory _screenHistory;
    private UIManager _uiManager;

    private void Awake()
    {
        _screenHistory = FindObjectOfType<ScreenHistory>();
        _uiManager = FindObjectOfType<UIManager>();

        if (_screenHistory == null)
        {
            Debug.LogError("[NavigationBar] ScreenHistory를 찾을 수 없습니다");
        }

        if (_uiManager == null)
        {
            Debug.LogError("[NavigationBar] UIManager를 찾을 수 없습니다");
        }

        if (_backButton == null)
        {
            Debug.LogError("[NavigationBar] _backButton이 비어 있습니다");
        }

        if (_homeButton == null)
        {
            Debug.LogError("[NavigationBar] _homeButton이 비어 있습니다");
        }

        // 버튼 이벤트 연결
        if (_backButton != null)
            _backButton.onClick.AddListener(OnBackClicked);

        if (_homeButton != null)
            _homeButton.onClick.AddListener(OnHomeClicked);
    }

    public void OnBackClicked()
    {
        if (_screenHistory == null || _uiManager == null)
        {
            Debug.LogError("[NavigationBar] OnBackClicked: ScreenHistory 또는 UIManager가 null입니다");
            return;
        }

        _screenHistory.Pop(); // 지금 화면을 꺼내고
        NavigateToScreen(_screenHistory.Peek()); // 그 아래에 있던 화면으로 간다
    }

    public void OnHomeClicked()
    {
        if (_screenHistory == null || _uiManager == null)
        {
            Debug.LogError("[NavigationBar] OnHomeClicked: ScreenHistory 또는 UIManager가 null입니다");
            return;
        }

        _screenHistory.Clear();
        NavigateToScreen(ScreenHistory.ScreenId.Exterior);
    }

    private void NavigateToScreen(ScreenHistory.ScreenId screenId)
    {
        switch (screenId)
        {
            case ScreenHistory.ScreenId.Exterior:
                _uiManager.ShowExterior();
                break;

            case ScreenHistory.ScreenId.Feature:
                _uiManager.ShowFeature();
                break;

            case ScreenHistory.ScreenId.DrivingScene:
                Debug.Log("[NavigationBar] 주행 화면으로 이동 요청 (ParkingScenario 미구현)");
                _uiManager.ShowDrivingScene();
                break;

            case ScreenHistory.ScreenId.ParkingScene:
                Debug.Log("[NavigationBar] 주차 화면으로 이동 요청 (ParkingScenario 미구현)");
                _uiManager.ShowParkingScene();
                break;

            default:
                Debug.LogWarning($"[NavigationBar] 알 수 없는 화면 ID: {screenId}");
                break;
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
