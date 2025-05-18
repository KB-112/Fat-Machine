using UnityEngine;

public class FreezeOnCollision : MonoBehaviour
{
    public float collisionThreshold = 1f; // Adjustable distance in Inspector

    private bool isDragging = false;
    private Vector3 originalPosition;

    private void Start()
    {
        originalPosition = transform.position; // Store the starting position
    }

    private void OnMouseDown()
    {
        isDragging = true;
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    private void Update()
    {
        GameObject[] allCubes = GameObject.FindGameObjectsWithTag("Cube"); // Find all cubes

        foreach (GameObject cube in allCubes)
        {
            if (cube != gameObject) // Avoid checking itself
            {
                float distance = Vector3.Distance(transform.position, cube.transform.position);

                if (distance < collisionThreshold) // Use inspector-defined threshold
                {
                    HandleCollision();
                    break;
                }
            }
        }
    }

    private void HandleCollision()
    {
        if (isDragging)
        {
            // Push away slightly
            Vector3 pushDirection = (transform.position - originalPosition).normalized;
            transform.position += pushDirection * 0.5f;
        }
        else
        {
            // Return to original position when colliding
            transform.position = originalPosition;
        }
    }
}
