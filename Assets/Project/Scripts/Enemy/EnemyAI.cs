using UnityEngine;
using Photon.Pun;

/// <summary>
/// 적 AI
/// - 슈터를 향해 이동
/// - 슈터에게 도달하면 공격
/// </summary>
public class EnemyAI : MonoBehaviour
{
    // ============================================================
    // 변수
    // ============================================================
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;          // 이동 속도
    [SerializeField] private float attackRange = 1.5f;      // 공격 범위
    [SerializeField] private float attackCooldown = 1f;     // 공격 쿨타임

    [Header("Combat")]
    [SerializeField] private float attackDamage = 5f;       // 공격 데미지

    // Components
    private Transform player;                                // 플레이어 Transform
    private Rigidbody rb;
    private PhotonView pv;

    // State
    private float nextAttackTime = 0f;

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Awake()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {       
        // 플레이어 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // 마스터 클라이언트만 물리이동, 나머지는 동기화를 위해 kinematic true
        if (pv != null && rb != null)
            rb.isKinematic = !pv.IsMine;
    }

    void FixedUpdate()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        if (player == null)
            return;
        // 플레이어와의 거리 계산
        float distance = Vector3.Distance(transform.position, player.position);

        // 공격 범위 밖이면 이동
        if (distance > attackRange)
        {
            MoveTowardsPlayer();
        }
        // 공격 범위 안이면 공격
        else
        {
            AttackPlayer();
        }
    }

    // ============================================================
    // 이동
    // ============================================================
    void MoveTowardsPlayer()
    {
        // 플레이어 방향 계산 (Y축 무시)
        Vector3 direction = (player.position - transform.position);
        direction.y = 0f;
        direction.Normalize();

        // 이동
        rb.MovePosition(transform.position + direction * moveSpeed * Time.fixedDeltaTime);

        // 플레이어 방향으로 회전
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        }
    }

    // ============================================================
    // 공격
    // ============================================================
    void AttackPlayer()
    {
        // 쿨타임 확인
        if (Time.time < nextAttackTime)
            return;

        // 공격 실행
        if (ShooterHealthNet.Instance != null)
        {
            ShooterHealthNet.Instance.MasterApplyDamageToShooter(Mathf.RoundToInt(attackDamage));
        }

        // 다음 공격 시간 설정
        nextAttackTime = Time.time + attackCooldown;
    }

    // ============================================================
    // 디버그용 (Scene View에서 공격 범위 표시)
    // ============================================================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}