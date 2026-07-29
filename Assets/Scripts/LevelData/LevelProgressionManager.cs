using System.Collections.Generic;
using UnityEngine;

public class LevelProgressionManager : MonoBehaviour
{
    public static LevelProgressionManager Instance;

    [Header("Campaign Setup")]
    [Tooltip("Index 0 = Tutorial 3, Index 1 = Manila Day 1, Index 2 = Manila Day 2, etc.")]
    public List<ShiftLevelData> campaignLevels;

    [Header("Current Progress")]
    public int currentLevelIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Any script in any scene can call this to get the rules for the day!
    public ShiftLevelData GetCurrentLevelData()
    {
        if (currentLevelIndex < campaignLevels.Count)
        {
            return campaignLevels[currentLevelIndex];
        }

        Debug.LogWarning("You beat all the levels! Returning the last level data.");
        return campaignLevels[campaignLevels.Count - 1];
    }

    // Call this when the shift ends successfully!
    public void AdvanceToNextDay()
    {
        currentLevelIndex++;
        Debug.Log("Advanced to Level Index: " + currentLevelIndex);
        // Save progress to PlayerPrefs or JSON here later
    }
}