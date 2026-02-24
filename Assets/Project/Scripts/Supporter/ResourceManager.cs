using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [SerializeField] private int money;
    [SerializeField] private int score;
    [SerializeField] private int kills;
    
    public event Action<int> OnMoneyChanged;
    public event Action<int> OnScoreChanged;
    public event Action<int> OnKillsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 로컬 UI 판단용
    public bool CanAfford(int cost) => money >= cost;

    public void SetMoneyFromNet(int newMoney)
    {
        if (money == newMoney) return;
        money = newMoney;
        OnMoneyChanged?.Invoke(money);
    }

    public void SetScoreFromNet(int newScore)
    {
        if (score == newScore) return;
        score = newScore;
        OnScoreChanged?.Invoke(score);
    }

    public void SetKillsFromNet(int newKills)
    {
        if (kills == newKills) return;
        kills = newKills;
        OnKillsChanged?.Invoke(kills);
    }

    public int Money => money;
    public int Score => score;
    public int Kills => kills;
}
