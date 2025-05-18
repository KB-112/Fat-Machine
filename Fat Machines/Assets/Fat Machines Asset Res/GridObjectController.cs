using System.Collections.Generic;
using UnityEngine;

public class GridObjectController : MonoBehaviour
{
    [Header("References")]
    public GridDrawer gridDrawer;
    public List<GameObject> draggableObjects;
    [Header("Drag Settings")]
    private GameObject selectedObject;
    private Vector3 offset;
    [SerializeField] private Camera cam;

    // Track occupied grid positions for all pivot points
    private HashSet<Vector2Int> occupiedGridPositions = new HashSet<Vector2Int>();
    // Store each object's pivot grid positions
    private Dictionary<GameObject, List<Vector2Int>> objectPivotGridPositions = new Dictionary<GameObject, List<Vector2Int>>();

    void Start()
    {
        if (gridDrawer == null)
        {
            Debug.LogError("GridDrawer reference is missing!");
        }
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("Camera reference is missing!");
            }
        }

        // Initialize positions for all draggable objects
        InitializeObjectPositions();
    }

    void Update()
    {
        HandleDragging();
    }

    void InitializeObjectPositions()
    {
        foreach (GameObject obj in draggableObjects)
        {
            EnsureObjectWithinGrid(obj);
            SnapToGrid(obj);
        }
    }

    void HandleDragging()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse button down");
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log("Raycast hit: " + hit.collider.gameObject.name);

                GameObject draggableParent = FindDraggableParent(hit.collider.gameObject);

                if (draggableParent != null)
                {
                    Debug.Log("Object selected: " + draggableParent.name);
                    selectedObject = draggableParent;
                    offset = selectedObject.transform.position - hit.point;

                    // Remove current pivot positions from occupied list
                    if (objectPivotGridPositions.ContainsKey(selectedObject))
                    {
                        foreach (Vector2Int pos in objectPivotGridPositions[selectedObject])
                        {
                            occupiedGridPositions.Remove(pos);
                        }
                    }

                    EnsureObjectWithinGrid(selectedObject);
                }
                else
                {
                    Debug.Log("Hit object is not draggable.");
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (selectedObject != null)
            {
                Debug.Log("Object released: " + selectedObject.name);
                SnapToGrid(selectedObject);
                selectedObject = null;
            }
        }

        if (selectedObject != null && Input.GetMouseButton(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 newPos = hit.point + offset;
                newPos.y = selectedObject.transform.position.y;

                Debug.Log($"Moving {selectedObject.name} to {newPos}");

                Vector3 clampedPos = ClampToGridBounds(selectedObject, newPos);
                selectedObject.transform.position = clampedPos;
            }
        }
    }

    void SnapToGrid(GameObject obj)
    {
        Vector3 origin = gridDrawer.gridOrigin != null ? gridDrawer.gridOrigin.position : gridDrawer.transform.position;
        float cellSize = gridDrawer.cellSize;

        // Collect all child pivots including root
        List<Transform> pivots = new List<Transform>();
        pivots.Add(obj.transform);
        foreach (Transform child in obj.transform)
            pivots.Add(child);

        // Store new grid positions for this object
        List<Vector2Int> newGridPositions = new List<Vector2Int>();
        Vector3 totalDelta = Vector3.zero;

        foreach (Transform pivot in pivots)
        {
            Vector3 pivotWorldPos = pivot.position;
            int gridX = Mathf.FloorToInt((pivotWorldPos.x - origin.x) / cellSize);
            int gridZ = Mathf.FloorToInt((pivotWorldPos.z - origin.z) / cellSize);

            // Find nearest unoccupied grid position for this pivot
            Vector2Int targetGridPos = new Vector2Int(gridX, gridZ);
            int searchRadius = 2;
            float minDistance = float.MaxValue;
            Vector3 snappedPos = Vector3.zero;

            for (int x = gridX - searchRadius; x <= gridX + searchRadius; x++)
            {
                for (int z = gridZ - searchRadius; z <= gridZ + searchRadius; z++)
                {
                    Vector2Int testGridPos = new Vector2Int(x, z);
                    if (!occupiedGridPositions.Contains(testGridPos) && !newGridPositions.Contains(testGridPos))
                    {
                        float snappedX = x * cellSize + origin.x + cellSize / 2;
                        float snappedZ = z * cellSize + origin.z + cellSize / 2;
                        Vector3 testPos = new Vector3(snappedX, pivotWorldPos.y, snappedZ);
                        float distance = Vector3.Distance(pivotWorldPos, testPos);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            targetGridPos = testGridPos;
                            snappedPos = testPos;
                        }
                    }
                }
            }

            // Calculate delta for this pivot
            Vector3 delta = snappedPos - pivotWorldPos;
            totalDelta += delta;

            // Reserve this grid position
            newGridPositions.Add(targetGridPos);
        }

        // Apply average delta to the parent object
        Vector3 averageDelta = totalDelta / pivots.Count;
        obj.transform.position += averageDelta;

        // Update tracking
        foreach (Vector2Int pos in newGridPositions)
        {
            occupiedGridPositions.Add(pos);
        }

        if (objectPivotGridPositions.ContainsKey(obj))
        {
            objectPivotGridPositions[obj] = newGridPositions;
        }
        else
        {
            objectPivotGridPositions.Add(obj, newGridPositions);
        }

        Debug.Log($"Snapped {obj.name} with {pivots.Count} pivots. Average delta: {averageDelta}");
    }

    Vector3 ClampToGridBounds(GameObject obj, Vector3 targetPos)
    {
        Vector3 origin = gridDrawer.gridOrigin != null ? gridDrawer.gridOrigin.position : gridDrawer.transform.position;
        float width = gridDrawer.columns * gridDrawer.cellSize;
        float height = gridDrawer.rows * gridDrawer.cellSize;

        Bounds gridBounds = new Bounds(origin + new Vector3(width / 2, 0, height / 2), new Vector3(width, 0.1f, height));
        Bounds objectBounds = GetCombinedBounds(obj);

        float halfWidth = objectBounds.extents.x;
        float halfDepth = objectBounds.extents.z;

        targetPos.x = Mathf.Clamp(targetPos.x, gridBounds.min.x + halfWidth, gridBounds.max.x - halfWidth);
        targetPos.z = Mathf.Clamp(targetPos.z, gridBounds.min.z + halfDepth, gridBounds.max.z - halfDepth);

        return targetPos;
    }

    GameObject FindDraggableParent(GameObject child)
    {
        GameObject current = child;
        while (current != null)
        {
            if (draggableObjects.Contains(current))
                return current;
            if (current.transform.parent != null)
                current = current.transform.parent.gameObject;
            else
                current = null;
        }
        return null;
    }

    Bounds GetCombinedBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.zero);

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combined.Encapsulate(renderers[i].bounds);
        }
        return combined;
    }

    void EnsureObjectWithinGrid(GameObject obj)
    {
        Vector3 position = obj.transform.position;
        Vector3 origin = gridDrawer.gridOrigin != null ? gridDrawer.gridOrigin.position : gridDrawer.transform.position;
        float width = gridDrawer.columns * gridDrawer.cellSize;
        float height = gridDrawer.rows * gridDrawer.cellSize;

        Bounds gridBounds = new Bounds(origin + new Vector3(width / 2, 0, height / 2), new Vector3(width, 0.1f, height));

        position.x = Mathf.Clamp(position.x, gridBounds.min.x, gridBounds.max.x);
        position.z = Mathf.Clamp(position.z, gridBounds.min.z, gridBounds.max.z);
        obj.transform.position = position;

        Debug.Log($"Adjusted {obj.name} to stay inside grid: {obj.transform.position}");
    }
}