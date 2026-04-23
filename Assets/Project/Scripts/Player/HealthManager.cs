using UnityEngine;
using UnityEngine.Events;
using System;

/// <summary>
/// 플레이어 체력 관리
/// - HP 관리
/// - 데미지 받기 / 회복
/// - 사망/부활 이벤트 알림
/// </summary>
public class HealthManager : MonoBehaviour
{
    // ============================================================
    // 변수
    // ============================================================
    public event Action OnDied;
    public event Action OnRevived; // 리스폰 시 입력/표시 상태를 되돌릴 구독 지점

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
    /// 네트워크에서 확정된 체력 값을 반영하고 0/양수 전환에 따라 사망/부활 이벤트를 발생시킴
    /// </summary>
    public void SetHpFromNetwork(float newHp, float newMaxHp)
    {
        maxHp = newMaxHp;
        currentHp = Mathf.Clamp(newHp, 0, maxHp);
        OnHealthChanged.Invoke(currentHp / maxHp);

        if (!isDead && currentHp <= 0)
            DieInternal();
        else if (isDead && currentHp > 0)
            ReviveInternal();
    }

    // ============================================================
    // 사망/부활
    // ============================================================
    /// <summary>
    /// 네트워크에서 받은 사망 확정을 로컬 HealthManager 상태와 이벤트로 반영
    /// </summary>
    public void ForceDieFromNetwork()
    {
        if (isDead) return;
        currentHp = 0;
        DieInternal();
    }

    /// <summary>
    /// 사망 상태를 한 번만 확정하고 사망 구독자에게 알림
    /// </summary>
    private void DieInternal()
    {
        isDead = true;
        OnDied?.Invoke();
    }

    /// <summary>
    /// 리스폰 RPC에서 받은 체력으로 사망 상태를 해제하고 부활 이벤트를 발생시킴
    /// </summary>
    public void ReviveFromNetwork(float newHp, float newMaxHp)
    {
        maxHp = newMaxHp;
        currentHp = Mathf.Clamp(newHp, 1f, maxHp);
        OnHealthChanged.Invoke(currentHp / maxHp);

        if (isDead)
            ReviveInternal();
    }

    /// <summary>
    /// 부활 상태를 한 번만 확정하고 입력/표시 복구 구독자에게 알림
    /// </summary>
    private void ReviveInternal()
    {
        isDead = false;
        OnRevived?.Invoke();
    }
}

/// <summary>
/// float 파라미터를 받는 UnityEvent
/// </summary>
public class UnityEventFloat : UnityEvent<float> { }
