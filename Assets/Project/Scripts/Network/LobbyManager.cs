using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 로비 관리
/// - Photon 서버 연결
/// - 방 만들기 / 참가하기
/// </summary>
public class LobbyManager : MonoBehaviourPunCallbacks
{
    // ============================================================
    // 싱글턴
    // ============================================================
    public static LobbyManager Instance { get; private set; }

    // ============================================================
    // 변수
    // ============================================================
    [Header("UI Panels")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject joinRoomPanel;

    [Header("Join Room UI")]
    [SerializeField] private InputField roomCodeInput;

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 닉네임 설정 (랜덤) ← 추가!
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Player" + Random.Range(1000, 9999);
        }

        // Photon 서버 연결
        if (!PhotonNetwork.IsConnected)
        {
            ShowLoadingPanel();
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("Connecting to Photon...");
        }
        else if (PhotonNetwork.InRoom)
        {
            ShowLoadingPanel();
            PhotonNetwork.LeaveRoom();
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            ShowLobbyPanel();
        }
        else
        {
            // 연결 중인 상태
            ShowLoadingPanel();
        }
    }

    // ============================================================
    // Photon 콜백
    // ============================================================
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server!");
        ShowLobbyPanel();
    }

    public override void OnLeftRoom()
    {
        ShowLobbyPanel();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        // 씬 전환으로 이미 destroy된 경우 무시
        if (this == null) return;
        Debug.LogWarning($"Disconnected: {cause}");
        ShowLoadingPanel();
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}");
        // RoomLobby 씬으로 이동
        SceneManager.LoadScene("RoomScene");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Join room failed: {message}");
        ShowLobbyPanel();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Create room failed: {message}");
    }

    // ============================================================
    // 패널 관리
    // ============================================================
    void ShowLoadingPanel()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
    }

    void ShowLobbyPanel()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
    }

    void ShowJoinRoomPanel()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(true);
    }

    // ============================================================
    // 버튼 이벤트 (LobbyPanel)
    // ============================================================
    /// <summary>
    /// 방 만들기 버튼
    /// </summary>
    public void OnCreateRoomButton()
    {
        // 연결 상태 확인
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("Not ready to create room. Please wait...");
            return;
        }
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("Connecting to Photon... Please wait.");
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            Debug.LogWarning("Already in a room!");
            return;
        }
        // 4자리 랜덤 코드 생성
        string roomCode = Random.Range(1000, 9999).ToString();

        // 방 설정
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,  // 최대 2명 (슈터 + 서포터)
            IsVisible = true,
            IsOpen = true
        };

        // 방 생성
        PhotonNetwork.CreateRoom(roomCode, roomOptions);
        Debug.Log($"Creating room with code: {roomCode}");
    }

    /// <summary>
    /// 방 참가하기 버튼
    /// </summary>
    public void OnJoinRoomButton()
    {
        ShowJoinRoomPanel();
    }

    /// <summary>
    /// 메인 메뉴 버튼
    /// </summary>
    public void OnBackButton()
    {
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("MainMenu");
    }

    // ============================================================
    // 버튼 이벤트 (JoinRoomPanel)
    // ============================================================
    /// <summary>
    /// 참가 버튼
    /// </summary>
    public void OnConfirmJoinButton()
    {
        // 연결 상태 확인
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("Not ready to join room. Please wait...");
            return;
        }

        string roomCode = roomCodeInput.text;

        // 4자리 확인
        if (roomCode.Length != 4)
        {
            Debug.LogWarning("Room code must be 4 digits!");
            return;
        }

        // 방 참가
        PhotonNetwork.JoinRoom(roomCode);
        Debug.Log($"Joining room with code: {roomCode}");
    }

    /// <summary>
    /// 취소 버튼
    /// </summary>
    public void OnCancelButton()
    {
        ShowLobbyPanel();
    }
}