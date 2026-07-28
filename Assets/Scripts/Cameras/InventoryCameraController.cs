using System.Collections;
using UnityEngine;

public class InventoryCameraController : MonoBehaviour
{
    public static InventoryCameraController Instance;

    [Header("Room Sections")]
    [Tooltip("0 = Main Room/Tablet, 1 = Bulletin Board")]
    public Transform[] sectionViews;

    [Header("Transition Settings")]
    public float transitionSpeed = 5f;
    public float transitionCooldown = 0.5f;

    private int currentSectionIndex = 0;
    private float nextTransitionTime = 0f;
    private Coroutine activeTransition;

    public bool isCameraLocked = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (sectionViews != null && sectionViews.Length > 0)
        {
            Camera.main.transform.SetPositionAndRotation(sectionViews[0].position, sectionViews[0].rotation);
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f || isCameraLocked || sectionViews == null || sectionViews.Length == 0) return;
        if (Time.time < nextTransitionTime) return;

        if (Input.GetKeyDown(KeyCode.A)) ChangeSection(-1);
        else if (Input.GetKeyDown(KeyCode.D)) ChangeSection(1);
    }

    private void ChangeSection(int direction)
    {
        nextTransitionTime = Time.time + transitionCooldown;
        currentSectionIndex += direction;

        if (currentSectionIndex < 0) currentSectionIndex = sectionViews.Length - 1;
        else if (currentSectionIndex >= sectionViews.Length) currentSectionIndex = 0;

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(TransitionRoutine(sectionViews[currentSectionIndex]));
    }

    private IEnumerator TransitionRoutine(Transform targetView)
    {
        Camera mainCam = Camera.main;
        while (Vector3.Distance(mainCam.transform.position, targetView.position) > 0.01f ||
               Quaternion.Angle(mainCam.transform.rotation, targetView.rotation) > 0.1f)
        {
            mainCam.transform.SetPositionAndRotation(
                Vector3.Lerp(mainCam.transform.position, targetView.position, transitionSpeed * Time.deltaTime),
                Quaternion.Lerp(mainCam.transform.rotation, targetView.rotation, transitionSpeed * Time.deltaTime)
            );
            yield return null;
        }
        mainCam.transform.SetPositionAndRotation(targetView.position, targetView.rotation);
        activeTransition = null;
    }

    public int GetCurrentSectionIndex() { return currentSectionIndex; }

    // For when the tablet UI closes and forces the camera back to the main view
    public void ReturnHome()
    {
        if (currentSectionIndex != 0) ChangeSection(-currentSectionIndex);
    }
}