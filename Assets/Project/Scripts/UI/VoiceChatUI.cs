using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보이스 챗 UI
/// - 음소거 버튼 (마이크 ON/OFF)
/// - 상대 음성 볼륨 슬라이더
/// - 말하는 중 표시 아이콘
/// </summary>
public class VoiceChatUI : MonoBehaviour
{
    // ============================================================
    // UI 참조
    // ============================================================
    [Header("Mute Button")]
    [SerializeField] private Button muteButton;
    [SerializeField] private Image muteIcon;
    [SerializeField] private Sprite micOnSprite;
    [SerializeField] private Sprite micOffSprite;

    [Header("Volume")]
    [SerializeField] private Slider voiceVolumeSlider;

    [Header("Speaking Indicator")]
    [SerializeField] private GameObject speakingIndicator;

    [Header("Colors")]
    [SerializeField] private Color micOnColor = Color.white;
    [SerializeField] private Color micOffColor = Color.red;

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Start()
    {
        // 버튼 이벤트
        if (muteButton != null)
            muteButton.onClick.AddListener(OnMuteButton);

        // 볼륨 슬라이더 이벤트
        if (voiceVolumeSlider != null)
        {
            voiceVolumeSlider.minValue = 0f;
            voiceVolumeSlider.maxValue = 1f;

            // 저장된 볼륨 로드
            float savedVolume = PlayerPrefs.GetFloat("VoiceVolume", 1f);
            voiceVolumeSlider.value = savedVolume;

            voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
        }

        UpdateUI();
    }

    void Update()
    {
        if (VoiceChatManager.Instance == null) return;

        // 음소거 아이콘 항상 동기화 (M키 토글 대응)
        UpdateUI();

        // 말하는 중 표시 업데이트
        if (speakingIndicator != null)
        {
            speakingIndicator.SetActive(
                VoiceChatManager.Instance.IsTransmitting &&
                !VoiceChatManager.Instance.IsMuted
            );
        }
    }

    // ============================================================
    // 버튼 이벤트
    // ============================================================
    void OnMuteButton()
    {
        if (VoiceChatManager.Instance == null) return;

        VoiceChatManager.Instance.ToggleMute();
        UpdateUI();
    }

    void OnVoiceVolumeChanged(float value)
    {
        if (VoiceChatManager.Instance == null) return;

        VoiceChatManager.Instance.SetVoiceVolume(value);
    }

    // ============================================================
    // UI 업데이트
    // ============================================================
    void UpdateUI()
    {
        if (VoiceChatManager.Instance == null) return;

        bool muted = VoiceChatManager.Instance.IsMuted;

        // 아이콘 변경
        if (muteIcon != null)
        {
            if (micOnSprite != null && micOffSprite != null)
            {
                muteIcon.sprite = muted ? micOffSprite : micOnSprite;
            }
            muteIcon.color = muted ? micOffColor : micOnColor;
        }
    }
}
