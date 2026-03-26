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

    // ============================================================
    // 해상도 목록
    // ============================================================
    private Resolution[] resolutions;

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
    }

    // ============================================================
    // 해상도 초기화
    // ============================================================
    void InitializeResolutions()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            // 현재 해상도 찾기
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

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
        tempResolutionIndex = SettingsManager.Instance.resolutionIndex;
        tempFullscreen = SettingsManager.Instance.isFullscreen;

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
        SettingsManager.Instance.resolutionIndex = tempResolutionIndex;
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