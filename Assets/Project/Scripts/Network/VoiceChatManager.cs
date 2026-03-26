using UnityEngine;
using Photon.Pun;
using Photon.Voice.Unity;
using Photon.Voice.PUN;
using System.Reflection;

/// <summary>
/// 인게임 보이스 챗 관리
/// - Photon Voice 2 (PunVoiceClient) 기반
/// - PUN2 연동 자동 연결
/// - 마이크 음소거/해제
/// - 음성 볼륨 조절
/// </summary>
public class VoiceChatManager : MonoBehaviour
{
    // ============================================================
    // 싱글턴
    // ============================================================
    public static VoiceChatManager Instance { get; private set; }

    // ============================================================
    // 컴포넌트 참조
    // ============================================================
    private Recorder recorder;
    private PunVoiceClient punVoiceClient;

    // ============================================================
    // 상태
    // ============================================================
    [Header("Voice Settings")]
    [SerializeField] private bool muteOnStart = false;
    [SerializeField] [Range(0f, 1f)] private float voiceVolume = 1f;

    private bool isMuted = false;

    // ============================================================
    // 프로퍼티
    // ============================================================
    public bool IsMuted => isMuted;
    public bool IsConnected => punVoiceClient != null && punVoiceClient.ClientState == Photon.Realtime.ClientState.Joined;
    public bool IsTransmitting => recorder != null && recorder.IsCurrentlyTransmitting;

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
        DontDestroyOnLoad(gameObject);

        SetupVoiceComponents();
    }

    void Start()
    {
        if (muteOnStart)
        {
            SetMute(true);
        }

        // 저장된 볼륨 설정 로드
        voiceVolume = PlayerPrefs.GetFloat("VoiceVolume", 1f);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ============================================================
    // 초기 설정
    // ============================================================
    void SetupVoiceComponents()
    {
        // PunVoiceClient 설정 (PUN2 방 연동 자동 연결)
        punVoiceClient = GetComponent<PunVoiceClient>();
        if (punVoiceClient == null)
        {
            punVoiceClient = gameObject.AddComponent<PunVoiceClient>();
        }

        // Speaker 프리팹 생성 (상대 음성 재생용)
        // 주의: SetActive(false)하면 Instantiate된 복제본도 비활성이라 소리 안 남
        // HideFlags로 씬에서 숨기되 활성 상태 유지
        if (punVoiceClient.SpeakerPrefab == null)
        {
            GameObject speakerPrefab = new GameObject("SpeakerPrefab");
            Speaker speaker = speakerPrefab.AddComponent<Speaker>();
            AudioSource audioSource = speakerPrefab.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = speakerPrefab.AddComponent<AudioSource>();
            }
            audioSource.spatialBlend = 0f;
            audioSource.volume = voiceVolume;
            audioSource.playOnAwake = false;

            speakerPrefab.transform.SetParent(this.transform);
            speakerPrefab.hideFlags = HideFlags.HideInHierarchy;
            punVoiceClient.SpeakerPrefab = speakerPrefab;
            Debug.Log("[VoiceChat] Speaker prefab created");
        }

        // Recorder 설정 (마이크 입력)
        recorder = GetComponent<Recorder>();
        if (recorder == null)
        {
            recorder = gameObject.AddComponent<Recorder>();
        }

        // Recorder 기본 설정
        recorder.TransmitEnabled = true;
        recorder.VoiceDetection = true;
        recorder.VoiceDetectionThreshold = 0.01f;
        recorder.DebugEchoMode = false;

        // PunVoiceClient에 Recorder 연결
        punVoiceClient.PrimaryRecorder = recorder;

        // UsePrimaryRecorder 활성화 (private 필드라 리플렉션 사용)
        // 이것이 없으면 Start()에서 AddRecorder가 호출되지 않아 음성이 전송되지 않음
        FieldInfo usePrimaryRecorderField = typeof(VoiceConnection).GetField(
            "usePrimaryRecorder",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        if (usePrimaryRecorderField != null)
        {
            usePrimaryRecorderField.SetValue(punVoiceClient, true);
            Debug.Log("[VoiceChat] UsePrimaryRecorder enabled");
        }
        else
        {
            Debug.LogError("[VoiceChat] Failed to set UsePrimaryRecorder via reflection");
        }

        Debug.Log("[VoiceChat] Voice components initialized");
    }

    // ============================================================
    // 공개 API
    // ============================================================

    /// <summary>
    /// 마이크 음소거 토글
    /// </summary>
    public void ToggleMute()
    {
        SetMute(!isMuted);
    }

    /// <summary>
    /// 마이크 음소거 설정
    /// </summary>
    public void SetMute(bool mute)
    {
        isMuted = mute;

        if (recorder != null)
        {
            recorder.TransmitEnabled = !mute;
        }

        Debug.Log($"[VoiceChat] Mute: {mute}");
    }

    /// <summary>
    /// 상대방 음성 볼륨 설정 (0~1)
    /// </summary>
    public void SetVoiceVolume(float volume)
    {
        voiceVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("VoiceVolume", voiceVolume);
        UpdateAllSpeakerVolumes();
        Debug.Log($"[VoiceChat] Voice volume: {voiceVolume}");
    }

    public float GetVoiceVolume()
    {
        return voiceVolume;
    }

    /// <summary>
    /// 모든 Speaker(상대 음성 출력) 볼륨 업데이트
    /// </summary>
    void UpdateAllSpeakerVolumes()
    {
        Speaker[] speakers = FindObjectsOfType<Speaker>();
        foreach (Speaker speaker in speakers)
        {
            AudioSource audioSource = speaker.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = voiceVolume;
            }
        }
    }
}
