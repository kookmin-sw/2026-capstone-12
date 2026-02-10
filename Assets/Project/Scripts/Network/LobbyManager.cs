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
        // Photon 서버 연결
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("Connecting to Photon...");
        }

        // 초기 패널 설정
        ShowLobbyPanel();
    }

    // ============================================================
    // Photon 콜백
    // ============================================================
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server!");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}");
        // RoomLobby 씬으로 이동
        SceneManager.LoadScene("RoomLobby");
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
    void ShowLobbyPanel()
    {
        lobbyPanel.SetActive(true);
        joinRoomPanel.SetActive(false);
    }

    void ShowJoinRoomPanel()
    {
        lobbyPanel.SetActive(false);
        joinRoomPanel.SetActive(true);
    }

    // ============================================================
    // 버튼 이벤트 (LobbyPanel)
    // ============================================================
    /// <summary>
    /// 방 만들기 버튼
    /// </summary>
    public void OnCreateRoomButton()
    {
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