using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 플레이어 체력 관리
/// - HP 관리
/// - 데미지 받기 / 회복
/// - 사망 시 GameManager에 게임 오버 알림
/// - 리스폰 없음 (슈터 사망 = 게임 오버)
/// </summary>
public class HealthManager : MonoBehaviour
{
    // ============================================================
    // 변수
    // ============================================================
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    // Components
    private PlayerController playerController;
    private WeaponController weaponController;

    // State
    private bool isDead = false;

    // ============================================================
    // Events
    // ============================================================
    public UnityEventFloat OnHealthChanged = new UnityEventFloat();

    // ============================================================
    // Public 속성
    // ============================================================
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Start()
    {
        playerController = GetComponent<PlayerController>();
        weaponController = GetComponent<WeaponController>();

        // 체력 초기화
        currentHealth = maxHealth;
    }

    // ============================================================
    // 체력 관련
    // ============================================================
    /// <summary>
    /// 데미지 받기
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // UI 업데이트 (0~1 비율로 전달)
        OnHealthChanged.Invoke(currentHealth / maxHealth);

        Debug.Log($"Damage: -{amount} | HP: {currentHealth}/{maxHealth}");

        // 체력 0이면 사망
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// 체력 회복 (나중에 서포터 지원용)
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnHealthChanged.Invoke(currentHealth / maxHealth);

        Debug.Log($"Heal: +{amount} | HP: {currentHealth}/{maxHealth}");
    }

    // ============================================================
    // 사망
    // ============================================================
    /// <summary>
    /// 사망 처리
    /// </summary>
    void Die()
    {
        isDead = true;

        // 이동/발사 불가
        playerController.SetEnabled(false);
        weaponController.SetEnabled(false);

        Debug.Log("Player Died! Triggering Game Over...");

        // GameManager에 게임 오버 알림
        GameManager.Instance.TriggerGameOver();
    }
}

/// <summary>
/// float 파라미터를 받는 UnityEvent
/// </summary>
public class UnityEventFloat : UnityEvent<float> { }