using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [SerializeField] private Text titleText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text killText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Sounds")]
    [SerializeField] private AudioClip loseClip;
    [SerializeField] [Range(0f, 1f)] private float loseVolume = 1f;

    [Header("Timing")]
    [SerializeField] private float resultDelay = 3.5f;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "TestScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";


    private AudioSource audioSource;

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
        GameManager.Instance.OnVictory.AddListener(ShowVictory);
    }

    // ============================================================
    // 게임 오버 표시
    // ============================================================
    // 패배 결과 UI와 패배 사운드 표시
    void ShowGameOver()
    {
        PlayLoseSound();
        DisablePlayerControls();
        EndgameCameraFocus.Get().FocusOn(GameManager.EndgameExplosionPosition, resultDelay);
        StartCoroutine(ShowResultDelayed("GAME OVER"));
        Debug.Log("Game Over UI Shown");
    }

    void ShowVictory()
    {
        DisablePlayerControls();
        EndgameCameraFocus.Get().FocusOn(GameManager.EndgameExplosionPosition, resultDelay);
        StartCoroutine(ShowResultDelayed("VICTORY"));
        Debug.Log("Victory UI Shown");
    }

    IEnumerator ShowResultDelayed(string title)
    {
        yield return new WaitForSecondsRealtime(resultDelay);
        ShowResult(title);
    }

    void ShowResult(string title)
    {
        // 패널 활성화
        gameOverPanel.SetActive(true);

        // 결과 표시
        if (titleText != null)
            titleText.text = title;

        scoreText.text = $"점수: {ResourceManager.Instance.Score}";
        killText.text = $"처치: {ResourceManager.Instance.Kills}명";

        // 마우스 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 시간 멈추기
        Time.timeScale = 0f;
    }

    void DisablePlayerControls()
    {
        PlayerController playerController = FindObjectOfType<PlayerController>();
        playerController?.SetEnabled(false);

        WeaponController weaponController = FindObjectOfType<WeaponController>();
        weaponController?.SetEnabled(false);
    }

    // 패배 결과 사운드 재생
    void PlayLoseSound()
    {
        if (loseClip == null)
            return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.PlayOneShot(loseClip, loseVolume);
    }

    // ============================================================
    // 버튼 이벤트
    // ============================================================
    void OnRestartButton()
    {
        // 시간 복구
        Time.timeScale = 1f;

        Debug.Log("Returning to room...");

        // 방은 유지하고 RoomScene으로 복귀
        InputLock.Unlock();
        SceneManager.LoadScene("RoomScene");
    }

    void OnMainMenuButton()
    {
        Time.timeScale = 1f;

        Debug.Log("Going to Main Menu...");

        // Voice 먼저 끊고 PUN 끊기 (순서 중요: Voice가 PUN 상태를 따라가므로)
        if (VoiceChatManager.Instance != null)
        {
            VoiceChatManager.Instance.DisconnectVoice();
        }
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
