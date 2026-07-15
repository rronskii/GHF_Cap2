using UnityEngine;
using UnityEngine.SceneManagement;

public class CoreBootstrapper : MonoBehaviour
{
    private void Awake()
    {
        // 1. Make this Canvas indestructible across all future scene loads
        DontDestroyOnLoad(gameObject);

        // 2. Load the actual gameplay level 
        // (Make sure "01_FoodTruckLevel" matches your exact scene name!)
        SceneManager.LoadScene("01_FoodTruckLevel");
    }
}