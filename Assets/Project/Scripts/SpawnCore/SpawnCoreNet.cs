using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class SpawnCoreNet : MonoBehaviourPunCallbacks
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

    // 마스터 변경 후 스폰 루프 재시작
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.LocalPlayer == newMasterClient && SpawnCoreManager.Instance != null && SpawnCoreManager.Instance.HasActivatedSpawnCore())
            EnemyManager.Instance?.BeginEnemySystem();
    }

    // 액티베이터 기반 코어 활성화 요청 전송
    public void RequestActivateCore(SpawnCore core, Vector3 activatorPosition, float activationDistance, int requesterViewId, Vector3 fallbackRequesterPosition)
    {
        if (core == null)
            return;

        PhotonView corePhotonView = core.photonView != null ? core.photonView : core.GetComponent<PhotonView>();
        if (corePhotonView == null || corePhotonView.ViewID == 0)
        {
            Debug.LogWarning($"{nameof(SpawnCoreNet)}: Target SpawnCore needs a valid PhotonView ViewID.", core);
            return;
        }

        photonView.RPC(nameof(RPC_RequestActivateCore), RpcTarget.MasterClient, corePhotonView.ViewID, activatorPosition, activationDistance, requesterViewId, fallbackRequesterPosition);
    }

    // 코어 파괴 상태 전파
    public void NotifyCoreDestroyed(int coreOrder, int destroyedCount, int coreId)
    {
        photonView.RPC(nameof(RPC_NotifyCoreDestroyed), RpcTarget.All, coreOrder, destroyedCount, coreId);
    }

    // 마스터 클라이언트 활성화 검증
    [PunRPC]
    private void RPC_RequestActivateCore(int coreId, Vector3 activatorPosition, float activationDistance, int requesterViewId, Vector3 fallbackRequesterPosition, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || SpawnCoreManager.Instance == null)
            return;

        SpawnCore core = SpawnCoreManager.Instance.FindCoreByUniqueId(coreId);
        if (!SpawnCoreManager.Instance.CanActivateCore(core))
            return;

        Vector3 requesterPosition = fallbackRequesterPosition;
        PhotonView requesterView = PhotonView.Find(requesterViewId);
        if (requesterView != null)
            requesterPosition = requesterView.transform.position;

        if (Vector3.Distance(activatorPosition, requesterPosition) > activationDistance)
            return;

        photonView.RPC(nameof(RPC_ActivateCore), RpcTarget.AllBuffered, coreId);
    }

    // 전체 클라이언트 코어 활성화 적용
    [PunRPC]
    private void RPC_ActivateCore(int coreId)
    {
        SpawnCoreManager.Instance?.ActivateCoreLocal(coreId);
    }

    [PunRPC]
    // 코어 파괴 상태 동기화 목적
    private void RPC_NotifyCoreDestroyed(int coreOrder, int destroyedCount, int coreId)
    {
        SpawnCoreManager.Instance?.ApplyCoreDestroyedNotification(coreOrder, destroyedCount, coreId);
    }
}
