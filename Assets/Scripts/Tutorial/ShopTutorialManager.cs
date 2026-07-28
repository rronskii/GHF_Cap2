using System.Collections;
using UnityEngine;

public class ShopTutorialManager : MonoBehaviour
{
    [Header("System References")]
    public ShopCameraController cameraController;
    public ShopUIManager shopUIManager;
    public GameObject continueButton; // Drag your UI Button here

    [Header("Purchase Settings")]
    public string panUpgradeID = "upgrade_pan_2";
    public int bonusCashAmount = 250;

    [Header("Section Indices")]
    public int veggiesIndex = 0;
    public int proteinsIndex = 1;
    public int aromaticsIndex = 2;
    public int appliancesIndex = 3;

    private void Start()
    {
        // 1. Force the player's bank cash to 0
        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.totalBankCash = 0;
            if (shopUIManager != null) shopUIManager.SendMessage("UpdateBankCashDisplay", SendMessageOptions.DontRequireReceiver);
        }

        // 2. Lock things up
        ShopItemInteractable.isInteractionLocked = true;
        if (cameraController != null)
        {
            cameraController.isCameraLocked = true;
            cameraController.disableRightPan = true;
        }

        if (continueButton != null) continueButton.SetActive(false);

        StartCoroutine(GuidedTourRoutine());
    }

    private IEnumerator GuidedTourRoutine()
    {
        bool dialogueDone = false;

        // ==========================================
        // 1. INTRO & VEGGIES
        // ==========================================
        DialogueManager.Instance.StartDialogue(new string[] {
            "Welcome to the Daily Shop!",
            "This is where you'll spend your hard-earned cash to restock ingredients and buy permanent upgrades.",
            "Right now, you're looking at the Veggies section.",
            "Press 'A' to look LEFT to the Proteins section."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        cameraController.isCameraLocked = false;

        // ==========================================
        // 2. PROTEINS
        // ==========================================
        while (cameraController.GetCurrentSectionIndex() != proteinsIndex) yield return null;

        cameraController.isCameraLocked = true;
        dialogueDone = false;

        DialogueManager.Instance.StartDialogue(new string[] {
            "Here you'll find all your heavy-hitting meats.",
            "Press 'A' again to look LEFT at the Aromatics & Others section."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        cameraController.isCameraLocked = false;

        // ==========================================
        // 3. AROMATICS & OTHERS
        // ==========================================
        while (cameraController.GetCurrentSectionIndex() != aromaticsIndex) yield return null;

        cameraController.isCameraLocked = true;
        dialogueDone = false;

        DialogueManager.Instance.StartDialogue(new string[] {
            "Garlic, onions, and side dishes live here.",
            "Let's move to the final stop. Press 'A' one more time to view the Appliances!"
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        cameraController.isCameraLocked = false;

        // ==========================================
        // 4. APPLIANCES & FREE CASH
        // ==========================================
        while (cameraController.GetCurrentSectionIndex() != appliancesIndex) yield return null;

        cameraController.isCameraLocked = true;

        // Inject free cash
        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.totalBankCash += bonusCashAmount;
            if (shopUIManager != null) shopUIManager.SendMessage("UpdateBankCashDisplay", SendMessageOptions.DontRequireReceiver);
        }

        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] {
            "Appliances are permanent upgrades that make your food truck run smoother.",
            "Since it's your first day, I'm covering tomorrow's food costs AND giving you some bonus cash!",
            "Click on the Frying Pan on the shelf and buy a second one so you can cook two things at once."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        // Unlock interactions so they can click the pan
        ShopItemInteractable.isInteractionLocked = false;

        // ==========================================
        // 5. WAIT FOR PURCHASE (Safely polled)
        // ==========================================
        // If they already bought it during dialogue, this skips instantly; otherwise it waits smoothly.
        while (!PlayerInventoryManager.Instance.HasPurchasedUpgrade(panUpgradeID))
        {
            yield return null;
        }

        // Small safety buffer frame to ensure ShopUIManager completely finishes closing panels
        yield return null;

        // ==========================================
        // 6. OUTRO & UNLOCK EVERYTHING
        // ==========================================
        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] {
            "Awesome! That pan is permanently yours.",
            "Feel free to look around the shop. When you are ready, click the Proceed button to head to the kitchen and set up your loadout!",
            "Don't worry about your ingredients for tomorrow, I'll cover that for you one last time."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        // Unlock everything: camera movement, right pan, and reveal the button
        if (cameraController != null)
        {
            cameraController.isCameraLocked = false;
            cameraController.disableRightPan = false;
        }

        if (continueButton != null) continueButton.SetActive(true);
    }
}