using UnityEngine;

// 원격 이동 중 차 앞·뒤 _stopDistance 안에 무엇이 있는지 본다 (FR-33).
// 차의 BoxCollider를 기준으로 범퍼 바로 바깥의 납작한 상자 영역을 검사한다.
// 벽·기둥은 레이어와 상관없이 잡고, 바닥과 표시용 오브젝트만 뺀다.
public class ProximityStop : MonoBehaviour
{
    [SerializeField] private float _stopDistance = 0.3f;   // 이 거리 안에 들어오면 막힌 것으로 본다 (m)
    [SerializeField] private float _groundClearance = 0.2f; // 바닥에서 이 높이까지는 보지 않는다 (m). 바닥 면을 장애물로 잡지 않기 위함

    // BoxCollider가 없을 때 쓰는 차체 치수 (차 원점 = 뒤차축 기준)
    private Vector3 _bodyCenter = new Vector3(0f, 0.7f, 1.4f);
    private Vector3 _bodySize = new Vector3(1.9f, 1.4f, 4.7f);

    private static readonly Collider[] Hits = new Collider[16];

    private int _mask;

    private void Awake()
    {
        BoxCollider body = GetComponent<BoxCollider>();
        if (body != null)
        {
            _bodyCenter = body.center;
            _bodySize = body.size;
        }
        else
        {
            Debug.LogWarning("[ProximityStop] BoxCollider가 없어 기본 차체 치수를 씀", this);
        }

        _mask = ~LayerMask.GetMask("Ground", "Visualization", "UI", "Ignore Raycast", "TransparentFX", "Water", "NightVisionOnly");
    }

    public bool IsBlockedFront => IsBlockedInDirection(1);
    public bool IsBlockedRear => IsBlockedInDirection(-1);
    public bool IsBlocked => IsBlockedFront || IsBlockedRear;

    // dir > 0 이면 차 앞, 아니면 차 뒤를 본다
    public bool IsBlockedInDirection(int dir)
    {
        return FindObstacle(dir) != null;
    }

    // 막고 있는 콜라이더 하나를 돌려준다. 없으면 null
    public Collider FindObstacle(int dir)
    {
        float sign = dir > 0 ? 1f : -1f;

        // 범퍼 면에서 바깥으로 _stopDistance 두께의 상자 (차 로컬 좌표)
        float bumperZ = _bodyCenter.z + sign * _bodySize.z * 0.5f;
        float bottom = _bodyCenter.y - _bodySize.y * 0.5f + _groundClearance;
        float top = _bodyCenter.y + _bodySize.y * 0.5f;

        Vector3 localCenter = new Vector3(_bodyCenter.x, (bottom + top) * 0.5f, bumperZ + sign * _stopDistance * 0.5f);
        Vector3 halfExtents = new Vector3(_bodySize.x * 0.5f, (top - bottom) * 0.5f, _stopDistance * 0.5f);

        int count = Physics.OverlapBoxNonAlloc(transform.TransformPoint(localCenter), halfExtents, Hits,
                                               transform.rotation, _mask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider hit = Hits[i];

            // 자기 차의 콜라이더는 뺀다
            if (hit.transform.IsChildOf(transform))
                continue;

            // 트리거는 다른 차의 차체일 때만 장애물로 본다 (구간 트리거 같은 것은 지나간다)
            if (hit.isTrigger && hit.GetComponentInParent<CarController>() == null)
                continue;

            return hit;
        }

        return null;
    }
}
