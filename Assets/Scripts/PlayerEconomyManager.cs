using UnityEngine;

public class PlayerEconomyManager : MonoBehaviour
{
    public static PlayerEconomyManager Instance;

    [Header("Global Currency")]
    public int totalBankCash = 0; // This carries over to your Meta Shop!

    [Header("Current Shift Stats")]
    public int shiftCash = 0;
    public int shiftPoints = 0;

    private void Awake()
    {
        // Persistent Singleton Pattern
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

    // Called at the start of every day/level
    public void StartNewShift()
    {
        shiftCash = 0;
        shiftPoints = 0;
    }

    public void AddShiftRevenue(int cash, int points)
    {
        shiftCash += cash;
        shiftPoints += points;
    }

    public int CalculateTotalShiftEarnings(out int tips)
    {
        tips = shiftPoints / 100; // Integer division automatically drops decimals
        return shiftCash + tips;
    }

    // Called when the win condition is met
    public void DepositShiftEarnings()
    {
        int totalEarnings = CalculateTotalShiftEarnings(out _);
        totalBankCash += totalEarnings;
    }
}