using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;  // ← 추가!

/// <summary>
/// 게임 전체 상태 관리 (중앙 매니저)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Playing,
        GameOver,
        Victory
    }

    [Header("Game Settings")]
    [SerializeField] private int totalScore = 0;
    [SerializeField] private int totalKills = 0;

    private GameState currentState = GameState.Playing;

    public UnityEventInt OnScoreChanged = new UnityEventInt();
    public UnityEvent OnGameOver = new UnityEvent();
    public UnityEvent OnVictory = new UnityEvent();

    public GameState CurrentState => currentState;
    public int TotalScore => totalScore;
    public int TotalKills => totalKills;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 씬 로드 이벤트에 구독 ← 추가!
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        InitializeGame();
    }

    // ← 추가!
    void OnDestroy()
    {
        // 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ← 추가!
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 로드될 때마다 게임 초기화
        InitializeGame();
    }

    // public으로 변경 ← 수정!
    public void InitializeGame()
    {
        totalScore = 0;
        totalKills = 0;
        currentState = GameState.Playing;
        OnScoreChanged.Invoke(totalScore);

        Debug.Log("Game Initialized!");
    }

    public void AddScore(int score)
    {
        if (currentState != GameState.Playing)
            return;

        totalScore += score;
        OnScoreChanged.Invoke(totalScore);

        Debug.Log($"Score +{score} | Total: {totalScore}");
    }

    public void AddKill()
    {
        if (currentState != GameState.Playing)
            return;

        totalKills++;
    }

    public void TriggerGameOver()
    {
        if (currentState != GameState.Playing)
            return;

        currentState = GameState.GameOver;
        OnGameOver.Invoke();

        Debug.Log("=== GAME OVER ===");
        Debug.Log($"Score: {totalScore} | Kills: {totalKills}");
    }

    public void TriggerVictory()
    {
        if (currentState != GameState.Playing)
            return;

        currentState = GameState.Victory;
        OnVictory.Invoke();

        Debug.Log("=== VICTORY ===");
        Debug.Log($"Score: {totalScore} | Kills: {totalKills}");
    }
}

public class UnityEventInt : UnityEvent<int> { }