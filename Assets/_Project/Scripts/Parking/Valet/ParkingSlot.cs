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
    [SerializeField] private float _swingAngle = 0f;      // 후진 전에 칸 반대쪽으로 꺾는 각도 (도). 0이면 꺾지 않음

    private const float WheelBase = 2.883f;
    private const float MaxSteerDeg = 35f;

    // 차체 크기 (뒤차축 기준, Car 프리팹의 콜라이더와 같음)
    private const float BodyHalfWidth = 0.95f;
    private const float BodyFront = 3.75f;   // 뒤차축에서 차 코까지
    private const float BodyRear = 0.95f;    // 뒤차축에서 꽁무니까지

    public void ResetSlot() //Allocator에서 호출
    {
        IsOccupied = _initiallyOccupied;
    }
    private void Awake()
    {
        ResetSlot();
    }

    // 후진 시작 지점 → 도착점 순서의 월드 좌표 점들
    // 꺾기 각도가 0이면 통로에서 바로 90도 호로, 0보다 크면 비스듬히 선 자세에서 남은 각도만 돈다
    public Vector3[] GetReverseRoute()
    {
        float rMin = WheelBase / Mathf.Tan(MaxSteerDeg * Mathf.Deg2Rad);
        if (_radius < rMin)
            Debug.LogWarning($"[ParkingSlot] {name}: 반경 {_radius}m가 최소 회전 반경 {rMin:F2}m보다 작음", this);

        float swing = SwingRad;
        float zc = ArcEndZ;   // 호 중심 z = 호 끝 z = 마지막 직선 길이
        if (zc < 0f)
        {
            Debug.LogError($"[ParkingSlot] {name}: laneZ({_laneZ})가 radius({_radius})·swingAngle({_swingAngle})에 비해 작음", this);
            return new Vector3[0];
        }

        var local = new System.Collections.Generic.List<Vector2>(); // (u, z), u는 부호 적용 전 x

        // 1) 앞 직선: (R+L1, laneZ) → (R, laneZ), 끝점 제외. 꺾기를 쓰면 없음
        if (swing <= 0f)
        {
            int n1 = Mathf.Max(1, Mathf.CeilToInt(_firstStraight / _spacing));
            for (int i = 0; i < n1; i++)
                local.Add(new Vector2(_radius + _firstStraight * (1f - (float)i / n1), _laneZ));
        }

        // 2) 호: 차 방향이 꺾기 각도 → 90°(칸 방향)가 될 때까지, 끝점 제외
        float quarter = Mathf.PI * 0.5f;
        int n2 = Mathf.Max(1, Mathf.CeilToInt(_radius * (quarter - swing) / _spacing));
        for (int i = 0; i < n2; i++)
        {
            float a = swing + (quarter - swing) * i / n2;
            local.Add(new Vector2(_radius - _radius * Mathf.Sin(a), zc + _radius * Mathf.Cos(a)));
        }

        // 3) 마지막 직선: (0, zc) → (0, 0), 양 끝 포함
        int n3 = Mathf.Max(1, Mathf.CeilToInt(zc / _spacing));
        for (int i = 0; i <= n3; i++)
            local.Add(new Vector2(0f, zc * (1f - (float)i / n3)));

        return ToWorld(local);
    }

    // 꺾기 구간: 통로 위 진입 준비 지점 → 후진 시작 지점 (전진). 꺾기 각도가 0이면 빈 배열
    public Vector3[] GetSwingRoute()
    {
        float swing = SwingRad;
        if (swing <= 0f)
            return new Vector3[0];

        var local = new System.Collections.Generic.List<Vector2>();
        float startU = _radius * (1f - 2f * Mathf.Sin(swing));

        int n = Mathf.Max(1, Mathf.CeilToInt(_radius * swing / _spacing));
        for (int i = 0; i <= n; i++)
        {
            float a = swing * i / n;
            local.Add(new Vector2(startU + _radius * Mathf.Sin(a), _laneZ + _radius * (1f - Mathf.Cos(a))));
        }

        return ToWorld(local);
    }

    // 진입 준비 지점: 통로 위에서 통로 방향을 보는 자세
    public Pose GetEntryPose()
    {
        float swing = SwingRad;
        float u = (swing > 0f) ? _radius * (1f - 2f * Mathf.Sin(swing)) : _radius + _firstStraight;
        return LocalPose(u, _laneZ, 0f);
    }

    // 후진 시작 지점: 꺾기를 마치고 칸 반대쪽으로 비스듬히 선 자세. 꺾기 각도가 0이면 진입 준비 지점과 같음
    public Pose GetReverseStartPose()
    {
        float swing = SwingRad;
        if (swing <= 0f)
            return GetEntryPose();

        return LocalPose(_radius * (1f - Mathf.Sin(swing)), _laneZ + _radius * (1f - Mathf.Cos(swing)), swing);
    }

    // 꺾기 구간을 정확히 도는 앞바퀴 조향각 크기 (도)
    public float SwingSteerDeg => Mathf.Atan(WheelBase / _radius) * Mathf.Rad2Deg;

    private float SwingRad => Mathf.Clamp(_swingAngle, 0f, 60f) * Mathf.Deg2Rad;

    // 후진 호가 끝나는 z. 꺾기 각도가 0이면 laneZ - R
    private float ArcEndZ => _laneZ + _radius * (1f - 2f * Mathf.Cos(SwingRad));

    // (u, z)에서 통로 방향 기준 headingRad만큼 칸 반대쪽으로 돌아선 자세를 월드로
    private Pose LocalPose(float u, float z, float headingRad)
    {
        Vector3 pos = transform.TransformPoint(new Vector3(u * _entrySign, 0f, z));
        Vector3 dir = transform.TransformDirection(
            new Vector3(Mathf.Cos(headingRad) * _entrySign, 0f, Mathf.Sin(headingRad)));
        dir.y = 0f;
        return new Pose(pos, Quaternion.LookRotation(dir));
    }

    private Vector3[] ToWorld(System.Collections.Generic.List<Vector2> local)
    {
        var world = new Vector3[local.Count];
        for (int i = 0; i < local.Count; i++)
            world[i] = transform.TransformPoint(new Vector3(local[i].x * _entrySign, 0f, local[i].y));
        return world;
    }

    // 슬롯을 선택하면 씬 뷰에 경로를 그림 (자홍: 전진 꺾기, 청록: 후진, 노랑: 그 지점에서의 차체 윤곽)
    private void OnDrawGizmosSelected()
    {
        Vector3[] swing = GetSwingRoute();
        Vector3[] reverse = GetReverseRoute();

        DrawRoute(swing, Color.magenta);
        DrawRoute(reverse, Color.cyan);

        // 차체가 기둥·옆 차에 닿는지 눈으로 확인하는 용도
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.6f);
        DrawBodies(swing, false);
        DrawBodies(reverse, true);
    }

    private void DrawRoute(Vector3[] route, Color color)
    {
        Gizmos.color = color;
        for (int i = 0; i < route.Length; i++)
        {
            Gizmos.DrawSphere(route[i], 0.1f);
            if (i > 0) Gizmos.DrawLine(route[i - 1], route[i]);
        }
    }

    // 경로의 점마다(하나 건너 하나) 차체 윤곽 사각형을 그린다. reversing이면 차는 진행 반대쪽을 본다
    private void DrawBodies(Vector3[] route, bool reversing)
    {
        for (int i = 0; i < route.Length; i += 2)
        {
            int a = Mathf.Min(i, route.Length - 2);
            if (a < 0)
                return;

            Vector3 travel = route[a + 1] - route[a];
            travel.y = 0f;
            if (travel.sqrMagnitude < 0.0001f)
                continue;

            Vector3 forward = (reversing ? -travel : travel).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 origin = route[i] + Vector3.up * 0.05f;

            Vector3 rearLeft = origin - forward * BodyRear - right * BodyHalfWidth;
            Vector3 rearRight = origin - forward * BodyRear + right * BodyHalfWidth;
            Vector3 frontLeft = origin + forward * BodyFront - right * BodyHalfWidth;
            Vector3 frontRight = origin + forward * BodyFront + right * BodyHalfWidth;

            Gizmos.DrawLine(rearLeft, rearRight);
            Gizmos.DrawLine(rearRight, frontRight);
            Gizmos.DrawLine(frontRight, frontLeft);
            Gizmos.DrawLine(frontLeft, rearLeft);
        }
    }
}
