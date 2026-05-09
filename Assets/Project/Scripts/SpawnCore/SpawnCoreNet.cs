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
    /// Core 파괴 상태와 정화 구역 생성 위치를 모든 클라이언트에 전파
    /// </summary>
    public void NotifyCoreDestroyed(int coreOrder, int destroyedCount, int coreId, Vector3 corePosition)
    {
        photonView.RPC(nameof(RPC_NotifyCoreDestroyed), RpcTarget.All, coreOrder, destroyedCount, coreId, corePosition);
    }

    /// <summary>
    /// Core 파괴 동기화 결과와 로컬 영구 정화 구역 생성을 적용
    /// </summary>
    [PunRPC]
    private void RPC_NotifyCoreDestroyed(int coreOrder, int destroyedCount, int coreId, Vector3 corePosition)
    {
        SpawnCoreManager.Instance?.ApplyCoreDestroyedNotification(coreOrder, destroyedCount, coreId, corePosition);
        AudioManager.Instance?.PlayEnemyBuff();
    }
}
