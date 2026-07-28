using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ShopUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject buyingPanel;

    [Header("Buying UI Elements")]
    public Transform cardContainer;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI bankCashText;

    [Header("Amount Controls")]
    public GameObject increaseButton;
    public GameObject decreaseButton;
    public GameObject amountTextObj;

    [Header("System References")]
    public ShopCameraController cameraController;
    public string nextSceneName = "03_Inventory";

    private int currentAmount = 1;
    private IngredientData currentIngredient;
    private UpgradeData currentUpgrade;
    private ShopItemInteractable currentInteractable;

    private GameObject spawnedCardUI;
    private GameObject spawned3DModel;

    private Color originalPriceColor;
    private Coroutine cantAffordCoroutine;
    private Coroutine cameraTransitionCoroutine;
    private bool isTransitioning = false;

    private bool isTransactionLocked = false;
    private bool isInfoCardVisible = false;
    private bool isCurrentlyInspecting = false;

    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;

    private void Awake()
    {
        if (priceText != null) originalPriceColor = priceText.color;
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
        buyingPanel.SetActive(false);
        UpdateBankCashDisplay();
    }

    private void OpenBuyingPanel(ShopItemInteractable interactable)
    {
        if (isCurrentlyInspecting || isTransitioning) return;

        isTransitioning = true;

        // Figure out what we just clicked!
        currentInteractable = interactable;
        currentIngredient = interactable.ingredientData;
        currentUpgrade = interactable.upgradeData;

        currentAmount = 1;
        isTransactionLocked = false;
        isInfoCardVisible = false;
        isCurrentlyInspecting = true;

        ShopItemInteractable.isInteractionLocked = true;
        if (cameraController != null) cameraController.isCameraLocked = true;

        // Hide + / - buttons if it's an upgrade
        bool isUpgrade = (currentUpgrade != null);
        if (increaseButton != null) increaseButton.SetActive(!isUpgrade);
        if (decreaseButton != null) decreaseButton.SetActive(!isUpgrade);
        if (amountTextObj != null) amountTextObj.SetActive(!isUpgrade);

        if (interactable.showcaseSpotlight != null) interactable.showcaseSpotlight.enabled = true;
        buyingPanel.SetActive(true);

        // --- SPAWN 3D MODEL ---
        if (spawned3DModel != null) Destroy(spawned3DModel);

        GameObject prefabToSpawn = isUpgrade ? currentUpgrade.worldPrefab : currentIngredient.worldPrefab;

        if (prefabToSpawn != null && interactable.inspectSpawnPoint != null)
        {
            GameObject rawModel = Instantiate(prefabToSpawn, interactable.inspectSpawnPoint.position, Quaternion.identity);

            // Strip scripts
            foreach (var dragScript in rawModel.GetComponentsInChildren<Draggable3DItem>(true))
            {
                dragScript.isLocked = true; dragScript.enabled = false; Destroy(dragScript);
            }
            foreach (var rb in rawModel.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
            foreach (var anim in rawModel.GetComponentsInChildren<Animator>(true)) Destroy(anim);

            // Auto-Center
            Renderer[] renderers = rawModel.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                GameObject pivotContainer = new GameObject("ShopPreviewPivot");
                pivotContainer.transform.position = bounds.center;

                rawModel.transform.SetParent(pivotContainer.transform);
                pivotContainer.transform.position = interactable.inspectSpawnPoint.position;
                pivotContainer.AddComponent<ItemRotator>();
                spawned3DModel = pivotContainer;
            }
            else
            {
                rawModel.AddComponent<ItemRotator>();
                spawned3DModel = rawModel;
            }
        }

        // --- SPAWN 2D CARD (Only if it's an ingredient!) ---
        if (spawnedCardUI != null) Destroy(spawnedCardUI);

        if (!isUpgrade && currentIngredient.cardUIPrefab != null)
        {
            spawnedCardUI = Instantiate(currentIngredient.cardUIPrefab, cardContainer);
            foreach (var drag in spawnedCardUI.GetComponentsInChildren<CardDragUI>(true)) Destroy(drag);
            foreach (var placer in spawnedCardUI.GetComponentsInChildren<CardGridPlacer>(true)) Destroy(placer);
            foreach (var tile in spawnedCardUI.GetComponentsInChildren<GridTileVisual>(true)) Destroy(tile);
            spawnedCardUI.SetActive(false);
        }

        // Camera Transition
        if (cameraTransitionCoroutine != null) StopCoroutine(cameraTransitionCoroutine);
        cameraTransitionCoroutine = StartCoroutine(MoveCameraRoutine(interactable.inspectCameraTarget));

        if (cantAffordCoroutine != null)
        {
            StopCoroutine(cantAffordCoroutine);
            if (priceText != null) priceText.color = originalPriceColor;
        }

        UpdatePanelUI();
    }

    public void ToggleInfoCard()
    {
        if (spawnedCardUI != null)
        {
            isInfoCardVisible = !isInfoCardVisible;
            spawnedCardUI.SetActive(isInfoCardVisible);
        }
    }

    private IEnumerator MoveCameraRoutine(Transform target)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null || target == null)
        {
            isTransitioning = false;
            yield break;
        }

        if (cameraController != null)
        {
            Transform trueHome = cameraController.InterruptAndGetTargetView();
            originalCameraPos = trueHome.position;
            originalCameraRot = trueHome.rotation;
        }
        else
        {
            originalCameraPos = mainCam.transform.position;
            originalCameraRot = mainCam.transform.rotation;
        }

        Vector3 flightStartPos = mainCam.transform.position;
        Quaternion flightStartRot = mainCam.transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            mainCam.transform.position = Vector3.Lerp(flightStartPos, target.position, Mathf.SmoothStep(0, 1, t));
            mainCam.transform.rotation = Quaternion.Lerp(flightStartRot, target.rotation, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        isTransitioning = false;
    }

    public void CloseBuyingPanel()
    {
        if (!isCurrentlyInspecting || isTransitioning) return;

        isTransitioning = true;
        buyingPanel.SetActive(false);

        if (spawnedCardUI != null) Destroy(spawnedCardUI);
        if (spawned3DModel != null) Destroy(spawned3DModel);

        if (currentInteractable != null && currentInteractable.showcaseSpotlight != null)
        {
            currentInteractable.showcaseSpotlight.enabled = false;
        }

        currentIngredient = null;
        currentUpgrade = null; // --- FIXED: Make sure we clear the upgrade out too! ---
        currentInteractable = null;
        isTransactionLocked = false;

        if (cameraTransitionCoroutine != null) StopCoroutine(cameraTransitionCoroutine);
        cameraTransitionCoroutine = StartCoroutine(ReturnCameraRoutine());
    }

    private IEnumerator ReturnCameraRoutine()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            FinishReturnSequence();
            yield break;
        }

        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            mainCam.transform.position = Vector3.Lerp(startPos, originalCameraPos, Mathf.SmoothStep(0, 1, t));
            mainCam.transform.rotation = Quaternion.Lerp(startRot, originalCameraRot, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        FinishReturnSequence();
    }

    private void FinishReturnSequence()
    {
        ShopItemInteractable.isInteractionLocked = false;
        isCurrentlyInspecting = false;
        isTransitioning = false;

        if (cameraController != null) cameraController.isCameraLocked = false;
    }

    public void IncreaseAmount()
    {
        // --- FIXED: Now checks the new currentUpgrade variable ---
        if (isTransactionLocked || currentUpgrade != null) return;
        currentAmount++;
        UpdatePanelUI();
    }

    public void DecreaseAmount()
    {
        // --- FIXED: Now checks the new currentUpgrade variable ---
        if (isTransactionLocked || currentUpgrade != null) return;
        if (currentAmount > 1)
        {
            currentAmount--;
            UpdatePanelUI();
        }
    }

    private void UpdatePanelUI()
    {
        bool isUpgrade = (currentUpgrade != null);
        amountText.text = currentAmount.ToString();

        int unitPrice = isUpgrade ? currentUpgrade.purchasePrice : currentIngredient.purchasePrice;
        int totalCost = currentAmount * unitPrice;

        priceText.text = "Cost: " + totalCost.ToString() + " P";
    }

    private void UpdateBankCashDisplay()
    {
        if (PlayerEconomyManager.Instance != null && bankCashText != null)
        {
            bankCashText.text = "Bank: " + PlayerEconomyManager.Instance.totalBankCash.ToString() + " P";
        }
    }

    public void ConfirmPurchase()
    {
        if (isTransactionLocked) return;
        if (currentIngredient == null && currentUpgrade == null) return;
        if (PlayerEconomyManager.Instance == null || PlayerInventoryManager.Instance == null) return;

        bool isUpgrade = (currentUpgrade != null);
        int unitPrice = isUpgrade ? currentUpgrade.purchasePrice : currentIngredient.purchasePrice;
        int totalCost = currentAmount * unitPrice;

        if (PlayerEconomyManager.Instance.totalBankCash >= totalCost)
        {
            PlayerEconomyManager.Instance.totalBankCash -= totalCost;

            if (isUpgrade)
            {
                PlayerInventoryManager.Instance.UnlockUpgrade(currentUpgrade.uniqueUpgradeID);
                if (currentInteractable != null) Destroy(currentInteractable.gameObject);
            }
            else
            {
                PlayerInventoryManager.Instance.AddStock(currentIngredient, currentAmount);
            }

            UpdateBankCashDisplay();
            CloseBuyingPanel();
        }
        else
        {
            if (cantAffordCoroutine != null) StopCoroutine(cantAffordCoroutine);
            cantAffordCoroutine = StartCoroutine(CantAffordRoutine());
        }
    }

    private IEnumerator CantAffordRoutine()
    {
        isTransactionLocked = true;
        if (priceText != null)
        {
            priceText.text = "Can't Afford!";
            priceText.color = Color.red;
        }

        yield return new WaitForSeconds(1.2f);

        if (priceText != null) priceText.color = originalPriceColor;
        UpdatePanelUI();

        isTransactionLocked = false;
        cantAffordCoroutine = null;
    }

    public void ProceedToInventoryPhase()
    {
        ShopItemInteractable.isInteractionLocked = false;
        SceneManager.LoadScene(nextSceneName);
    }
}