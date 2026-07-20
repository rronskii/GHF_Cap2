using UnityEngine;
using UnityEngine.SceneManagement;

public class CoreBootstrapper : MonoBehaviour
{
    public string InitialScene = "00a_Tutorial_Basics";

    private void Awake()
    {
        // 1. Make this Canvas indestructible across all future scene loads
        DontDestroyOnLoad(gameObject);

        // 2. Load the actual gameplay level 
        // (Make sure "01_FoodTruckLevel" matches your exact scene name!)
        SceneManager.LoadScene(InitialScene);
    }
}