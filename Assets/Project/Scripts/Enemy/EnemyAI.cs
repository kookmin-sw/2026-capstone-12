using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// 적 AI
/// - 슈터를 향해 이동
/// - 슈터에게 도달하면 공격
/// </summary>
public class EnemyAI : MonoBehaviour
{
    private struct SlowState
    {
        public float moveSpeedMultiplier;
        public float attackSpeedMultiplier;
        public float expireTime;
    }

    // ============================================================
    // 변수
    // ============================================================
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;          // 기본 이동 속도
    [SerializeField] private float attackRange = 1.5f;      // 공격 범위
    [SerializeField] private float attackCooldown = 1f;     // 기본 공격 쿨타임
    [SerializeField] private bool useNavMeshMovement = true; // NavMesh 경로 탐색 사용 여부
    [SerializeField] private float pathRefreshInterval = 0.2f; // 목적지 갱신 간격
    [SerializeField] private float navMeshSampleDistance = 2f; // 타겟 주변 NavMesh 검색 반경

    [Header("Combat")]
    [SerializeField] private float attackDamage = 5f;       // 공격 데미지

    [Header("Structure Attack")]
    [SerializeField] private LayerMask structureMask;   // 건물 레이어
    [SerializeField] private float structureDetectDistance = 1.8f;  // 전방 감지 거리(attackRange보다 약간 크게)
    [SerializeField] private Vector3 boxHalfExtents = new Vector3(0.3f, 1.5f, 0.3f);  // BoxCast 두께

    [Header("Aggro Target")]
    [SerializeField] private float aggroDetectRadius = 6f; // 슈터와 어그로 대상 구조물을 임시 목표로 인식하는 반경
    [SerializeField] private float aggroScanInterval = 0.25f; // 주변 대상 탐색 부하를 줄이기 위한 스캔 간격

    // Components
    private Transform player;                                // 플레이어 Transform
    private Rigidbody rb;
    private PhotonView pv;
    private CapsuleCollider col;
    private EnemyAnimationNet animationNet;
    private NavMeshAgent agent;

    // State
    // 난이도 배율 반복 적용 시 누적 방지를 위한 프리팹 원본 스탯 보관
    private float baseMoveSpeed;
    private float baseAttackCooldown;
    private float baseAttackDamage;
    private float difficultyMultiplier = 1f;
    private float nextAttackTime = 0f;
    private float currentMoveSpeed;
    private float currentAttackCooldown;
    private float nextPathRefreshTime;
    private readonly Dictionary<int, SlowState> activeSlows = new Dictionary<int, SlowState>();

    // 건물 공격 관련 변수
    private Transform structureTarget;
    private float structureTargetExpireTime;
    private Transform aggroTarget; // CommandTower보다 우선하지만 길막 구조물보다는 낮은 우선순위의 임시 목표
    private float nextAggroScanTime;
    private readonly Collider[] aggroHits = new Collider[16]; // OverlapSphereNonAlloc 재사용 버퍼

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Awake()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        animationNet = GetComponent<EnemyAnimationNet>();
        agent = GetComponent<NavMeshAgent>();

        if (agent == null && useNavMeshMovement)
            agent = gameObject.AddComponent<NavMeshAgent>();

        baseMoveSpeed = moveSpeed;
        baseAttackCooldown = attackCooldown;
        baseAttackDamage = attackDamage;
        ApplyDifficultyValues();
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        ConfigureMovementAuthority();

        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
            health.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
            health.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (agent != null)
            agent.enabled = false;

        if (rb != null && !rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void FixedUpdate()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        // 사망 시 이동/공격 중단
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null && health.IsDead)
            return;

        UpdateSlowState();
        UpdateCurrentStats();

        // 주 목표 방향 계산
        Transform primaryTarget = GetPrimaryTarget();
        if (primaryTarget == null)
            return;

        // 주 목표 방향을 기준으로 이동 중간의 구조물을 감지
        Vector3 toPrimaryTarget = (primaryTarget.position - transform.position);
        toPrimaryTarget.y = 0f;
        if (toPrimaryTarget.sqrMagnitude < 0.0001f) return;
        Vector3 dir = toPrimaryTarget.normalized;

        // 전방 구조물 감지
        UpdateStructureTarget(dir);
        UpdateAggroTarget();

        // 타겟 선택: 구조물 없으면 주 목표
        Transform target = GetCurrentTarget(primaryTarget);

        Vector3 origin = (col != null) ? transform.TransformPoint(col.center) : transform.position + Vector3.up * 1.0f;
        float distance = GetDistanceToTarget(origin, target);

        if (distance > attackRange)
        {
            animationNet?.SetMoveState(true, currentMoveSpeed);
            MoveTowards(target);
        }
        else
        {
            animationNet?.SetMoveState(false, 0);
            StopMovement();
            FaceTarget(target);
            TryAttack(target);
        }
    }

    // ============================================================
    // 슬로우
    // ============================================================
    public void ApplySlow(int sourceId, float moveSpeedMultiplier, float attackSpeedMultiplier, float duration)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        activeSlows[sourceId] = new SlowState
        {
            moveSpeedMultiplier = Mathf.Clamp(moveSpeedMultiplier, 0.01f, 1f),
            attackSpeedMultiplier = Mathf.Clamp(attackSpeedMultiplier, 0.01f, 1f),
            expireTime = Time.time + Mathf.Max(0.01f, duration)
        };
    }

    public void ApplyDifficultyMultiplier(float multiplier)
    {
        multiplier = Mathf.Max(0.01f, multiplier);

        // 네트워크 방에서 Enemy PhotonView를 통한 모든 클라이언트 동일 배율 적용
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient && pv != null)
        {
            pv.RPC(nameof(RpcApplyDifficultyMultiplier), RpcTarget.AllBuffered, multiplier);
            return;
        }

        ApplyDifficultyMultiplierLocal(multiplier);
    }

    [PunRPC]
    private void RpcApplyDifficultyMultiplier(float multiplier)
    {
        ApplyDifficultyMultiplierLocal(multiplier);
    }

    private void ApplyDifficultyMultiplierLocal(float multiplier)
    {
        difficultyMultiplier = Mathf.Max(0.01f, multiplier);
        ApplyDifficultyValues();

        EnemyHealth health = GetComponent<EnemyHealth>();
        health?.ApplyDifficultyMultiplier(difficultyMultiplier);
    }

    public void RemoveSlow(int sourceId)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        activeSlows.Remove(sourceId);
    }

    private void UpdateSlowState()
    {
        if (activeSlows.Count == 0)
            return;

        List<int> expiredKeys = null;
        foreach (KeyValuePair<int, SlowState> pair in activeSlows)
        {
            if (pair.Value.expireTime > Time.time)
                continue;

            expiredKeys ??= new List<int>();
            expiredKeys.Add(pair.Key);
        }

        if (expiredKeys == null)
            return;

        for (int i = 0; i < expiredKeys.Count; i++)
            activeSlows.Remove(expiredKeys[i]);
    }

    private void UpdateCurrentStats()
    {
        float moveSpeedMultiplier = 1f;
        float attackSpeedMultiplier = 1f;

        foreach (KeyValuePair<int, SlowState> pair in activeSlows)
        {
            moveSpeedMultiplier = Mathf.Min(moveSpeedMultiplier, pair.Value.moveSpeedMultiplier);
            attackSpeedMultiplier = Mathf.Min(attackSpeedMultiplier, pair.Value.attackSpeedMultiplier);
        }

        currentMoveSpeed = moveSpeed * moveSpeedMultiplier;
        currentAttackCooldown = attackCooldown / Mathf.Max(0.01f, attackSpeedMultiplier);

        if (agent != null)
            agent.speed = currentMoveSpeed;
    }

    private void ApplyDifficultyValues()
    {
        // 공격속도 증가의 쿨다운 감소 표현
        moveSpeed = baseMoveSpeed * difficultyMultiplier;
        attackCooldown = baseAttackCooldown / difficultyMultiplier;
        attackDamage = baseAttackDamage * difficultyMultiplier;

        currentMoveSpeed = moveSpeed;
        currentAttackCooldown = attackCooldown;

        if (agent != null)
            agent.speed = currentMoveSpeed;
    }

    private void ConfigureMovementAuthority()
    {
        bool masterControlled = PhotonNetwork.IsMasterClient;

        if (agent != null)
        {
            agent.enabled = masterControlled && useNavMeshMovement;
            agent.speed = currentMoveSpeed;
            agent.stoppingDistance = 0f;
            agent.updateRotation = false;
            agent.autoBraking = false;
        }

        if (rb != null)
        {
            bool usingAgent = agent != null && agent.enabled;
            rb.isKinematic = !masterControlled || usingAgent;
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

    private Transform GetPrimaryTarget()
    {
        // CommandTower가 없는 상태는 게임 종료 상태로 보고 기본 목표를 대체하지 않음
        return CommandTower.ActiveTarget;
    }

    private Transform GetCurrentTarget(Transform primaryTarget)
    {
        // 길을 직접 막는 구조물은 펜스 여부와 관계없이 먼저 처리
        if (structureTarget != null)
            return structureTarget;

        // marker가 붙은 구조물과 슈터만 반경 어그로 대상으로 전환
        if (aggroTarget != null)
            return aggroTarget;

        return primaryTarget;
    }

    private void UpdateAggroTarget()
    {
        if (Time.time < nextAggroScanTime)
            return;

        nextAggroScanTime = Time.time + Mathf.Max(0.01f, aggroScanInterval);

        float radiusSqr = aggroDetectRadius * aggroDetectRadius;
        Transform bestTarget = null;
        float bestDistanceSqr = float.MaxValue;

        // 슈터가 늦게 생성되거나 재참조가 끊긴 경우를 보정
        TryRefreshPlayerTarget();

        if (IsValidPlayerAggroTarget(player))
        {
            float playerDistanceSqr = (player.position - transform.position).sqrMagnitude;
            if (playerDistanceSqr <= radiusSqr)
            {
                bestTarget = player;
                bestDistanceSqr = playerDistanceSqr;
            }
        }

        // EnemyAggroTarget marker가 붙은 구조물만 반경 어그로 후보로 인정
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, aggroDetectRadius, aggroHits, structureMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = aggroHits[i];
            if (hit == null)
                continue;

            EnemyAggroTarget candidate = hit.GetComponentInParent<EnemyAggroTarget>();
            if (!IsValidStructureAggroTarget(candidate))
                continue;

            Transform candidateTransform = candidate.transform;
            float distanceSqr = (candidateTransform.position - transform.position).sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr)
                continue;

            bestTarget = candidateTransform;
            bestDistanceSqr = distanceSqr;
        }

        aggroTarget = bestTarget;
    }

    private void TryRefreshPlayerTarget()
    {
        if (player != null)
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private bool IsValidPlayerAggroTarget(Transform target)
    {
        if (target == null)
            return false;

        HealthManager healthManager = target.GetComponent<HealthManager>();
        return healthManager == null || !healthManager.IsDead;
    }

    private bool IsValidStructureAggroTarget(EnemyAggroTarget target)
    {
        if (target == null || !target.isActiveAndEnabled)
            return false;

        // 이미 파괴된 구조물에 어그로가 남지 않도록 체력 상태를 확인
        BuildingHealthNet health = target.GetComponent<BuildingHealthNet>();
        if (health == null)
            health = target.GetComponentInParent<BuildingHealthNet>();

        return health == null || health.CurrentHp > 0f;
    }

    // ============================================================
    // 이동
    // ============================================================
    void MoveTowards(Transform target)
    {
        if (TryMoveWithNavMesh(target))
            return;

        Vector3 direction = (target.position - transform.position);
        direction.y = 0f;
        direction.Normalize();

        rb.MovePosition(transform.position + direction * currentMoveSpeed * Time.fixedDeltaTime);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * GetRotationSpeed());
        }
    }

    // NavMesh가 준비된 맵에서는 장애물을 우회하는 경로로 이동
    private bool TryMoveWithNavMesh(Transform target)
    {
        if (!useNavMeshMovement || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        agent.speed = currentMoveSpeed;

        if (Time.time >= nextPathRefreshTime)
        {
            nextPathRefreshTime = Time.time + pathRefreshInterval;
            agent.SetDestination(GetNavDestination(target));
        }

        Vector3 velocity = agent.desiredVelocity;
        velocity.y = 0f;

        if (velocity.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * GetRotationSpeed());
        }

        return true;
    }

    private void StopMovement()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();
    }

    private void FaceTarget(Transform target)
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.fixedDeltaTime * GetRotationSpeed());
    }

    // 이동속도에 비례한 회전 속도 (빠른 적일수록 회전도 빠르게)
    private float GetRotationSpeed() => 5f * Mathf.Max(1f, currentMoveSpeed / baseMoveSpeed);

    // 건물은 표면까지, 플레이어는 중심까지 거리 측정
    private float GetDistanceToTarget(Vector3 from, Transform target)
    {
        Collider targetCol = target.GetComponent<Collider>() ?? target.GetComponentInChildren<Collider>();
        if (targetCol != null)
            return Vector3.Distance(from, targetCol.ClosestPoint(from));
        return Vector3.Distance(from, target.position);
    }

    // 건물 표면 바깥쪽 NavMesh 지점을 목적지로 설정해 건물 내부로 경로가 잡히지 않게 함
    private Vector3 GetNavDestination(Transform target)
    {
        Collider targetCol = target.GetComponent<Collider>() ?? target.GetComponentInChildren<Collider>();
        if (targetCol != null)
        {
            Vector3 surface = targetCol.ClosestPoint(transform.position);
            Vector3 toEnemy = transform.position - surface;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude < 0.0001f)
                toEnemy = transform.position - target.position;
            Vector3 approachPoint = surface + toEnemy.normalized * 0.1f;
            if (NavMesh.SamplePosition(approachPoint, out NavMeshHit hit, navMeshSampleDistance * 2f, NavMesh.AllAreas))
                return hit.position;
        }

        if (NavMesh.SamplePosition(target.position, out NavMeshHit fallback, navMeshSampleDistance, NavMesh.AllAreas))
            return fallback.position;

        return target.position;
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

        nextAttackTime = Time.time + currentAttackCooldown;

        int attackIndex = Random.Range(0, 4);
        animationNet?.PlayAttack(attackIndex, currentAttackCooldown);

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
