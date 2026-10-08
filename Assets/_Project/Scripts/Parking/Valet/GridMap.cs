using System.ComponentModel;
using UnityEngine;

public class GridMap : MonoBehaviour
{
    [SerializeField]
    private float _cellSize = 2.5f;
    [SerializeField]
    private int _width = 12;
    [SerializeField]
    private int _height = 6;

    public Vector2Int WorldToCell(Vector3 pos)
    {
        int x = Mathf.FloorToInt((pos.x - transform.position.x) / _cellSize);
        int y = Mathf.FloorToInt((pos.z - transform.position.z) / _cellSize);
        return new Vector2Int(x, y);
    }
    public Vector3 CellToWorld(Vector2Int cell)
    {
        float x = transform.position.x + (cell.x + 0.5f) * _cellSize;
        float z = transform.position.z + (cell.y + 0.5f) * _cellSize;
        return new Vector3(x, transform.position.y, z);
    }

    public bool Contains(Vector2Int c)
    {
        return c.x >= 0 && c.x < _width && c.y >= 0 && c.y < _height;
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 o = transform.position + Vector3.up * 0.05f; //격자의 원점 (바닥과 안 겹칠려고 0.05m 띄움)
        float w = _width * _cellSize; //격자 전체 가로 30
        float h = _height * _cellSize; //격자 전체 세로 15

        for (int i = 0; i <= _width; i++) // 12칸 그리기 위해 세로선 13줄 그려야 함
        {
            Vector3 a = o + new Vector3(i * _cellSize, 0, 0); //i번째 선 시작점 a는 원점으로부터 x방향 ix2.5m간 곳
            Gizmos.DrawLine(a, a + new Vector3(0, 0, h)); //z방향은 거기서 +h만큼 선 그림 
        }
        for (int j = 0; j <= _height; j++) // 6칸 그리기 위해 가로선 7개
        {
            Vector3 a = o + new Vector3(0, 0, j * _cellSize); //j번째 선 시작점 - z방향) ix2.5m
            Gizmos.DrawLine(a, a + new Vector3(w, 0, 0)); //x방향 - +w만큼 더
        }
    }
}
