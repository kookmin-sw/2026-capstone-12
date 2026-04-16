using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 적 스폰 관리
/// - 적 생성 처리
/// - 스폰 위치 관리
/// </summary>
public class EnemyManager : MonoBehaviourPunCallbacks
{
    // ============================================================
    // 싱글턴
    // ============================================================
    public static EnemyManager Instance { get; private set; }

    // ============================================================
    // 변수
    // ============================================================
    [Header("Enemy Prefabs")]
    [SerializeField] private string basicEnemyPrefabPath = "Prefabs/Enemies/BasicEnemy";
    [SerializeField] private string tankEnemyPrefabPath  = "Prefabs/Enemies/TankEnemy";
    [SerializeField] private string fastEnemyPrefabPath  = "Prefabs/Enemies/FastEnemy";

    [Header("Group Spawn Settings")]
    [SerializeField] private float baseGroupSpawnInterval = 0.4f; // Core가 파괴되지 않았을 때 Group별 기본 생성 주기
    [SerializeField] private float groupSpawnIntervalReductionPerDestroyedCore = 0.1f; // Core 파괴 수에 따른 Group 생성 주기 감소값
    [SerializeField] private float minimumGroupSpawnInterval = 0.1f; // 마지막 Group 압박을 위한 최소 생성 주기
    // 최대 활성 적 수
    [SerializeField] private int maxActiveEnemies = 500;
    [SerializeField] private bool startAutomatically = true; // 씬 시작 시 스폰 루프 자동 시작 여부

    [Header("Enemy Tier Settings")]
    [SerializeField] private float baseFastEnemyChance = 0.15f; // Core 미파괴 상태의 Fast Enemy 생성 확률
    [SerializeField] private float baseTankEnemyChance = 0.1f; // Core 미파괴 상태의 Tank Enemy 생성 확률
    [SerializeField] private float fastEnemyChancePerDestroyedCore = 0.1f; // Core 파괴 수에 따른 Fast Enemy 확률 증가값
    [SerializeField] private float tankEnemyChancePerDestroyedCore = 0.15f; // Core 파괴 수에 따른 Tank Enemy 확률 증가값

    [Header("Test Settings")]
    [SerializeField] private bool testMode = false;
    [SerializeField] private int testEnemyCount = 5;

    // State
    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private readonly List<EnemyNest> enemyNests = new List<EnemyNest>(); // 현재 사용 가능한 EnemyNest 후보 목록
    private readonly Dictionary<EnemyNestGroup, float> nextGroupSpawnTimes = new Dictionary<EnemyNestGroup, float>(); // Group별 다음 생성 시간
    private int spawnCoreDifficulty = 0;
    private bool enemySystemStarted = false;

    // ============================================================
    // Unity 생명주기
    // ============================================================
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
    /// 씬 배치 Nest 수집과 자동 스폰 시작
    /// </summary>
    private void Start()
    {
        RefreshEnemyNests();

        if (startAutomatically)
            BeginEnemySystem();
    }

    /// <summary>
    /// Photon 방 입장 완료 후 자동 스폰 시작 재시도
    /// </summary>
    public override void OnJoinedRoom()
    {
        if (startAutomatically)
            BeginEnemySystem();
    }

    /// <summary>
    /// MasterClient 변경 후 자동 스폰 권한 재획득 처리
    /// </summary>
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (startAutomatically && PhotonNetwork.LocalPlayer == newMasterClient)
            BeginEnemySystem();
    }

    // ============================================================
    // 네트워크 동기화 테스트용 코드
    // ============================================================
    public void BeginEnemySystem()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom)
            return;

        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            return;

        if (enemySystemStarted)
            return;

        enemySystemStarted = true;

        if (testMode)
            SpawnTestEnemies();

        StartCoroutine(ContinuousSpawnCoroutine());
    }

    // ============================================================
    // 스폰
    // ============================================================
    /// <summary>
    /// 테스트 적 생성
    /// </summary>
    private void SpawnTestEnemies()
    {
        for (int i = 0; i < testEnemyCount; i++)
        {
            string enemyPrefabPath = GetRandomEnemyPrefabPath();
            Vector3 spawnPosition = GetRandomSpawnPosition();
            SpawnEnemy(enemyPrefabPath, spawnPosition);
        }

        Debug.Log($"Spawned {testEnemyCount} test enemies");
    }

    /// <summary>
    /// 살아있는 Group별 공유 주기 기반 지속 생성 루프
    /// </summary>
    private IEnumerator ContinuousSpawnCoroutine()
    {
        yield return null;

        while (true)
        {
            if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
                yield break;

            if (SpawnCoreManager.Instance != null && SpawnCoreManager.Instance.AreAllCoresDestroyed)
                yield break;

            TickSpawnGroups();

            yield return null;
        }
    }

    /// <summary>
    /// 적 스폰
    /// </summary>
    public GameObject SpawnEnemy(string prefabPath, Vector3 position)
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom)
            return null;

        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            return null;

        GameObject enemy;
        if (PhotonNetwork.InRoom)
        {
            enemy = PhotonNetwork.Instantiate(prefabPath, position, Quaternion.identity);
        }
        else
        {
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"EnemyManager: Enemy prefab not found at Resources/{prefabPath}.");
                return null;
            }

            enemy = Instantiate(prefab, position, Quaternion.identity);
        }

        activeEnemies.Add(enemy);
        CombatUIManager.Instance?.SetRemainingEnemyCount(ActiveEnemyCount);
        return enemy;
    }

    /// <summary>
    /// Core 파괴 수만 반영한 Enemy 종류 선택
    /// </summary>
    private string GetRandomEnemyPrefabPath()
    {
        float fastChance = Mathf.Clamp01(baseFastEnemyChance + spawnCoreDifficulty * fastEnemyChancePerDestroyedCore); // Fast Enemy 최종 확률
        float tankChance = Mathf.Clamp01(baseTankEnemyChance + spawnCoreDifficulty * tankEnemyChancePerDestroyedCore); // Tank Enemy 최종 확률
        float totalAdvancedChance = Mathf.Min(0.9f, fastChance + tankChance); // Basic Enemy 최소 여지를 남기는 상위 Enemy 확률 합
        if (fastChance + tankChance > totalAdvancedChance)
        {
            float scale = totalAdvancedChance / (fastChance + tankChance); // 상위 Enemy 확률 합 보정 배율
            fastChance *= scale;
            tankChance *= scale;
        }

        float roll = Random.value; // Enemy 종류 선택 난수
        if (roll < tankChance)
            return tankEnemyPrefabPath;

        if (roll < tankChance + fastChance)
            return fastEnemyPrefabPath;

        return basicEnemyPrefabPath;
    }

    /// <summary>
    /// 랜덤 스폰 위치 가져오기
    /// </summary>
    public Vector3 GetRandomSpawnPosition()
    {
        if (TryGetRandomSpawnPosition(out Vector3 position))
            return position;

        Debug.LogWarning("EnemyManager: No spawnable EnemyNest found.");
        return Vector3.zero;
    }

    /// <summary>
    /// Nest 기반 스폰 위치 조회 성공 여부 반환
    /// </summary>
    public bool TryGetRandomSpawnPosition(out Vector3 position)
    {
        EnemyNestGroup group = GetRandomSpawnGroup(); // 랜덤 위치 조회용 Group
        EnemyNest nest = GetRandomSpawnNestInGroup(group); // 선택된 Group 내 Nest
        if (nest == null)
        {
            position = Vector3.zero;
            return false;
        }

        position = nest.GetSpawnPosition();
        return true;
    }

    /// <summary>
    /// 적 제거 시 리스트에서 삭제
    /// </summary>
    public void RemoveEnemy(GameObject enemy)
    {
        activeEnemies.Remove(enemy);
        CombatUIManager.Instance?.SetRemainingEnemyCount(ActiveEnemyCount);
    }

    public void ApplySpawnCoreDifficulty(int destroyedCoreCount)
    {
        // 코어 파괴 단계 저장
        spawnCoreDifficulty = Mathf.Max(0, destroyedCoreCount);
    }

    /// <summary>
    /// Nest 생성과 활성화 시 Manager 후보 목록 등록
    /// </summary>
    public void RegisterSpawnNest(EnemyNest nest)
    {
        if (nest == null || enemyNests.Contains(nest))
            return;

        enemyNests.Add(nest);
    }

    /// <summary>
    /// Nest 파괴와 비활성화 시 Manager 후보 목록 해제
    /// </summary>
    public void UnregisterSpawnNest(EnemyNest nest)
    {
        if (nest == null)
            return;

        enemyNests.Remove(nest);
    }

    /// <summary>
    /// Core 파괴 시 연결된 Nest 일괄 제거
    /// </summary>
    public void HandleSpawnCoreDestroyed(SpawnCore core)
    {
        if (core == null)
            return;

        List<EnemyNest> linkedNests = GetEnemyNestsForCore(core); // 파괴된 Core 소속 Nest 목록
        for (int i = 0; i < linkedNests.Count; i++)
            linkedNests[i]?.DestroyByOwnerCore();
    }

    /// <summary>
    /// Group별 생성 타이머 진행과 Enemy 생성 실행
    /// </summary>
    private void TickSpawnGroups()
    {
        RefreshEnemyNests();

        List<EnemyNestGroup> groups = GetSpawnableGroups(); // 현재 살아있는 스폰 Group 목록
        PruneGroupTimers(groups);

        float spawnInterval = GetCurrentGroupSpawnInterval(); // Core 파괴 수가 반영된 Group 생성 주기
        for (int i = 0; i < groups.Count; i++)
        {
            EnemyNestGroup group = groups[i]; // 생성 타이머 검사 대상 Group
            if (!nextGroupSpawnTimes.TryGetValue(group, out float nextSpawnTime))
                nextGroupSpawnTimes[group] = Time.time + Random.Range(0f, spawnInterval);

            if (Time.time < nextGroupSpawnTimes[group])
                continue;

            nextGroupSpawnTimes[group] = Time.time + spawnInterval;

            if (ActiveEnemyCount >= maxActiveEnemies)
                continue;

            SpawnFromGroup(group);
        }
    }

    /// <summary>
    /// 특정 Group 안의 살아있는 Nest 하나에서 Enemy 생성
    /// </summary>
    private void SpawnFromGroup(EnemyNestGroup group)
    {
        EnemyNest nest = GetRandomSpawnNestInGroup(group); // Group 내부 생성 담당 Nest
        if (nest == null)
            return;

        string enemyPrefabPath = GetRandomEnemyPrefabPath(); // Core 파괴 수만 반영한 Enemy 프리팹 경로
        SpawnEnemy(enemyPrefabPath, nest.GetSpawnPosition());
    }

    /// <summary>
    /// 테스트 위치 조회용 Group 랜덤 선택
    /// </summary>
    private EnemyNestGroup GetRandomSpawnGroup()
    {
        List<EnemyNestGroup> groups = GetSpawnableGroups(); // 현재 스폰 가능한 Group 후보 목록
        if (groups.Count == 0)
            return null;

        return groups[Random.Range(0, groups.Count)];
    }

    /// <summary>
    /// Group 안에서 가중치 기반 Nest 선택
    /// </summary>
    private EnemyNest GetRandomSpawnNestInGroup(EnemyNestGroup group)
    {
        if (group == null)
            return null;

        List<EnemyNest> nests = GetSpawnableNestsInGroup(group); // Group 내부 스폰 가능 Nest 후보 목록
        if (nests.Count == 0)
            return null;

        float totalWeight = 0f; // 후보 Nest 가중치 합계
        for (int i = 0; i < nests.Count; i++)
            totalWeight += Mathf.Max(0.01f, nests[i].Weight);

        float pick = Random.Range(0f, totalWeight); // 누적 가중치 선택값
        for (int i = 0; i < nests.Count; i++)
        {
            pick -= Mathf.Max(0.01f, nests[i].Weight);
            if (pick > 0f)
                continue;

            return nests[i];
        }

        return nests[nests.Count - 1];
    }

    /// <summary>
    /// 살아있는 Nest를 보유한 Group 목록 조회
    /// </summary>
    private List<EnemyNestGroup> GetSpawnableGroups()
    {
        List<EnemyNestGroup> result = new List<EnemyNestGroup>(); // 필터링된 스폰 가능 Group 목록
        for (int i = 0; i < enemyNests.Count; i++)
        {
            EnemyNest nest = enemyNests[i]; // 검사 대상 Nest
            if (nest == null || !nest.isActiveAndEnabled || !nest.CanSpawnEnemies)
                continue;

            EnemyNestGroup group = nest.Group; // Nest가 속한 공유 주기 Group
            if (group == null || !group.isActiveAndEnabled || !group.IsCoreAlive || result.Contains(group))
                continue;

            result.Add(group);
        }

        return result;
    }

    /// <summary>
    /// 특정 Group의 살아있는 Nest 목록 조회
    /// </summary>
    private List<EnemyNest> GetSpawnableNestsInGroup(EnemyNestGroup group)
    {
        List<EnemyNest> result = new List<EnemyNest>(); // Group 내부 스폰 가능 Nest 목록
        for (int i = 0; i < enemyNests.Count; i++)
        {
            EnemyNest nest = enemyNests[i]; // Group 소속 검사 대상 Nest
            if (nest == null || nest.Group != group || !nest.isActiveAndEnabled || !nest.CanSpawnEnemies)
                continue;

            result.Add(nest);
        }

        return result;
    }

    /// <summary>
    /// 특정 Core에 소속된 Nest 목록 조회
    /// </summary>
    private List<EnemyNest> GetEnemyNestsForCore(SpawnCore core)
    {
        RefreshEnemyNests();

        List<EnemyNest> result = new List<EnemyNest>(); // Core 소속 Nest 목록
        for (int i = 0; i < enemyNests.Count; i++)
        {
            EnemyNest nest = enemyNests[i]; // 소속 비교 대상 Nest
            if (nest == null || nest.OwnerCore != core)
                continue;

            result.Add(nest);
        }

        return result;
    }

    /// <summary>
    /// 빈 Nest 참조 정리와 씬 Nest 재수집
    /// </summary>
    private void RefreshEnemyNests()
    {
        for (int i = enemyNests.Count - 1; i >= 0; i--)
        {
            if (enemyNests[i] == null)
                enemyNests.RemoveAt(i);
        }

        EnemyNest[] sceneNests = FindObjectsOfType<EnemyNest>(); // 씬에 존재하는 Nest 전체
        for (int i = 0; i < sceneNests.Length; i++)
            RegisterSpawnNest(sceneNests[i]);
    }

    /// <summary>
    /// 사라진 Group의 생성 타이머 정리
    /// </summary>
    private void PruneGroupTimers(List<EnemyNestGroup> aliveGroups)
    {
        List<EnemyNestGroup> expiredGroups = null; // 제거할 Group 타이머 목록
        foreach (KeyValuePair<EnemyNestGroup, float> pair in nextGroupSpawnTimes)
        {
            if (pair.Key != null && aliveGroups.Contains(pair.Key))
                continue;

            expiredGroups ??= new List<EnemyNestGroup>();
            expiredGroups.Add(pair.Key);
        }

        if (expiredGroups == null)
            return;

        for (int i = 0; i < expiredGroups.Count; i++)
            nextGroupSpawnTimes.Remove(expiredGroups[i]);
    }

    /// <summary>
    /// Core 파괴 수에 따른 Group 공유 생성 주기 계산
    /// </summary>
    private float GetCurrentGroupSpawnInterval()
    {
        float interval = baseGroupSpawnInterval - (spawnCoreDifficulty * groupSpawnIntervalReductionPerDestroyedCore); // 현재 Group 생성 주기
        return Mathf.Max(minimumGroupSpawnInterval, interval);
    }

    // ============================================================
    // Public 속성
    // ============================================================
    public int ActiveEnemyCount => activeEnemies.Count;
}
