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

    // 로컬 UI 판단용
    public bool CanAfford(int cost) => gold >= cost;

    // 마스터가 확정한 값이 내려오면 클라들은 이걸로만 갱신
    public void SetGoldFromMaster(int newGold)
    {
        if (gold == newGold) return;
        gold = newGold;
        OnGoldChanged?.Invoke(gold);
    }

    // 마스터에서만 쓰는 헬퍼
    public bool TrySpendMaster(int cost)
    {
        if (gold < cost) return false;
        gold -= cost;
        OnGoldChanged?.Invoke(gold);
        return true;
    }

    public void AddGoldMaster(int amount)
    {
        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }

    // 테스트용
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            AddGoldMaster(50);
    }
}
