using UnityEngine;

public class PlayerEconomyManager : MonoBehaviour
{
    public static PlayerEconomyManager Instance;

    [Header("Global Currency")]
    public int totalBankCash = 0; // This carries over to your Meta Shop!

    [Header("UI References")]
    public PiggyBankUI piggyBankUI;

    [Header("Current Shift Stats")]
    public int currentDailyQuota = 100; // NEW: The finish line for the piggy bank
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
    // NEW: Pass in the quota for the day so the piggy bank knows the goal!
    public void StartNewShift(int dailyQuotaTarget)
    {
        shiftCash = 0;
        shiftPoints = 0;
        currentDailyQuota = dailyQuotaTarget;

        // Reset the piggy bank visual to zero at the start of the day
        if (piggyBankUI != null)
        {
            piggyBankUI.UpdatePiggyBank(shiftCash, currentDailyQuota);
        }
    }

    public void AddShiftRevenue(int cash, int points)
    {
        shiftCash += cash;
        shiftPoints += points;

        // NEW: Tell the piggy bank to fill up a little bit more!
        if (piggyBankUI != null)
        {
            piggyBankUI.UpdatePiggyBank(shiftCash, currentDailyQuota);
        }
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