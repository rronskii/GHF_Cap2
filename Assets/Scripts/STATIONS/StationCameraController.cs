using System.Collections;
using UnityEngine;

public class StationCameraController : MonoBehaviour
{
    [Header("Station Views (Transforms)")]
    [Tooltip("0 = Cooking, 1 = Inventory, 2 = Order")]
    public Transform[] stationViews;

    [Header("Transition Settings")]
    public float transitionSpeed = 5f;

    [Header("Mouse Edge Drag Settings")]
    public float edgeThresholdPixels = 50f; // How close to the edge (in pixels) triggers the transition
    public bool requireMouseDrag = true;    // If true, player must be holding left-click to edge-pan

    private int currentStationIndex = 0;
    private bool isTransitioning = false;
    private bool canTriggerEdgePan = true; // Prevents rapid-fire transitions

    private void Start()
    {
        // Snap camera to the starting station immediately
        if (stationViews != null && stationViews.Length > 0)
        {
            transform.position = stationViews[0].position;
            transform.rotation = stationViews[0].rotation;
        }
    }

    private void Update()
    {
        if (isTransitioning || stationViews.Length == 0) return;

        HandleKeyboardInput();
        HandleMouseEdgeInput();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangeStation(1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangeStation(-1);
        }
    }

    private void HandleMouseEdgeInput()
    {
        // Check if the mouse is at the extreme left or right of the screen
        bool isAtLeftEdge = Input.mousePosition.x <= edgeThresholdPixels;
        bool isAtRightEdge = Input.mousePosition.x >= Screen.width - edgeThresholdPixels;

        // Are we holding down the mouse button? (For carrying items across screens)
        bool isDragging = requireMouseDrag;

        // Reset the trigger lock ONLY if the mouse leaves the edge areas
        if (!isAtLeftEdge && !isAtRightEdge)
        {
            canTriggerEdgePan = true;
        }

        // Trigger transition if conditions are met
        if (canTriggerEdgePan && isDragging)
        {
            if (isAtLeftEdge)
            {
                canTriggerEdgePan = false; // Lock until mouse leaves edge
                ChangeStation(1);
            }
            else if (isAtRightEdge)
            {
                canTriggerEdgePan = false; // Lock until mouse leaves edge
                ChangeStation(-1);
            }
        }
    }

    private void ChangeStation(int direction)
    {
        currentStationIndex += direction;

        if (currentStationIndex < 0) currentStationIndex = stationViews.Length - 1;
        else if (currentStationIndex >= stationViews.Length) currentStationIndex = 0;

        // NEW: Tell the HandManager we moved!
        if (HandManager.Instance != null) HandManager.Instance.UpdateStationState(currentStationIndex);

        StartCoroutine(TransitionCameraRoutine(stationViews[currentStationIndex]));
    }

    private IEnumerator TransitionCameraRoutine(Transform targetView)
    {
        isTransitioning = true;

        // Smoothly interpolate position and rotation
        while (Vector3.Distance(transform.position, targetView.position) > 0.01f ||
               Quaternion.Angle(transform.rotation, targetView.rotation) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, targetView.position, transitionSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetView.rotation, transitionSpeed * Time.deltaTime);
            yield return null;
        }

        // Snap to perfect precision at the end
        transform.position = targetView.position;
        transform.rotation = targetView.rotation;

        isTransitioning = false;
    }
}