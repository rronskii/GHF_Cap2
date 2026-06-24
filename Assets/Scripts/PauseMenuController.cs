using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance;

    [Header("UI Element References")]
    // Notice we removed the pauseMenuRootCanvas variable!
    [SerializeField] private GameObject darkenOverlayPanel;
    [SerializeField] private GameObject mainPauseMenuPanel;
    [SerializeField] private GameObject cookbookPanelPlaceholder;

    private bool isPaused = false;
    private bool isCookbookOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Ensure UI elements are initialized to closed states on launch
        InitializeUIState();
    }

    private void Update()
    {
        // NEW: Do not allow pausing or unpausing if the level is officially cleared!
        if (OrderManager.Instance != null && OrderManager.Instance.isLevelCleared) return;

        // UPDATED: Now listens for Escape OR the 'P' key
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (isCookbookOpen)
            {
                CloseCookbook();
            }
            else
            {
                if (isPaused) ResumeGame();
                else PauseGame();
            }
        }
    }

    private void InitializeUIState()
    {
        // Only turn off the child panels, NEVER the Canvas itself!
        if (darkenOverlayPanel != null) darkenOverlayPanel.SetActive(false);
        if (mainPauseMenuPanel != null) mainPauseMenuPanel.SetActive(false);
        if (cookbookPanelPlaceholder != null) cookbookPanelPlaceholder.SetActive(false);
    }

    public void PauseGame()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        isPaused = true;
        Time.timeScale = 0f; // Freezes physics, DeltaTime, and standard Coroutines

        if (darkenOverlayPanel != null) darkenOverlayPanel.SetActive(true);
        if (mainPauseMenuPanel != null) mainPauseMenuPanel.SetActive(true);
        if (cookbookPanelPlaceholder != null) cookbookPanelPlaceholder.SetActive(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        isCookbookOpen = false;
        Time.timeScale = 1f; // Restores full simulation execution speed

        InitializeUIState();
    }

    public void OpenCookbook()
    {
        if (!isPaused) return;

        isCookbookOpen = true;
        if (mainPauseMenuPanel != null) mainPauseMenuPanel.SetActive(false);
        if (cookbookPanelPlaceholder != null) cookbookPanelPlaceholder.SetActive(true);
    }

    public void CloseCookbook()
    {
        if (!isPaused) return;

        isCookbookOpen = false;
        if (cookbookPanelPlaceholder != null) cookbookPanelPlaceholder.SetActive(false);
        if (mainPauseMenuPanel != null) mainPauseMenuPanel.SetActive(true);
    }
}