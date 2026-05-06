using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
#if UNITY_EDITOR
using ExitGames.Client.Photon;
#endif

public class QuickConnectPun : MonoBehaviourPunCallbacks
{
    [SerializeField] private string roomName = "DevRoom";
    [SerializeField] private byte maxPlayers = 2;

#if UNITY_EDITOR
    [Header("Editor Direct Play Test")]
    [SerializeField] private bool applyEditorDirectPlaySettings = true;
    [SerializeField] private RoleType editorDirectPlayRole = RoleType.Supporter;
    [SerializeField] private int editorDirectPlayStartMoney = 300;
#endif

    public bool UsesEditorDirectPlaySettings
    {
        get
        {
#if UNITY_EDITOR
            return applyEditorDirectPlaySettings;
#else
            return false;
#endif
        }
    }

    private void Start()
    {
        if (PhotonNetwork.IsConnected) return;

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom(
            roomName,
            new RoomOptions { MaxPlayers = maxPlayers },
            TypedLobby.Default
        );
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"JoinedRoom: {PhotonNetwork.CurrentRoom.Name}  Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}  Actor: {PhotonNetwork.LocalPlayer.ActorNumber}  Master: {PhotonNetwork.IsMasterClient}");

#if UNITY_EDITOR
        ApplyEditorDirectPlaySettings();
#endif
    }

#if UNITY_EDITOR
    // MultiPlayScene 직접 실행 테스트용 역할과 초기 자원 설정
    private void ApplyEditorDirectPlaySettings()
    {
        if (!applyEditorDirectPlaySettings)
            return;

        Hashtable props = new Hashtable { { "Role", editorDirectPlayRole.ToString() } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        if (PhotonNetwork.IsMasterClient && ResourceNet.Instance != null)
            ResourceNet.Instance.EditorSetStartMoney(editorDirectPlayStartMoney);
    }
#endif

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"PlayerEntered: actor={newPlayer.ActorNumber}  NowPlayers={PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"PlayerLeft: actor={otherPlayer.ActorNumber}  NowPlayers={PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
    }
}
