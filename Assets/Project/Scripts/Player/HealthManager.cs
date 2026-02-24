using UnityEngine;
using UnityEngine.Events;
using System;

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
    public event Action OnDied;

    [Header("Health Settings")]
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float currentHp = 100;

    // State
    private bool isDead = false;

    // ============================================================
    // Events
    // ============================================================
    public UnityEventFloat OnHealthChanged = new UnityEventFloat();

    // ============================================================
    // Public 속성
    // ============================================================
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public bool IsDead => isDead;

    // ============================================================
    // 체력 관리
    // ============================================================
    /// <summary>
    /// 체력 변경
    /// </summary>
    public void SetHpFromNetwork(float newHp, float newMaxHp)
    {
        maxHp = newMaxHp;
        currentHp = Mathf.Clamp(newHp, 0, maxHp);
        OnHealthChanged.Invoke(currentHp / maxHp);

        if (!isDead && currentHp <= 0)
            DieInternal();
    }

    // ============================================================
    // 사망
    // ============================================================
    /// <summary>
    /// 사망 처리
    /// </summary>
    public void ForceDieFromNetwork()
    {
        if (isDead) return;
        currentHp = 0;
        DieInternal();
    }

    private void DieInternal()
    {
        isDead = true;
        OnDied?.Invoke();
    }
}

/// <summary>
/// float 파라미터를 받는 UnityEvent
/// </summary>
public class UnityEventFloat : UnityEvent<float> { }