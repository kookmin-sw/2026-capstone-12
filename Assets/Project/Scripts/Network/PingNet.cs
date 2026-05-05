using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class PingNet : MonoBehaviourPun
{
    // 싱글턴 참조
    public static PingNet Instance { get; private set; }

    // 표시 시간 설정
    [Header("Display")]
    [SerializeField] private float visibleDuration = 1f;
    [SerializeField] private float fadeDuration = 1f;

    // 로컬 핑 요청 도배 방지 설정
    [Header("Rate Limit")]
    [SerializeField] private int maxPingCount = 5;
    [SerializeField] private float pingLimitSeconds = 5f;

    private const string NormalPingPrefabPath = "Prefabs/UI/NormalPing";
    private const string DangerPingPrefabPath = "Prefabs/UI/DangerPing";
    private const string HelpPingPrefabPath = "Prefabs/UI/HelpPing";

    private readonly Queue<float> localPingTimes = new Queue<float>();
    private SoundNet soundNet;

    /// <summary>
    /// 싱글턴 인스턴스와 핑 사운드 재생기 초기화
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        soundNet = SoundNet.Instance;
    }

    /// <summary>
    /// 핑 생성 요청 수신 후 로컬 제한을 통과한 요청만 로컬 또는 RPC로 전파
    /// </summary>
    public void RequestSpawnPing(ShooterPingType pingType, Vector3 worldPosition)
    {
        if (!TryConsumeLocalPing())
            return;

        RequestPingSound(pingType);

        if (PhotonNetwork.InRoom && photonView.ViewID != 0)
        {
            // PingSystem의 PhotonView를 통해 모든 클라이언트가 같은 핑 이벤트를 처리
            photonView.RPC(nameof(RpcSpawnPing), RpcTarget.All, (int)pingType, worldPosition);
            return;
        }

        SpawnPingVisualLocal(pingType, worldPosition);
    }

    /// <summary>
    /// 모든 클라이언트에서 동일한 핑 생성 요청 처리
    /// </summary>
    [PunRPC]
    private void RpcSpawnPing(int pingTypeValue, Vector3 worldPosition)
    {
        SpawnPingVisualLocal((ShooterPingType)pingTypeValue, worldPosition);
    }

    /// <summary>
    /// 현재 클라이언트 화면에 실제 핑 UI 생성
    /// </summary>
    private void SpawnPingVisualLocal(ShooterPingType pingType, Vector3 worldPosition)
    {
        Canvas canvas = FindTargetCanvas();
        Camera cam = ResolveCamera();
        GameObject pingPrefab = Resources.Load<GameObject>(GetPingPrefabPath(pingType));
        if (canvas == null || cam == null || pingPrefab == null)
            return;

        GameObject ping = Instantiate(pingPrefab, canvas.transform);
        // 각 클라이언트는 공유된 월드 좌표를 자기 활성 카메라 기준으로 화면에 투영
        ping.AddComponent<ScreenSpaceWorldPing>().Initialize(worldPosition, cam, visibleDuration, fadeDuration);
    }

    /// <summary>
    /// 핑 사운드 동기화는 SoundNet에 위임하고, 필요 시 공용 사운드 재생기로 대체
    /// </summary>
    private void RequestPingSound(ShooterPingType pingType)
    {
        if (soundNet == null)
            soundNet = SoundNet.Instance;

        if (soundNet != null)
        {
            soundNet.RequestPlay(GetPingSoundType(pingType));
            return;
        }

        GameEventSoundPlayer.Instance?.Play(GetPingSoundType(pingType));
    }

    /// <summary>
    /// 지정된 시간 창 안에서 허용된 로컬 핑 요청 횟수만 소비
    /// </summary>
    private bool TryConsumeLocalPing()
    {
        float now = Time.unscaledTime;
        float limitSeconds = Mathf.Max(0.01f, pingLimitSeconds);
        int limitCount = Mathf.Max(1, maxPingCount);

        while (localPingTimes.Count > 0 && now - localPingTimes.Peek() >= limitSeconds)
            localPingTimes.Dequeue();

        if (localPingTimes.Count >= limitCount)
            return false;

        localPingTimes.Enqueue(now);
        return true;
    }

    /// <summary>
    /// 핑 타입에 대응하는 프리팹 경로 반환
    /// </summary>
    private static string GetPingPrefabPath(ShooterPingType pingType)
    {
        switch (pingType)
        {
            case ShooterPingType.Danger:
                return DangerPingPrefabPath;
            case ShooterPingType.Help:
                return HelpPingPrefabPath;
            default:
                return NormalPingPrefabPath;
        }
    }

    /// <summary>
    /// 핑 타입에 대응하는 공용 게임 사운드 타입 반환
    /// </summary>
    private static GameSoundType GetPingSoundType(ShooterPingType pingType)
    {
        switch (pingType)
        {
            case ShooterPingType.Danger:
                return GameSoundType.PingDanger;
            case ShooterPingType.Help:
                return GameSoundType.PingHelp;
            default:
                return GameSoundType.PingNormal;
        }
    }

    /// <summary>
    /// 핑을 렌더링할 활성 캔버스 탐색
    /// </summary>
    private static Canvas FindTargetCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (!canvas.isActiveAndEnabled || !canvas.gameObject.activeInHierarchy)
                continue;

            if (canvas.name == "SharedCanvas")
                return canvas;
        }

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (!canvas.isActiveAndEnabled || !canvas.gameObject.activeInHierarchy)
                continue;

            if (canvas.name == "ShooterCanvas" || canvas.name == "SuppoterCanvas")
                return canvas;
        }

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas.isActiveAndEnabled && canvas.gameObject.activeInHierarchy)
                return canvas;
        }

        return null;
    }

    /// <summary>
    /// 핑 화면 투영에 사용할 활성 카메라 탐색
    /// </summary>
    private static Camera ResolveCamera()
    {
        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null || !cam.isActiveAndEnabled)
                continue;

            if (cam.CompareTag("MainCamera"))
                return cam;
        }

        return Camera.main;
    }
}
