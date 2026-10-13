using UnityEngine;
using TMPro;

public class DrivingDrawer : MonoBehaviour
{
    [SerializeField] private MeshFilter _headlampConeA;
    [SerializeField] private MeshFilter _headlampConeB;
    [SerializeField] private MeshFilter _thermalConeB;
    [SerializeField] private float _headlampRange = 50f;
    [SerializeField] private float _thermalRange = 120f;
    [SerializeField] private float _headlampHalfAngle = 15f;
    [SerializeField] private float _thermalHalfAngle = 10f;
    [SerializeField] private float _drawScale = 0.25f; // 그릴 때만 쓰는 축소 비율

    [Header("유턴 표시")]
    [SerializeField] private CarController _carA;
    [SerializeField] private CarController _carB;
    [SerializeField] private LineRenderer _trailA;
    [SerializeField] private LineRenderer _trailB;
    [SerializeField] private Transform _icrA;
    [SerializeField] private Transform _icrB;
    [SerializeField] private TMP_Text _radiusTextA;
    [SerializeField] private TMP_Text _radiusTextB;
    [SerializeField] private float _trailStep = 0.3f;   // 이만큼 움직일 때마다 궤적에 점을 찍는다 (m)
    [SerializeField] private float _drawHeight = 0.05f; // 바닥에서 띄우는 높이 (m)
    [SerializeField] private LineRenderer _circleA; // 최소 반경으로 그린 회전 원
    [SerializeField] private LineRenderer _circleB;
    [SerializeField] private int _circleSegments = 48;

    private bool _showTurn;
    private float _minRadiusA;
    private float _minRadiusB;


    private void Awake()
    {
        Mesh headlamp = BuildFan(_headlampRange * _drawScale, _headlampHalfAngle);
        if (_headlampConeA != null) _headlampConeA.mesh = headlamp;
        if (_headlampConeB != null) _headlampConeB.mesh = headlamp;

        if(_thermalConeB == null) Debug.LogError("[DrivingDrawer] Awake: _thermalConeB가 비어 있음 (Inspector 연결 확인)");
        else _thermalConeB.mesh = BuildFan(_thermalRange *_drawScale , _thermalHalfAngle);
    }

    private Mesh BuildFan(float range, float halfAngleDeg, int segments = 16)
    {
        Vector3[] verts = new Vector3[segments + 2];
        int[] tris = new int[segments * 3];
        verts[0] = Vector3.zero;                       // 부채꼴 꼭짓점 (센서 위치)
        for (int i = 0; i <= segments; i++)
        {
        float a = Mathf.Lerp(-halfAngleDeg, halfAngleDeg, i / (float)segments) * Mathf.Deg2Rad;
        verts[i + 1] = new Vector3(Mathf.Sin(a) * range, 0f, Mathf.Cos(a) * range);
        }
        for (int i = 0; i < segments; i++)
        {
        tris[i * 3] = 0; tris[i * 3 + 1] = i + 1; tris[i * 3 + 2] = i + 2;
        }
        Mesh mesh = new Mesh { vertices = verts, triangles = tris };
        mesh.RecalculateBounds();
        return mesh;
    }
    
    public void ShowCones(bool show)
    {
        if (_headlampConeA != null) _headlampConeA.gameObject.SetActive(show);
        if (_headlampConeB != null) _headlampConeB.gameObject.SetActive(show);
    }
    public void ShowThermalCone(bool show)
    {
       if (_thermalConeB != null) _thermalConeB.gameObject.SetActive(show);
    }

    public void ShowTurn(bool show)
    {
        _showTurn = show;
        _minRadiusA = float.PositiveInfinity;
        _minRadiusB = float.PositiveInfinity;
        ClearTurn(_trailA, _icrA, _radiusTextA, show);
        ClearTurn(_trailB, _icrB, _radiusTextB, show);
        if (_circleA != null) _circleA.positionCount = 0;
        if (_circleB != null) _circleB.positionCount = 0;
    }
    private void ClearTurn(LineRenderer trail, Transform icr, TMP_Text text, bool show)
    {
        if (trail != null)
        {
            trail.positionCount = 0;
            trail.gameObject.SetActive(show);
        }
        if (icr != null) icr.gameObject.SetActive(false);
        if (text != null)
        {
            text.text = "최소 회전 반경 -";
            text.gameObject.SetActive(show);
        }
    }
    // 주행 영역이 꺼질 때 Canvas에 있는 글자가 남지 않게 한다
    private void OnDisable()
    {
        _showTurn = false;
        if (_radiusTextA != null) _radiusTextA.gameObject.SetActive(false);
        if (_radiusTextB != null) _radiusTextB.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!_showTurn) return;
        _minRadiusA = DrawTurn(_carA, _trailA, _icrA, _radiusTextA, _circleA, _minRadiusA);
        _minRadiusB = DrawTurn(_carB, _trailB, _icrB, _radiusTextB, _circleB, _minRadiusB);
    }

    // 차 한 대의 궤적에 점을 더하고, 회전 중심을 옮기고, 가장 작았던 반경을 돌려준다
    private float DrawTurn(CarController car, LineRenderer trail, Transform icr, TMP_Text text, LineRenderer circle, float minRadius)
    {
        if (car == null) return minRadius;
        Vector3 up = Vector3.up * _drawHeight;
        Vector3 pos = car.transform.position + up;

        if (trail != null)
        {
            int count = trail.positionCount;
            if (count == 0 || Vector3.Distance(trail.GetPosition(count - 1), pos) >= _trailStep)
            {
                trail.positionCount = count + 1;
                trail.SetPosition(count, pos);
            }
        }

        float radius = TurningRadiusCalculator.Radius(car.WheelBase, car.SteerAngle, car.RearSteerAngle);
        bool turning = !float.IsInfinity(radius);
        if (icr != null)
        {
            icr.gameObject.SetActive(turning);
            if (turning)
                icr.position = TurningRadiusCalculator.Icr(car.transform,
    car.WheelBase, car.SteerAngle, car.RearSteerAngle) + up;
        }

        if (radius < minRadius)
        {
            minRadius = radius;
            if (text != null) text.text = $"최소 회전 반경 {minRadius:F2} m";
            DrawCircle(circle, TurningRadiusCalculator.Icr(car.transform, car.WheelBase, car.SteerAngle, car.RearSteerAngle) + up, minRadius);
        }
        return minRadius;
    }

    // 회전 중심을 가운데로 하는 반경 radius의 원을 그린다
    private void DrawCircle(LineRenderer circle, Vector3 center, float radius)
    {
        if (circle == null) return;
        circle.positionCount = _circleSegments;
        for (int i = 0; i < _circleSegments; i++)
        {
            float a = i / (float)_circleSegments * Mathf.PI * 2f;
            circle.SetPosition(i, center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
        }
    }
    
}
