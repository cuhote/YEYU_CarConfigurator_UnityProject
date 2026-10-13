using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using TMPro;

// 예약 테이블의 셀을 바닥에 차량 색 타일로 칠하고, 그 위에 "예약" 표지를 띄운다.
// 주차장마다 하나씩 둔다 (GridMap이 주차장마다 다르므로).
public class ParkingDrawer : MonoBehaviour
{
    [SerializeField] private GridMap _gridMap;
    [SerializeField] private ReservationTable[] _reservationTables;
    // 차량 번호별 색. 투명도는 아래 상수를 쓴다
    [SerializeField] private Color[] _carColors = new Color[]
    {
        new Color(1f, 0.25f, 0.2f),   // 빨강 (차량 0)
        new Color(0.2f, 0.5f, 1f),    // 파랑 (차량 1)
        new Color(0.2f, 0.9f, 0.35f)  // 초록 (차량 2)
    };
    [SerializeField] private bool _showLabel = true;        // "예약" 표지를 띄울지
    [SerializeField] private float _labelHeight = 0.9f;     // 바닥에서 표지까지 높이 (m)
    [SerializeField] private float _labelFontSize = 5f;     // 글자 크기 (10이 약 1m)

    private const float _floorOffset = 0.03f;   // 바닥과 겹쳐 깜빡이지 않게 띄우는 높이 (m)
    private const float _cellGap = 0.04f;       // 타일 사이 틈 (m). 셀 하나하나가 구분되어 보인다
    private const float _borderWidth = 0.07f;   // 타일 테두리 두께 (m)
    private const float _fillAlpha = 0.4f;
    private const float _borderAlpha = 0.95f;
    private const float _pulsePeriod = 1.6f;    // 타일 밝기가 한 번 오르내리는 시간 (초)
    private static readonly Vector2 _labelPadding = new Vector2(0.25f, 0.12f);

    // 차량 한 대의 예약 표시 (타일 메시 + 표지)
    private class CarVisual
    {
        public GameObject Root;
        public Mesh Mesh;
        public Material Material;
        public Transform Label;
        public Material LabelMaterial;
        public HashSet<Vector2Int> Cells = new HashSet<Vector2Int>();
    }

    private int _visualizationLayer;
    private GameObject _rootObject;
    private Dictionary<int, CarVisual> _visuals = new Dictionary<int, CarVisual>();
    private static Mesh _quadMesh;

    private void Awake()
    {
        _visualizationLayer = LayerMask.NameToLayer("Visualization");
        if (_visualizationLayer == -1)
        {
            Debug.LogError("[ParkingDrawer] Visualization 레이어를 찾을 수 없습니다");
            return;
        }

        // 타일 꼭짓점을 월드 좌표로 넣으므로 루트는 원점에 둔다
        _rootObject = new GameObject("ParkingDrawer_Root");
        _rootObject.layer = _visualizationLayer;
        _rootObject.transform.SetParent(transform, true);
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
        foreach (CarVisual visual in _visuals.Values)
        {
            Destroy(visual.Mesh);
            Destroy(visual.Material);
            Destroy(visual.LabelMaterial);
        }
    }

    private void Update()
    {
        if (_rootObject == null || _gridMap == null || _reservationTables == null)
            return;

        for (int tableIdx = 0; tableIdx < _reservationTables.Length; tableIdx++)
        {
            ReservationTable table = _reservationTables[tableIdx];
            if (table == null)
                continue;

            for (int carId = 0; carId < _carColors.Length; carId++)
            {
                int drawerId = tableIdx * 10 + carId;

                // 예약이 꺼진 주차장은 그리지 않는다
                if (!table.IsOn)
                {
                    Hide(drawerId);
                    continue;
                }

                List<Vector2Int> cells = table.GetReservedCells(carId, Time.time);
                if (cells.Count == 0)
                {
                    Hide(drawerId);
                    continue;
                }

                CarVisual visual = GetVisual(drawerId, carId);
                if (!visual.Root.activeSelf || !SameCells(visual.Cells, cells))
                {
                    visual.Cells.Clear();
                    visual.Cells.UnionWith(cells);
                    Rebuild(visual, cells, _carColors[carId]);
                    visual.Root.SetActive(true);
                }
            }
        }

        // 타일 밝기를 천천히 오르내려 "잡혀 있는 자리"임을 눈에 띄게 한다
        float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / _pulsePeriod);
        Color tint = new Color(1f, 1f, 1f, Mathf.Lerp(0.6f, 1f, wave));
        foreach (CarVisual visual in _visuals.Values)
        {
            if (visual.Root.activeSelf)
                visual.Material.color = tint;
        }
    }

    private CarVisual GetVisual(int drawerId, int carId)
    {
        if (_visuals.TryGetValue(drawerId, out CarVisual visual))
            return visual;

        visual = new CarVisual();
        visual.Root = new GameObject($"Car{carId}_Reserved_{drawerId}");
        visual.Root.layer = _visualizationLayer;
        visual.Root.transform.SetParent(_rootObject.transform, false);

        // 타일: 꼭짓점 색으로 칠하는 반투명 메시
        visual.Mesh = new Mesh { name = "ReservedCells" };
        visual.Mesh.MarkDynamic();
        visual.Material = new Material(Shader.Find("Sprites/Default"));
        visual.Root.AddComponent<MeshFilter>().sharedMesh = visual.Mesh;
        MeshRenderer renderer = visual.Root.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = visual.Material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        if (_showLabel)
            BuildLabel(visual, _carColors[carId]);

        visual.Root.SetActive(false);
        _visuals[drawerId] = visual;
        return visual;
    }

    // 차량 색 바탕에 "예약"이라고 쓴 표지. 카메라를 향해 돈다
    private void BuildLabel(CarVisual visual, Color carColor)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.layer = _visualizationLayer;
        visual.Label = labelObj.transform;
        visual.Label.SetParent(visual.Root.transform, false);

        GameObject textObj = new GameObject("Text");
        textObj.layer = _visualizationLayer;
        textObj.transform.SetParent(visual.Label, false);
        TextMeshPro text = textObj.AddComponent<TextMeshPro>();
        TMP_FontAsset font = VehicleStatusLabel.GetKoreanFont();
        if (font != null)
            text.font = font;
        text.text = "예약";
        text.fontSize = _labelFontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(4f, 1f);
        text.GetComponent<MeshRenderer>().sortingOrder = 1;

        GameObject bgObj = new GameObject("Background");
        bgObj.layer = _visualizationLayer;
        bgObj.transform.SetParent(visual.Label, false);
        bgObj.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        Vector2 size = text.GetPreferredValues("예약");
        bgObj.transform.localScale = new Vector3(size.x + _labelPadding.x * 2f, size.y + _labelPadding.y * 2f, 1f);
        bgObj.AddComponent<MeshFilter>().sharedMesh = GetQuadMesh();
        MeshRenderer bgRenderer = bgObj.AddComponent<MeshRenderer>();
        visual.LabelMaterial = new Material(Shader.Find("Sprites/Default"));
        // 흰 글자가 읽히게 차량 색을 조금 어둡게 쓴다
        visual.LabelMaterial.color = new Color(carColor.r * 0.75f, carColor.g * 0.75f, carColor.b * 0.75f, 0.9f);
        bgRenderer.sharedMaterial = visual.LabelMaterial;
        bgRenderer.shadowCastingMode = ShadowCastingMode.Off;
        bgRenderer.receiveShadows = false;
        bgRenderer.sortingOrder = 0;
    }

    // 셀마다 타일 하나: 안쪽은 옅게, 테두리는 진하게
    private void Rebuild(CarVisual visual, List<Vector2Int> cells, Color carColor)
    {
        Color fillColor = new Color(carColor.r, carColor.g, carColor.b, _fillAlpha);
        Color borderColor = new Color(carColor.r, carColor.g, carColor.b, _borderAlpha);

        float outer = _gridMap.CellSize * 0.5f - _cellGap * 0.5f;
        float inner = outer - _borderWidth;

        List<Vector3> vertices = new List<Vector3>(cells.Count * 12);
        List<Color> colors = new List<Color>(cells.Count * 12);
        List<int> triangles = new List<int>(cells.Count * 30);
        Vector3 centroid = Vector3.zero;

        foreach (Vector2Int cell in cells)
        {
            Vector3 center = _gridMap.CellToWorld(cell) + Vector3.up * _floorOffset;
            centroid += center;
            int start = vertices.Count;

            // 0~3 테두리 바깥, 4~7 테두리 안쪽, 8~11 안쪽 면 (색이 달라 꼭짓점을 따로 둔다)
            AddSquare(vertices, colors, center, outer, borderColor);
            AddSquare(vertices, colors, center, inner, borderColor);
            AddSquare(vertices, colors, center, inner, fillColor);

            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                triangles.Add(start + i); triangles.Add(start + 4 + i); triangles.Add(start + next);
                triangles.Add(start + next); triangles.Add(start + 4 + i); triangles.Add(start + 4 + next);
            }
            triangles.Add(start + 8); triangles.Add(start + 10); triangles.Add(start + 9);
            triangles.Add(start + 8); triangles.Add(start + 11); triangles.Add(start + 10);
        }

        visual.Mesh.Clear();
        visual.Mesh.SetVertices(vertices);
        visual.Mesh.SetColors(colors);
        visual.Mesh.SetTriangles(triangles, 0);
        visual.Mesh.RecalculateBounds();

        // 표지는 예약 셀들의 가운데에 가장 가까운 셀 위에 둔다 (ㄱ자 경로에서도 셀 위에 오게)
        if (visual.Label != null)
        {
            centroid /= cells.Count;
            Vector3 best = centroid;
            float bestDist = float.MaxValue;
            foreach (Vector2Int cell in cells)
            {
                Vector3 center = _gridMap.CellToWorld(cell);
                float dist = (center - centroid).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = center;
                }
            }
            visual.Label.position = best + Vector3.up * _labelHeight;
        }
    }

    private static void AddSquare(List<Vector3> vertices, List<Color> colors, Vector3 center, float half, Color color)
    {
        vertices.Add(center + new Vector3(-half, 0f, -half));
        vertices.Add(center + new Vector3(half, 0f, -half));
        vertices.Add(center + new Vector3(half, 0f, half));
        vertices.Add(center + new Vector3(-half, 0f, half));
        for (int i = 0; i < 4; i++)
            colors.Add(color);
    }

    private void Hide(int drawerId)
    {
        if (_visuals.TryGetValue(drawerId, out CarVisual visual) && visual.Root.activeSelf)
        {
            visual.Root.SetActive(false);
            visual.Cells.Clear();
        }
    }

    // 화면을 그리는 카메라마다 표지가 그 카메라를 정면으로 보게 돌린다
    private void FaceCamera(ScriptableRenderContext context, Camera cam)
    {
        foreach (CarVisual visual in _visuals.Values)
        {
            if (visual.Label != null && visual.Root.activeSelf)
                visual.Label.rotation = cam.transform.rotation;
        }
    }

    public void Clear()
    {
        foreach (CarVisual visual in _visuals.Values)
        {
            visual.Root.SetActive(false);
            visual.Cells.Clear();
        }

        Debug.Log("[ParkingDrawer] Clear: 모든 예약 시각화 초기화");
    }

    private static bool SameCells(HashSet<Vector2Int> shown, List<Vector2Int> cells)
    {
        if (shown.Count != cells.Count)
            return false;

        foreach (Vector2Int cell in cells)
        {
            if (!shown.Contains(cell))
                return false;
        }
        return true;
    }

    // 가운데가 원점인 1x1 사각형 (표지 배경)
    private static Mesh GetQuadMesh()
    {
        if (_quadMesh != null)
            return _quadMesh;

        _quadMesh = new Mesh { name = "ReservedLabelQuad" };
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
