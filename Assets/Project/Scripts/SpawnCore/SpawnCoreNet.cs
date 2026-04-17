using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class SpawnCoreNet : MonoBehaviourPun
{
    public static SpawnCoreNet Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Core 파괴 상태를 모든 클라이언트에 전파
    /// </summary>
    public void NotifyCoreDestroyed(int coreOrder, int destroyedCount, int coreId)
    {
        photonView.RPC(nameof(RPC_NotifyCoreDestroyed), RpcTarget.All, coreOrder, destroyedCount, coreId);
    }

    /// <summary>
    /// Core 파괴 상태 동기화 결과 적용
    /// </summary>
    [PunRPC]
    private void RPC_NotifyCoreDestroyed(int coreOrder, int destroyedCount, int coreId)
    {
        SpawnCoreManager.Instance?.ApplyCoreDestroyedNotification(coreOrder, destroyedCount, coreId);
    }
}
