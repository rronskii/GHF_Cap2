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
            if (isCookbookOpen) CloseCookbook();
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
    }

    public void PauseGame()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        isPaused = true;
        Time.timeScale = 0f;

        if (darkenOverlayPanel != null) darkenOverlayPanel.SetActive(true);
        if (mainPauseMenuPanel != null) mainPauseMenuPanel.SetActive(true);
        if (cookbookPanelPlaceholder != null) cookbookPanelPlaceholder.SetActive(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        isCookbookOpen = false;
        Time.timeScale = 1f;
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

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}