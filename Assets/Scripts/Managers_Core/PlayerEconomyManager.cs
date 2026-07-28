using UnityEngine;
using System; // Required for Actions/Events

public class PlayerEconomyManager : MonoBehaviour
{
    public static PlayerEconomyManager Instance;

    [Header("Global Currency")]
    public int totalBankCash = 0; // This carries over to your Meta Shop!

    [Header("Current Shift Stats")]
    public int currentDailyQuota = 100;
    public int shiftCash = 0;
    public int shiftPoints = 0;

    // --- NEW: The Decoupled Event System ---
    // Any script in any scene can listen to this to know when money changes!
    public event Action<int, int> OnRegisterUpdated;

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

    public void StartNewShift(int dailyQuotaTarget)
    {
        shiftCash = 0;
        shiftPoints = 0;
        currentDailyQuota = dailyQuotaTarget;

        // Shout to the world that the register reset
        OnRegisterUpdated?.Invoke(shiftCash, currentDailyQuota);
    }

    public void AddShiftRevenue(int cash, int points)
    {
        shiftCash += cash;
        shiftPoints += points;

        // Shout to the world that money was added
        OnRegisterUpdated?.Invoke(shiftCash, currentDailyQuota);
    }

    public int CalculateTotalShiftEarnings(out int tips)
    {
        tips = shiftPoints / 100;
        return shiftCash + tips;
    }

    public void DepositShiftEarnings()
    {
        int totalEarnings = CalculateTotalShiftEarnings(out _);
        totalBankCash += totalEarnings;
    }
}