using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip[] attackClips;

    [Header("Move Settings")]
    [SerializeField] private float baseWalkSpeed = 3f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int DeathIndexHash = Animator.StringToHash("DeathIndex");
    private static readonly int MoveAnimationSpeedHash = Animator.StringToHash("MoveAnimationSpeed");
    private static readonly int AttackAnimationSpeedHash = Animator.StringToHash("AttackAnimationSpeed");

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // 화면 밖 적의 본 연산 생략 (화면 안 적만 스키닝 계산)
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
    }

    public void SetMove(bool isMoving, float currentMoveSpeed)
    {
        float moveAnimationSpeed = 1f;

        if (isMoving && baseWalkSpeed > 0.0001f)
            moveAnimationSpeed = Mathf.Max(0.01f, currentMoveSpeed / baseWalkSpeed);

        animator.SetFloat(SpeedHash, isMoving ? 1f : 0f);
        animator.SetFloat(MoveAnimationSpeedHash, moveAnimationSpeed);
    }

    public float GetAttackAnimationSpeed(int attackIndex, float attackCooldown)
    {
        if (attackCooldown <= 0.0001f)
            return 1f;

        if (attackClips == null || attackIndex < 0 || attackIndex >= attackClips.Length)
            return 1f;

        AnimationClip clip = attackClips[attackIndex];
        if (clip == null || clip.length <= 0.0001f)
            return 1f;

        return Mathf.Max(0.01f, clip.length / attackCooldown);
    }

    public void PlayAttack(int attackIndex, float moveAnimationSpeed = 1f)
    {
        animator.SetFloat(AttackAnimationSpeedHash, moveAnimationSpeed);
        animator.SetInteger(AttackIndexHash, attackIndex);
        animator.SetTrigger(AttackHash);
    }

    public void SetDead()
    {
        animator.SetFloat(SpeedHash, 0f);

        // Trigger 대신 CrossFade로 직접 전환 (전환 중 trigger 소실 방지)
        string deathState = Random.Range(0, 2) == 0 ? "death1" : "death2";
        animator.CrossFade(deathState, 0.1f, 0);
    }
}