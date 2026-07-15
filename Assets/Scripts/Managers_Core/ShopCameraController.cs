using System.Collections;
using UnityEngine;

public class ShopCameraController : MonoBehaviour
{
    [Header("Shop Sections")]
    [Tooltip("0 = Proteins, 1 = Aromatics/Veg, 2 = Bases")]
    public Transform[] sectionViews;

    [Header("Transition Settings")]
    public float transitionSpeed = 5f;
    public float transitionCooldown = 0.5f;

    private int currentSectionIndex = 0;
    private float nextTransitionTime = 0f;
    private Coroutine activeTransition;

    // We will use this flag later to lock the camera when the UI overlay is open
    public bool isCameraLocked = false;

    private void Start()
    {
        if (sectionViews != null)
        {
            if (sectionViews.Length > 0)
            {
                transform.SetPositionAndRotation(sectionViews[0].position, sectionViews[0].rotation);
            }
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (isCameraLocked) return;
        if (sectionViews == null) return;
        if (sectionViews.Length == 0) return;
        if (Time.time < nextTransitionTime) return;

        if (Input.GetKeyDown(KeyCode.A))
        {
            ChangeSection(1);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            ChangeSection(-1);
        }
    }

    private void ChangeSection(int direction)
    {
        nextTransitionTime = Time.time + transitionCooldown;
        currentSectionIndex += direction;

        if (currentSectionIndex < 0)
        {
            currentSectionIndex = sectionViews.Length - 1;
        }
        else if (currentSectionIndex >= sectionViews.Length)
        {
            currentSectionIndex = 0;
        }

        if (activeTransition != null)
        {
            StopCoroutine(activeTransition);
        }

        activeTransition = StartCoroutine(TransitionRoutine(sectionViews[currentSectionIndex]));
    }

    private IEnumerator TransitionRoutine(Transform targetView)
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