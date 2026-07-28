using System.Collections;
using UnityEngine;

public class InventoryTutorialManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject startShiftButton;

    private void Start()
    {
        // Hide the start shift button until they learn the UI
        if (startShiftButton != null) startShiftButton.SetActive(false);

        // Lock the tablet initially!
        InventorySetupManager.Instance.isTabletLocked = true;
        InventoryCameraController.Instance.isCameraLocked = true; // Lock camera during dialogue

        StartCoroutine(TutorialFlow());
    }

    private IEnumerator TutorialFlow()
    {
        bool dialogueDone = false;

        // 1. INTRO
        DialogueManager.Instance.StartDialogue(new string[] {
            "Welcome to the Pantry!",
            "Good news: that second Frying Pan you bought was installed straight into the food truck.",
            "Before we head to the truck, we need to pack our ingredients.",
            "Press 'D' to pan the camera right and look at the Bulletin Board."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        InventoryCameraController.Instance.isCameraLocked = false;

        // 2. WAIT FOR BULLETIN BOARD
        while (InventoryCameraController.Instance.GetCurrentSectionIndex() != 1) yield return null;

        InventoryCameraController.Instance.isCameraLocked = true;
        dialogueDone = false;

        DialogueManager.Instance.StartDialogue(new string[] {
            "This board shows your active recipes for the region, and exactly what ingredients you need to pack.",
            "Take note of the list, then press 'A' to return to the main counter."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        InventoryCameraController.Instance.isCameraLocked = false;

        // 3. RETURN TO COUNTER & UNLOCK TABLET
        while (InventoryCameraController.Instance.GetCurrentSectionIndex() != 0) yield return null;

        InventoryCameraController.Instance.isCameraLocked = true; // Keep them here while they interact with UI
        InventorySetupManager.Instance.isTabletLocked = false; // Unlock the tablet!
        dialogueDone = false;

        DialogueManager.Instance.StartDialogue(new string[] {
            "Now, click the Tablet on the counter.",
            "Select the ingredients you need, and assign them to the storage crates in the room."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        while (!InventorySetupManager.Instance.pantryPanel.activeSelf) yield return null;

        // 4. UNLOCK SHIFT BUTTON
        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] {
            "Awesome. Go ahead and pack your loadout.",
            "When you are ready, click 'Start Shift'. If you forget an ingredient, I'll warn you!"
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        if (startShiftButton != null) startShiftButton.SetActive(true);

        // Final unlock of camera so they can look back at the board if they want
        InventoryCameraController.Instance.isCameraLocked = false;
    }

    public void BlockReturnToShop()
    {
        DialogueManager.Instance.StartDialogue(new string[] {
            "We already bought what we needed for today. Let's focus on packing our ingredients!"
        }, () => { });
    }
}