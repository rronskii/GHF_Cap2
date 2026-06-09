using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InventoryStation : MonoBehaviour
{
    [Header("Station Inventory")]
    [Tooltip("The pool of card prefabs specific to this storage container (e.g., Meats, Dairy, Grains)")]
    public GameObject[] stationCardPrefabs;

    private void OnMouseDown()
    {
        // Pass this specific station's localized card pool over to the HandManager
        if (HandManager.Instance != null)
        {
            HandManager.Instance.DrawCardFromPool(stationCardPrefabs);
        }
        else
        {
            Debug.LogWarning($"[Inventory Station] HandManager Instance not found when clicking {gameObject.name}!");
        }
    }
}