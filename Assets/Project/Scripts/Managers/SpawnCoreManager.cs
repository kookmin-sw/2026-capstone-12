using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class SpawnCoreManager : MonoBehaviourPun
{
    // 전역 접근 싱글턴
    public static SpawnCoreManager Instance { get; private set; }

    [Header("Events")]
    // 코어 파괴 알림 이벤트
    public UnityEventInt OnSpawnCoreDestroyed = new UnityEventInt();
    // 난이도 변경 알림 이벤트
    public UnityEventInt OnSpawnCoreDifficultyChanged = new UnityEventInt();

    // 파괴 완료 코어 순서 기록
    private readonly HashSet<int> destroyedCoreOrders = new HashSet<int>();

    // 누적 파괴 코어 수
    public int DestroyedCoreCount => destroyedCoreOrders.Count;

    // 싱글턴 초기화 목적
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // 외부 피격 가능 여부 조회 목적
    public bool CanDamageCore(SpawnCore core)
    {
        return core != null && IsCoreUnlocked(core);
    }

    // 코어 잠금 해제 판정 목적
    public bool IsCoreUnlocked(SpawnCore core)
    {
        if (core == null)
            return false;

        // 선행 파괴 수 기반 잠금 해제 판정
        return DestroyedCoreCount >= core.RequiredDestroyedCoreCountToUnlock;
    }

    // 코어 파괴 진행 반영 목적
    public void HandleCoreDestroyed(SpawnCore core)
    {
        if (!PhotonNetwork.IsMasterClient || core == null)
            return;

        if (!destroyedCoreOrders.Add(core.CoreOrder))
            return;

        // 전체 클라이언트 진행 상태 동기화
        photonView.RPC(nameof(RPC_NotifyCoreDestroyed), RpcTarget.All, core.CoreOrder, DestroyedCoreCount);
    }

    [PunRPC]
    // 코어 파괴 상태 동기화 목적
    private void RPC_NotifyCoreDestroyed(int coreOrder, int destroyedCount)
    {
        // 파괴된 코어 순서 기록
        destroyedCoreOrders.Add(coreOrder);

        // 코어 파괴 이벤트 전파
        OnSpawnCoreDestroyed.Invoke(coreOrder);
        OnSpawnCoreDifficultyChanged.Invoke(destroyedCount);

        // 난이도 placeholder 반영
        WaveManager.Instance?.ApplySpawnCoreDifficulty(destroyedCount);
        EnemyManager.Instance?.ApplySpawnCoreDifficulty(destroyedCount);
    }
}
