using UnityEngine;
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
    [SerializeField] private bool muteOnStart = false;
    [SerializeField] [Range(0f, 1f)] private float micVolume = 1f;

    private bool isMuted = false;

    private const string MIC_VOL_KEY = "MicVol";

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

        // 저장된 볼륨 설정 로드 및 Custom Properties에 반영
        micVolume = PlayerPrefs.GetFloat("VoiceVolume", 1f);
        SyncMicVolumeToNetwork();
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
    /// 내 마이크 볼륨 설정 (0~1 슬라이더 값)
    /// Custom Properties를 통해 상대방에게 전달 → 상대방이 Speaker 볼륨 조절
    /// </summary>
    public void SetVoiceVolume(float sliderValue)
    {
        micVolume = Mathf.Clamp01(sliderValue);
        PlayerPrefs.SetFloat("VoiceVolume", micVolume);
        SyncMicVolumeToNetwork();
        Debug.Log($"[VoiceChat] My mic volume: {micVolume} (actual: {SliderToActualVolume(micVolume)})");
    }

    public float GetVoiceVolume()
    {
        return micVolume;
    }

    /// <summary>
    /// 내 마이크 볼륨을 Photon Custom Properties로 동기화
    /// </summary>
    void SyncMicVolumeToNetwork()
    {
        if (PhotonNetwork.InRoom)
        {
            float actualVolume = SliderToActualVolume(micVolume);
            Hashtable props = new Hashtable { { MIC_VOL_KEY, actualVolume } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    /// <summary>
    /// 슬라이더 값(0~1)을 체감 볼륨으로 변환 (지수 커브)
    /// </summary>
    float SliderToActualVolume(float sliderValue)
    {
        return sliderValue * sliderValue * sliderValue;
    }

    /// <summary>
    /// 상대방의 MicVol Custom Property를 읽어 Speaker 볼륨에 반영
    /// </summary>
    void ApplyRemoteMicVolumes()
    {
        Speaker[] speakers = FindObjectsOfType<Speaker>();
        foreach (Speaker speaker in speakers)
        {
            AudioSource audioSource = speaker.GetComponent<AudioSource>();
            if (audioSource == null) continue;

            // 상대방이 설정한 마이크 볼륨 가져오기
            float remoteVolume = GetRemotePlayerMicVolume();
            audioSource.volume = remoteVolume;
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
