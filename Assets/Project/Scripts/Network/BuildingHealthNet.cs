using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class BuildingHealthNet : MonoBehaviourPun
{
    [Header("HP")]
    [SerializeField] private float maxHp;
    [SerializeField] private float currentHp;

    public float MaxHp => maxHp;
    public float CurrentHp => currentHp;
    public bool IsFullHp => currentHp >= maxHp;

    public void Init(float maxHp)
    {
        this.maxHp = maxHp;
        currentHp = maxHp;
    }

    public void MasterTakeDamage(float damage)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (damage <= 0f) return;

        ApplyDamageInternal(damage);
    }

    public void MasterRepair(float amount)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (amount <= 0f) return;

        float newHp = Mathf.Clamp(currentHp + amount, 0f, maxHp);
        if (Mathf.Approximately(newHp, currentHp)) return;

        currentHp = newHp;
        photonView.RPC(nameof(RpcSetHp), RpcTarget.All, currentHp);
    }

    public void MasterSetHp(float hp)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        currentHp = Mathf.Clamp(hp, 0f, maxHp);
        photonView.RPC(nameof(RpcSetHp), RpcTarget.All, currentHp);

        if (currentHp <= 0f)
            MasterKill();
    }

    private void ApplyDamageInternal(float damage)
    {
        float newHp = currentHp - damage;
        currentHp = Mathf.Clamp(newHp, 0f, maxHp);

        photonView.RPC(nameof(RpcSetHp), RpcTarget.All, currentHp);

        if (currentHp <= 0f)
            MasterKill();
    }

    private void MasterKill()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    private void RpcSetHp(float hp)
    {
        currentHp = Mathf.Clamp(hp, 0f, maxHp);
    }
}