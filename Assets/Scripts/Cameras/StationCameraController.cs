using System;
using System.Collections;
using UnityEngine;

public class StationCameraController : MonoBehaviour
{
    public static StationCameraController Instance;
    public static event Action<int> OnStationChanged;

    [Header("Station Views (Transforms)")]
    [Tooltip("0 = Cooking, 1 = Inventory, 2 = Order")]
    public Transform[] stationViews;

    [Header("Transition Settings")]
    public float transitionSpeed = 5f;
    [Tooltip("How quickly the player can switch stations again (in seconds)")]
    public float transitionCooldown = 0.5f;

    [Header("Tutorial Locks")]
    public bool isLocked = false;
    public bool allowLooping = true;
    public int minStationIndex = 0;
    public int maxStationIndex = 2; // Default 2 allows all 3 stations

    private int currentStationIndex = 0;
    private float nextTransitionTime = 0f;
    private Coroutine activeTransition;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (stationViews != null && stationViews.Length > 0)
        {
            transform.SetPositionAndRotation(stationViews[0].position, stationViews[0].rotation);
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (stationViews == null || stationViews.Length == 0) return;
        if (Time.time < nextTransitionTime) return;
        if (isLocked) return; // Complete input lock for tutorials

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
        nextTransitionTime = Time.time + transitionCooldown;

        int nextStation = currentStationIndex + direction;

        if (allowLooping)
        {
            if (nextStation < 0) nextStation = stationViews.Length - 1;
            else if (nextStation >= stationViews.Length) nextStation = 0;
        }
        else
        {
            // Clamp the movement so they hit a "wall" based on tutorial limits
            nextStation = Mathf.Clamp(nextStation, minStationIndex, maxStationIndex);

            // If they are at the edge and try to go further, do nothing
            if (nextStation == currentStationIndex) return;
        }

        currentStationIndex = nextStation;

        if (HandManager.Instance != null) HandManager.Instance.UpdateStationState(currentStationIndex);
        if (OnStationChanged != null) OnStationChanged(currentStationIndex);

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(TransitionCameraRoutine(stationViews[currentStationIndex]));
    }

    // Allows managers to instantly override the player's camera position
    public void ForceGoToStation(int index)
    {
        currentStationIndex = Mathf.Clamp(index, 0, stationViews.Length - 1);

        if (HandManager.Instance != null) HandManager.Instance.UpdateStationState(currentStationIndex);
        if (OnStationChanged != null) OnStationChanged(currentStationIndex);

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(TransitionCameraRoutine(stationViews[currentStationIndex]));
    }

    private IEnumerator TransitionCameraRoutine(Transform targetView)
    {
        while (Vector3.Distance(transform.position, targetView.position) > 0.01f ||
               Quaternion.Angle(transform.rotation, targetView.rotation) > 0.1f)
        {
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, targetView.position, transitionSpeed * Time.deltaTime),
                Quaternion.Lerp(transform.rotation, targetView.rotation, transitionSpeed * Time.deltaTime)
            );
            yield return null;
        }

        transform.SetPositionAndRotation(targetView.position, targetView.rotation);
        activeTransition = null;
    }
}