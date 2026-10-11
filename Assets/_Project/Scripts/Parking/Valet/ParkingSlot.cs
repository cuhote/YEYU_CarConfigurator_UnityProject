using UnityEngine;

public class ParkingSlot : MonoBehaviour
{
    [SerializeField]
    private int _id;
    [SerializeField]
    private bool _initiallyOccupied; //처음부터 차가 있는 칸인지
    [SerializeField]
    private Transform _entryPoint; //진입준비지점 (후진 시작 위치,방향)
    [SerializeField]
    private Transform[] _reverseRoute; //후진 웨이포인트(D.3)

    public bool IsOccupied { get; set; }
    public int Id => _id;
    public Transform EntryPoint => _entryPoint;
    public Transform[] ReverseRoute => _reverseRoute;

    // D3 후진 웨이포인트 점
    [Header("후진 경로 (로컬: 원점=도착 뒤차축, +Z=통로)")]
    [SerializeField] private float _radius = 4.5f;        // 호 반경 (R_min 4.12 이상)
    [SerializeField] private float _firstStraight = 1.5f; // 호 앞 직선 L1
    [SerializeField] private float _laneZ = 5.05f;        // 통로 레인의 로컬 z (7.05 - 2.0)
    [SerializeField] private float _spacing = 0.5f;       // 점 간격
    [SerializeField] private int _entrySign = 1;          // 남쪽 1 / 북쪽(180도 회전) -1

    private const float WheelBase = 2.883f;
    private const float MaxSteerDeg = 35f;

    public void ResetSlot() //Allocator에서 호출
    {
        IsOccupied = _initiallyOccupied;
    }
    private void Awake()
    {
        ResetSlot();
    }

    // 진입 준비 지점 → 도착점 순서의 월드 좌표 점들
    public Vector3[] GetReverseRoute()
    {
        float rMin = WheelBase / Mathf.Tan(MaxSteerDeg * Mathf.Deg2Rad);
        if (_radius < rMin)
            Debug.LogWarning($"[ParkingSlot] {name}: 반경 {_radius}m가 최소 회전 반경 {rMin:F2}m보다 작음", this);

        float zc = _laneZ - _radius;   // 호 중심 z = 호 끝 z = 마지막 직선 길이
        if (zc < 0f)
        {
            Debug.LogError($"[ParkingSlot] {name}: laneZ({_laneZ})가 radius({_radius})보다 작음", this);
            return new Vector3[0];
        }

        var local = new System.Collections.Generic.List<Vector2>(); // (u, z), u는 부호 적용 전 x

        // 1) 앞 직선: (R+L1, laneZ) → (R, laneZ), 끝점 제외
        int n1 = Mathf.Max(1, Mathf.CeilToInt(_firstStraight / _spacing));
        for (int i = 0; i < n1; i++)
            local.Add(new Vector2(_radius + _firstStraight * (1f - (float)i / n1), _laneZ));

        // 2) 호: 각도 90° → 180°, 끝점 제외
        int n2 = Mathf.Max(1, Mathf.CeilToInt(_radius * Mathf.PI * 0.5f / _spacing));
        for (int i = 0; i < n2; i++)
        {
            float a = (90f + 90f * i / n2) * Mathf.Deg2Rad;
            local.Add(new Vector2(_radius + _radius * Mathf.Cos(a), zc + _radius * Mathf.Sin(a)));
        }

        // 3) 마지막 직선: (0, zc) → (0, 0), 양 끝 포함
        int n3 = Mathf.Max(1, Mathf.CeilToInt(zc / _spacing));
        for (int i = 0; i <= n3; i++)
            local.Add(new Vector2(0f, zc * (1f - (float)i / n3)));

        var world = new Vector3[local.Count];
        for (int i = 0; i < local.Count; i++)
            world[i] = transform.TransformPoint(new Vector3(local[i].x * _entrySign, 0f, local[i].y));
        return world;
    }

    // EntryPoint 자동으로 계산
    public Pose GetEntryPose()
    {
        Vector3[] route = GetReverseRoute();
        if (route.Length < 2){
            Debug.LogError($"[ParkingSlot] {name}: 후진 경로 점이 2개 미만이라 진입 자세를 만들 수 없음 ",this);
            return default;
        }
        Vector3 dir = route[0] - route[1]; 
        dir.y = 0f;
        if(dir.sqrMagnitude < 0.0001f)
        {
            Debug.LogError($"[ParkingSlot] {name}: 첫 두점이 같은 위치라 방향을 구할 수 없음 ",this);
            return default;
        }
        return new Pose(route[0], Quaternion.LookRotation(dir));
    }

    // 슬롯을 선택하면 씬 뷰에 경로를 그림
    private void OnDrawGizmosSelected()
    {
        Vector3[] route = GetReverseRoute();
        Gizmos.color = Color.cyan;
        for (int i = 0; i < route.Length; i++)
        {
            Gizmos.DrawSphere(route[i], 0.1f);
            if (i > 0) Gizmos.DrawLine(route[i - 1], route[i]);
        }
    }
}
