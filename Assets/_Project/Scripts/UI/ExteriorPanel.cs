using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// UI-01 외관 선택: 차체 색상·휠 탭, 색상 버튼, 휠 버튼, 선택 요약 카드, [다음] 버튼
// 배열 순서 — 탭: 0 차체 색상, 1 휠 / 버튼: CarAppearance의 Body Materials / Wheels 순서와 같아야 한다
public class ExteriorPanel : MonoBehaviour
{
    private const int TabCount = 2;

    [SerializeField] private CarConfig _config;
    // 버튼을 누를 때마다 미리보기 차에 적용하려고 부른다
    [SerializeField] private ConfiguratorArea _area;

    [Header("Tabs")]
    [SerializeField] private Button[] _tabs = new Button[TabCount];
    [SerializeField] private TMP_Text[] _tabLabels = new TMP_Text[TabCount];
    [SerializeField] private Graphic[] _tabUnderlines = new Graphic[TabCount];   // 선택된 탭 아래 파란 줄
    [SerializeField] private GameObject[] _tabPages = new GameObject[TabCount];

    [Header("Color")]
    [SerializeField] private Button[] _colorButtons;
    [SerializeField] private Graphic[] _colorBorders;   // 선택된 색상 버튼을 두르는 고리
    [SerializeField] private string[] _colorNames;      // 화면에 띄울 이름
    [SerializeField] private Color[] _colorSwatches;    // 요약 카드의 색 점에 넣을 색
    [SerializeField] private TMP_Text _colorNameText;   // 색상 버튼 아래 선택 이름

    [Header("Wheel")]
    [SerializeField] private Button[] _wheelButtons;
    [SerializeField] private Graphic[] _wheelBorders;
    [SerializeField] private string[] _wheelNames;
    [SerializeField] private TMP_Text _wheelNameText;

    // 선택 요약 카드 (FR-58)
    [Header("Summary")]
    [SerializeField] private Graphic _summaryColorDot;
    [SerializeField] private TMP_Text _summaryColorText;
    [SerializeField] private TMP_Text _summaryWheelText;

    [Header("Bottom")]
    [SerializeField] private Button _nextButton;

    [Header("Colors")]
    [SerializeField] private Color _accentColor = new Color32(0x28, 0x66, 0xF2, 0xFF);
    [SerializeField] private Color _ringIdleColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color _borderIdleColor = new Color(1f, 1f, 1f, 0.22f);
    [SerializeField] private Color _tabActiveTextColor = Color.white;
    [SerializeField] private Color _tabIdleTextColor = new Color32(0x9A, 0xA0, 0xAC, 0xFF);

    [Header("Events")]
    [SerializeField] private UnityEvent _onNext;

    private int _currentTab;

    public int CurrentTab => _currentTab;
    public UnityEvent NextClicked => _onNext;

    private void Awake()
    {
        for (int i = 0; i < _tabs.Length; i++)
        {
            int tab = i;
            _tabs[i].onClick.AddListener(() => ShowTab(tab));
        }

        for (int i = 0; i < _colorButtons.Length; i++)
        {
            int index = i;
            _colorButtons[i].onClick.AddListener(() => OnColorClicked(index));
        }

        for (int i = 0; i < _wheelButtons.Length; i++)
        {
            int index = i;
            _wheelButtons[i].onClick.AddListener(() => OnWheelClicked(index));
        }

        _nextButton.onClick.AddListener(() => _onNext.Invoke());
    }

    // 뒤로가기·홈으로 돌아왔을 때도 CarConfig 값을 그대로 보여 준다 (FR-55)
    private void OnEnable()
    {
        Refresh();
        ShowTab(_currentTab);
    }

    public void Bind(CarConfig config)
    {
        _config = config;
        Refresh();
    }

    public void ShowTab(int tab)
    {
        _currentTab = Mathf.Clamp(tab, 0, TabCount - 1);

        for (int i = 0; i < TabCount; i++)
        {
            bool active = i == _currentTab;
            _tabPages[i].SetActive(active);
            _tabLabels[i].color = active ? _tabActiveTextColor : _tabIdleTextColor;
            _tabUnderlines[i].color = active ? _accentColor : Color.clear;
        }
    }

    // 색상 버튼을 누르면 차체 색을 바꾼다 (FR-01)
    public void OnColorClicked(int index)
    {
        if (_config == null) return;
        _config.BodyColor = index;
        ApplyAndRefresh();
    }

    // 휠 버튼을 누르면 휠을 바꾼다 (FR-03)
    public void OnWheelClicked(int index)
    {
        if (_config == null) return;
        _config.WheelIndex = index;
        ApplyAndRefresh();
    }

    // 미리보기 차에 적용하고 화면을 다시 그린다
    private void ApplyAndRefresh()
    {
        if (_area != null)
            _area.ApplyAppearance(_config);
        else
            Debug.LogError("[ExteriorPanel] ApplyAndRefresh: _area가 비어 있음 (Inspector 연결 확인)");

        Refresh();
    }

    // CarConfig 값으로 버튼 강조, 선택 이름, 요약 카드를 맞춘다. 화면 표시는 여기서만 바꾼다
    private void Refresh()
    {
        if (_config == null)
        {
            Debug.LogError("[ExteriorPanel] Refresh: _config가 비어 있음 (Inspector 연결 확인)");
            return;
        }

        int color = _config.BodyColor;
        int wheel = _config.WheelIndex;

        Highlight(_colorBorders, color, _ringIdleColor);
        Highlight(_wheelBorders, wheel, _borderIdleColor);

        string colorName = NameOf(_colorNames, color);
        string wheelName = NameOf(_wheelNames, wheel);

        if (_colorNameText != null) _colorNameText.text = colorName;
        if (_wheelNameText != null) _wheelNameText.text = wheelName;
        if (_summaryColorText != null) _summaryColorText.text = colorName;
        if (_summaryWheelText != null) _summaryWheelText.text = wheelName;

        if (_summaryColorDot != null && color >= 0 && color < _colorSwatches.Length)
            _summaryColorDot.color = _colorSwatches[color];
    }

    // 선택된 칸의 테두리만 강조색으로 바꾼다
    private void Highlight(Graphic[] borders, int selected, Color idleColor)
    {
        for (int i = 0; i < borders.Length; i++)
        {
            if (borders[i] != null)
                borders[i].color = i == selected ? _accentColor : idleColor;
        }
    }

    // 이름이 없는 번호면 빈 글자를 돌려준다
    private static string NameOf(string[] names, int index)
    {
        return index >= 0 && index < names.Length ? names[index] : "";
    }
}
