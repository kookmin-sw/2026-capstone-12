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

    [Header("Structure Attack")]
    [SerializeField] private LayerMask structureMask;   // 건물 레이어
    [SerializeField] private float structureDetectDistance = 1.8f;  // 전방 감지 거리(attackRange보다 약간 크게)
    [SerializeField] private Vector3 boxHalfExtents = new Vector3(0.3f, 1.5f, 0.3f);  // BoxCast 두께

    // Components
    private Transform player;                                // 플레이어 Transform
    private Rigidbody rb;
    private PhotonView pv;
    private CapsuleCollider col;
    private EnemyAnimationNet animationNet;

    // State
    private float nextAttackTime = 0f;

    // 건물 공격 관련 변수
    private Transform structureTarget;
    private float structureTargetExpireTime;

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Awake()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        animationNet = GetComponent<EnemyAnimationNet>();
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

        // 플레이어 방향 계산
        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;
        Vector3 dir = toPlayer.normalized;

        // 전방 구조물 감지
        UpdateStructureTarget(dir);

        // 타겟 선택: 구조물 없으면 플레이어
        Transform target = GetCurrentTarget();

        Vector3 origin = (col != null) ? transform.TransformPoint(col.center) : transform.position + Vector3.up * 1.0f;
        float distance = Vector3.Distance(origin, target.position);

        if (distance > attackRange)
        {
            animationNet?.SetMoveState(true, moveSpeed);
            MoveTowards(target);
            // 애니메이션 동기화
        }
        else
        {
            animationNet?.SetMoveState(false, 0);
            TryAttack(target);
        }
    }

    // ============================================================
    // 전방 감지
    // ============================================================
    private void UpdateStructureTarget(Vector3 dirToPlayer)
    {
        Vector3 origin = (col != null) ? transform.TransformPoint(col.center) : transform.position + Vector3.up * 1.0f;

        // 플레이어 방향으로 전방 BoxCast
        if (Physics.BoxCast(origin, boxHalfExtents, dirToPlayer, out RaycastHit hit,
                Quaternion.LookRotation(dirToPlayer), structureDetectDistance, structureMask, QueryTriggerInteraction.Ignore))
        {
            Transform t = hit.collider.transform;
            structureTarget = t;
        }
        else
        {
            structureTarget = null;
        }
    }

    private Transform GetCurrentTarget()
    {
        if (structureTarget != null)
            return structureTarget;

        return player;
    }

    // ============================================================
    // 이동
    // ============================================================
    void MoveTowards(Transform target)
    {
        Vector3 direction = (target.position - transform.position);
        direction.y = 0f;
        direction.Normalize();

        rb.MovePosition(transform.position + direction * moveSpeed * Time.fixedDeltaTime);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        }
    }

    // ============================================================
    // 공격
    // ============================================================
    private void TryAttack(Transform target)
    {
        if (target == null || target.gameObject == null)
            return;

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        int attackIndex = Random.Range(0, 4);
        animationNet.PlayAttack(attackIndex, attackCooldown);

        if (target == player) AttackPlayer();
        else AttackStructure(target);
    }
    void AttackPlayer()
    {
        // 공격 실행
        ShooterHealthNet.Instance?.MasterApplyDamageToShooter(Mathf.RoundToInt(attackDamage));
    }

    private void AttackStructure(Transform target)
    {
        if (target == null || target.gameObject == null)
            return;

        BuildingHealthNet healthNet = target.GetComponentInParent<BuildingHealthNet>();
        healthNet?.MasterTakeDamage(attackDamage);
    }

    // ============================================================
    // 디버그용 (Scene View에서 공격 범위 표시)
    // ============================================================
    void OnDrawGizmosSelected()
    {
        Vector3 origin = (col != null) ? transform.TransformPoint(col.center) : transform.position + Vector3.up * 1.0f;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRange);

        // boxCast(적 유닛 구조물 탐지 거리)
        Vector3 dir = transform.forward;

        float dist = structureDetectDistance;
        Vector3 half = boxHalfExtents;

        Quaternion rot = Quaternion.LookRotation(dir);

        // 시작 박스
        Gizmos.matrix = Matrix4x4.TRS(origin, rot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, half * 2f);

        // 끝 박스
        Vector3 end = origin + dir * dist;
        Gizmos.matrix = Matrix4x4.TRS(end, rot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, half * 2f);

        // 스윕 구간
        Vector3 mid = origin + dir * (dist * 0.5f);
        Gizmos.matrix = Matrix4x4.TRS(mid, rot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(half.x * 2f, half.y * 2f, dist));
        
        Gizmos.matrix = Matrix4x4.identity;
    }
}