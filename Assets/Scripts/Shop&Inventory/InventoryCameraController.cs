using System.Collections;
using UnityEngine;

public class InventoryCameraController : MonoBehaviour
{
    public static InventoryCameraController Instance;

    [Header("Camera Transforms")]
    [Tooltip("The default wide-shot view of the inventory room")]
    public Transform homeView;

    [Header("Settings")]
    public float transitionSpeed = 3f;

    private Coroutine transitionCoroutine;
    public bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // Snap to the home view when the scene starts
        if (homeView != null)
        {
            Camera.main.transform.SetPositionAndRotation(homeView.position, homeView.rotation);
        }
    }

    public void MoveToTarget(Transform target)
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(MoveRoutine(target));
    }

    public void ReturnHome()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(MoveRoutine(homeView));
    }

    private IEnumerator MoveRoutine(Transform target)
    {
        isTransitioning = true;
        Camera mainCam = Camera.main;

        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * transitionSpeed;
            mainCam.transform.position = Vector3.Lerp(startPos, target.position, Mathf.SmoothStep(0, 1, t));
            mainCam.transform.rotation = Quaternion.Lerp(startRot, target.rotation, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        isTransitioning = false;
    }
}