using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class ServiceBell : MonoBehaviour
{
    public static event Action OnTutorialBellRung; // NEW: The broadcast event

    [Header("Link to Station")]
    public PlateStation linkedPlateStation;

    private void OnMouseDown()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        if (linkedPlateStation != null)
        {
            linkedPlateStation.ServePlate();
            OnTutorialBellRung?.Invoke();
        }
        else
        {
            Debug.LogWarning("[Service Bell] No Plate Station linked!");
        }
    }
}