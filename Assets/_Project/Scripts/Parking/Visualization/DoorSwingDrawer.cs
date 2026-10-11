using UnityEngine;

public class DoorSwingDrawer : MonoBehaviour
{
    [SerializeField] private float _doorLength = 1.1f;
    [SerializeField] private float _requiredGap = 0.6f;
    [SerializeField] private Vector3 _hingeOffset = new Vector3(-0.95f, 0f, 1.3f);

    private int _visualizationLayer;
    private bool _lastCanOpenState = true;
    private string[] _lastObstacleNames = new string[0];

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

        // 상태 변화를 감지해 로그 출력
        bool canOpen = colliders.Length == 0;
        if (canOpen != _lastCanOpenState)
        {
            _lastCanOpenState = canOpen;

            string obstacleInfo = canOpen ? "없음" : string.Join(", ", System.Array.ConvertAll(colliders, c => c.gameObject.name));
            Debug.Log($"[DoorSwingDrawer] CanOpen() = {canOpen}, 겹침 대상: {obstacleInfo}");
        }

        return canOpen;
    }

    private void OnDrawGizmos()
    {
        Vector3 carPos = transform.position;
        Vector3 carRight = transform.right;
        Vector3 carForward = transform.forward;

        // 문 경첩 위치 (차 원점 기준 로컬 좌표)
        Vector3 doorBasePos = carPos + carRight * _hingeOffset.x + carForward * _hingeOffset.z;
        Vector3 doorTipPos = doorBasePos - carRight * _doorLength;

        // 시각화 색상 (상태에 따라 초록/빨강)
        bool canOpen = CanOpen();
        Color color = canOpen ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
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
