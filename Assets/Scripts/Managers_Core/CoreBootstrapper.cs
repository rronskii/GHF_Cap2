using UnityEngine;
using UnityEngine.SceneManagement;

public class CoreBootstrapper : MonoBehaviour
{
    public string InitialScene = "00a_Tutorial_Basics";

    private void Awake()
    {
        // 1. Make this manager indestructible across all future scene loads
        DontDestroyOnLoad(gameObject);

        // The auto-load has been removed! The game will now wait for player input.
    }

    // --- NEW: Link this to your "Start" 3D plane ---
    public void StartFirstTutorial()
    {
        Debug.Log("[Bootstrap] Starting game, loading: " + InitialScene);
        SceneManager.LoadScene(InitialScene);
    }

    // --- NEW: Link this to your "Exit" 3D plane ---
    public void QuitGame()
    {
        Debug.Log("[Bootstrap] Quitting Game...");

        // This quits the actual built application
        Application.Quit();

        // This stops Play Mode if you are testing inside the Unity Editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}