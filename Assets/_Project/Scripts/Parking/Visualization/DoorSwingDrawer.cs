using UnityEngine;

public class DoorSwingDrawer : MonoBehaviour
{
    [SerializeField] private float _doorLength = 1.1f;
    [SerializeField] private float _requiredGap = 0.6f;
    [SerializeField] private Vector3 _hingeOffset = new Vector3(-0.95f, 0f, 1.3f);

    private Material _greenMaterial;
    private Material _redMaterial;
    private int _visualizationLayer;

    private void Start()
    {
        _visualizationLayer = LayerMask.NameToLayer("Visualization");
        if (_visualizationLayer == -1)
        {
            Debug.LogError("[DoorSwingDrawer] 'Visualization' 레이어가 없습니다");
        }

        _greenMaterial = new Material(Shader.Find("Standard"));
        _greenMaterial.color = new Color(0, 1, 0, 0.3f);

        _redMaterial = new Material(Shader.Find("Standard"));
        _redMaterial.color = new Color(1, 0, 0, 0.3f);
    }

    public bool CanOpen()
    {
        Vector3 carPos = transform.position;
        Vector3 carRight = transform.right;
        Vector3 carForward = transform.forward;

        // 문 경첩 위치 (차 원점 기준 로컬 좌표)
        Vector3 doorBasePos = carPos + carRight * _hingeOffset.x + carForward * _hingeOffset.z;

        // 문 열림 영역 앞쪽 끝
        Vector3 doorTipPos = doorBasePos - carRight * _doorLength;

        // BoxCast: 문 열림 영역 검사
        Vector3 boxCenter = (doorBasePos + doorTipPos) / 2f;
        Vector3 boxHalfExtents = new Vector3(_doorLength / 2f, 0.1f, _requiredGap);
        Quaternion boxRotation = Quaternion.LookRotation(transform.forward);

        LayerMask obstacleLayer = LayerMask.GetMask("Obstacle", "Vehicle");
        Collider[] colliders = Physics.OverlapBox(boxCenter, boxHalfExtents, boxRotation, obstacleLayer);

        return colliders.Length == 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (_visualizationLayer == -1)
            _visualizationLayer = LayerMask.NameToLayer("Visualization");

        Vector3 carPos = transform.position;
        Vector3 carRight = transform.right;
        Vector3 carForward = transform.forward;

        // 문 경첩 위치 (차 원점 기준 로컬 좌표)
        Vector3 doorBasePos = carPos + carRight * _hingeOffset.x + carForward * _hingeOffset.z;
        Vector3 doorTipPos = doorBasePos - carRight * _doorLength;

        // 시각화 색상
        Color color = CanOpen() ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
        Gizmos.color = color;

        // 문 열림 영역 호 그리기
        Vector3 upOffset = Vector3.up * 0.05f;
        int segments = 16;
        Vector3 prevPos = doorBasePos + upOffset;

        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI / 2f;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            Vector3 offset = carRight * (-_doorLength * x) + carForward * (_requiredGap * z);
            Vector3 currPos = doorBasePos + offset + upOffset;

            Gizmos.DrawLine(prevPos, currPos);
            prevPos = currPos;
        }

        // 끝점 연결
        Gizmos.DrawLine(prevPos, doorTipPos + carForward * _requiredGap + upOffset);
    }
}
