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
            // 오프라인 테스트에서도 Core 위치 기반 영구 정화 구역을 생성
            ApplyCoreDestroyedNotification(core.CoreOrder, DestroyedCoreCount, coreId, core.transform.position);
            return;
        }

        if (SpawnCoreNet.Instance == null)
        {
            Debug.LogWarning($"{nameof(SpawnCoreManager)}: SpawnCoreNet is missing. Core destruction will not be synchronized.", this);
            // 중계자가 없어도 Master 로컬 상태에는 Core 위치 기반 영구 정화 구역을 반영
            ApplyCoreDestroyedNotification(core.CoreOrder, DestroyedCoreCount, coreId, core.transform.position);
            return;
        }

        // 영구 정화 구역은 각 클라이언트에서 로컬 생성하므로 Core 위치를 함께 전파
        SpawnCoreNet.Instance.NotifyCoreDestroyed(core.CoreOrder, DestroyedCoreCount, coreId, core.transform.position);
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
        ApplyCoreDestroyedNotification(coreOrder, destroyedCount, coreId, Vector3.zero, false);
    }

    public void ApplyCoreDestroyedNotification(int coreOrder, int destroyedCount, int coreId, Vector3 corePosition)
    {
        ApplyCoreDestroyedNotification(coreOrder, destroyedCount, coreId, corePosition, true);
    }

    // 기존 난이도/승리 알림과 영구 정화 구역 생성을 같은 파괴 확정 이벤트에서 처리
    private void ApplyCoreDestroyedNotification(int coreOrder, int destroyedCount, int coreId, Vector3 corePosition, bool createPurifiedZone)
    {
        destroyedCoreIds.Add(coreId);

        if (createPurifiedZone)
            PermanentPurifiedZoneNet.SpawnOrUpdate(coreId, corePosition);

        OnSpawnCoreDestroyed.Invoke(coreOrder);
        OnSpawnCoreDifficultyChanged.Invoke(destroyedCount);

        EnemyManager.Instance?.ApplySpawnCoreDifficulty(destroyedCount);

        if (AreAllCoresDestroyed)
            GameManager.Instance?.TriggerVictory();
    }
}
