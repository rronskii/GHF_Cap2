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
    [Tooltip("How quickly the player can switch stations again (in seconds)")]
    public float transitionCooldown = 0.5f;

    private int currentStationIndex = 0;

    // Tracks when the player is allowed to press the button again
    private float nextTransitionTime = 0f;

    // Tracks the active movement so we don't have two coroutines fighting
    private Coroutine activeTransition;

    private void Start()
    {
        // Snap camera to the starting station immediately
        if (stationViews != null)
        {
            if (stationViews.Length > 0)
            {
                transform.SetPositionAndRotation(stationViews[0].position, stationViews[0].rotation);
            }
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        if (stationViews == null) return;
        if (stationViews.Length == 0) return;

        // Use a strict time cooldown instead of waiting for the slide to physically finish
        if (Time.time < nextTransitionTime) return;

        if (DialogueManager.Instance != null)
        {
            if (DialogueManager.Instance.IsDialogueActive) return;
        }

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
        // Lock the inputs for the duration of the cooldown
        nextTransitionTime = Time.time + transitionCooldown;

        currentStationIndex += direction;

        if (currentStationIndex < 0)
        {
            currentStationIndex = stationViews.Length - 1;
        }
        else if (currentStationIndex >= stationViews.Length)
        {
            currentStationIndex = 0;
        }

        if (HandManager.Instance != null)
        {
            HandManager.Instance.UpdateStationState(currentStationIndex);
        }

        // FIXED: Replaced '?.' operator to adhere to strict architecture rules
        if (OnStationChanged != null)
        {
            OnStationChanged(currentStationIndex);
        }

        // Stop the old movement coroutine if the player triggered a new one 
        // before the camera perfectly settled
        if (activeTransition != null)
        {
            StopCoroutine(activeTransition);
        }

        activeTransition = StartCoroutine(TransitionCameraRoutine(stationViews[currentStationIndex]));
    }

    private IEnumerator TransitionCameraRoutine(Transform targetView)
    {
        // Smoothly interpolate position and rotation
        while (Vector3.Distance(transform.position, targetView.position) > 0.01f ||
               Quaternion.Angle(transform.rotation, targetView.rotation) > 0.1f)
        {
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, targetView.position, transitionSpeed * Time.deltaTime),
                Quaternion.Lerp(transform.rotation, targetView.rotation, transitionSpeed * Time.deltaTime)
            );
            yield return null;
        }

        // Snap to perfect precision at the end
        transform.SetPositionAndRotation(targetView.position, targetView.rotation);

        activeTransition = null;
    }
}