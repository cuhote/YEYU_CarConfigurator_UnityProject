using UnityEngine;

public class DoorSwingDrawer : MonoBehaviour
{
    [SerializeField] private float _doorLength = 1.1f;
    [SerializeField] private float _requiredGap = 0.6f;
    [SerializeField] private Vector3 _hingeOffset = new Vector3(-0.95f, 0f, 1.3f);
    [SerializeField] private bool _driverOnLeft = true;

    private LineRenderer _lineRenderer;
    private Vector3[] _arcPoints;
    private const int SEGMENTS = 16;
    private bool _lastCanOpenState = true;
    private Material _lineMaterial;
    private int _visualizationLayer;

    private void Awake()
    {
        // Visualization 레이어 확인
        _visualizationLayer = LayerMask.NameToLayer("Visualization");
        if (_visualizationLayer == -1)
        {
            Debug.LogError("[DoorSwingDrawer] Visualization 레이어를 찾을 수 없습니다");
            return;
        }

        // LineRenderer 생성 (자식 오브젝트)
        GameObject lineObj = new GameObject("DoorArcRenderer");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.layer = _visualizationLayer;

        _lineRenderer = lineObj.AddComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = false;
        _lineRenderer.widthMultiplier = 0.08f;
        _lineRenderer.positionCount = SEGMENTS + 1;

        // 머티리얼 생성 (단색 Unlit)
        _lineMaterial = new Material(Shader.Find("Unlit/Color"));
        _lineRenderer.material = _lineMaterial;

        // 호 점들 미리 계산 (로컬 좌표)
        CalculateArcPoints();

        // 초기 색상 설정
        UpdateLineColor();

        // 경첩 위치 디버그 로그
        LogHingePosition();
    }

    private void CalculateArcPoints()
    {
        _arcPoints = new Vector3[SEGMENTS + 1];

        // 경첩 위치 (차의 로컬 좌표, _hingeOffset 반영)
        // _driverOnLeft는 그리기 방향에만 영향 (판정 로직은 항상 같음)
        Vector3 doorBasePos = _hingeOffset;
        Vector3 upOffset = Vector3.up * 0.05f;

        // 호 점들 계산 (차의 로컬 좌표계)
        for (int i = 0; i <= SEGMENTS; i++)
        {
            float angle = (i / (float)SEGMENTS) * Mathf.PI / 2f;
            float cosAngle = Mathf.Cos(angle);
            float sinAngle = Mathf.Sin(angle);

            // 문이 열리는 방향: 경첩 x가 음수(-0.95)면 양수 방향으로 열림
            // _driverOnLeft가 true면 원래 방향, false면 반대 방향
            float doorDir = (_hingeOffset.x < 0 ? 1f : -1f);
            if (!_driverOnLeft)
            {
                doorDir *= -1f;
            }

            // 호의 x, z 오프셋 (로컬 좌표)
            float arcX = _doorLength * cosAngle * doorDir;
            float arcZ = _requiredGap * sinAngle;

            _arcPoints[i] = doorBasePos + new Vector3(arcX, 0, arcZ) + upOffset;
        }
    }

    private void LogHingePosition()
    {
        Vector3 hingeWorldPos = transform.TransformPoint(_hingeOffset);
        Vector3 arcStartWorldPos = transform.TransformPoint(_arcPoints[0]);
        Debug.Log($"[DoorSwingDrawer] 경첩 위치(월드): {hingeWorldPos}, 호 시작점(월드): {arcStartWorldPos}");
    }

    private void UpdateLineColor()
    {
        bool canOpen = CanOpen();
        Color color = canOpen ? new Color(0, 1, 0, 1f) : new Color(1, 0, 0, 1f);

        if (canOpen != _lastCanOpenState)
        {
            _lastCanOpenState = canOpen;
            string obstacleInfo = GetObstacleInfo();
            Debug.Log($"[DoorSwingDrawer] CanOpen() = {canOpen}, 겹침 대상: {obstacleInfo}");
        }

        if (_lineMaterial != null)
        {
            _lineMaterial.color = color;
        }

        if (_lineRenderer != null)
        {
            _lineRenderer.startColor = color;
            _lineRenderer.endColor = color;
        }
    }

    private string GetObstacleInfo()
    {
        Vector3 carPos = transform.position;
        Vector3 carRight = transform.right;
        Vector3 carForward = transform.forward;

        // 판정 로직: _driverOnLeft 미적용 (항상 _hingeOffset 그대로 사용)
        Vector3 doorBasePos = carPos + carRight * _hingeOffset.x + carForward * _hingeOffset.z;

        // 문이 열리는 방향 (경첩 x 기준, _driverOnLeft 미적용)
        float doorDir = _hingeOffset.x < 0 ? 1f : -1f;
        Vector3 doorTipPos = doorBasePos + carRight * (_doorLength * doorDir);

        Vector3 boxCenter = (doorBasePos + doorTipPos) / 2f;
        Vector3 boxHalfExtents = new Vector3(_doorLength / 2f, 0.1f, _requiredGap);
        Quaternion boxRotation = Quaternion.LookRotation(transform.forward);

        LayerMask obstacleLayer = LayerMask.GetMask("Obstacle", "Vehicle");
        Collider[] colliders = Physics.OverlapBox(boxCenter, boxHalfExtents, boxRotation, obstacleLayer);

        return colliders.Length == 0 ? "없음" : string.Join(", ", System.Array.ConvertAll(colliders, c => c.gameObject.name));
    }

    private void Update()
    {
        if (_lineRenderer != null && _arcPoints != null)
        {
            _lineRenderer.SetPositions(_arcPoints);
            UpdateLineColor();
        }
    }

    private void OnValidate()
    {
        // 에디터에서 값 변경 시 즉시 반영
        if (_arcPoints != null && _arcPoints.Length > 0)
        {
            CalculateArcPoints();
            if (_lineRenderer != null)
            {
                _lineRenderer.SetPositions(_arcPoints);
            }
        }
    }

    public bool CanOpen()
    {
        Vector3 carPos = transform.position;
        Vector3 carRight = transform.right;
        Vector3 carForward = transform.forward;

        // 판정 로직: _driverOnLeft 미적용 (항상 _hingeOffset 그대로 사용)
        Vector3 doorBasePos = carPos + carRight * _hingeOffset.x + carForward * _hingeOffset.z;

        // 문이 열리는 방향 (경첩 x 기준)
        float doorDir = _hingeOffset.x < 0 ? 1f : -1f;
        Vector3 doorTipPos = doorBasePos + carRight * (_doorLength * doorDir);

        // BoxCast: 문 열림 영역 검사
        Vector3 boxCenter = (doorBasePos + doorTipPos) / 2f;
        Vector3 boxHalfExtents = new Vector3(_doorLength / 2f, 0.1f, _requiredGap);
        Quaternion boxRotation = Quaternion.LookRotation(transform.forward);

        LayerMask obstacleLayer = LayerMask.GetMask("Obstacle", "Vehicle");
        Collider[] colliders = Physics.OverlapBox(boxCenter, boxHalfExtents, boxRotation, obstacleLayer);

        return colliders.Length == 0;
    }
}

