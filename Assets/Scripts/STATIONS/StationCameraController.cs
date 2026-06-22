using System.Collections;
using UnityEngine;
using System;

public class StationCameraController : MonoBehaviour
{
    public static event Action<int> OnStationChanged;

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

        // NEW: If the dialogue manager exists AND is currently showing text, lock the camera!
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            ChangeStation(1);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            ChangeStation(-1);
        }
    }

    private void ChangeStation(int direction)
    {
        currentStationIndex += direction;
        if (currentStationIndex < 0) currentStationIndex = stationViews.Length - 1;
        else if (currentStationIndex >= stationViews.Length) currentStationIndex = 0;

        if (HandManager.Instance != null) HandManager.Instance.UpdateStationState(currentStationIndex);

        // NEW: Broadcast the change!
        OnStationChanged?.Invoke(currentStationIndex);

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