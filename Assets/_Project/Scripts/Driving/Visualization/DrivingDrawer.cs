using UnityEngine;

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
    
}
