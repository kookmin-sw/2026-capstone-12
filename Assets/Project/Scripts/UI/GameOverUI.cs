using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 오버 화면 UI 관리
/// - 게임 오버 시 패널 표시
/// - 점수, 처치 수 표시
/// - 재시작 / 메인 메뉴 버튼
/// </summary>
public class GameOverUI : MonoBehaviour
{
    // ============================================================
    // 참조
    // ============================================================
    [Header("Panels")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Texts")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text killText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "TestScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Start()
    {
        // 시작 시 패널 숨김
        gameOverPanel.SetActive(false);

        // 버튼 이벤트 연결
        restartButton.onClick.AddListener(OnRestartButton);
        mainMenuButton.onClick.AddListener(OnMainMenuButton);

        // GameManager 이벤트에 구독
        GameManager.Instance.OnGameOver.AddListener(ShowGameOver);
    }

    // ============================================================
    // 게임 오버 표시
    // ============================================================
    void ShowGameOver()
    {
        // 패널 활성화
        gameOverPanel.SetActive(true);

        // 결과 표시
        scoreText.text = $"점수: {ResourceManager.Instance.Score}";
        killText.text = $"처치: {ResourceManager.Instance.Kills}명";

        // 마우스 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 시간 멈추기
        Time.timeScale = 0f;

        Debug.Log("Game Over UI Shown");
    }

    // ============================================================
    // 버튼 이벤트
    // ============================================================
    void OnRestartButton()
    {
        Time.timeScale = 1f;

        Debug.Log("Restarting...");
        SceneManager.LoadScene(gameSceneName);
    }

    void OnMainMenuButton()
    {
        Time.timeScale = 1f;

        Debug.Log("Going to Main Menu...");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}