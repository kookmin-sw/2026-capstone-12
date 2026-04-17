using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class ShooterWeaponNet : MonoBehaviourPun
{
    // 히트 결과만 전송하기 때문에 검증은 약함.
    public void RequestHitEnemy(int enemyViewId, float damage)
    {
        if (!photonView.IsMine) return;

        photonView.RPC(nameof(RpcRequestHitEnemy), RpcTarget.MasterClient, enemyViewId, damage);
    }

    [PunRPC]
    private void RpcRequestHitEnemy(int enemyViewId, float damage)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (EnemyHealthNet.Instance != null)
            EnemyHealthNet.Instance.MasterApplyDamage(enemyViewId, damage);
    }

    public void RequestHitStructure(int structureViewId, float damage)
    {
        if (!photonView.IsMine) return;

        photonView.RPC(nameof(RpcRequestHitStructure), RpcTarget.MasterClient, structureViewId, damage);
    }

    [PunRPC]
    private void RpcRequestHitStructure(int structureViewId, float damage)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView structurePv = PhotonView.Find(structureViewId);
        if (structurePv == null) return;

        BuildingHealthNet buildingHealth = structurePv.GetComponent<BuildingHealthNet>();
        if (buildingHealth == null) return;

        bool isDamageableStructure =
            structurePv.GetComponent<SpawnCore>() != null ||
            structurePv.GetComponent<EnemyNest>() != null;

        if (!isDamageableStructure) return;

        buildingHealth.MasterTakeDamage(damage);
    }
}
