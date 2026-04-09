using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

/// <summary>
/// 적 스폰 관리
/// - 적 생성 처리
/// - 스폰 위치 관리
/// </summary>
public class EnemyManager : MonoBehaviour
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

    [Header("Spawn Core Settings")]
    // 스폰 반경 보정값
    [SerializeField] private float spawnRadiusPadding = 0.1f;
    // 스폰 높이 보정값
    [SerializeField] private float spawnHeightOffset = 0.5f;

    [Header("Continuous Spawn Settings")]
    // 기본 생성 주기
    [SerializeField] private float baseSpawnInterval = 6f;
    // 코어 파괴당 주기 감소값
    [SerializeField] private float spawnIntervalReductionPerDestroyedCore = 0.75f;
    // 최소 생성 주기
    [SerializeField] private float minimumSpawnInterval = 1.5f;
    // 최대 활성 적 수
    [SerializeField] private int maxActiveEnemies = 20;

    [Header("Test Settings")]
    [SerializeField] private bool testMode = false;
    [SerializeField] private int testEnemyCount = 5;

    // State
    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private readonly List<SpawnCore> spawnCoreSequence = new List<SpawnCore>();
    private int spawnCoreDifficulty = 0;
    private int spawnCoreCursor = 0;
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

    void Start()
    {
        // 마스터 클라이언트만 실행
        if (!PhotonNetwork.IsMasterClient)
            return;

        BeginEnemySystem();
    }

    // ============================================================
    // 네트워크 동기화 테스트용 코드
    // ============================================================
    public void BeginEnemySystem()
    {
        // 마스터 클라이언트만 실행
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (enemySystemStarted)
            return;

        enemySystemStarted = true;

        // 테스트 모드면 초기 적 스폰
        if (testMode)
        {
            SpawnTestEnemies();
        }

        // 지속 생성 루프 시작
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
    /// 지속 생성 루프
    /// </summary>
    private IEnumerator ContinuousSpawnCoroutine()
    {
        while (true)
        {
            if (!PhotonNetwork.IsMasterClient)
                yield break;

            if (SpawnCoreManager.Instance != null && SpawnCoreManager.Instance.AreAllCoresDestroyed)
                yield break;

            if (HasSpawnableCore() && ActiveEnemyCount < maxActiveEnemies)
            {
                string enemyPrefabPath = GetRandomEnemyPrefabPath();
                Vector3 spawnPosition = GetRandomSpawnPosition();
                SpawnEnemy(enemyPrefabPath, spawnPosition);
            }

            yield return new WaitForSeconds(GetCurrentSpawnInterval());
        }
    }

    /// <summary>
    /// 적 스폰
    /// </summary>
    public GameObject SpawnEnemy(string prefabPath, Vector3 position)
    {
        if (!PhotonNetwork.IsMasterClient)
            return null;

        GameObject enemy = PhotonNetwork.Instantiate(prefabPath, position, Quaternion.identity);
        activeEnemies.Add(enemy);
        CombatUIManager.Instance?.SetRemainingEnemyCount(ActiveEnemyCount);
        return enemy;
    }

    /// <summary>
    /// 랜덤 적 프리팹 반환
    /// </summary>
    private string GetRandomEnemyPrefabPath()
    {
        int random = Random.Range(0, 3);
        switch (random)
        {
            case 0: return basicEnemyPrefabPath;
            case 1: return tankEnemyPrefabPath;
            case 2: return fastEnemyPrefabPath;
            default: return basicEnemyPrefabPath;
        }
    }

    /// <summary>
    /// 랜덤 스폰 위치 가져오기
    /// </summary>
    public Vector3 GetRandomSpawnPosition()
    {
        SpawnCore targetCore = GetNextSpawnCore();
        if (targetCore == null)
        {
            Debug.LogWarning("EnemyManager: No spawnable SpawnCore found.");
            return Vector3.zero;
        }

        return GetSpawnPositionAroundCore(targetCore);
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
    /// 스폰 가능 코어 존재 여부 조회
    /// </summary>
    private bool HasSpawnableCore()
    {
        return GetSpawnableCores().Count > 0;
    }

    /// <summary>
    /// 스폰 가능 코어 목록 조회
    /// </summary>
    private List<SpawnCore> GetSpawnableCores()
    {
        if (SpawnCoreManager.Instance != null)
            return SpawnCoreManager.Instance.GetSpawnableCores();

        SpawnCore[] cores = FindObjectsOfType<SpawnCore>();
        List<SpawnCore> result = new List<SpawnCore>();

        for (int i = 0; i < cores.Length; i++)
        {
            SpawnCore core = cores[i];
            if (core == null || !core.isActiveAndEnabled || !core.AllowEnemySpawn)
                continue;

            result.Add(core);
        }

        result.Sort((left, right) => left.CoreOrder.CompareTo(right.CoreOrder));
        return result;
    }

    /// <summary>
    /// 다음 스폰 코어 선택
    /// </summary>
    private SpawnCore GetNextSpawnCore()
    {
        if (spawnCoreCursor >= spawnCoreSequence.Count)
            RefreshSpawnCoreSequence();

        while (spawnCoreCursor < spawnCoreSequence.Count)
        {
            SpawnCore core = spawnCoreSequence[spawnCoreCursor++];
            if (core != null && core.isActiveAndEnabled && core.AllowEnemySpawn)
                return core;
        }

        RefreshSpawnCoreSequence();
        if (spawnCoreCursor >= spawnCoreSequence.Count)
            return null;

        return spawnCoreSequence[spawnCoreCursor++];
    }

    /// <summary>
    /// 코어 순환 순서 갱신
    /// </summary>
    private void RefreshSpawnCoreSequence()
    {
        spawnCoreSequence.Clear();
        spawnCoreCursor = 0;

        List<SpawnCore> cores = GetSpawnableCores();
        for (int i = 0; i < cores.Count; i++)
            spawnCoreSequence.Add(cores[i]);
    }

    /// <summary>
    /// 코어 주변 스폰 위치 계산
    /// </summary>
    private Vector3 GetSpawnPositionAroundCore(SpawnCore core)
    {
        float minRadius = Mathf.Max(0f, core.SpawnRadiusMin + spawnRadiusPadding);
        float maxRadius = Mathf.Max(minRadius, core.SpawnRadiusMax + spawnRadiusPadding);
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minRadius, maxRadius);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
        return core.transform.position + offset + Vector3.up * spawnHeightOffset;
    }

    /// <summary>
    /// 현재 생성 주기 계산
    /// </summary>
    private float GetCurrentSpawnInterval()
    {
        float interval = baseSpawnInterval - (spawnCoreDifficulty * spawnIntervalReductionPerDestroyedCore);
        return Mathf.Max(minimumSpawnInterval, interval);
    }

    // ============================================================
    // Public 속성
    // ============================================================
    public int ActiveEnemyCount => activeEnemies.Count;
}
