using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

// 차 머리 위에 ValetAgent의 상태를 글자로 띄운다.
// ValetAgent가 Start에서 붙이고 Bind로 연결한다. 라벨 오브젝트는 코드로 만든다.
public class VehicleStatusLabel : MonoBehaviour
{
    [SerializeField] private float _height = 2.2f;         // 차 바닥에서 라벨까지 높이 (m)
    [SerializeField] private float _forwardOffset = 1.4f;  // 뒤차축에서 차 가운데까지 (m)
    [SerializeField] private float _fontSize = 7f;         // 글자 크기 (10이 약 1m)
    [SerializeField] private Vector2 _padding = new Vector2(0.5f, 0.25f); // 글자 둘레 여백 (m)
    [SerializeField] private float _blinkPeriod = 0.5f;    // 정지 경고 깜빡임 주기 (초)

    private static readonly Color MovingColor = Color.white;
    private static readonly Color WaitingColor = new Color(1f, 0.9f, 0.2f);
    private static readonly Color ReversingColor = new Color(0.45f, 0.8f, 1f);
    private static readonly Color ParkedColor = new Color(0.4f, 0.9f, 0.45f);
    private static readonly Color NormalBackground = new Color(0f, 0f, 0f, 0.65f);
    private static readonly Color BlockedBackground = new Color(0.85f, 0.1f, 0.1f, 0.9f);

    private const string KoreanFontName = "Pretendard-Bold SDF";
    private const string KoreanFontPath = "Assets/_Project/UI/Fonts/Pretendard-Bold SDF.asset";

    private static TMP_FontAsset _sharedFont;
    private static bool _fontSearched;
    private static Mesh _quadMesh;

    private ValetAgent _agent;
    private Transform _root;            // 라벨 전체 (카메라를 향해 돈다)
    private Transform _background;
    private TextMeshPro _text;
    private Material _backgroundMaterial;

    private ValetAgent.AgentState _shownState;
    private bool _shownBlocked;
    private bool _hasShown;
    private string _blockedText;

    // ValetAgent가 한 번 부른다. car는 라벨이 따라다닐 차
    public void Bind(ValetAgent agent, Transform car)
    {
        _agent = agent;
        if (_root == null)
            Build(car);
    }

    private void Build(Transform car)
    {
        GameObject rootObj = new GameObject("StatusLabel");
        _root = rootObj.transform;
        _root.SetParent(car, false);
        _root.localPosition = new Vector3(0f, _height, _forwardOffset);

        // 배경: 단색 사각형. 글자보다 살짝 뒤에 둔다
        GameObject bgObj = new GameObject("Background");
        _background = bgObj.transform;
        _background.SetParent(_root, false);
        _background.localPosition = new Vector3(0f, 0f, 0.02f);
        bgObj.AddComponent<MeshFilter>().sharedMesh = GetQuadMesh();
        MeshRenderer bgRenderer = bgObj.AddComponent<MeshRenderer>();
        _backgroundMaterial = new Material(Shader.Find("Sprites/Default"));
        bgRenderer.sharedMaterial = _backgroundMaterial;
        bgRenderer.shadowCastingMode = ShadowCastingMode.Off;
        bgRenderer.receiveShadows = false;
        bgRenderer.sortingOrder = 0;

        // 글자
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(_root, false);
        _text = textObj.AddComponent<TextMeshPro>();
        TMP_FontAsset font = GetKoreanFont();
        if (font != null)
            _text.font = font;
        _text.fontSize = _fontSize;
        _text.alignment = TextAlignmentOptions.Center;
        _text.textWrappingMode = TextWrappingModes.NoWrap;
        _text.raycastTarget = false;
        _text.rectTransform.sizeDelta = new Vector2(8f, 2f);
        _text.GetComponent<MeshRenderer>().sortingOrder = 1;

        // 경고 기호가 글꼴에 없으면 느낌표로 대신한다
        bool hasWarningSign = _text.font != null && _text.font.HasCharacter('⚠', true, true);
        _blockedText = hasWarningSign ? "⚠ 정지" : "! 정지";

        _root.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += FaceCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= FaceCamera;
    }

    private void OnDestroy()
    {
        if (_backgroundMaterial != null)
            Destroy(_backgroundMaterial);
    }

    // 화면을 그리는 카메라마다 그 카메라를 정면으로 보게 돌린다.
    // 버드뷰에서는 바닥에 눕고 글자 위쪽이 화면 위쪽을 향한다
    private void FaceCamera(ScriptableRenderContext context, Camera cam)
    {
        if (_root != null && _root.gameObject.activeSelf)
            _root.rotation = cam.transform.rotation;
    }

    private void Update()
    {
        if (_agent == null || _root == null)
            return;

        ValetAgent.AgentState state = _agent.State;
        bool blocked = state == ValetAgent.AgentState.Moving && _agent.IsBlocked;

        // 상태가 바뀔 때만 글자와 색을 다시 넣는다
        if (!_hasShown || state != _shownState || blocked != _shownBlocked)
        {
            _hasShown = true;
            _shownState = state;
            _shownBlocked = blocked;
            Refresh(state, blocked);
        }

        // 정지 경고는 배경의 투명도만 약하게 깜빡인다
        if (blocked)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / _blinkPeriod);
            Color color = BlockedBackground;
            color.a = Mathf.Lerp(0.6f, BlockedBackground.a, wave);
            _backgroundMaterial.color = color;
        }
    }

    private void Refresh(ValetAgent.AgentState state, bool blocked)
    {
        string message;
        Color textColor;

        if (blocked)
        {
            message = _blockedText;
            textColor = Color.white;
        }
        else
        {
            switch (state)
            {
                case ValetAgent.AgentState.Moving: message = "이동 중"; textColor = MovingColor; break;
                case ValetAgent.AgentState.Waiting: message = "대기 중"; textColor = WaitingColor; break;
                case ValetAgent.AgentState.Reversing: message = "후진 중"; textColor = ReversingColor; break;
                case ValetAgent.AgentState.Parked: message = "주차 완료"; textColor = ParkedColor; break;
                default:
                    // Idle, Full은 라벨을 숨긴다
                    _root.gameObject.SetActive(false);
                    return;
            }
        }

        _root.gameObject.SetActive(true);
        _text.text = message;
        _text.color = textColor;
        _backgroundMaterial.color = blocked ? BlockedBackground : NormalBackground;

        // 배경 크기를 글자 크기에 맞춘다
        Vector2 size = _text.GetPreferredValues(message);
        _background.localScale = new Vector3(size.x + _padding.x * 2f, size.y + _padding.y * 2f, 1f);
    }

    // 한글이 들어 있는 글꼴을 찾는다. TMP 기본 글꼴에는 한글이 없다
    private static TMP_FontAsset GetKoreanFont()
    {
        if (_fontSearched)
            return _sharedFont;
        _fontSearched = true;

        // UI가 이미 쓰고 있어 메모리에 올라와 있는 글꼴에서 찾는다
        foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (font.name == KoreanFontName)
            {
                _sharedFont = font;
                break;
            }
        }

#if UNITY_EDITOR
        if (_sharedFont == null)
            _sharedFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
#endif

        if (_sharedFont == null)
            Debug.LogWarning($"[VehicleStatusLabel] 한글 글꼴({KoreanFontName})을 찾지 못해 기본 글꼴을 씀. 한글이 네모로 보일 수 있음");

        return _sharedFont;
    }

    // 가운데가 원점인 1x1 사각형
    private static Mesh GetQuadMesh()
    {
        if (_quadMesh != null)
            return _quadMesh;

        _quadMesh = new Mesh { name = "StatusLabelQuad" };
        _quadMesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
        };
        _quadMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
        _quadMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        _quadMesh.RecalculateNormals();
        return _quadMesh;
    }
}
