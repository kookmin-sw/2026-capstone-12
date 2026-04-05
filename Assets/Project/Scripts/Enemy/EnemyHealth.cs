using UnityEngine;
using Photon.Pun;
using System;

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
    public event Action OnDied;

    [Header("Health Settings")]
    [SerializeField] private float maxHp = 30f;
    [SerializeField] private float currentHp;

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
        currentHp = maxHp;
    }

    // ============================================================
    // 데미지
    // ============================================================
    /// <summary>
    /// 데미지 받기
    /// </summary>
    public void SetHealthFromNet(float newHp, float newMaxHp)
    {
        maxHp = newMaxHp;
        currentHp = Mathf.Clamp(newHp, 0f, maxHp);

        if (!isDead && currentHp <= 0f)
            DieLocal();
    }

    // ============================================================
    // 사망
    // ============================================================
    /*void Die()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        
        isDead = true;

        // GameManager에 점수 및 처치 수 추가
        GameManager.Instance.AddScore(scoreReward);
        GameManager.Instance.AddKill();

        EnemyManager.Instance.RemoveEnemy(gameObject);
        // TODO: 나중에 EconomyManager에 돈 추가
        Debug.Log($"{gameObject.name} died! Score +{scoreReward}, Money +{moneyReward}");

        // 적 제거
        PhotonNetwork.Destroy(gameObject);
    }*/

    private void DieLocal()
    {
        isDead = true;
        OnDied?.Invoke();

        // 바닥에 내려놓기 (공중에서 죽는 경우 대비)
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 10f))
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);

        // kinematic으로 전환 (중력/물리 무시) 후 콜라이더 비활성화
        // → 바닥 안 뚫고, 다른 적/총알도 통과
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        // 여기서는 Destroy하지 않음 (Destroy는 마스터가 PhotonNetwork.Destroy로)
    }

    // ============================================================
    // Public 속성
    // ============================================================
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public bool IsDead => isDead;
    public float DamageReduction => damageReduction;
    public int ScoreReward => scoreReward;
    public int MoneyReward => moneyReward;
}