using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


// UI-02 기능 선택: 주행·주차 탭, 기능 체크박스 4개, 설명 문장, 탭별 시작 버튼
// 배열 순서 — 탭: 0 주행, 1 주차 / 기능: 0 나이트 비전, 1 후륜 조향, 2 발렛, 3 원격 주차
public class FeaturePanel : MonoBehaviour
{
    private const int TabCount = 2;       //주행/주차
    private const int FeaturesPerTab = 2; // 탭당 옵션 2개

    [SerializeField] private CarConfig _config;

    [Header("Tabs")]
    [SerializeField] private Button[] _tabs = new Button[TabCount]; //[2]; 0주행 1주차
    [SerializeField] private TMP_Text[] _tabLabels = new TMP_Text[TabCount];         //[2] 주행,주차 텍스트 - 선택되면 글자색 바꿀려고
    [SerializeField] private GameObject[] _tabPages = new GameObject[TabCount]; //[2] 0 DrivingTab , 1 ParkingTab
    [SerializeField] private Button[] _startButtons = new Button[TabCount];
    [SerializeField] private Graphic[] _startGlows = new Graphic[TabCount];         //[2] 시작 버튼 뒤 불빛 이미지

    [Header("Features")]
    [SerializeField] private Toggle[] _toggles = new Toggle[TabCount * FeaturesPerTab]; //[4] 0 NightVisionToggle , 1 RearWheelToggle , 2 	ValetToggle , 3 	RemoteToggle
    [SerializeField] private TMP_Text[] _descriptionTexts = new TMP_Text[TabCount * FeaturesPerTab];
    [SerializeField] private Graphic[] _cardBorders = new Graphic[TabCount * FeaturesPerTab]; // 옵션 카드 테두리
    [SerializeField] private GameObject[] _checkMarks = new GameObject[TabCount * FeaturesPerTab]; // 체크 아이콘

    [Header("Bottom")]
    [SerializeField] private Button _previousButton; //이전 버튼
    [SerializeField] private TMP_Text _hintText;
    [SerializeField] private string _hintMessage = "기능을 1개 이상 선택하면 시작할 수 있어요.";
    [SerializeField] private string _emptyMessage = "기능을 1개 이상 선택하세요.";

    [Header("Colors")]
    [SerializeField] private Color _accentColor = new Color32(0x28, 0x66, 0xF2, 0xFF); // 선택되었을 때 파란색
    [SerializeField] private Color _tabIdleColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color _tabActiveTextColor = Color.white;
    [SerializeField] private Color _tabIdleTextColor = new Color32(0xC9, 0xCD, 0xD6, 0xFF);
    [SerializeField] private Color _borderIdleColor = new Color(1f, 1f, 1f, 0.22f);

    [Header("Events")]
    [SerializeField] private UnityEvent<int> _onTabChanged;
    [SerializeField] private UnityEvent _onStartDriving; 
    [SerializeField] private UnityEvent _onStartParking;
    [SerializeField] private UnityEvent _onPrevious;

    private int _currentTab;

    public int CurrentTab => _currentTab;
    public UnityEvent<int> TabChanged => _onTabChanged;
    public UnityEvent StartDrivingClicked => _onStartDriving;
    public UnityEvent StartParkingClicked => _onStartParking;
    public UnityEvent PreviousClicked => _onPrevious;

    private void Awake()
    {
        for (int i = 0; i < _tabs.Length; i++)
        {
            int tab = i;
            _tabs[i].onClick.AddListener(() => ShowTab(tab)); // 0주행버튼->주행 1주차버튼->주차
        }

        for (int i = 0; i < _toggles.Length; i++)
        {
            int feature = i;
            _toggles[i].onValueChanged.AddListener(isOn => OnToggleChanged(feature, isOn)); //체크박스 누르면 CarConfig 바뀜
        }

        _startButtons[0].onClick.AddListener(() => _onStartDriving.Invoke()); //주행 시작 버튼 누르면 _onStartDriving 방송
        _startButtons[1].onClick.AddListener(() => _onStartParking.Invoke());
        _previousButton.onClick.AddListener(() => _onPrevious.Invoke());      // 이전 버튼 누르면 _onPrevious 방송
    }

    // 뒤로가기·홈으로 돌아왔을 때도 CarConfig 값을 그대로 보여 준다 (FR-55)
    private void OnEnable()  //화면이 켜질 때마다 새로 맞추는 함수
    {
        SyncFromConfig(); //CarConfig 값 -> 체크박스 맞춤
        ShowTab(_currentTab); //마지막에 보던 챕 보여줌
    }

    // 밖에서 직접 부를 때, 인자로 받은 CarConfig로 교체하고 그 내용으로 화면 갱신   - 패널이 켜져있을 때 Carconfig가 바뀌면 바로 갱신할 수 있게(누군가가 Bind(새 CarConfig)를 부르면)
    public void Bind(CarConfig config) 
    {
        _config = config;
        SyncFromConfig(); 
        RefreshStartButtons(); //시작 버튼 상태 갱신
    }

    public void ShowTab(int tab)
    {
        _currentTab = Mathf.Clamp(tab, 0, TabCount - 1); // _currentTab (0~탭개수)

        for (int i = 0; i < TabCount; i++)
        {
            bool active = i == _currentTab;
            _tabPages[i].SetActive(active);
            _tabs[i].targetGraphic.color = active ? _accentColor : _tabIdleColor;
            _tabLabels[i].color = active ? _tabActiveTextColor : _tabIdleTextColor;
        }

        RefreshStartButtons(); //시작 버튼과 안내 문구를 다시 맞춤
        _onTabChanged.Invoke(_currentTab); // 탭이 바뀌었다 방송 -> 다른 클래스가 이 소식 들을 수 있게
    }

    //체크박스 누르면 CarConfig 바뀜
    private void OnToggleChanged(int feature, bool isOn)  //몇번째 체크  박스, 체크=true
    {
        if (_config != null)
        {
            switch (feature)
            {
                case 0: _config.NightVision = isOn; break;
                case 1: _config.RearWheelSteering = isOn; break;
                case 2: _config.ValetParking = isOn; break;
                case 3: _config.RemoteParking = isOn; break;
            }
        }

        RefreshCard(feature); //방금 바뀐 그 카드 하나의 체크 표시와 테두리 색을 갱신
        RefreshStartButtons(); //시작 버튼과 안내 문구를 다시 맞춤
    }

    //CarConfig 값 -> 체크박스 맞춤
    private void SyncFromConfig()
    {
        if (_config != null)
        {
            _toggles[0].SetIsOnWithoutNotify(_config.NightVision);
            _toggles[1].SetIsOnWithoutNotify(_config.RearWheelSteering);
            _toggles[2].SetIsOnWithoutNotify(_config.ValetParking);
            _toggles[3].SetIsOnWithoutNotify(_config.RemoteParking);
        }

        for (int i = 0; i < _toggles.Length; i++)
            RefreshCard(i); //카드모양(체크 아이콘,테두리 색) 각각 갱신
    }

    private void RefreshCard(int feature)
    {
        bool isOn = _toggles[feature].isOn;
        _checkMarks[feature].SetActive(isOn);
        _cardBorders[feature].color = isOn ? _accentColor : _borderIdleColor;
    }

    // 탭에서 선택한 기능이 0개면 그 탭의 시작 버튼을 비활성화한다 (FR-61)
    private void RefreshStartButtons()
    {
        bool hasDriving = _config.HasDrivingFeature(); //CarConfig에게 주행기능 하나라도 켜졌는지?
        bool hasParking = _config.HasParkingFeature(); //CarConfig에게 주차기능 하나라도 켜졌는지?

        _startButtons[0].interactable = hasDriving;  //시작 버튼 눌러도 되는지 결정 (true면 시작버튼 활성화)
        _startGlows[0].enabled = hasDriving;
        _startButtons[1].interactable = hasParking;
        _startGlows[1].enabled = hasParking;

        bool currentOk = _currentTab == 0 ? hasDriving : hasParking;  //_currentTab 0 이면 주행기능 여부 , 1이면 주차기능 여부
        _hintText.text = currentOk ? _hintMessage : _emptyMessage;  //주행or주차일 때 시작 불가능 상태면 empty문구 뜸
    }
}

