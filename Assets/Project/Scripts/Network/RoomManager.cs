using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

/// <summary>
/// 방 대기실 관리
/// - 방 코드 표시
/// - 플레이어 목록
/// - 역할 선택
/// - 준비 시스템
/// </summary>
public class RoomManager : MonoBehaviourPunCallbacks
{
    // ============================================================
    // 싱글턴
    // ============================================================
    public static RoomManager Instance { get; private set; }

    // ============================================================
    // UI 참조
    // ============================================================
    [Header("Room Info")]
    [SerializeField] private Text roomCodeText;

    [Header("Player 1")]
    [SerializeField] private Text player1Name;
    [SerializeField] private Text player1Role;
    [SerializeField] private Text player1Ready;

    [Header("Player 2")]
    [SerializeField] private Text player2Name;
    [SerializeField] private Text player2Role;
    [SerializeField] private Text player2Ready;

    [Header("Buttons")]
    [SerializeField] private Button shooterButton;
    [SerializeField] private Button supporterButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button leaveButton;

    // ============================================================
    // 상수
    // ============================================================
    private const string ROLE_KEY = "Role";
    private const string READY_KEY = "Ready";
    private const string SKIN_KEY = "SkinIndex";
    private const string ROLE_SHOOTER = "Shooter";
    private const string ROLE_SUPPORTER = "Supporter";

    public const string RoleShooter = ROLE_SHOOTER; // 역할 패널 UI에서 기존 Shooter 키를 안전하게 참조
    public const string RoleSupporter = ROLE_SUPPORTER; // 역할 패널 UI에서 기존 Supporter 키를 안전하게 참조

    public event System.Action RoleStateChanged; // Custom Property 변경 후 역할 패널 표시를 갱신할 때 사용

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 방 코드 표시
        roomCodeText.text = PhotonNetwork.CurrentRoom.Name;

        // 입장 시 속성 초기화 ← 문제 3 해결!
        ClearPlayerProperties();

        // 버튼 이벤트 연결
        if (shooterButton != null)
            shooterButton.onClick.AddListener(OnShooterButton);
        if (supporterButton != null)
            supporterButton.onClick.AddListener(OnSupporterButton);
        readyButton.onClick.AddListener(OnReadyButton);
        leaveButton.onClick.AddListener(OnLeaveButton);

        // 플레이어 정보 업데이트
        UpdatePlayerList();
    }

    // ============================================================
    // 속성 초기화 (문제 3 해결)
    // ============================================================
    void ClearPlayerProperties()
    {
        Hashtable props = new Hashtable
        {
            { ROLE_KEY, null },
            { READY_KEY, false },
            { SKIN_KEY, 0 }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log("Player properties cleared!");
    }

    // ============================================================
    // Photon 콜백
    // ============================================================
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player joined: {newPlayer.NickName}");
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Player left: {otherPlayer.NickName}");
        UpdatePlayerList();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        UpdatePlayerList();
    }

    // 역할 패널이 로컬 플레이어의 현재 선택 역할을 표시할 수 있게 노출
    public string LocalRole
    {
        get
        {
            if (PhotonNetwork.LocalPlayer == null ||
                !PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(ROLE_KEY))
                return null;

            return PhotonNetwork.LocalPlayer.CustomProperties[ROLE_KEY] as string;
        }
    }

    // 역할 패널이 로컬 Ready 완료 여부를 버튼 강조에 반영할 수 있게 노출
    public bool LocalReady
    {
        get
        {
            if (PhotonNetwork.LocalPlayer == null ||
                !PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(READY_KEY))
                return false;

            return PhotonNetwork.LocalPlayer.CustomProperties[READY_KEY] is bool ready && ready;
        }
    }

    // 상대 플레이어가 이미 선택한 역할을 Locked 상태로 표시하기 위해 확인
    public bool IsRoleTakenByOtherPlayer(string role)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player == PhotonNetwork.LocalPlayer)
                continue;

            if (player.CustomProperties.ContainsKey(ROLE_KEY) &&
                player.CustomProperties[ROLE_KEY] as string == role)
                return true;
        }

        return false;
    }

    // 상대 플레이어가 역할 선택 후 Ready까지 완료했을 때만 패널을 Locked로 표시
    public bool IsRoleReadyByOtherPlayer(string role)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player == PhotonNetwork.LocalPlayer)
                continue;

            bool hasRole = player.CustomProperties.ContainsKey(ROLE_KEY) &&
                           player.CustomProperties[ROLE_KEY] as string == role; // 상대의 해당 역할 선택 여부
            bool isReady = player.CustomProperties.ContainsKey(READY_KEY) &&
                           player.CustomProperties[READY_KEY] is bool ready &&
                           ready; // 상대의 Ready 완료 여부

            if (hasRole && isReady)
                return true;
        }

        return false;
    }

    // 새 역할 패널의 Shooter 클릭을 기존 Shooter 버튼 로직으로 연결
    public void RequestSelectShooterFromRolePanel()
    {
        if (IsRoleTakenByOtherPlayer(ROLE_SHOOTER))
            return;

        OnShooterButton();
    }

    // 새 역할 패널의 Supporter 클릭을 기존 Supporter 버튼 로직으로 연결
    public void RequestSelectSupporterFromRolePanel()
    {
        if (IsRoleTakenByOtherPlayer(ROLE_SUPPORTER))
            return;

        OnSupporterButton();
    }

    // ============================================================
    // 플레이어 목록 업데이트
    // ============================================================
    void UpdatePlayerList()
    {
        Player[] players = PhotonNetwork.PlayerList;

        // Player 1
        if (players.Length > 0)
        {
            player1Name.text = players[0].NickName;
            player1Role.text = GetRoleText(players[0]);
            player1Ready.text = GetReadyText(players[0]);
        }
        else
        {
            player1Name.text = "대기 중...";
            player1Role.text = "역할: 미선택";
            player1Ready.text = "준비: X";
        }

        // Player 2
        if (players.Length > 1)
        {
            player2Name.text = players[1].NickName;
            player2Role.text = GetRoleText(players[1]);
            player2Ready.text = GetReadyText(players[1]);
        }
        else
        {
            player2Name.text = "대기 중...";
            player2Role.text = "역할: 미선택";
            player2Ready.text = "준비: X";
        }

        // 역할 선택 버튼 활성화/비활성화
        UpdateRoleButtons();

        // 게임 시작 가능 확인
        CheckStartGame();
    }

    string GetRoleText(Player player)
    {
        if (player.CustomProperties.ContainsKey(ROLE_KEY) && player.CustomProperties[ROLE_KEY] != null)
        {
            string role = (string)player.CustomProperties[ROLE_KEY];
            return $"역할: {(role == ROLE_SHOOTER ? "슈터" : "서포터")}";
        }
        return "역할: 미선택";
    }

    string GetReadyText(Player player)
    {
        if (player.CustomProperties.ContainsKey(READY_KEY))
        {
            bool ready = (bool)player.CustomProperties[READY_KEY];
            return ready ? "준비: O" : "준비: X";
        }
        return "준비: X";
    }

    // ============================================================
    // 역할 선택 버튼 (문제 2 해결)
    // ============================================================
    void UpdateRoleButtons()
    {
        // 이미 선택된 역할 확인
        bool shooterTaken = false;
        bool supporterTaken = false;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.ContainsKey(ROLE_KEY) && player.CustomProperties[ROLE_KEY] != null)
            {
                string role = (string)player.CustomProperties[ROLE_KEY];

                // 다른 사람이 선택한 역할만 체크
                if (player != PhotonNetwork.LocalPlayer)
                {
                    if (role == ROLE_SHOOTER) shooterTaken = true;
                    if (role == ROLE_SUPPORTER) supporterTaken = true;
                }
            }
        }

        // 내가 선택한 역할 확인
        string myRole = null;
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(ROLE_KEY))
        {
            myRole = (string)PhotonNetwork.LocalPlayer.CustomProperties[ROLE_KEY];
        }

        // 버튼 활성화/비활성화
        // 내가 선택한 역할은 활성화 (다시 클릭하면 취소)
        // 다른 사람이 선택한 역할은 비활성화
        if (shooterButton != null)
            shooterButton.interactable = !shooterTaken || myRole == ROLE_SHOOTER;
        if (supporterButton != null)
            supporterButton.interactable = !supporterTaken || myRole == ROLE_SUPPORTER;

        RoleStateChanged?.Invoke();
    }

    // ============================================================
    // 버튼 이벤트
    // ============================================================
    void OnShooterButton()
    {
        // 이미 선택했으면 취소 ← 문제 2 해결!
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(ROLE_KEY) &&
            PhotonNetwork.LocalPlayer.CustomProperties[ROLE_KEY] != null &&
            (string)PhotonNetwork.LocalPlayer.CustomProperties[ROLE_KEY] == ROLE_SHOOTER)
        {
            ClearRole();
        }
        else
        {
            SetRole(ROLE_SHOOTER);
        }
    }

    void OnSupporterButton()
    {
        // 이미 선택했으면 취소 ← 문제 2 해결!
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(ROLE_KEY) &&
            PhotonNetwork.LocalPlayer.CustomProperties[ROLE_KEY] != null &&
            (string)PhotonNetwork.LocalPlayer.CustomProperties[ROLE_KEY] == ROLE_SUPPORTER)
        {
            ClearRole();
        }
        else
        {
            SetRole(ROLE_SUPPORTER);
        }
    }

    void SetRole(string role)
    {
        Hashtable props = new Hashtable
        {
            { ROLE_KEY, role },
            { READY_KEY, false }  // 역할 변경 시 준비 취소
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log($"Role selected: {role}");
    }

    void ClearRole()
    {
        Hashtable props = new Hashtable
        {
            { ROLE_KEY, null },
            { READY_KEY, false }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log("Role cleared");
    }

    void OnReadyButton()
    {
        // 역할을 선택했는지 확인
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(ROLE_KEY) ||
            PhotonNetwork.LocalPlayer.CustomProperties[ROLE_KEY] == null)
        {
            Debug.LogWarning("Select a role first!");
            return;
        }

        // 준비 상태 토글
        bool currentReady = false;
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(READY_KEY))
        {
            currentReady = (bool)PhotonNetwork.LocalPlayer.CustomProperties[READY_KEY];
        }

        Hashtable props = new Hashtable
        {
            { READY_KEY, !currentReady }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log($"Ready: {!currentReady}");
    }

    void OnLeaveButton()
    {
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("Lobby");
    }

    // ============================================================
    // 게임 시작 (문제 1 해결)
    // ============================================================
    void CheckStartGame()
    {
        // 씬 로드 후 잠깐 대기 ← 추가!
        if (Time.timeSinceLevelLoad < 1f)
            return;

        // 2명이 모두 준비되었는지 확인
        if (PhotonNetwork.PlayerList.Length < 2)
            return;

        bool allReady = true;
        bool allHaveRole = true;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey(ROLE_KEY) ||
                player.CustomProperties[ROLE_KEY] == null)
                allHaveRole = false;

            if (!player.CustomProperties.ContainsKey(READY_KEY) ||
                !(bool)player.CustomProperties[READY_KEY])
                allReady = false;
        }

        // 모두 준비 완료 → 게임 시작
        if (allReady && allHaveRole)
        {
            Debug.Log("All players ready! Starting game...");
            StartGame();
        }
    }

    void StartGame()
    {
        // MultiPlayScene으로 이동
        SceneManager.LoadScene("MultiPlayScene");
    }
}
