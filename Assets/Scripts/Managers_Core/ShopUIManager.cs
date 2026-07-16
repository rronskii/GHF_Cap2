using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ShopUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject buyingPanel;
    public GameObject overlayBackground;

    [Header("Buying UI Elements")]
    public Transform cardContainer;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI bankCashText;

    [Header("System References")]
    public ShopCameraController cameraController;

    private int currentAmount = 1;
    private IngredientData currentIngredient;
    private GameObject spawnedCardUI;

    private Color originalPriceColor;
    private Coroutine cantAffordCoroutine;
    private bool isTransactionLocked = false; // NEW: Locks buttons during animations

    private void Awake()
    {
        if (priceText != null)
        {
            originalPriceColor = priceText.color;
        }
    }

    private void OnEnable()
    {
        ShopItemInteractable.OnShopItemClicked += OpenBuyingPanel;
    }

    private void OnDisable()
    {
        ShopItemInteractable.OnShopItemClicked -= OpenBuyingPanel;
    }

    private void Start()
    {
        CloseBuyingPanel();
        UpdateBankCashDisplay();
    }

    private void OpenBuyingPanel(IngredientData ingredient)
    {
        currentIngredient = ingredient;
        currentAmount = 1;
        isTransactionLocked = false; // Ensure buttons are unlocked when opening

        ShopItemInteractable.isInteractionLocked = true;
        if (cameraController != null)
        {
            cameraController.isCameraLocked = true;
        }

        buyingPanel.SetActive(true);
        if (overlayBackground != null)
        {
            overlayBackground.SetActive(true);
        }

        if (spawnedCardUI != null)
        {
            Destroy(spawnedCardUI);
        }

        if (ingredient.cardUIPrefab != null)
        {
            spawnedCardUI = Instantiate(ingredient.cardUIPrefab, cardContainer);

            // --- UPDATED: AGGRESSIVE COMPONENT STRIPPING ---
            // We search the root and all children, disable them instantly, then destroy them.

            CardDragUI[] dragScripts = spawnedCardUI.GetComponentsInChildren<CardDragUI>(true);
            foreach (CardDragUI drag in dragScripts)
            {
                if (drag != null)
                {
                    drag.enabled = false;
                    Destroy(drag);
                }
            }

            CardGridPlacer[] placerScripts = spawnedCardUI.GetComponentsInChildren<CardGridPlacer>(true);
            foreach (CardGridPlacer placer in placerScripts)
            {
                if (placer != null)
                {
                    placer.enabled = false;
                    Destroy(placer);
                }
            }

            GridTileVisual[] tileScripts = spawnedCardUI.GetComponentsInChildren<GridTileVisual>(true);
            foreach (GridTileVisual tile in tileScripts)
            {
                if (tile != null)
                {
                    tile.enabled = false;
                    Destroy(tile);
                }
            }
        }

        if (cantAffordCoroutine != null)
        {
            StopCoroutine(cantAffordCoroutine);
            if (priceText != null)
            {
                priceText.color = originalPriceColor;
            }
        }

        UpdatePanelUI();
    }

    public void CloseBuyingPanel()
    {
        buyingPanel.SetActive(false);
        if (overlayBackground != null)
        {
            overlayBackground.SetActive(false);
        }

        if (spawnedCardUI != null)
        {
            Destroy(spawnedCardUI);
        }

        currentIngredient = null;
        isTransactionLocked = false;

        ShopItemInteractable.isInteractionLocked = false;
        if (cameraController != null)
        {
            cameraController.isCameraLocked = false;
        }
    }

    public void IncreaseAmount()
    {
        if (isTransactionLocked) return; // NEW: Lock check

        currentAmount++;
        UpdatePanelUI();
    }

    public void DecreaseAmount()
    {
        if (isTransactionLocked) return; // NEW: Lock check

        if (currentAmount > 1)
        {
            currentAmount--;
            UpdatePanelUI();
        }
    }

    private void UpdatePanelUI()
    {
        if (currentIngredient != null)
        {
            amountText.text = currentAmount.ToString();

            int totalCost = currentAmount * currentIngredient.purchasePrice;
            priceText.text = "Cost: " + totalCost.ToString() + " P";
        }
    }

    private void UpdateBankCashDisplay()
    {
        if (PlayerEconomyManager.Instance != null)
        {
            if (bankCashText != null)
            {
                bankCashText.text = "Bank: " + PlayerEconomyManager.Instance.totalBankCash.ToString() + " P";
            }
        }
    }

    public void ConfirmPurchase()
    {
        if (isTransactionLocked) return; // NEW: Lock check
        if (currentIngredient == null) return;
        if (PlayerEconomyManager.Instance == null) return;
        if (PlayerInventoryManager.Instance == null) return;

        int totalCost = currentAmount * currentIngredient.purchasePrice;

        if (PlayerEconomyManager.Instance.totalBankCash >= totalCost)
        {
            PlayerEconomyManager.Instance.totalBankCash -= totalCost;
            PlayerInventoryManager.Instance.AddStock(currentIngredient, currentAmount);

            UpdateBankCashDisplay();
            CloseBuyingPanel();
        }
        else
        {
            if (cantAffordCoroutine != null)
            {
                StopCoroutine(cantAffordCoroutine);
            }
            cantAffordCoroutine = StartCoroutine(CantAffordRoutine());
        }
    }

    private IEnumerator CantAffordRoutine()
    {
        isTransactionLocked = true; // Lock the buttons

        if (priceText != null)
        {
            priceText.text = "Can't Afford!";
            priceText.color = Color.red;
        }

        yield return new WaitForSeconds(1.2f);

        if (priceText != null)
        {
            priceText.color = originalPriceColor;
        }

        UpdatePanelUI();

        isTransactionLocked = false; // Unlock the buttons
        cantAffordCoroutine = null;
    }

    public void ProceedToInventoryPhase()
    {
        ShopItemInteractable.isInteractionLocked = false;
        SceneManager.LoadScene("03_Inventory");
    }
}