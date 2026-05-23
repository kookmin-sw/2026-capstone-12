using Photon.Pun;
using UnityEngine;

public class SupporterPingController : MonoBehaviour
{
    // 입력 설정
    [Header("Input")]
    [SerializeField] private KeyCode pingKey = KeyCode.G;
    [SerializeField] private float selectorSensitivity = 45f;
    [SerializeField] private float selectorThreshold = 35f;
    [SerializeField] private float selectorClamp = 120f;

    // 월드 좌표 산출 설정
    [Header("Raycast")]
    [SerializeField] private LayerMask pingMask = ~0;
    [SerializeField] private float maxPingDistance = 500f;

    // 리소스 경로
    private const string SelectorPrefabPath = "Prefabs/UI/PingSelectorUI";

    // 런타임 참조
    private Camera worldCamera;

    // 선택 UI 상태
    private GameObject selectorInstance;
    private GameObject topHighlight;
    private GameObject centerHighlight;
    private GameObject bottomHighlight;

    // 선택 값
    private Vector3 selectedWorldPosition;
    private float selectorOffset;
    private bool isPlacementArmed;

    /// <summary>
    /// 서포터 입력에 사용할 카메라 참조 준비
    /// </summary>
    private void Awake()
    {
        worldCamera = GetComponent<Camera>();
        if (worldCamera == null)
            worldCamera = GetComponentInChildren<Camera>(true);
    }

    /// <summary>
    /// 비활성화 시 남아 있는 선택 UI 정리
    /// </summary>
    private void OnDisable()
    {
        ClearSelector();
    }

    /// <summary>
    /// 서포터 핑 입력, 배치 대기 상태, 선택 상태 갱신
    /// </summary>
    private void Update()
    {
        if (!CanUsePing())
        {
            ClearSelector();
            return;
        }

        if (selectorInstance == null)
        {
            // 서포터는 먼저 핑 배치 대기 상태에 들어가고, 이후 클릭으로 정확한 위치를 고른다. // 롤 참고
            if (KeybindingManager.GetKeyDownStatic(KeyAction.Ping))
                isPlacementArmed = true;

            if (isPlacementArmed && Input.GetMouseButtonDown(0))
                BeginSelection();

            if (isPlacementArmed && Input.GetKeyDown(KeyCode.Escape))
                isPlacementArmed = false;

            return;
        }

        selectorOffset += Input.GetAxisRaw("Mouse Y") * selectorSensitivity;
        selectorOffset = Mathf.Clamp(selectorOffset, -selectorClamp, selectorClamp);
        SetHighlight(GetCurrentPingType());

        // 배치 클릭을 떼는 순간 현재 하이라이트된 핑 타입으로 확정된다.
        if (Input.GetMouseButtonUp(0))
            CommitSelection();
    }

    /// <summary>
    /// 현재 로컬 클라이언트의 서포터 핑 입력 가능 여부 검사
    /// </summary>
    private bool CanUsePing()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null)
            return true;

        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
            return true;

        return (PhotonNetwork.LocalPlayer.CustomProperties["Role"] as string) == "Supporter";
    }

    /// <summary>
    /// 클릭한 월드 지점에 핑 셀렉터 생성
    /// </summary>
    private void BeginSelection()
    {
        if (!TryGetPingWorldPosition(out selectedWorldPosition))
            return;

        Canvas canvas = FindTargetCanvas();
        GameObject selectorPrefab = Resources.Load<GameObject>(SelectorPrefabPath);
        if (canvas == null || selectorPrefab == null)
            return;

        isPlacementArmed = false;
        selectorOffset = 0f;
        selectorInstance = Instantiate(selectorPrefab, canvas.transform);
        // 카메라는 계속 움직여도 셀렉터는 클릭한 월드 지점에 고정된다.
        selectorInstance.AddComponent<ScreenSpaceWorldPing>().Initialize(selectedWorldPosition, ResolveCamera());

        topHighlight = selectorInstance.transform.Find("TopHighlight")?.gameObject;
        centerHighlight = selectorInstance.transform.Find("CircleHighlight")?.gameObject;
        bottomHighlight = selectorInstance.transform.Find("BottomHighlight")?.gameObject;
        SetHighlight(ShooterPingType.Normal);
    }

    /// <summary>
    /// 현재 선택된 핑 타입을 네트워크 생성 요청으로 전달
    /// </summary>
    private void CommitSelection()
    {
        ShooterPingType pingType = GetCurrentPingType();
        Vector3 worldPosition = selectedWorldPosition;

        ClearSelector();
        PingNet pingNet = PingNet.Instance;
        if (pingNet != null)
            pingNet.RequestSpawnPing(pingType, worldPosition);
    }

    /// <summary>
    /// 셀렉터와 배치 관련 임시 상태 초기화
    /// </summary>
    private void ClearSelector()
    {
        if (selectorInstance != null)
            Destroy(selectorInstance);

        selectorInstance = null;
        topHighlight = null;
        centerHighlight = null;
        bottomHighlight = null;
        selectorOffset = 0f;
    }

    /// <summary>
    /// 누적된 마우스 Y 입력으로 현재 핑 타입 계산
    /// </summary>
    private ShooterPingType GetCurrentPingType()
    {
        if (selectorOffset > selectorThreshold)
            return ShooterPingType.Danger;

        if (selectorOffset < -selectorThreshold)
            return ShooterPingType.Help;

        return ShooterPingType.Normal;
    }

    /// <summary>
    /// 현재 핑 타입에 맞는 셀렉터 하이라이트 표시
    /// </summary>
    private void SetHighlight(ShooterPingType pingType)
    {
        if (topHighlight != null)
            topHighlight.SetActive(pingType == ShooterPingType.Danger);

        if (centerHighlight != null)
            centerHighlight.SetActive(pingType == ShooterPingType.Normal);

        if (bottomHighlight != null)
            bottomHighlight.SetActive(pingType == ShooterPingType.Help);
    }

    /// <summary>
    /// 마우스로 클릭한 지점의 월드 좌표 계산
    /// </summary>
    private bool TryGetPingWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        Camera cam = ResolveCamera();
        if (cam == null)
            return false;

        // 서포터 핑은 실제 클릭한 화면 위치를 기준으로 월드 지점을 잡는다.
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxPingDistance, pingMask, QueryTriggerInteraction.Ignore))
        {
            worldPosition = hit.point;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 핑 셀렉터를 붙일 활성 캔버스 탐색
    /// </summary>
    private Canvas FindTargetCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (!canvas.isActiveAndEnabled || !canvas.gameObject.activeInHierarchy)
                continue;

            if (canvas.name == "SharedCanvas" || canvas.name == "SuppoterCanvas")
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
    /// 현재 입력과 화면 투영에 사용할 카메라 탐색
    /// </summary>
    private Camera ResolveCamera()
    {
        if (worldCamera != null && worldCamera.isActiveAndEnabled)
            return worldCamera;

        worldCamera = GetComponent<Camera>();
        if (worldCamera != null && worldCamera.isActiveAndEnabled)
            return worldCamera;

        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            return Camera.main;

        return null;
    }
}
