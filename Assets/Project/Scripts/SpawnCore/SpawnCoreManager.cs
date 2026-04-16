using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SpawnCoreManager : MonoBehaviour
{
    public static SpawnCoreManager Instance { get; private set; }

    [Header("Events")]
    public UnityEventInt OnSpawnCoreDestroyed = new UnityEventInt(); // Core 파괴 알림 이벤트
    public UnityEventInt OnSpawnCoreDifficultyChanged = new UnityEventInt(); // 난이도 변경 알림 이벤트

    private readonly HashSet<int> destroyedCoreIds = new HashSet<int>(); // 파괴 완료 Core ID 기록
    private int totalCoreCount = 0; // 씬의 전체 Core 수

    public int DestroyedCoreCount => destroyedCoreIds.Count;
    public bool AreAllCoresDestroyed => totalCoreCount > 0 && DestroyedCoreCount >= totalCoreCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        totalCoreCount = FindObjectsOfType<SpawnCore>().Length;
    }

    /// <summary>
    /// Core 공략 순서 잠금 기반 공격 가능 여부 판정
    /// </summary>
    public bool CanDamageCore(SpawnCore core)
    {
        return core != null && IsCoreUnlocked(core);
    }

    /// <summary>
    /// 선행 파괴 수 기반 Core 잠금 해제 여부 판정
    /// </summary>
    public bool IsCoreUnlocked(SpawnCore core)
    {
        if (core == null)
            return false;

        return DestroyedCoreCount >= core.RequiredDestroyedCoreCountToUnlock;
    }

    /// <summary>
    /// Core 파괴 상태를 MasterClient 기준으로 반영
    /// </summary>
    public void HandleCoreDestroyed(SpawnCore core)
    {
        if ((PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) || core == null)
            return;

        int coreId = GetCoreUniqueId(core); // 네트워크 또는 로컬 Core 식별자
        if (!destroyedCoreIds.Add(coreId))
            return;

        if (!PhotonNetwork.IsConnected)
        {
            ApplyCoreDestroyedNotification(core.CoreOrder, DestroyedCoreCount, coreId);
            return;
        }

        if (SpawnCoreNet.Instance == null)
        {
            Debug.LogWarning($"{nameof(SpawnCoreManager)}: SpawnCoreNet is missing. Core destruction will not be synchronized.", this);
            ApplyCoreDestroyedNotification(core.CoreOrder, DestroyedCoreCount, coreId);
            return;
        }

        SpawnCoreNet.Instance.NotifyCoreDestroyed(core.CoreOrder, DestroyedCoreCount, coreId);
    }

    /// <summary>
    /// Core 네트워크/로컬 식별자 조회
    /// </summary>
    public int GetCoreUniqueId(SpawnCore core)
    {
        PhotonView corePhotonView = core.photonView != null ? core.photonView : core.GetComponent<PhotonView>(); // 네트워크 Core 식별자
        if (corePhotonView != null && corePhotonView.ViewID != 0)
            return corePhotonView.ViewID;

        return core.GetInstanceID();
    }

    /// <summary>
    /// Core 파괴 동기화 결과 적용
    /// </summary>
    public void ApplyCoreDestroyedNotification(int coreOrder, int destroyedCount, int coreId)
    {
        destroyedCoreIds.Add(coreId);

        OnSpawnCoreDestroyed.Invoke(coreOrder);
        OnSpawnCoreDifficultyChanged.Invoke(destroyedCount);

        EnemyManager.Instance?.ApplySpawnCoreDifficulty(destroyedCount);

        if (AreAllCoresDestroyed)
            GameManager.Instance?.TriggerVictory();
    }
}
