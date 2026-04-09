using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Photon.Pun;

/// <summary>
/// 웨이브 시스템 관리
/// - 웨이브 진행 및 타이머
/// - 적 스폰
/// - 승리 조건 확인
/// </summary>
public class WaveManager : MonoBehaviourPun
{
    // ============================================================
    // 싱글턴
    // ============================================================
    public static WaveManager Instance { get; private set; }

    // ============================================================
    // 웨이브 구성
    // ============================================================
    [System.Serializable]
    public class WaveConfig
    {
        public int waveNumber;              // 웨이브 번호
        public int basicEnemyCount;         // 기본 적 수
        public int tankEnemyCount;          // 방어형 적 수
        public int fastEnemyCount;          // 고속 적 수
    }

    [Header("Wave Settings")]
    [SerializeField] private WaveConfig[] waves;        // 웨이브 구성들
    [SerializeField] private float waveDuration = 100f; // 웨이브 시간 (1분 40초)
    [SerializeField] private float prepareTime = 20f;   // 준비 시간 (20초)

    // State
    private int currentWaveIndex = 0;                   // 현재 웨이브 인덱스 (0~4)
    private float waveTimer = 0f;                       // 웨이브 타이머
    private float prepareTimer = 0f;                    // 준비 타이머
    private bool isWaveActive = false;                  // 웨이브 진행 중
    private bool isPreparing = false;                   // 준비 중
    private int totalEnemiesInWave = 0;                 // 현재 웨이브 총 적 수
    private int spawnedEnemies = 0;                     // 스폰된 적 수

    // ============================================================
    // Events
    // ============================================================
    public UnityEventInt OnWaveStart = new UnityEventInt();         // 웨이브 시작
    public UnityEventInt OnWaveComplete = new UnityEventInt();      // 웨이브 완료
    public UnityEventFloat OnWaveTimerUpdate = new UnityEventFloat(); // 타이머 업데이트
    public UnityEventFloat OnPrepareTimerUpdate = new UnityEventFloat(); // 준비 타이머 업데이트
    public UnityEventInt OnRemainingEnemiesUpdate = new UnityEventInt(); // 남은 적 수 업데이트

    // ============================================================
    // Public 속성
    // ============================================================
    public int CurrentWave => currentWaveIndex + 1;     // 현재 웨이브 (1~5)
    public bool IsWaveActive => isWaveActive;
    public bool IsPreparing => isPreparing;
    public float WaveTimer => waveTimer;
    public float PrepareTimer => prepareTimer;
    public int SpawnCoreDifficulty { get; private set; }

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
        if (!PhotonNetwork.IsMasterClient)
            return;

        // 첫 웨이브 시작
        StartPreparePhase();
    }

    void Update()
    {
        // 타이머와 UI 업데이트는 모든 클라이언트에서 실행
        if (isPreparing)
        {
            prepareTimer -= Time.deltaTime;
            OnPrepareTimerUpdate.Invoke(prepareTimer);

            // 웨이브 시작 판정은 마스터만
            if (PhotonNetwork.IsMasterClient && prepareTimer <= 0f)
            {
                isPreparing = false;
                StartWave();
            }
        }
        else if (isWaveActive)
        {
            waveTimer -= Time.deltaTime;
            OnWaveTimerUpdate.Invoke(waveTimer);

            // 웨이브 완료 판정은 마스터만
            if (PhotonNetwork.IsMasterClient)
            {
                if (EnemyManager.Instance.ActiveEnemyCount == 0 && spawnedEnemies >= totalEnemiesInWave)
                    CompleteWave();
                else if (waveTimer <= 0f)
                    CompleteWave();
            }
        }
    }

    // ============================================================
    // 네트워크 동기화 테스트용 코드
    // ============================================================
    public void BeginWaveSystem()
    {
        // 여기서부터 원래 Start에서 하던 로직 실행
        if (!PhotonNetwork.IsMasterClient)
            return;

        StartPreparePhase();
    }

    // ============================================================
    // 준비 단계
    // ============================================================
    void StartPreparePhase()
    {
        // 마스터가 모든 클라이언트에 준비 단계 시작 알림
        photonView.RPC(nameof(RPC_StartPrepare), RpcTarget.All, currentWaveIndex);
    }

    [PunRPC]
    private void RPC_StartPrepare(int waveIndex)
    {
        currentWaveIndex = waveIndex;
        isPreparing = true;
        isWaveActive = false;
        prepareTimer = prepareTime;

        Debug.Log($"=== Prepare for Wave {CurrentWave} ===");
        OnPrepareTimerUpdate.Invoke(prepareTimer);
    }

    // ============================================================
    // 웨이브 진행
    // ============================================================
    void StartWave()
    {
        // 웨이브 구성 가져오기 (적 스폰은 마스터만)
        WaveConfig wave = waves[currentWaveIndex];
        SpawnWaveEnemies(wave);

        // 모든 클라이언트에 웨이브 시작 알림
        photonView.RPC(nameof(RPC_StartWave), RpcTarget.All, currentWaveIndex);
    }

    [PunRPC]
    private void RPC_StartWave(int waveIndex)
    {
        currentWaveIndex = waveIndex;
        isWaveActive = true;
        isPreparing = false;
        waveTimer = waveDuration;

        Debug.Log($"=== Wave {CurrentWave} Start! ===");
        OnWaveStart.Invoke(CurrentWave);
    }

    void CompleteWave()
    {
        // 모든 클라이언트에 웨이브 완료 알림
        photonView.RPC(nameof(RPC_CompleteWave), RpcTarget.All, currentWaveIndex);

        // 다음 웨이브로
        currentWaveIndex++;

        // 모든 웨이브 클리어 시 승리
        if (currentWaveIndex >= waves.Length)
        {
            Victory();
        }
        else
        {
            StartPreparePhase();
        }
    }

    [PunRPC]
    private void RPC_CompleteWave(int waveIndex)
    {
        isWaveActive = false;
        isPreparing = false;

        Debug.Log($"=== Wave {waveIndex + 1} Complete! ===");
        OnWaveComplete.Invoke(waveIndex + 1);
    }

    // ============================================================
    // 적 스폰
    // ============================================================
    void SpawnWaveEnemies(WaveConfig wave)
    {
        totalEnemiesInWave = wave.basicEnemyCount + wave.tankEnemyCount + wave.fastEnemyCount;
        spawnedEnemies = 0;

        StartCoroutine(SpawnEnemiesCoroutine(wave));
    }

    IEnumerator SpawnEnemiesCoroutine(WaveConfig wave)
    {
        // 기본 적 스폰
        for (int i = 0; i < wave.basicEnemyCount; i++)
        {
            EnemyManager.Instance.SpawnEnemy(
                EnemyManager.Instance.GetEnemyPrefabPath(EnemyType.Basic),
                EnemyManager.Instance.GetRandomSpawnPosition()
            );
            spawnedEnemies++;
            photonView.RPC(nameof(RPC_UpdateEnemyCount), RpcTarget.All, EnemyManager.Instance.ActiveEnemyCount);
            yield return new WaitForSeconds(0.5f);
        }

        // 방어형 적 스폰
        for (int i = 0; i < wave.tankEnemyCount; i++)
        {
            EnemyManager.Instance.SpawnEnemy(
                EnemyManager.Instance.GetEnemyPrefabPath(EnemyType.Tank),
                EnemyManager.Instance.GetRandomSpawnPosition()
            );
            spawnedEnemies++;
            photonView.RPC(nameof(RPC_UpdateEnemyCount), RpcTarget.All, EnemyManager.Instance.ActiveEnemyCount);
            yield return new WaitForSeconds(0.5f);
        }

        // 고속 적 스폰
        for (int i = 0; i < wave.fastEnemyCount; i++)
        {
            EnemyManager.Instance.SpawnEnemy(
                EnemyManager.Instance.GetEnemyPrefabPath(EnemyType.Fast),
                EnemyManager.Instance.GetRandomSpawnPosition()
            );
            spawnedEnemies++;
            photonView.RPC(nameof(RPC_UpdateEnemyCount), RpcTarget.All, EnemyManager.Instance.ActiveEnemyCount);
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log($"All enemies spawned for Wave {CurrentWave}");
    }

    // ============================================================
    // 적 처치 이벤트
    // ============================================================
    /// <summary>
    /// 적이 죽었을 때 호출됨 (마스터에서 호출)
    /// </summary>
    public void OnEnemyKilled()
    {
        if (isWaveActive && PhotonNetwork.IsMasterClient)
        {
            int remaining = EnemyManager.Instance.ActiveEnemyCount;
            photonView.RPC(nameof(RPC_UpdateEnemyCount), RpcTarget.All, remaining);
        }
    }

    [PunRPC]
    private void RPC_UpdateEnemyCount(int count)
    {
        OnRemainingEnemiesUpdate.Invoke(count);
    }

    // ============================================================
    // 승리
    // ============================================================
    void Victory()
    {
        Debug.Log("=== ALL WAVES COMPLETE! VICTORY! ===");
        GameManager.Instance.TriggerVictory();
    }

    public void ApplySpawnCoreDifficulty(int destroyedCoreCount)
    {
        // 코어 파괴 단계 저장
        SpawnCoreDifficulty = Mathf.Max(0, destroyedCoreCount);

        // Placeholder:
        // SpawnCore 파괴 단계에 따라 웨이브 길이, 스폰 간격, 웨이브 구성 가중치 등을
        // 나중에 여기서 조정할 수 있다.
    }
}

/// <summary>
/// 적 타입 열거형
/// </summary>
public enum EnemyType
{
    Basic,
    Tank,
    Fast
}
