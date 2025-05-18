using UnityEngine;

[ExecuteAlways]
public class GridDrawer : MonoBehaviour
{
    [Header("Grid Settings")]
    public int rows = 6;
    public int columns = 6;
    public float cellSize = 1f;

    [Header("Grid Transform")]
    public Transform gridOrigin;  // Drag any Transform here to define grid position

    [Header("Visual Settings")]
    public Color gridColor = Color.white;
    public Color midpointColor = Color.red;
    public float midpointGizmoSize = 0.1f;

    private void OnDrawGizmos()
    {
        if (gridOrigin == null)
            gridOrigin = transform;

        Gizmos.color = gridColor;

        // Draw vertical lines
        for (int x = 0; x <= columns; x++)
        {
            Vector3 start = gridOrigin.position + new Vector3(x * cellSize, 0, 0);
            Vector3 end = gridOrigin.position + new Vector3(x * cellSize, 0, rows * cellSize);
            Gizmos.DrawLine(start, end);
        }

        // Draw horizontal lines
        for (int y = 0; y <= rows; y++)
        {
            Vector3 start = gridOrigin.position + new Vector3(0, 0, y * cellSize);
            Vector3 end = gridOrigin.position + new Vector3(columns * cellSize, 0, y * cellSize);
            Gizmos.DrawLine(start, end);
        }

        // Draw midpoints
        Gizmos.color = midpointColor;
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 localMid = new Vector3((x + 0.5f) * cellSize, 0, (y + 0.5f) * cellSize);
                Vector3 worldMid = gridOrigin.position + localMid;
                Gizmos.DrawSphere(worldMid, midpointGizmoSize);
            }
        }
    }

    public Vector3 GetLocalMidpoint(int col, int row)
    {
        return new Vector3((col + 0.5f) * cellSize, 0, (row + 0.5f) * cellSize);
    }

    public Vector3 GetWorldMidpoint(int col, int row)
    {
        return (gridOrigin != null ? gridOrigin.position : transform.position) + GetLocalMidpoint(col, row);
    }
}
