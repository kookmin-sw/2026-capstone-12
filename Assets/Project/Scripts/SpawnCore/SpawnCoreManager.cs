using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SpawnCoreManager : MonoBehaviour
{
    // 스폰 코어 상태 관리 싱글턴
    public static SpawnCoreManager Instance { get; private set; }

    [Header("Events")]
    // 코어 파괴 알림 이벤트
    public UnityEventInt OnSpawnCoreDestroyed = new UnityEventInt();
    // 난이도 변경 알림 이벤트
    public UnityEventInt OnSpawnCoreDifficultyChanged = new UnityEventInt();

    // 파괴 완료 코어 ID 기록
    private readonly HashSet<int> destroyedCoreIds = new HashSet<int>();
    private readonly HashSet<int> activatedCoreIds = new HashSet<int>(); // 활성화 완료 코어 ID 기록
    private int totalCoreCount = 0; // 씬의 전체 코어 수

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

    // 코어 피격 가능 여부 판정
    public bool CanDamageCore(SpawnCore core)
    {
        return core != null && core.IsSpawnActivated && IsCoreUnlocked(core);
    }

    // 코어 파괴 잠금 해제 판정
    public bool IsCoreUnlocked(SpawnCore core)
    {
        if (core == null)
            return false;

        // 선행 파괴 수 기반 잠금 해제 판정
        return DestroyedCoreCount >= core.RequiredDestroyedCoreCountToUnlock;
    }

    // 코어 활성화 가능 여부 판정
    public bool CanActivateCore(SpawnCore core)
    {
        if (core == null || core.IsSpawnActivated)
            return false;

        int coreId = GetCoreUniqueId(core);
        int activatedCount = activatedCoreIds.Contains(coreId) ? activatedCoreIds.Count - 1 : activatedCoreIds.Count;
        return activatedCount >= core.RequiredActivatedCoreCountToActivate;
    }

    // 코어 파괴 상태 반영
    public void HandleCoreDestroyed(SpawnCore core)
    {
        if ((PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) || core == null)
            return;

        int coreId = GetCoreUniqueId(core);
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

    // 적 스폰 가능 코어 목록 조회
    public List<SpawnCore> GetSpawnableCores()
    {
        SpawnCore[] allCores = FindObjectsOfType<SpawnCore>();
        List<SpawnCore> result = new List<SpawnCore>();

        for (int i = 0; i < allCores.Length; i++)
        {
            SpawnCore core = allCores[i];
            if (core == null || !core.isActiveAndEnabled || !core.CanSpawnEnemies)
                continue;

            result.Add(core);
        }

        result.Sort((left, right) => left.CoreOrder.CompareTo(right.CoreOrder));
        return result;
    }

    // 액티베이터 입력 기반 코어 활성화 요청
    public void RequestActivateCore(SpawnCore core, Vector3 activatorPosition, float activationDistance, int requesterViewId, Vector3 fallbackRequesterPosition)
    {
        if (!CanActivateCore(core))
            return;

        if (!PhotonNetwork.IsConnected)
        {
            ActivateCoreLocal(GetCoreUniqueId(core));
            return;
        }

        if (SpawnCoreNet.Instance == null)
        {
            Debug.LogWarning($"{nameof(SpawnCoreManager)}: SpawnCoreNet is missing. Core activation request was ignored.", this);
            return;
        }

        SpawnCoreNet.Instance.RequestActivateCore(core, activatorPosition, activationDistance, requesterViewId, fallbackRequesterPosition);
    }

    // 코어 네트워크/로컬 식별자 조회
    public int GetCoreUniqueId(SpawnCore core)
    {
        PhotonView corePhotonView = core.photonView != null ? core.photonView : core.GetComponent<PhotonView>();
        if (corePhotonView != null && corePhotonView.ViewID != 0)
            return corePhotonView.ViewID;

        return core.GetInstanceID();
    }

    // 코어 활성화 로컬 반영
    public void ActivateCoreLocal(int coreId)
    {
        SpawnCore core = FindCoreByUniqueId(coreId);
        if (core == null)
            return;

        activatedCoreIds.Add(coreId);
        core.SetSpawnActivated(true);

        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
            EnemyManager.Instance?.BeginEnemySystem();
    }

    // 코어 ID 기반 조회
    public SpawnCore FindCoreByUniqueId(int coreId)
    {
        SpawnCore[] allCores = FindObjectsOfType<SpawnCore>();
        for (int i = 0; i < allCores.Length; i++)
        {
            SpawnCore core = allCores[i];
            if (core != null && GetCoreUniqueId(core) == coreId)
                return core;
        }

        return null;
    }

    // 활성화된 스폰 코어 존재 여부 조회
    public bool HasActivatedSpawnCore()
    {
        SpawnCore[] allCores = FindObjectsOfType<SpawnCore>();
        for (int i = 0; i < allCores.Length; i++)
        {
            SpawnCore core = allCores[i];
            if (core != null && core.CanSpawnEnemies)
                return true;
        }

        return false;
    }

    // 코어 파괴 동기화 결과 적용
    public void ApplyCoreDestroyedNotification(int coreOrder, int destroyedCount, int coreId)
    {
        // 파괴된 코어 순서 기록
        destroyedCoreIds.Add(coreId);

        // 코어 파괴 이벤트 전파
        OnSpawnCoreDestroyed.Invoke(coreOrder);
        OnSpawnCoreDifficultyChanged.Invoke(destroyedCount);

        // 난이도 placeholder 반영
        EnemyManager.Instance?.ApplySpawnCoreDifficulty(destroyedCount);

        if (AreAllCoresDestroyed)
            GameManager.Instance?.TriggerVictory();
    }

}
