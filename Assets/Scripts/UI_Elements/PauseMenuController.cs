using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance;

    [Header("UI Element References")]
    [SerializeField] private GameObject darkenOverlayPanel;
    [SerializeField] private GameObject mainPauseMenuPanel;
    [SerializeField] private GameObject cookbookPanelPlaceholder;

    private bool isPaused = false;
    private bool isCookbookOpen = false;
    private bool openedFromTablet = false; // --- NEW: Tracks how the player opened the cookbook

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeUIState();
    }

    private void Update()
    {
        if (OrderManager.Instance != null && OrderManager.Instance.isLevelCleared) return;

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
        if (darkenOverlayPanel != null) darkenOverlayPanel.SetActive(false);
        if (mainPauseMenuPanel != null) mainPauseMenuPanel.SetActive(false);
        if (cookbookPanelPlaceholder != null) cookbookPanelPlaceholder.SetActive(false);
        openedFromTablet = false;
    }

    public void PauseGame()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        isPaused = true;
        Time.timeScale = 0f;

        if (darkenOverlayPanel != null) darkenOverlayPanel.SetActive(true);
        if (mainPauseMenuPanel != null) mainPauseMenuPanel.SetActive(true);
        if (cookbookPanelPlaceholder != null) cookbookPanelPlaceholder.SetActive(false);

        openedFromTablet = false;
    }

    public void ResumeGame()
    {
        isPaused = false;
        isCookbookOpen = false;
        openedFromTablet = false;
        Time.timeScale = 1f;

        InitializeUIState();
    }

    // --- NEW: Called when clicking the 3D Tablet ---
    public void OpenCookbookDirectly()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        isPaused = true;
        isCookbookOpen = true;
        openedFromTablet = true;
        Time.timeScale = 0f; // Freeze the game in the background

        if (darkenOverlayPanel != null) darkenOverlayPanel.SetActive(true);
        if (mainPauseMenuPanel != null) mainPauseMenuPanel.SetActive(false); // Skip main menu
        if (cookbookPanelPlaceholder != null) cookbookPanelPlaceholder.SetActive(true);
    }

    // Called when clicking "Cookbook" from the Pause Menu UI
    public void OpenCookbook()
    {
        if (!isPaused) return;

        isCookbookOpen = true;
        openedFromTablet = false;

        if (mainPauseMenuPanel != null) mainPauseMenuPanel.SetActive(false);
        if (cookbookPanelPlaceholder != null) cookbookPanelPlaceholder.SetActive(true);
    }

    public void CloseCookbook()
    {
        if (!isPaused) return;

        isCookbookOpen = false;
        if (cookbookPanelPlaceholder != null) cookbookPanelPlaceholder.SetActive(false);

        // --- NEW: Route them back to where they came from ---
        if (openedFromTablet)
        {
            // They opened it from the 3D world, so closing it should unpause everything
            ResumeGame();
        }
        else
        {
            // They opened it from the pause menu, so return them to the pause menu
            if (mainPauseMenuPanel != null) mainPauseMenuPanel.SetActive(true);
        }
    }

    public void RestartLevel()
    {
        if (darkenOverlayPanel != null) darkenOverlayPanel.SetActive(false);
        if (mainPauseMenuPanel != null) mainPauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}