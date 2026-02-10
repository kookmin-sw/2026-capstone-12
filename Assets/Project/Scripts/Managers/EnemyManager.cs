using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 스폰 관리
/// - 웨이브별로 적 생성
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
    [SerializeField] private GameObject basicEnemyPrefab;
    [SerializeField] private GameObject tankEnemyPrefab;
    [SerializeField] private GameObject fastEnemyPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Test Settings")]
    [SerializeField] private bool testMode = false;
    [SerializeField] private int testEnemyCount = 5;

    // State
    private List<GameObject> activeEnemies = new List<GameObject>();

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Awake()
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
        // 테스트 모드면 적 스폰
        if (testMode)
        {
            SpawnTestEnemies();
        }
    }

    // ============================================================
    // 스폰
    // ============================================================
    void SpawnTestEnemies()
    {
        for (int i = 0; i < testEnemyCount; i++)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            Vector3 randomOffset = new Vector3(
                Random.Range(-2f, 2f),
                0f,
                Random.Range(-2f, 2f)
            );
            Vector3 spawnPosition = spawnPoint.position + randomOffset;

            GameObject enemyPrefab = GetRandomEnemyPrefab();

            SpawnEnemy(enemyPrefab, spawnPosition);
        }

        Debug.Log($"Spawned {testEnemyCount} test enemies");
    }

    /// <summary>
    /// 적 스폰
    /// </summary>
    public GameObject SpawnEnemy(GameObject prefab, Vector3 position)
    {
        GameObject enemy = Instantiate(prefab, position, Quaternion.identity);
        activeEnemies.Add(enemy);
        return enemy;
    }

    /// <summary>
    /// 랜덤 적 프리팹 반환
    /// </summary>
    GameObject GetRandomEnemyPrefab()
    {
        int random = Random.Range(0, 3);
        switch (random)
        {
            case 0: return basicEnemyPrefab;
            case 1: return tankEnemyPrefab;
            case 2: return fastEnemyPrefab;
            default: return basicEnemyPrefab;
        }
    }

    /// <summary>
    /// 적 타입으로 프리팹 가져오기 (WaveManager용) ← 추가!
    /// </summary>
    public GameObject GetEnemyPrefab(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Basic: return basicEnemyPrefab;
            case EnemyType.Tank: return tankEnemyPrefab;
            case EnemyType.Fast: return fastEnemyPrefab;
            default: return basicEnemyPrefab;
        }
    }

    /// <summary>
    /// 랜덤 스폰 위치 가져오기 (WaveManager용) ← 추가!
    /// </summary>
    public Vector3 GetRandomSpawnPosition()
    {
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Vector3 randomOffset = new Vector3(
            Random.Range(-2f, 2f),
            0f,
            Random.Range(-2f, 2f)
        );

        return spawnPoint.position + randomOffset;
    }

    /// <summary>
    /// 적 제거 시 리스트에서 삭제
    /// </summary>
    public void RemoveEnemy(GameObject enemy)
    {
        activeEnemies.Remove(enemy);

        // WaveManager에 알림 ← 추가!
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemyKilled();
        }
    }

    // ============================================================
    // Public 속성
    // ============================================================
    public int ActiveEnemyCount => activeEnemies.Count;
}