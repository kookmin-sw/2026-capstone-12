using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인 메뉴 관리
/// - 게임 시작
/// - 설정
/// - 게임 종료
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    // ============================================================
    // 씬 이름
    // ============================================================
    [Header("Scene Names")]
    [SerializeField] private string lobbySceneName = "Lobby";  // 나중에 만들 로비 씬

    // ============================================================
    // 버튼 이벤트
    // ============================================================
    /// <summary>
    /// 게임 시작 버튼
    /// </summary>
    public void OnStartButton()
    {
        Debug.Log("게임 시작!");
        
        SceneManager.LoadScene("Lobby");
    }

    /// <summary>
    /// 설정 버튼
    /// </summary>
    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;

    public void OnSettingsButton()
    {
        Debug.Log("설정 열기!");

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 게임 종료 버튼
    /// </summary>
    public void OnQuitButton()
    {
        Debug.Log("게임 종료!");

        // 에디터에서는 플레이 모드 종료
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 빌드에서는 애플리케이션 종료
        Application.Quit();
#endif
    }
}