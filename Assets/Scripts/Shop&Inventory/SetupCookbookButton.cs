using UnityEngine;
using TMPro;

public class SetupCookbookButton : MonoBehaviour
{
    public TextMeshProUGUI itemNameText;
    private IngredientData myIngredient;
    private InventorySetupManager manager;

    public void Setup(IngredientData ingredient, InventorySetupManager setupManager)
    {
        myIngredient = ingredient;
        manager = setupManager;

        if (itemNameText != null)
        {
            itemNameText.text = ingredient.displayName;
        }
    }

    public void OnButtonClicked()
    {
        if (manager != null)
        {
            manager.SelectItemForPlacement(myIngredient);
        }
    }
}