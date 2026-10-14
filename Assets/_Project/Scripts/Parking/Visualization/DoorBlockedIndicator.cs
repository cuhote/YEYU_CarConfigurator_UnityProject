using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

// 문을 열 공간이 없을 때 운전석 문이 열리다 옆 차에 막히는 모습을 되풀이하고, 문 위에 "탑승 불가"를 띄운다.
// 문을 열 수 있게 되면(DoorSwingDrawer.CanOpen) 문을 닫고 글자를 숨긴다.
[RequireComponent(typeof(DoorSwingDrawer))]
public class DoorBlockedIndicator : MonoBehaviour
{
    [Header("문")]
    [SerializeField] private string _doorPivotName = "Door_Ex_FL_GT"; // 경첩 자리에 축이 있는 운전석 문 묶음
    [SerializeField] private float _doorLength = 1.1f;     // 경첩에서 문 끝까지 (m)
    [SerializeField] private float _doorHeight = 0.8f;     // 옆 차와 닿는지 볼 문의 높이 (m)
    [SerializeField] private float _maxOpenAngle = 60f;    // 막는 것이 없을 때 여는 각도 (도)
    [SerializeField] private float _minShownAngle = 4f;    // 틈이 거의 없어도 이만큼은 움직여 보인다 (도)
    [SerializeField] private bool _invertOpenDirection;    // 문이 안쪽으로 돌면 켠다

    [Header("움직임")]
    [SerializeField] private float _openSpeed = 45f;       // 여는 속도 (도/초)
    [SerializeField] private float _closeSpeed = 60f;      // 닫는 속도 (도/초)
    [SerializeField] private float _bounceAngle = 3f;      // 옆 차에 닿은 뒤 되튕기는 각도 (도)
    [SerializeField] private float _holdTime = 0.6f;       // 막힌 채로 멈춰 있는 시간 (초)
    [SerializeField] private float _retryDelay = 1.5f;     // 닫은 뒤 다시 열어 볼 때까지 (초)
    [SerializeField] private float _boardOpenSpeed = 90f;  // [탑승] 때 문을 여는 속도 (도/초)

    [Header("글자")]
    [SerializeField] private string _labelText = "탑승 불가";
    [SerializeField] private float _labelHeight = 1.75f;   // 차 바닥에서 글자까지 높이 (m)
    [SerializeField] private float _labelFontSize = 6f;    // 글자 크기 (10이 약 1m)
    [SerializeField] private Vector2 _labelPadding = new Vector2(0.4f, 0.2f);
    [SerializeField] private float _flashTime = 0.5f;      // 문이 닿았을 때 배경이 깜빡이는 시간 (초)

    private static readonly Color LabelBackground = new Color(0.85f, 0.1f, 0.1f, 0.9f);
    private static readonly Color FlashBackground = new Color(1f, 0.5f, 0.45f, 1f);
    private const float SweepStep = 1f;         // 막히는 각도를 찾을 때의 간격 (도)
    private const float DoorHalfThickness = 0.03f;

    private enum Phase { Wait, Open, Bounce, Hold, Close }

    private static readonly Collider[] Hits = new Collider[8];
    private static Mesh _quadMesh;

    private DoorSwingDrawer _drawer;
    private CarController _car;
    private Transform _doorPivot;
    private Quaternion _closedRotation;
    private float _openSign = 1f;
    private int _obstacleMask;

    private Phase _phase = Phase.Wait;
    private float _angle;           // 지금 문이 열린 각도 (도)
    private float _blockedAngle;    // 옆 차에 닿는 각도 (도)
    private float _timer;
    private bool _boarding;         // [탑승]으로 문을 여는 중
    private float _boardAngle;      // [탑승] 때 여는 각도 (도)

    // [탑승] 때 연 문이 다 열렸는지
    public bool IsOpenForBoarding => _boarding && (_doorPivot == null || Mathf.Approximately(_angle, _boardAngle));

    private Transform _label;
    private Material _labelMaterial;
    private float _flashTimer;

    private void Awake()
    {
        _drawer = GetComponent<DoorSwingDrawer>();
        _car = GetComponent<CarController>();
        _obstacleMask = LayerMask.GetMask("Obstacle", "Vehicle");

        _doorPivot = FindChild(transform, _doorPivotName);
        if (_doorPivot == null)
        {
            Debug.LogError($"[DoorBlockedIndicator] 문 오브젝트({_doorPivotName})를 찾을 수 없습니다", this);
        }
        else
        {
            _closedRotation = _doorPivot.localRotation;
            // 경첩이 차 왼쪽에 있으면 위에서 볼 때 시계 방향으로 돌아야 바깥으로 열린다
            _openSign = transform.InverseTransformPoint(_doorPivot.position).x < 0f ? 1f : -1f;
            if (_invertOpenDirection) _openSign = -_openSign;
        }

        BuildLabel();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += FaceCamera;
        _phase = Phase.Wait;
        _timer = _retryDelay;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= FaceCamera;

        // 꺼질 때는 문을 닫아 둔다
        _boarding = false;
        _angle = 0f;
        ApplyAngle();
        if (_label != null) _label.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_labelMaterial != null) Destroy(_labelMaterial);
    }

    private void Update()
    {
        if (_drawer == null)
            return;

        bool blocked = !_drawer.CanOpen();
        UpdateLabel(blocked);

        if (_doorPivot == null)
            return;

        if (_boarding)
        {
            MoveAngle(_boardAngle, _boardOpenSpeed);
            return;
        }

        // 문을 열 수 있거나 차가 움직이는 동안에는 문을 닫아 둔다
        bool stopped = _car == null || Mathf.Abs(_car.SpeedKmh) < 0.05f;
        if (!blocked || !stopped)
        {
            MoveAngle(0f, _closeSpeed);
            _phase = Phase.Wait;
            _timer = _retryDelay;
            return;
        }

        switch (_phase)
        {
            case Phase.Wait:
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _blockedAngle = Mathf.Max(FindBlockedAngle(), _minShownAngle);
                    _phase = Phase.Open;
                }
                break;

            case Phase.Open:
                if (MoveAngle(_blockedAngle, _openSpeed))
                {
                    _flashTimer = _flashTime;
                    _phase = Phase.Bounce;
                }
                break;

            case Phase.Bounce:
                if (MoveAngle(Mathf.Max(0f, _blockedAngle - _bounceAngle), _openSpeed))
                {
                    _timer = _holdTime;
                    _phase = Phase.Hold;
                }
                break;

            case Phase.Hold:
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                    _phase = Phase.Close;
                break;

            case Phase.Close:
                if (MoveAngle(0f, _closeSpeed))
                {
                    _timer = _retryDelay;
                    _phase = Phase.Wait;
                }
                break;
        }
    }

    // [탑승]: 운전석 문을 열 수 있는 데까지 연다. CloseDoor를 부를 때까지 열어 둔다
    public void OpenForBoarding()
    {
        _boarding = true;
        _boardAngle = _doorPivot != null ? FindBlockedAngle() : 0f;
    }

    // 탑승이 끝났거나 장면을 되돌릴 때 문을 닫는다
    public void CloseDoor()
    {
        _boarding = false;
        _phase = Phase.Wait;
        _timer = _retryDelay;
    }

    // 문을 target 각도 쪽으로 돌린다. 다 왔으면 true
    private bool MoveAngle(float target, float speed)
    {
        _angle = Mathf.MoveTowards(_angle, target, speed * Time.deltaTime);
        ApplyAngle();
        return Mathf.Approximately(_angle, target);
    }

    private void ApplyAngle()
    {
        if (_doorPivot != null)
            _doorPivot.localRotation = _closedRotation * Quaternion.Euler(0f, _openSign * _angle, 0f);
    }

    // 문을 조금씩 열어 보며 다른 차나 장애물에 닿기 직전의 각도를 찾는다
    private float FindBlockedAngle()
    {
        Vector3 hinge = _doorPivot.position;
        Vector3 up = transform.up;
        Vector3 closedDir = -transform.forward;   // 닫힌 문은 경첩에서 차 뒤쪽으로 뻗는다
        Vector3 halfExtents = new Vector3(DoorHalfThickness, _doorHeight * 0.5f, _doorLength * 0.5f);

        for (float angle = SweepStep; angle <= _maxOpenAngle; angle += SweepStep)
        {
            Vector3 dir = Quaternion.AngleAxis(_openSign * angle, up) * closedDir;
            Vector3 center = hinge + dir * (_doorLength * 0.5f);
            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, Hits, Quaternion.LookRotation(dir, up),
                                                   _obstacleMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                // 자기 차의 콜라이더는 뺀다
                if (!Hits[i].transform.IsChildOf(transform))
                    return angle - SweepStep;
            }
        }
        return _maxOpenAngle;
    }

    // 빨간 바탕에 흰 글자. 운전석 문 위에 두고 카메라를 향해 돈다
    private void BuildLabel()
    {
        int layer = LayerMask.NameToLayer("Visualization");
        if (layer == -1) layer = gameObject.layer;

        GameObject labelObj = new GameObject("DoorBlockedLabel");
        labelObj.layer = layer;
        _label = labelObj.transform;
        _label.SetParent(transform, false);

        // 문 가운데 위. 문 오브젝트를 못 찾았으면 차 가운데 위에 둔다
        Vector3 doorCenter = Vector3.zero;
        if (_doorPivot != null)
        {
            doorCenter = transform.InverseTransformPoint(_doorPivot.position);
            doorCenter.z -= _doorLength * 0.5f;
        }
        _label.localPosition = new Vector3(doorCenter.x, _labelHeight, doorCenter.z);

        GameObject textObj = new GameObject("Text");
        textObj.layer = layer;
        textObj.transform.SetParent(_label, false);
        TextMeshPro text = textObj.AddComponent<TextMeshPro>();
        TMP_FontAsset font = VehicleStatusLabel.GetKoreanFont();
        if (font != null)
            text.font = font;
        text.text = _labelText;
        text.fontSize = _labelFontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(8f, 2f);
        text.GetComponent<MeshRenderer>().sortingOrder = 1;

        GameObject bgObj = new GameObject("Background");
        bgObj.layer = layer;
        bgObj.transform.SetParent(_label, false);
        bgObj.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        Vector2 size = text.GetPreferredValues(_labelText);
        bgObj.transform.localScale = new Vector3(size.x + _labelPadding.x * 2f, size.y + _labelPadding.y * 2f, 1f);
        bgObj.AddComponent<MeshFilter>().sharedMesh = GetQuadMesh();
        MeshRenderer bgRenderer = bgObj.AddComponent<MeshRenderer>();
        _labelMaterial = new Material(Shader.Find("Sprites/Default"));
        _labelMaterial.color = LabelBackground;
        bgRenderer.sharedMaterial = _labelMaterial;
        bgRenderer.shadowCastingMode = ShadowCastingMode.Off;
        bgRenderer.receiveShadows = false;
        bgRenderer.sortingOrder = 0;

        labelObj.SetActive(false);
    }

    private void UpdateLabel(bool blocked)
    {
        if (_label == null)
            return;

        if (_label.gameObject.activeSelf != blocked)
            _label.gameObject.SetActive(blocked);
        if (!blocked)
        {
            _flashTimer = 0f;
            return;
        }

        // 문이 옆 차에 닿은 순간 배경을 잠깐 깜빡인다
        Color color = LabelBackground;
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            float wave = 0.5f + 0.5f * Mathf.Sin(_flashTimer * Mathf.PI * 2f / 0.25f);
            color = Color.Lerp(LabelBackground, FlashBackground, wave);
        }
        _labelMaterial.color = color;
    }

    // 화면을 그리는 카메라마다 글자가 그 카메라를 정면으로 보게 돌린다
    private void FaceCamera(ScriptableRenderContext context, Camera cam)
    {
        if (_label != null && _label.gameObject.activeSelf)
            _label.rotation = cam.transform.rotation;
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }
        return null;
    }

    // 가운데가 원점인 1x1 사각형 (글자 배경)
    private static Mesh GetQuadMesh()
    {
        if (_quadMesh != null)
            return _quadMesh;

        _quadMesh = new Mesh { name = "DoorBlockedLabelQuad" };
        _quadMesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
        };
        _quadMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        _quadMesh.RecalculateNormals();
        return _quadMesh;
    }
}
