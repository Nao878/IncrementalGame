using UnityEngine;

/// <summary>
/// Tracks player coin balance.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private int initialCoins = 300;
    private int coins;

    public int Coins => coins;
    public bool HasEarnedFirstIncome { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        coins = Mathf.Max(0, initialCoins);
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
        HasEarnedFirstIncome = true;
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (coins < amount) return false;
        coins -= amount;
        return true;
    }
}
