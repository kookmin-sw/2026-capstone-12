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

    // 리소스 경로
    private const string NormalPingPrefabPath = "Prefabs/UI/NormalPing";
    private const string DangerPingPrefabPath = "Prefabs/UI/DangerPing";
    private const string HelpPingPrefabPath = "Prefabs/UI/HelpPing";

    /// <summary>
    /// 싱글턴 인스턴스 초기화
    /// </summary>
    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 핑 생성 요청 수신 후 로컬 또는 RPC로 전체 전파
    /// </summary>
    public void RequestSpawnPing(ShooterPingType pingType, Vector3 worldPosition)
    {
        if (PhotonNetwork.IsConnected)
        {
            // 핑 선택은 로컬에서 하지만, 최종 생성은 RPC로 뿌려서 양쪽이 같은 핑을 보게 한다.
            photonView.RPC(nameof(RpcSpawnPing), RpcTarget.All, (int)pingType, worldPosition);
            return;
        }

        SpawnPingLocal(pingType, worldPosition);
    }

    /// <summary>
    /// 모든 클라이언트에서 동일한 핑 생성 요청 처리
    /// </summary>
    [PunRPC]
    private void RpcSpawnPing(int pingTypeValue, Vector3 worldPosition)
    {
        SpawnPingLocal((ShooterPingType)pingTypeValue, worldPosition);
    }

    /// <summary>
    /// 현재 클라이언트 화면에 실제 핑 UI 생성
    /// </summary>
    private void SpawnPingLocal(ShooterPingType pingType, Vector3 worldPosition)
    {
        Canvas canvas = FindTargetCanvas();
        Camera cam = ResolveCamera();
        GameObject pingPrefab = Resources.Load<GameObject>(GetPingPrefabPath(pingType));
        if (canvas == null || cam == null || pingPrefab == null)
            return;

        GameObject ping = Instantiate(pingPrefab, canvas.transform);
        // 각 클라이언트는 공유된 월드 좌표를 자기 활성 카메라 기준으로 화면에 투영한다.
        ping.AddComponent<ScreenSpaceWorldPing>().Initialize(worldPosition, cam, visibleDuration, fadeDuration);

        SpawnMinimapPingMarker(pingType, worldPosition);
    }

    /// <summary>
    /// 미니맵 카메라가 있는 클라이언트(서포터)에서만 기존 핑 이미지를 미니맵 UI에 표시
    /// </summary>
    private void SpawnMinimapPingMarker(ShooterPingType pingType, Vector3 worldPosition)
    {
        Camera minimapCam = FindMinimapCamera();
        if (minimapCam == null) return;

        RectTransform minimapRect = FindMinimapRect();
        if (minimapRect == null) return;

        GameObject pingPrefab = Resources.Load<GameObject>(GetPingPrefabPath(pingType));
        if (pingPrefab == null) return;

        GameObject marker = Instantiate(pingPrefab, minimapRect);
        marker.GetComponent<RectTransform>().localScale = Vector3.one * 0.4f;
        marker.AddComponent<MinimapSpaceWorldPing>().Initialize(worldPosition, minimapCam, minimapRect, visibleDuration, fadeDuration);
    }

    private static Camera FindMinimapCamera()
    {
        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].name == "MinimapCamera")
                return cameras[i];
        }
        return null;
    }

    private static RectTransform FindMinimapRect()
    {
        UnityEngine.UI.RawImage[] rawImages = FindObjectsOfType<UnityEngine.UI.RawImage>();
        for (int i = 0; i < rawImages.Length; i++)
        {
            if (rawImages[i].name == "MinimapRawImage")
                return rawImages[i].GetComponent<RectTransform>();
        }
        return null;
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
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            return Camera.main;

        // 서포터 카메라처럼 MainCamera 태그가 없는 경우 미니맵 전용 카메라를 제외한 첫 번째 활성 카메라 사용
        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null || !cam.isActiveAndEnabled)
                continue;

            if (cam.name == "MinimapCamera")
                continue;

            return cam;
        }

        return null;
    }
}
