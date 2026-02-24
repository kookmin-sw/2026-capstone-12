using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class QuickConnectPun : MonoBehaviourPunCallbacks
{
    [SerializeField] private string roomName = "DevRoom";
    [SerializeField] private byte maxPlayers = 2;

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

        if (PhotonNetwork.IsMasterClient)
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.BeginWaveSystem();

            if (EnemyManager.Instance != null)
                EnemyManager.Instance.BeginEnemySystem();
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"PlayerEntered: actor={newPlayer.ActorNumber}  NowPlayers={PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"PlayerLeft: actor={otherPlayer.ActorNumber}  NowPlayers={PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
    }
}