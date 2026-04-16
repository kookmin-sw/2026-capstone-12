using System;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 슈터 레벨/경험치 시스템 (MasterClient 권위)
/// - 적 처치 시 XP 획득
/// - 레벨업 시 랜덤 능력치 1개 상승 (데미지 / 이동속도 / 체력)
/// </summary>
public class ShooterLevelNet : MonoBehaviourPunCallbacks
{
    public static ShooterLevelNet Instance { get; private set; }

    [Header("Level Settings")]
    [SerializeField] private int baseXpToLevel = 100;
    [SerializeField] private float xpScalePerLevel = 1.3f;

    [Header("Stat Bonuses Per Level")]
    [SerializeField] private float damageBonusPerLevel = 5f;
    [SerializeField] private float speedBonusPerLevel = 0.4f;
    [SerializeField] private float hpBonusPerLevel = 15f;

    // 현재 상태
    private int currentLevel = 1;
    private int currentXp = 0;
    private int xpToNextLevel;

    // 누적 보너스
    private float bonusDamage = 0f;
    private float bonusSpeed = 0f;
    private float bonusHp = 0f;

    // 스탯 종류
    public enum StatType { Damage = 0, Speed = 1, Health = 2 }

    // UI 이벤트
    public event Action<int, int, int> OnXpChanged;       // (level, currentXp, xpToNext)
    public event Action<int, StatType> OnLevelUp;          // (newLevel, statType)

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        xpToNextLevel = baseXpToLevel;

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RpcSyncLevelState), RpcTarget.All,
                currentLevel, currentXp, xpToNextLevel, bonusDamage, bonusSpeed, bonusHp);
        }
    }

    // ========================================
    // Master API
    // ========================================

    /// <summary>
    /// 마스터에서 호출: XP 추가 및 레벨업 체크
    /// </summary>
    public void MasterAddXp(int amount)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        currentXp += amount;

        while (currentXp >= xpToNextLevel)
        {
            currentXp -= xpToNextLevel;
            currentLevel++;
            xpToNextLevel = Mathf.RoundToInt(baseXpToLevel * Mathf.Pow(xpScalePerLevel, currentLevel - 1));

            // 랜덤 스탯 선택 및 적용
            StatType chosen = (StatType)UnityEngine.Random.Range(0, 3);
            ApplyStatBonus(chosen);

            photonView.RPC(nameof(RpcLevelUp), RpcTarget.All, currentLevel, (int)chosen);
        }

        photonView.RPC(nameof(RpcSyncLevelState), RpcTarget.All,
            currentLevel, currentXp, xpToNextLevel, bonusDamage, bonusSpeed, bonusHp);
    }

    private void ApplyStatBonus(StatType stat)
    {
        switch (stat)
        {
            case StatType.Damage:
                bonusDamage += damageBonusPerLevel;
                break;
            case StatType.Speed:
                bonusSpeed += speedBonusPerLevel;
                break;
            case StatType.Health:
                bonusHp += hpBonusPerLevel;
                break;
        }
    }

    // ========================================
    // RPC
    // ========================================

    [PunRPC]
    private void RpcSyncLevelState(int level, int xp, int xpNeeded,
        float bDamage, float bSpeed, float bHp)
    {
        currentLevel = level;
        currentXp = xp;
        xpToNextLevel = xpNeeded;
        bonusDamage = bDamage;
        bonusSpeed = bSpeed;
        bonusHp = bHp;

        // 실제 스탯에 반영
        ApplyStatsToShooter();

        OnXpChanged?.Invoke(currentLevel, currentXp, xpToNextLevel);
    }

    [PunRPC]
    private void RpcLevelUp(int newLevel, int statIndex)
    {
        StatType stat = (StatType)statIndex;
        string statName = stat switch
        {
            StatType.Damage => "공격력",
            StatType.Speed => "이동속도",
            StatType.Health => "체력",
            _ => ""
        };
        Debug.Log($"[LevelUp] 레벨 {newLevel} 달성! {statName} 상승!");

        OnLevelUp?.Invoke(newLevel, stat);
    }

    // ========================================
    // 스탯 적용
    // ========================================

    private void ApplyStatsToShooter()
    {
        // 데미지 적용
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            WeaponController weapon = playerObj.GetComponent<WeaponController>();
            weapon?.SetBonusDamage(bonusDamage);

            PlayerController movement = playerObj.GetComponent<PlayerController>();
            movement?.SetBonusSpeed(bonusSpeed);
        }

        // 체력 적용 (마스터에서만)
        if (ShooterHealthNet.Instance != null)
            ShooterHealthNet.Instance.SetBonusHp(bonusHp);
    }

    // ========================================
    // Public 속성 (UI용)
    // ========================================
    public int CurrentLevel => currentLevel;
    public int CurrentXp => currentXp;
    public int XpToNextLevel => xpToNextLevel;
    public float BonusDamage => bonusDamage;
    public float BonusSpeed => bonusSpeed;
    public float BonusHp => bonusHp;
}
