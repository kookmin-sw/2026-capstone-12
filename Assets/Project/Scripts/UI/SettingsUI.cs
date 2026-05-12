using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 UI 관리
/// - 슬라이더/드롭다운 이벤트 처리
/// - SettingsManager와 연동
/// </summary>
public class SettingsUI : MonoBehaviour
{
    // ============================================================
    // UI 참조
    // ============================================================
    [Header("UI Elements")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider voiceVolumeSlider;
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;

    // ============================================================
    // 임시 설정값 (적용 전)
    // ============================================================
    private float tempSensitivity;
    private float tempVolume;
    private float tempVoiceVolume;
    private int tempResolutionIndex;
    private bool tempFullscreen;

    private bool initialized = false;

    // ============================================================
    // 해상도 목록
    // ============================================================
    private System.Collections.Generic.List<Resolution> filteredResolutions = new System.Collections.Generic.List<Resolution>();

    private static readonly (int w, int h)[] CommonResolutions = new[]
    {
        (1280, 720),
        (1366, 768),
        (1600, 900),
        (1920, 1080),
        (2560, 1440),
        (3840, 2160),
    };

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Start()
    {
        // 해상도 목록 초기화
        InitializeResolutions();

        // 현재 설정값 로드
        LoadCurrentSettings();

        // 버튼 이벤트 연결
        applyButton.onClick.AddListener(OnApplyButton);
        cancelButton.onClick.AddListener(OnCancelButton);

        // 슬라이더/드롭다운 이벤트 연결
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (voiceVolumeSlider != null)
            voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        initialized = true;
    }

    void OnEnable()
    {
        // 패널이 열릴 때마다 현재 설정값으로 새로고침 (Start 이후부터만)
        if (initialized)
            LoadCurrentSettings();
    }

    // ============================================================
    // 해상도 초기화
    // ============================================================
    void InitializeResolutions()
    {
        filteredResolutions.Clear();
        resolutionDropdown.ClearOptions();

        var supported = new System.Collections.Generic.HashSet<(int, int)>();
        foreach (var r in Screen.resolutions)
            supported.Add((r.width, r.height));

        foreach (var (w, h) in CommonResolutions)
        {
            if (!supported.Contains((w, h))) continue;
            var res = new Resolution();
            res.width = w;
            res.height = h;
            filteredResolutions.Add(res);
        }

        // 현재 해상도가 목록에 없으면 추가
        int cur_w = Screen.currentResolution.width;
        int cur_h = Screen.currentResolution.height;
        bool hasCurrent = false;
        int currentResolutionIndex = 0;
        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            if (filteredResolutions[i].width == cur_w && filteredResolutions[i].height == cur_h)
            {
                hasCurrent = true;
                currentResolutionIndex = i;
                break;
            }
        }
        if (!hasCurrent)
        {
            var res = new Resolution();
            res.width = cur_w;
            res.height = cur_h;
            filteredResolutions.Add(res);
            currentResolutionIndex = filteredResolutions.Count - 1;
        }

        var options = new System.Collections.Generic.List<string>();
        foreach (var r in filteredResolutions)
            options.Add(r.width + " x " + r.height);

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    // ============================================================
    // 현재 설정값 로드
    // ============================================================
    void LoadCurrentSettings()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SettingsManager not found!");
            return;
        }

        // SettingsManager에서 현재 설정 가져오기
        tempSensitivity = SettingsManager.Instance.mouseSensitivity;
        tempVolume = SettingsManager.Instance.masterVolume;
        tempVoiceVolume = SettingsManager.Instance.voiceVolume;
        tempFullscreen = SettingsManager.Instance.isFullscreen;

        // 저장된 해상도와 일치하는 드롭다운 인덱스 찾기
        int savedW = SettingsManager.Instance.resolutionWidth;
        int savedH = SettingsManager.Instance.resolutionHeight;
        tempResolutionIndex = 0;
        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            if (filteredResolutions[i].width == savedW && filteredResolutions[i].height == savedH)
            {
                tempResolutionIndex = i;
                break;
            }
        }

        // UI에 반영
        sensitivitySlider.value = tempSensitivity;
        volumeSlider.value = tempVolume;
        if (voiceVolumeSlider != null)
            voiceVolumeSlider.value = tempVoiceVolume;
        resolutionDropdown.value = tempResolutionIndex;
        fullscreenToggle.isOn = tempFullscreen;
    }

    // ============================================================
    // UI 이벤트
    // ============================================================
    void OnSensitivityChanged(float value)
    {
        tempSensitivity = value;
    }

    void OnVolumeChanged(float value)
    {
        tempVolume = value;
    }

    void OnVoiceVolumeChanged(float value)
    {
        tempVoiceVolume = value;
    }

    void OnResolutionChanged(int index)
    {
        tempResolutionIndex = index;
    }

    void OnFullscreenChanged(bool value)
    {
        tempFullscreen = value;
    }

    // ============================================================
    // 버튼 이벤트
    // ============================================================
    void OnApplyButton()
    {
        if (SettingsManager.Instance == null)
            return;

        // SettingsManager에 설정 저장
        SettingsManager.Instance.mouseSensitivity = tempSensitivity;
        SettingsManager.Instance.masterVolume = tempVolume;
        SettingsManager.Instance.voiceVolume = tempVoiceVolume;
        SettingsManager.Instance.resolutionWidth = filteredResolutions[tempResolutionIndex].width;
        SettingsManager.Instance.resolutionHeight = filteredResolutions[tempResolutionIndex].height;
        SettingsManager.Instance.isFullscreen = tempFullscreen;

        // 설정 적용 및 저장
        SettingsManager.Instance.ApplySettings();
        SettingsManager.Instance.SaveSettings();

        Debug.Log("Settings applied and saved");

        // 패널 닫기
        gameObject.SetActive(false);
    }

    void OnCancelButton()
    {
        // 변경사항 취소하고 패널 닫기
        Debug.Log("Settings cancelled");
        gameObject.SetActive(false);
    }
}