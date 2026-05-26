using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ServiceBell : MonoBehaviour
{
    [Header("Link to Station")]
    public PlateStation linkedPlateStation;

    private void OnMouseDown()
    {
        if (linkedPlateStation != null)
        {
            linkedPlateStation.ServePlate();
        }
        else
        {
            Debug.LogWarning("[Service Bell] No Plate Station linked!");
        }
    }
}