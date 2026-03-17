using Photon.Pun;
using UnityEngine;

public class EnemyAnimationNet : MonoBehaviourPun
{
    [SerializeField] private EnemyAnimationController controller;

    private bool isMoving;
    private bool isDead;
    private float lastMoveSpeed;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<EnemyAnimationController>();
    }

    public void SetMoveState(bool moving , float currentMoveSpeed)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (isDead)
            return;

        bool sameMoving = isMoving == moving;
        bool sameSpeed = Mathf.Abs(lastMoveSpeed - currentMoveSpeed) < 0.01f;

        if (sameMoving && sameSpeed)
            return;

        photonView.RPC(nameof(RPC_SetMoveState), RpcTarget.All, moving, currentMoveSpeed);
    }

    public void PlayAttack(int attackIndex, float attackCooldown)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (isDead)
            return;
        
        float attackAnimationSpeed = controller.GetAttackAnimationSpeed(attackIndex, attackCooldown);

        photonView.RPC(nameof(RPC_PlayAttack), RpcTarget.All, attackIndex, attackAnimationSpeed);
    }

    public void PlayDeath()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (isDead)
            return;

        photonView.RPC(nameof(RPC_PlayDeath), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_SetMoveState(bool moving, float currentMoveSpeed)
    {
        if (isDead)
            return;

        isMoving = moving;
        lastMoveSpeed = currentMoveSpeed;
        controller.SetMove(moving, currentMoveSpeed);
    }

    [PunRPC]
    private void RPC_PlayAttack(int attackIndex, float attackAnimationSpeed)
    {
        if (isDead)
            return;

        Debug.Log($"attackIndex: {attackIndex}");
        Debug.Log($"attackAnimationSpeed: {attackAnimationSpeed}");
        controller.PlayAttack(attackIndex, attackAnimationSpeed);
    }

    [PunRPC]
    private void RPC_PlayDeath()
    {
        if (isDead)
            return;

        isDead = true;
        controller.SetDead();
    }
}