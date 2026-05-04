using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class SoundNet : MonoBehaviourPun
{
    public static SoundNet Instance { get; private set; }

    [SerializeField] private PingSoundPlayer pingSoundPlayer;
    [SerializeField] private GameEventSoundPlayer gameEventSoundPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        if (pingSoundPlayer == null)
            pingSoundPlayer = GetComponent<PingSoundPlayer>();

        if (gameEventSoundPlayer == null)
            gameEventSoundPlayer = FindObjectOfType<GameEventSoundPlayer>(true);
    }

    // 게임 사운드 재생 요청을 네트워크 상황에 맞게 전달
    public void RequestPlay(GameSoundType soundType)
    {
        if (PhotonNetwork.InRoom && photonView.ViewID != 0)
        {
            photonView.RPC(nameof(RpcPlay), RpcTarget.All, (int)soundType);
            return;
        }

        PlayLocal(soundType);
    }

    // RPC로 전달받은 게임 사운드를 현재 클라이언트에서 재생
    [PunRPC]
    private void RpcPlay(int soundTypeValue)
    {
        PlayLocal((GameSoundType)soundTypeValue);
    }

    // 네트워크 동기화 없이 현재 클라이언트에서 게임 사운드 재생
    public void PlayLocal(GameSoundType soundType)
    {
        EnsureGameEventSoundPlayer();

        switch (soundType)
        {
            case GameSoundType.PingDanger:
                pingSoundPlayer?.Play(ShooterPingType.Danger);
                break;
            case GameSoundType.PingHelp:
                pingSoundPlayer?.Play(ShooterPingType.Help);
                break;
            case GameSoundType.PingNormal:
                pingSoundPlayer?.Play(ShooterPingType.Normal);
                break;
            case GameSoundType.BuildStructure:
                gameEventSoundPlayer?.Play(GameEventSoundType.BuildStructure);
                break;
            case GameSoundType.SupplyItem:
                gameEventSoundPlayer?.Play(GameEventSoundType.SupplyItem);
                break;
            case GameSoundType.ShooterDeath:
                gameEventSoundPlayer?.Play(GameEventSoundType.ShooterDeath);
                break;
        }
    }

    // 게임 이벤트 사운드 재생기 참조 보정
    private void EnsureGameEventSoundPlayer()
    {
        if (gameEventSoundPlayer != null)
            return;

        gameEventSoundPlayer = GameEventSoundPlayer.Instance;
        if (gameEventSoundPlayer == null)
            gameEventSoundPlayer = FindObjectOfType<GameEventSoundPlayer>(true);
    }
}
