using UnityEngine;

public class ItemRotator : MonoBehaviour
{
    public float rotationSpeed = 15f;
    [Tooltip("How fast the item stops spinning after you let go (higher = stops faster)")]
    public float friction = 5f;

    private Vector2 spinVelocity;
    private bool isDragging;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            // Capture the exact mouse movement frame-by-frame
            float deltaX = Input.GetAxis("Mouse X") * rotationSpeed;
            float deltaY = Input.GetAxis("Mouse Y") * rotationSpeed;

            // Store it as our current speed
            spinVelocity = new Vector2(deltaX, deltaY);
        }
        else
        {
            // If we aren't dragging, slowly reduce the speed to 0 using friction
            spinVelocity = Vector2.Lerp(spinVelocity, Vector2.zero, Time.deltaTime * friction);
        }

        // Apply the rotation as long as it's still moving
        if (spinVelocity.sqrMagnitude > 0.0001f)
        {
            transform.Rotate(Vector3.up, -spinVelocity.x, Space.World);
            transform.Rotate(Vector3.right, spinVelocity.y, Space.World);
        }
    }
}