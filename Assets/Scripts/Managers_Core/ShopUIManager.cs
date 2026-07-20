using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ShopUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject buyingPanel;
    // Overlay background removed!

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

    private int currentAmount = 1;
    private IngredientData currentIngredient;
    private ShopItemInteractable currentInteractable;

    private GameObject spawnedCardUI;
    private GameObject spawned3DModel;

    private Color originalPriceColor;
    private Coroutine cantAffordCoroutine;
    private Coroutine cameraTransitionCoroutine;
    private bool isTransitioning = false;

    private bool isTransactionLocked = false;
    private bool isInfoCardVisible = false;
    private bool isCurrentlyInspecting = false; // Prevents the 0,0,0 bug!

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
        // Only hide the UI on start, don't trigger camera movements!
        buyingPanel.SetActive(false);
        UpdateBankCashDisplay();
    }

    private void OpenBuyingPanel(IngredientData ingredient, ShopItemInteractable interactable)
    {
        // Don't restart the sequence if we are already inspecting
        if (isCurrentlyInspecting || isTransitioning) return;

        isTransitioning = true; // Lock the camera! 
        currentIngredient = ingredient;
        currentInteractable = interactable;
        currentAmount = 1;
        isTransactionLocked = false;
        isInfoCardVisible = false;
        isCurrentlyInspecting = true;

        ShopItemInteractable.isInteractionLocked = true;
        if (cameraController != null) cameraController.isCameraLocked = true;

        bool showAmountControls = !interactable.isUpgrade;
        if (increaseButton != null) increaseButton.SetActive(showAmountControls);
        if (decreaseButton != null) decreaseButton.SetActive(showAmountControls);
        if (amountTextObj != null) amountTextObj.SetActive(showAmountControls);

        if (interactable.showcaseSpotlight != null) interactable.showcaseSpotlight.enabled = true;

        buyingPanel.SetActive(true);

        // --- SPAWN 3D MODEL ---
        if (spawned3DModel != null) Destroy(spawned3DModel);

        if (ingredient.worldPrefab != null && interactable.inspectSpawnPoint != null)
        {
            spawned3DModel = Instantiate(ingredient.worldPrefab, interactable.inspectSpawnPoint.position, Quaternion.identity);

            // Aggressive stripping of physics, logic, AND animators so it doesn't snap!
            if (spawned3DModel.GetComponent<Draggable3DItem>() != null) Destroy(spawned3DModel.GetComponent<Draggable3DItem>());
            if (spawned3DModel.GetComponent<Rigidbody>() != null) Destroy(spawned3DModel.GetComponent<Rigidbody>());
            if (spawned3DModel.GetComponent<Animator>() != null) Destroy(spawned3DModel.GetComponent<Animator>());

            spawned3DModel.AddComponent<ItemRotator>();
        }

        // --- SPAWN 2D CARD ---
        if (spawnedCardUI != null) Destroy(spawnedCardUI);

        if (ingredient.cardUIPrefab != null)
        {
            spawnedCardUI = Instantiate(ingredient.cardUIPrefab, cardContainer);

            foreach (var drag in spawnedCardUI.GetComponentsInChildren<CardDragUI>(true)) Destroy(drag);
            foreach (var placer in spawnedCardUI.GetComponentsInChildren<CardGridPlacer>(true)) Destroy(placer);
            foreach (var tile in spawnedCardUI.GetComponentsInChildren<GridTileVisual>(true)) Destroy(tile);

            spawnedCardUI.SetActive(false);
        }

        // --- CAMERA TRANSITION ---
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

        // --- NEW: Cancel any active station panning and get the true shelf destination ---
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

        // Capture exactly where the camera physically is right now so the flight is smooth
        Vector3 flightStartPos = mainCam.transform.position;
        Quaternion flightStartRot = mainCam.transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;

            // Fly from our physical mid-air spot to the inspect target
            mainCam.transform.position = Vector3.Lerp(flightStartPos, target.position, Mathf.SmoothStep(0, 1, t));
            mainCam.transform.rotation = Quaternion.Lerp(flightStartRot, target.rotation, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        isTransitioning = false;
    }

    public void CloseBuyingPanel()
    {
        // Don't try to close if we aren't actually inspecting, or if we are mid-flight
        if (!isCurrentlyInspecting || isTransitioning) return;

        isTransitioning = true; // Lock everything for the return flight
        buyingPanel.SetActive(false);

        if (spawnedCardUI != null) Destroy(spawnedCardUI);
        if (spawned3DModel != null) Destroy(spawned3DModel);

        if (currentInteractable != null && currentInteractable.showcaseSpotlight != null)
        {
            currentInteractable.showcaseSpotlight.enabled = false;
        }

        currentIngredient = null;
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
        // Unlock shelf items ONLY when the camera has fully docked back at the shelf
        ShopItemInteractable.isInteractionLocked = false;
        isCurrentlyInspecting = false;
        isTransitioning = false;

        if (cameraController != null) cameraController.isCameraLocked = false;
    }

    public void IncreaseAmount()
    {
        if (isTransactionLocked || (currentInteractable != null && currentInteractable.isUpgrade)) return;
        currentAmount++;
        UpdatePanelUI();
    }

    public void DecreaseAmount()
    {
        if (isTransactionLocked || (currentInteractable != null && currentInteractable.isUpgrade)) return;
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
        if (PlayerEconomyManager.Instance != null && bankCashText != null)
        {
            bankCashText.text = "Bank: " + PlayerEconomyManager.Instance.totalBankCash.ToString() + " P";
        }
    }

    public void ConfirmPurchase()
    {
        if (isTransactionLocked) return;
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
        SceneManager.LoadScene("03_Inventory");
    }
}