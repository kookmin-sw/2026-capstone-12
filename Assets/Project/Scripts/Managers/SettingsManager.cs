using UnityEngine;

/// <summary>
/// 설정 관리 (마우스 감도, 볼륨, 그래픽)
/// </summary>
public class SettingsManager : MonoBehaviour
{
    // ============================================================
    // 싱글턴
    // ============================================================
    public static SettingsManager Instance { get; private set; }

    // ============================================================
    // 설정값
    // ============================================================
    [Header("Gameplay")]
    public float mouseSensitivity = 2f;

    [Header("Audio")]
    public float masterVolume = 1f;

    [Header("Graphics")]
    public int resolutionIndex = 0;
    public bool isFullscreen = true;

    // ============================================================
    // PlayerPrefs Keys
    // ============================================================
    private const string MOUSE_SENSITIVITY_KEY = "MouseSensitivity";
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string RESOLUTION_INDEX_KEY = "ResolutionIndex";
    private const string FULLSCREEN_KEY = "Fullscreen";

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Awake()
    {
        // 싱글턴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 설정 로드
        LoadSettings();
    }

    // ============================================================
    // 설정 저장/로드
    // ============================================================
    public void LoadSettings()
    {
        mouseSensitivity = PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, 2f);
        masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        resolutionIndex = PlayerPrefs.GetInt(RESOLUTION_INDEX_KEY, 0);
        isFullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, 1) == 1;

        Debug.Log("Settings loaded");
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(MOUSE_SENSITIVITY_KEY, mouseSensitivity);
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
        PlayerPrefs.SetInt(RESOLUTION_INDEX_KEY, resolutionIndex);
        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("Settings saved");
    }

    // ============================================================
    // 설정 적용
    // ============================================================
    public void ApplySettings()
    {
        // 볼륨 적용
        AudioListener.volume = masterVolume;

        // 해상도 적용
        Resolution[] resolutions = Screen.resolutions;
        if (resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
        {
            Resolution res = resolutions[resolutionIndex];
            Screen.SetResolution(res.width, res.height, isFullscreen);
        }

        // 마우스 감도는 PlayerController에서 직접 읽어감

        Debug.Log($"Settings applied: Sensitivity={mouseSensitivity}, Volume={masterVolume}");
    }
}