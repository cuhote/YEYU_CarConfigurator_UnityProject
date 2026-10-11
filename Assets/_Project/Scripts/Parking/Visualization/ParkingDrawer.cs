using UnityEngine;
using System.Collections.Generic;

public class ParkingDrawer : MonoBehaviour
{
    [SerializeField] private GridMap _gridMap;
    [SerializeField] private ReservationTable[] _reservationTables;
    [SerializeField] private Color[] _carColors = new Color[]
    {
        new Color(1, 0, 0, 0.3f),  // 빨강 (차량 0)
        new Color(0, 0, 1, 0.3f),  // 파랑 (차량 1)
        new Color(0, 1, 0, 0.3f)   // 초록 (차량 2)
    };

    private int _visualizationLayer;
    private GameObject _rootObject;
    private Dictionary<int, LineRenderer> _cellRenderers = new Dictionary<int, LineRenderer>();
    private Dictionary<int, HashSet<Vector2Int>> _lastReservedCells = new Dictionary<int, HashSet<Vector2Int>>();

    private void Awake()
    {
        // Visualization 레이어 확인
        _visualizationLayer = LayerMask.NameToLayer("Visualization");
        if (_visualizationLayer == -1)
        {
            Debug.LogError("[ParkingDrawer] Visualization 레이어를 찾을 수 없습니다");
            return;
        }

        // 루트 오브젝트 생성
        _rootObject = new GameObject("ParkingDrawer_Root");
        _rootObject.layer = _visualizationLayer;
        _rootObject.transform.SetParent(transform);

        // 초기 상태 저장
        for (int i = 0; i < _carColors.Length; i++)
        {
            _lastReservedCells[i] = new HashSet<Vector2Int>();
        }
    }

    private void Update()
    {
        if (_gridMap == null || _reservationTables == null || _reservationTables.Length == 0)
            return;

        // 각 ReservationTable(Lot)마다 처리
        for (int tableIdx = 0; tableIdx < _reservationTables.Length; tableIdx++)
        {
            ReservationTable table = _reservationTables[tableIdx];
            if (table == null)
                continue;

            // IsOn == false면 그리지 않음
            if (!table.IsOn)
            {
                HideCarReservations(tableIdx);
                continue;
            }

            // 각 차량별로 예약된 셀 업데이트
            for (int carId = 0; carId < _carColors.Length; carId++)
            {
                List<Vector2Int> currentCells = table.GetReservedCells(carId, Time.time);
                HashSet<Vector2Int> currentSet = new HashSet<Vector2Int>(currentCells);

                int drawerId = tableIdx * 10 + carId;

                // 변경 감지: 이전과 다르면 업데이트
                if (!SetsEqual(_lastReservedCells.ContainsKey(drawerId) ? _lastReservedCells[drawerId] : new HashSet<Vector2Int>(), currentSet))
                {
                    _lastReservedCells[drawerId] = currentSet;

                    if (currentSet.Count > 0)
                    {
                        DrawCarReservation(drawerId, currentCells, carId);
                    }
                    else
                    {
                        HideCarReservation(drawerId);
                    }
                }
            }
        }
    }

    private void DrawCarReservation(int drawerId, List<Vector2Int> cells, int carId)
    {
        // 기존 LineRenderer 재사용 또는 새로 생성
        if (!_cellRenderers.ContainsKey(drawerId))
        {
            GameObject obj = new GameObject($"Car{carId}_Cells_{drawerId}");
            obj.layer = _visualizationLayer;
            obj.transform.SetParent(_rootObject.transform);

            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.1f;

            Material mat = new Material(Shader.Find("Unlit/Color"));
            lr.material = mat;

            _cellRenderers[drawerId] = lr;
        }

        LineRenderer lineRenderer = _cellRenderers[drawerId];
        Color color = carId < _carColors.Length ? _carColors[carId] : Color.white;

        // 각 셀을 사각형으로 그리기 (4개 꼭짓점 + 닫기)
        List<Vector3> allPoints = new List<Vector3>();

        foreach (Vector2Int cell in cells)
        {
            Vector3 cellCenter = _gridMap.CellToWorld(cell);
            Vector3 upOffset = Vector3.up * 0.05f;
            float halfSize = _gridMap.CellSize / 2f;

            // 셀의 4개 꼭짓점 (사각형)
            Vector3[] corners = new Vector3[]
            {
                cellCenter + new Vector3(-halfSize, 0, -halfSize) + upOffset,
                cellCenter + new Vector3(halfSize, 0, -halfSize) + upOffset,
                cellCenter + new Vector3(halfSize, 0, halfSize) + upOffset,
                cellCenter + new Vector3(-halfSize, 0, halfSize) + upOffset,
                cellCenter + new Vector3(-halfSize, 0, -halfSize) + upOffset  // 닫기
            };

            allPoints.AddRange(corners);
        }

        if (allPoints.Count > 0)
        {
            lineRenderer.positionCount = allPoints.Count;
            lineRenderer.SetPositions(allPoints.ToArray());
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.gameObject.SetActive(true);
        }
    }

    private void HideCarReservation(int drawerId)
    {
        if (_cellRenderers.ContainsKey(drawerId))
        {
            _cellRenderers[drawerId].gameObject.SetActive(false);
        }
    }

    private void HideCarReservations(int tableIdx)
    {
        for (int carId = 0; carId < _carColors.Length; carId++)
        {
            int drawerId = tableIdx * 10 + carId;
            HideCarReservation(drawerId);
            _lastReservedCells[drawerId] = new HashSet<Vector2Int>();
        }
    }

    public void Clear()
    {
        foreach (var renderer in _cellRenderers.Values)
        {
            if (renderer != null && renderer.gameObject != null)
            {
                Destroy(renderer.gameObject);
            }
        }

        _cellRenderers.Clear();
        _lastReservedCells.Clear();

        for (int i = 0; i < _carColors.Length; i++)
        {
            _lastReservedCells[i] = new HashSet<Vector2Int>();
        }

        Debug.Log("[ParkingDrawer] Clear: 모든 예약 시각화 초기화");
    }

    private bool SetsEqual(HashSet<Vector2Int> set1, HashSet<Vector2Int> set2)
    {
        return set1.SetEquals(set2);
    }
}
