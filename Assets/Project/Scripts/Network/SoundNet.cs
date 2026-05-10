using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class SoundNet : MonoBehaviourPun
{
    public static SoundNet Instance { get; private set; }

    private enum SoundPlaybackMode
    {
        Network2D,
        Network3D,
        Local2D,
        Local3D
    }

    [SerializeField] private GameEventSoundPlayer gameEventSoundPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        if (gameEventSoundPlayer == null)
            gameEventSoundPlayer = FindObjectOfType<GameEventSoundPlayer>(true);
    }

    // 게임 사운드 재생 요청을 네트워크 상황에 맞게 전달
    public void RequestPlay(GameSoundType soundType)
    {
        SoundPlaybackMode playbackMode = GetPlaybackMode(soundType); // 사운드 타입별 기본 재생 정책
        if (playbackMode == SoundPlaybackMode.Local2D)
        {
            PlayLocal(soundType);
            return;
        }

        if (playbackMode == SoundPlaybackMode.Network3D || playbackMode == SoundPlaybackMode.Local3D)
        {
            Debug.LogWarning($"{soundType} requires a position. Use RequestPlayAt instead.");
            return;
        }

        RequestPlayNetwork(soundType);
    }

    // 위치 기반 게임 사운드 재생 요청을 네트워크 상황에 맞게 전달
    public void RequestPlayAt(GameSoundType soundType, Vector3 position)
    {
        SoundPlaybackMode playbackMode = GetPlaybackMode(soundType); // 사운드 타입별 기본 재생 정책
        if (playbackMode == SoundPlaybackMode.Local3D)
        {
            PlayLocalAt(soundType, position);
            return;
        }

        if (playbackMode == SoundPlaybackMode.Local2D)
        {
            PlayLocal(soundType);
            return;
        }

        if (playbackMode == SoundPlaybackMode.Network2D)
        {
            RequestPlayNetwork(soundType);
            return;
        }

        RequestPlayAtNetwork(soundType, position);
    }

    // 위치 없는 네트워크 사운드 전파
    private void RequestPlayNetwork(GameSoundType soundType)
    {
        if (PhotonNetwork.InRoom && photonView.ViewID != 0)
        {
            photonView.RPC(nameof(RpcPlay), RpcTarget.All, (int)soundType);
            return;
        }

        PlayLocal(soundType);
    }

    // 위치 기반 네트워크 사운드 전파
    private void RequestPlayAtNetwork(GameSoundType soundType, Vector3 position)
    {
        if (PhotonNetwork.InRoom && photonView.ViewID != 0)
        {
            photonView.RPC(nameof(RpcPlayAt), RpcTarget.All, (int)soundType, position);
            return;
        }

        PlayLocalAt(soundType, position);
    }

    // RPC로 전달받은 게임 사운드를 현재 클라이언트에서 재생
    [PunRPC]
    private void RpcPlay(int soundTypeValue)
    {
        PlayLocal((GameSoundType)soundTypeValue);
    }

    // RPC로 전달받은 위치 기반 게임 사운드를 현재 클라이언트에서 재생
    [PunRPC]
    private void RpcPlayAt(int soundTypeValue, Vector3 position)
    {
        PlayLocalAt((GameSoundType)soundTypeValue, position);
    }

    // 네트워크 동기화 없이 현재 클라이언트에서 게임 사운드 재생
    public void PlayLocal(GameSoundType soundType)
    {
        EnsureGameEventSoundPlayer();
        gameEventSoundPlayer?.Play(soundType);
    }

    // 네트워크 동기화 없이 현재 클라이언트에서 위치 기반 게임 사운드 재생
    public void PlayLocalAt(GameSoundType soundType, Vector3 position)
    {
        EnsureGameEventSoundPlayer();
        gameEventSoundPlayer?.PlayAt(soundType, position);
    }

    // 사운드 타입별 기본 재생 방식 조회
    private SoundPlaybackMode GetPlaybackMode(GameSoundType soundType)
    {
        return soundType switch
        {
            GameSoundType.TurretAttack => SoundPlaybackMode.Network3D,
            GameSoundType.BuildingDestroyed => SoundPlaybackMode.Network3D,
            GameSoundType.SpawnCoreDestroyed => SoundPlaybackMode.Network3D,
            GameSoundType.SlowTowerActivated => SoundPlaybackMode.Network3D,
            GameSoundType.LightPylonActivated => SoundPlaybackMode.Network3D,
            GameSoundType.PurificationBeaconActivated => SoundPlaybackMode.Network3D,
            GameSoundType.BuildStructure => SoundPlaybackMode.Network3D,
            GameSoundType.SupplyItem => SoundPlaybackMode.Network3D,
            GameSoundType.GetItem => SoundPlaybackMode.Local3D,
            GameSoundType.ApplyHealthPack => SoundPlaybackMode.Local2D,
            GameSoundType.ApplyAmmoPack => SoundPlaybackMode.Local2D,
            _ => SoundPlaybackMode.Network2D
        };
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
