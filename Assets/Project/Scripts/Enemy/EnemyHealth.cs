using UnityEngine;

/// <summary>
/// 적 체력 관리
/// - 데미지 받기
/// - 사망 처리
/// - 점수 및 돈 지급
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    // ============================================================
    // 변수
    // ============================================================
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float currentHealth;

    [Header("Rewards")]
    [SerializeField] private int scoreReward = 100;         // 처치 시 점수
    [SerializeField] private int moneyReward = 10;          // 처치 시 돈

    [Header("Defense")]
    [SerializeField] private float damageReduction = 0f;    // 데미지 감소율 (0~1)

    // State
    private bool isDead = false;

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Start()
    {
        currentHealth = maxHealth;
    }

    // ============================================================
    // 데미지
    // ============================================================
    /// <summary>
    /// 데미지 받기
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        // 데미지 감소 적용 (방어형 적용)
        float actualDamage = damage * (1f - damageReduction);
        currentHealth -= actualDamage;

        Debug.Log($"{gameObject.name} took {actualDamage} damage! HP: {currentHealth}/{maxHealth}");

        // 체력 0이면 사망
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // ============================================================
    // 사망
    // ============================================================
    void Die()
    {
        isDead = true;

        // GameManager에 점수 및 처치 수 추가
        GameManager.Instance.AddScore(scoreReward);
        GameManager.Instance.AddKill();

        EnemyManager.Instance.RemoveEnemy(gameObject);
        // TODO: 나중에 EconomyManager에 돈 추가
        Debug.Log($"{gameObject.name} died! Score +{scoreReward}, Money +{moneyReward}");

        // 적 제거
        Destroy(gameObject);
    }

    // ============================================================
    // Public 속성
    // ============================================================
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
}