using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

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

    [SerializeField] private string basicEnemyPrefabPath = "Prefabs/Enemies/BasicEnemy";
    [SerializeField] private string tankEnemyPrefabPath  = "Prefabs/Enemies/TankEnemy";
    [SerializeField] private string fastEnemyPrefabPath  = "Prefabs/Enemies/FastEnemy";

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Test Settings")]
    [SerializeField] private bool testMode = false;
    [SerializeField] private int testEnemyCount = 5;

    // State
    private List<GameObject> activeEnemies = new List<GameObject>();
    private int spawnCoreDifficulty = 0;

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
        // 마스터 클라이언트만 실행
        if (!PhotonNetwork.IsMasterClient)
            return;

        // 테스트 모드면 적 스폰
        if (testMode)
        {
            SpawnTestEnemies();
        }
    }

    // ============================================================
    // 네트워크 동기화 테스트용 코드
    // ============================================================
    public void BeginEnemySystem()
    {
        // 마스터 클라이언트만 실행
        if (!PhotonNetwork.IsMasterClient)
            return;

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

            string enemyPrefabPath = GetRandomEnemyPrefabPath();

            SpawnEnemy(enemyPrefabPath, spawnPosition);
        }

        Debug.Log($"Spawned {testEnemyCount} test enemies");
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
        return enemy;
    }

    /// <summary>
    /// 랜덤 적 프리팹 반환
    /// </summary>
    string GetRandomEnemyPrefabPath()
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
    /// 적 타입으로 프리팹 경로 가져오기 (WaveManager용) ← 추가!
    /// </summary>
    public string GetEnemyPrefabPath(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Basic: return basicEnemyPrefabPath;
            case EnemyType.Tank: return tankEnemyPrefabPath;
            case EnemyType.Fast: return fastEnemyPrefabPath;
            default: return basicEnemyPrefabPath;
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

    public void ApplySpawnCoreDifficulty(int destroyedCoreCount)
    {
        // 코어 파괴 단계 저장
        spawnCoreDifficulty = Mathf.Max(0, destroyedCoreCount);

        // Placeholder:
        // SpawnCore 파괴 단계에 따라 적 능력치 버프, 추가 스폰 수, 특수 적 비율 등을
        // 나중에 여기서 적용한다.
    }

    // ============================================================
    // Public 속성
    // ============================================================
    public int ActiveEnemyCount => activeEnemies.Count;
}
