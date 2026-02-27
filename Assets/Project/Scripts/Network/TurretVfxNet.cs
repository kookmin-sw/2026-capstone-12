using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class TurretVfxNet : GhostDisabledBehaviour
{
    private PhotonView pv;
    private TurretShootVfx vfx;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        vfx = GetComponent<TurretShootVfx>();   
    }

    public void BroadcastShotFx()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 마스터는 이미 로컬에서 VFX를 재생했을 테니 Others로만 전파
        pv.RPC(nameof(RpcPlayShotFx), RpcTarget.All);
    }

    [PunRPC]
    private void RpcPlayShotFx()
    {
        if (vfx != null)
            vfx.PlayShootVfx();
    }
}