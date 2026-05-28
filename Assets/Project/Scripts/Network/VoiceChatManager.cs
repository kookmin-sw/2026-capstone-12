using UnityEngine;
using UnityEngine.Audio;
using Photon.Pun;
using Photon.Voice.Unity;
using Photon.Voice.PUN;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Reflection;

/// <summary>
/// 인게임 보이스 챗 관리
/// - Photon Voice 2 (PunVoiceClient) 기반
/// - PUN2 연동 자동 연결
/// - 마이크 음소거/해제
/// - 내 마이크 볼륨 조절 (Custom Properties → 상대방 Speaker 볼륨에 반영)
/// </summary>
public class VoiceChatManager : MonoBehaviour, IInRoomCallbacks
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
    [SerializeField] private bool muteOnStart = true;
    [SerializeField] [Range(0f, 1f)] private float micVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float remoteSpeakerVolume = 0.35f;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer voiceMixer;          // VoiceMixer.mixer
    private const string VOICE_BOOST_PARAM = "VoiceBoost";  // 노출된 파라미터 이름

    private bool isMuted = false;

    private const string MIC_VOL_KEY = "MicVol";
    private const string REMOTE_SPEAKER_VOL_KEY = "RemoteSpeakerVol";

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

        // 저장된 볼륨 설정 로드 및 적용
        micVolume = PlayerPrefs.GetFloat("VoiceVolume", 1f);
        remoteSpeakerVolume = PlayerPrefs.GetFloat(REMOTE_SPEAKER_VOL_KEY, 0.35f);
        SyncMicVolumeToNetwork();
        ApplyRemoteSpeakerMixer();
    }

    void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMute();
        }
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
            audioSource.volume = 1f;
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
    /// Voice 연결 해제 (PUN Disconnect 전에 호출)
    /// </summary>
    public void DisconnectVoice()
    {
        if (punVoiceClient != null && punVoiceClient.Client != null && punVoiceClient.Client.IsConnected)
        {
            punVoiceClient.Client.Disconnect();
            Debug.Log("[VoiceChat] Voice client disconnected");
        }
    }

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
    /// 내 마이크 볼륨 설정 (0~1) — 상대방 Speaker에 반영
    /// </summary>
    public void SetVoiceVolume(float sliderValue)
    {
        micVolume = Mathf.Clamp01(sliderValue);
        PlayerPrefs.SetFloat("VoiceVolume", micVolume);
        SyncMicVolumeToNetwork();
    }

    public float GetVoiceVolume() => micVolume;

    /// <summary>
    /// 상대방 음성 볼륨 설정 (0~1)
    /// 슬라이더 0 = 묵음, 0.8 = 0dB(정상), 1.0 = +20dB(최대 부스트)
    /// </summary>
    public void SetRemoteSpeakerVolume(float sliderValue)
    {
        remoteSpeakerVolume = Mathf.Clamp01(sliderValue);
        PlayerPrefs.SetFloat(REMOTE_SPEAKER_VOL_KEY, remoteSpeakerVolume);
        ApplyRemoteSpeakerMixer();
    }

    public float GetRemoteSpeakerVolume() => remoteSpeakerVolume;

    /// <summary>
    /// 내 마이크 볼륨을 Photon Custom Properties로 동기화 (선형 전달)
    /// </summary>
    void SyncMicVolumeToNetwork()
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable { { MIC_VOL_KEY, micVolume } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    /// <summary>
    /// 슬라이더 → dB 변환
    /// 0 → -80dB, 0.8 → 0dB, 1.0 → +20dB
    /// </summary>
    float SliderToDB(float sliderValue)
    {
        if (sliderValue <= 0f) return -80f;
        // 0~0.35 구간: -80dB ~ 0dB (정상 볼륨)
        // 0.35~1.0 구간: 0dB ~ +20dB (부스트)
        if (sliderValue <= 0.35f)
            return Mathf.Lerp(-80f, 0f, sliderValue / 0.35f);
        else
            return Mathf.Lerp(0f, 20f, (sliderValue - 0.35f) / 0.65f);
    }

    /// <summary>
    /// 상대방 음성 볼륨을 AudioMixer로 적용 (볼륨 부스트 지원)
    /// </summary>
    void ApplyRemoteSpeakerMixer()
    {
        if (voiceMixer != null)
        {
            float dB = SliderToDB(remoteSpeakerVolume);
            voiceMixer.SetFloat(VOICE_BOOST_PARAM, dB);

            // Speaker AudioSource를 믹서 그룹에 연결
            var mixerGroups = voiceMixer.FindMatchingGroups("Master");
            if (mixerGroups.Length > 0)
            {
                Speaker[] speakers = FindObjectsOfType<Speaker>();
                foreach (Speaker speaker in speakers)
                {
                    AudioSource audioSource = speaker.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.outputAudioMixerGroup = mixerGroups[0];
                        audioSource.volume = 1f;
                    }
                }
            }
        }
        else
        {
            // 믹서 없으면 기존 방식(AudioSource.volume 직접 제어)으로 폴백
            float vol = remoteSpeakerVolume;
            Speaker[] speakers = FindObjectsOfType<Speaker>();
            foreach (Speaker speaker in speakers)
            {
                AudioSource audioSource = speaker.GetComponent<AudioSource>();
                if (audioSource != null) audioSource.volume = vol;
            }
        }
    }

    /// <summary>
    /// 상대방 MicVol Custom Property 변경 시 AudioSource.volume 반영
    /// </summary>
    void ApplyRemoteMicVolumes()
    {
        float remoteMicVol = GetRemotePlayerMicVolume();
        Speaker[] speakers = FindObjectsOfType<Speaker>();
        foreach (Speaker speaker in speakers)
        {
            AudioSource audioSource = speaker.GetComponent<AudioSource>();
            if (audioSource != null)
                audioSource.volume = remoteMicVol;
        }
    }

    /// <summary>
    /// 상대 플레이어의 MicVol Custom Property 값 반환
    /// </summary>
    float GetRemotePlayerMicVolume()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal) continue;

            if (player.CustomProperties.TryGetValue(MIC_VOL_KEY, out object vol))
            {
                return (float)vol;
            }
        }
        return 1f; // 기본값
    }

    // ============================================================
    // IInRoomCallbacks - 상대방 Custom Properties 변경 감지
    // ============================================================
    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // 상대방이 MicVol을 변경했을 때 Speaker 볼륨 업데이트
        if (!targetPlayer.IsLocal && changedProps.ContainsKey(MIC_VOL_KEY))
        {
            ApplyRemoteMicVolumes();
            Debug.Log($"[VoiceChat] Remote player mic volume changed: {changedProps[MIC_VOL_KEY]}");
        }
    }

    public void OnPlayerEnteredRoom(Player newPlayer) { }
    public void OnPlayerLeftRoom(Player otherPlayer) { }
    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) { }
    public void OnMasterClientSwitched(Player newMasterClient) { }
}
