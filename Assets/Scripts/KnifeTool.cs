using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KnifeTool : MonoBehaviour
{
    [Header("Settings")]
    public float returnSpeed = 15f;
    public float dragHeight = 1f;
    public Vector3 holdRotation = new Vector3(90f, 0f, 0f);
    public Vector3 grabOffset = new Vector3(0f, 0f, 0f);
    public float chopHeight = 1f;
    public float chopRebound = 0.8f;

    [Header("Physics Settings")]
    public LayerMask foodLayerMask;

    private Camera mainCamera;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isDragging = false;
    private Collider knifeCollider;

    // NEW: Variables for QoL Dragging and Hovering
    private Draggable3DItem currentlyHoveredFood;
    private bool isChopping = false;

    private void Awake()
    {
        mainCamera = Camera.main;
        knifeCollider = GetComponent<Collider>();

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void OnMouseDown()
    {
        if (isChopping) return;

        StopAllCoroutines();
        isDragging = true;
        knifeCollider.enabled = false;

        // Snap the knife into the held/chopping rotation
        transform.rotation = Quaternion.Euler(holdRotation);
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, startPosition.y + dragHeight, 0));
        Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (dragPlane.Raycast(mouseRay, out float distance))
        {
            // 1. Get the exact world point on the invisible plane your mouse is hovering over
            Vector3 rawMousePosition = mouseRay.GetPoint(distance);

            // 2. Add your custom public grabOffset to shift the knife's pivot away from the exact center
            Vector3 targetPos = rawMousePosition + grabOffset;

            // 3. Smoothly move the knife to the new target
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 20f);
        }

        // --- HOVER GROWTH LOGIC ---
        // Shoot a ray straight down from the mouse cursor to see what we are holding the knife over
        Ray dropRay = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(dropRay, out RaycastHit hit, Mathf.Infinity, foodLayerMask))
        {
            Draggable3DItem foodItem = hit.collider.GetComponent<Draggable3DItem>();

            // Ensure it exists, is choppable, AND is sitting safely on a Chopping Board
            bool isValidChopTarget = foodItem != null &&
                                     foodItem.myData.isChoppable &&
                                     foodItem.currentStation != null &&
                                     foodItem.currentStation.stationType == StationType.ChoppingBoard;

            if (isValidChopTarget)
            {
                // If we moved the knife over a NEW valid food item
                if (currentlyHoveredFood != foodItem)
                {
                    ClearHoverState(); // Shrink the old one
                    currentlyHoveredFood = foodItem;
                    currentlyHoveredFood.SetHoverGrowth(true); // Grow the new one
                }
            }
            else
            {
                ClearHoverState(); // We hit food, but it can't be chopped
            }
        }
        else
        {
            ClearHoverState(); // We aren't hovering over any food
        }
    }

    private void OnMouseUp()
    {
        isDragging = false;
        bool successfullyChopped = false;

        Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, foodLayerMask))
        {
            Draggable3DItem foodItem = hit.collider.GetComponent<Draggable3DItem>();
            if (foodItem != null)
            {
                // Capture whether the food was actually valid and successfully chopped
                successfullyChopped = foodItem.TryChop();
            }
        }

        ClearHoverState(); // Ensure the food shrinks back to normal if we drop the knife

        if (successfullyChopped)
        {
            // NEW: Do the chop animation first, THEN glide back to the hook!
            StartCoroutine(ChopAnimationRoutine(() =>
            {
                knifeCollider.enabled = true;
                StartCoroutine(ReturnRoutine());
            }));
        }
        else
        {
            // If you missed the food or it wasn't choppable, just return normally
            knifeCollider.enabled = true;
            StartCoroutine(ReturnRoutine());
        }
    }

    // Helper method to keep things clean
    private void ClearHoverState()
    {
        if (currentlyHoveredFood != null)
        {
            currentlyHoveredFood.SetHoverGrowth(false);
            currentlyHoveredFood = null;
        }
    }

    private IEnumerator ReturnRoutine()
    {
        while (Vector3.Distance(transform.position, startPosition) > 0.01f || Quaternion.Angle(transform.rotation, startRotation) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, startPosition, Time.deltaTime * returnSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, startRotation, Time.deltaTime * returnSpeed);
            yield return null;
        }

        transform.position = startPosition;
        transform.rotation = startRotation;
    }

    // --- NEW: Link to Camera Events ---
    private void OnEnable()
    {
        StationCameraController.OnStationChanged += ForceDropKnife;
    }

    private void OnDisable()
    {
        StationCameraController.OnStationChanged -= ForceDropKnife;
    }

    private void ForceDropKnife(int newStationIndex)
    {
        if (isDragging)
        {
            isDragging = false;
            knifeCollider.enabled = true;
            ClearHoverState(); // Shrink any hovered food

            StopAllCoroutines(); // Stop any existing return glides
            StartCoroutine(ReturnRoutine()); // Glide safely back to hook
        }
    }

    // NEW: The 3-phase chopping animation
    private System.Collections.IEnumerator ChopAnimationRoutine(System.Action onComplete)
    {
        isChopping = true;

        Vector3 startPos = transform.position;
        // The strike depth (tweak the Y value if the knife clips too far through the board)
        Vector3 downPos = startPos + new Vector3(0, chopHeight, 0);
        // The slight rebound height
        Vector3 reboundPos = startPos + new Vector3(0, chopRebound, 0);

        // Phase 1: Accelerate Down (The Strike)
        float elapsed = 0f;
        float strikeDuration = 0.05f; // Extremely fast
        while (elapsed < strikeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / strikeDuration;
            // Squaring 't' makes it start slow and accelerate downward!
            transform.position = Vector3.Lerp(startPos, downPos, t * t);
            yield return null;
        }

        // Phase 2: Glide Back Up (The Rebound)
        elapsed = 0f;
        float reboundDuration = 0.1f;
        while (elapsed < reboundDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / reboundDuration;
            // Using Sqrt makes it pop up quickly, then ease/slow down into the peak
            transform.position = Vector3.Lerp(downPos, reboundPos, Mathf.Sqrt(t));
            yield return null;
        }

        // Phase 3: Return to the Original Drop Position
        elapsed = 0f;
        float returnDuration = 0.1f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(reboundPos, startPos, elapsed / returnDuration);
            yield return null;
        }

        transform.position = startPos;
        isChopping = false;

        // Trigger whatever needs to happen next (like returning to the hook)
        onComplete?.Invoke();
    }
}