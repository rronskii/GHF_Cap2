using UnityEngine;

[CreateAssetMenu(fileName = "New Upgrade", menuName = "Food Truck/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Upgrade Identity")]
    public string upgradeName;
    [Tooltip("Must be totally unique (e.g., 'frying_pan_2', 'lucky_cat_1')")]
    public string uniqueUpgradeID;
    public string description;

    [Header("Shop Settings")]
    public int purchasePrice = 250;
    public int unlockLevel = 1;
    public GameObject worldPrefab; // The 3D model that spins on the shop pedestal
}