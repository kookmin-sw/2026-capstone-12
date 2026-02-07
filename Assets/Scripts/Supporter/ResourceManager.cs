using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [SerializeField] private int gold = 200;
    public int Gold => gold;

    public event Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        OnGoldChanged?.Invoke(gold);
    }

    public bool CanAfford(int amount)
    {
        return gold >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;

        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }

    // 테스트용
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            AddGold(50);
    }
}
