using UnityEngine;

public class TopDownCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam; // 기준 카메라
    [SerializeField] private LayerMask groundMask; // 지면 레이어
    [SerializeField] private Renderer mapBoundsRenderer; // 맵 경계 렌더러
    [SerializeField] private string mapBoundsObjectPath = "MapGround/Plane"; // 맵 경계 경로

    [Header("Move")]
    [SerializeField, Min(0f)] private float moveSpeed = 20f; // 키보드 이동 속도
    [SerializeField, Min(0f)] private float edgeMoveSpeed = 20f; // 테두리 이동 속도
    [SerializeField, Min(0f)] private float edgePanThreshold = 20f; // 테두리 판정 폭

    [Header("Zoom")]
    [SerializeField, Min(0.01f)] private float zoomSpeed = 5f; // 줌 속도
    [SerializeField, Min(0.01f)] private float minDistance = 5f; // 최소 거리
    [SerializeField, Min(0.01f)] private float maxDistance = 60f; // 최대 거리

    [Header("Zoom Fallback Plane")]
    [SerializeField] private bool fallbackToGroundPlane = true; // 평면 보정
    [SerializeField] private float groundPlaneY = 0f; // 평면 높이

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;

    private Vector3 lastMousePos;
    private bool isRotating;
    private float yaw;
    private float pitch;
    private bool wasSettingsOpen = false;

    private void Reset()
    {
        cam = GetComponent<Camera>();
    }

    private void Awake()
    {
        if (cam == null)
            cam = GetComponent<Camera>();
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        edgeMoveSpeed = Mathf.Max(0f, edgeMoveSpeed);
        edgePanThreshold = Mathf.Max(0f, edgePanThreshold);
        zoomSpeed = Mathf.Max(0.01f, zoomSpeed);
        minDistance = Mathf.Max(0.01f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
    }

    private void Start()
    {
        ResolveMapBoundsRenderer();
        TryLookAtLocalPlayer();
    }

    private void Update()
    {
        if (cam == null) return;

        bool settingsOpen = settingsPanel != null && settingsPanel.activeSelf;

        // ESC: 설정창 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel != null)
            {
                if (!settingsOpen) { settingsPanel.SetActive(true); InputLock.Lock(); }
                else               { settingsPanel.SetActive(false); InputLock.Unlock(); }
            }
        }

        // Apply/Cancel 버튼으로 닫힌 경우 InputLock 해제
        if (wasSettingsOpen && !settingsOpen)
            InputLock.Unlock();
        wasSettingsOpen = settingsOpen;

        if (InputLock.IsLocked) return;

        HandleMove();
        HandleZoom();
        ClampToMapBounds();
    }

    // 월드 타겟을 화면 중앙에 맞추기 위한 카메라 목적지 위치 계산
    public bool TryGetCenteredPositionFor(Vector3 worldTarget, out Vector3 destCamPos)
    {
        destCamPos = transform.position;
        if (cam == null) return false;

        Vector3 screenCenter = new Vector3(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f, 0f);
        if (!TryGetMouseWorldOnGround(screenCenter, out Vector3 centerWorld))
            return false;

        Vector3 planarOffset = Vector3.ProjectOnPlane(worldTarget - centerWorld, Vector3.up);
        destCamPos = transform.position + planarOffset;
        return true;
    }

    // 게임 시작 시 로컬 플레이어 화면 중앙 정렬 목적
    private void TryLookAtLocalPlayer()
    {
        Transform playerTarget = FindLocalPlayerTarget();
        if (playerTarget == null || cam == null)
            return;

        Vector3 screenCenter = new Vector3(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f, 0f);
        if (!TryGetMouseWorldOnGround(screenCenter, out Vector3 centerWorld))
            return;

        Vector3 offset = playerTarget.position - centerWorld;
        Vector3 planarOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
        transform.position += planarOffset;
    }

    /// <summary>
    /// 로컬 플레이어 탐색
    /// </summary>
    private Transform FindLocalPlayerTarget()
    {
        PlayerBodyController body = FindObjectOfType<PlayerBodyController>();
        return body != null ? body.transform : null;
    }

    /// <summary>
    /// 맵 경계 렌더러 해석
    /// </summary>
    private void ResolveMapBoundsRenderer()
    {
        if (mapBoundsRenderer != null)
            return;

        GameObject mapBoundsObject = GameObject.Find(mapBoundsObjectPath);
        if (mapBoundsObject == null)
            return;

        mapBoundsRenderer = mapBoundsObject.GetComponent<Renderer>();
    }

    /// <summary>
    /// 키보드와 테두리 이동
    /// </summary>
    private void HandleMove()
    {
        Vector2 keyboardInput = GetKeyboardMoveInput();
        Vector2 edgeInput = GetEdgeMoveInput();
        Vector2 totalInput = keyboardInput * moveSpeed + edgeInput * edgeMoveSpeed;

        if (totalInput.sqrMagnitude < 0.0001f)
            return;

        Vector3 right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up);
        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);

        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.right;
        else
            right.Normalize();

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        Vector3 move = (right * totalInput.x + forward * totalInput.y) * Time.deltaTime;
        transform.position += move;
    }

    /// <summary>
    /// 키보드 이동 입력
    /// </summary>
    private Vector2 GetKeyboardMoveInput()
    {
        Vector2 input = Vector2.zero;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            input.x -= 1f;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            input.x += 1f;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            input.y -= 1f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            input.y += 1f;

        return Vector2.ClampMagnitude(input, 1f);
    }

    /// <summary>
    /// 테두리 이동 입력
    /// </summary>
    private Vector2 GetEdgeMoveInput()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector2 input = Vector2.zero;

        if (mousePosition.x <= edgePanThreshold)
            input.x -= 1f;
        else if (mousePosition.x >= Screen.width - edgePanThreshold)
            input.x += 1f;

        if (mousePosition.y <= edgePanThreshold)
            input.y -= 1f;
        else if (mousePosition.y >= Screen.height - edgePanThreshold)
            input.y += 1f;

        return input;
    }

    /// <summary>
    /// 휠 줌 입력
    /// </summary>
    private void HandleZoom()
    {
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(wheel, 0f))
            return;

        ZoomToCursor(wheel);
    }

    /// <summary>
    /// 커서 기준 줌 이동
    /// </summary>
    private void ZoomToCursor(float wheel)
    {
        if (!TryGetMouseWorldOnGround(Input.mousePosition, out Vector3 hitPoint))
        {
            transform.position += cam.transform.forward * (wheel * zoomSpeed);
            return;
        }

        Vector3 camToHit = hitPoint - transform.position;
        float currentDist = camToHit.magnitude;
        if (currentDist < 0.001f)
            return;

        Vector3 dir = camToHit / currentDist;
        float targetDist = Mathf.Clamp(currentDist - (wheel * zoomSpeed), minDistance, maxDistance);
        float moveAmount = currentDist - targetDist;
        transform.position += dir * moveAmount;
    }

    /// <summary>
    /// 맵 경계 제한
    /// </summary>
    private void ClampToMapBounds()
    {
        Bounds mapBounds = GetMapBounds();
        if (mapBounds.size.sqrMagnitude < 0.0001f)
            return;

        if (!TryGetViewportWorldOnGround(new Vector2(0.5f, 0.5f), out Vector3 centerWorld))
            return;

        Vector3 correction = Vector3.zero;

        if (centerWorld.x < mapBounds.min.x)
            correction.x = mapBounds.min.x - centerWorld.x;
        else if (centerWorld.x > mapBounds.max.x)
            correction.x = mapBounds.max.x - centerWorld.x;

        if (centerWorld.z < mapBounds.min.z)
            correction.z = mapBounds.min.z - centerWorld.z;
        else if (centerWorld.z > mapBounds.max.z)
            correction.z = mapBounds.max.z - centerWorld.z;

        if (correction.sqrMagnitude > 0.0001f)
            transform.position += correction;
    }

    /// <summary>
    /// 맵 경계 영역
    /// </summary>
    private Bounds GetMapBounds()
    {
        if (mapBoundsRenderer != null)
            return mapBoundsRenderer.bounds;

        ResolveMapBoundsRenderer();
        return mapBoundsRenderer != null ? mapBoundsRenderer.bounds : default;
    }

    /// <summary>
    /// 화면 사각형 지면 발자국
    /// </summary>
    private bool TryGetViewportGroundFootprint(out Bounds footprint)
    {
        footprint = default;

        if (!TryGetViewportWorldOnGround(new Vector2(0f, 0f), out Vector3 bottomLeft))
            return false;

        if (!TryGetViewportWorldOnGround(new Vector2(1f, 0f), out Vector3 bottomRight))
            return false;

        if (!TryGetViewportWorldOnGround(new Vector2(0f, 1f), out Vector3 topLeft))
            return false;

        if (!TryGetViewportWorldOnGround(new Vector2(1f, 1f), out Vector3 topRight))
            return false;

        footprint = new Bounds(bottomLeft, Vector3.zero);
        footprint.Encapsulate(bottomRight);
        footprint.Encapsulate(topLeft);
        footprint.Encapsulate(topRight);
        return true;
    }

    /// <summary>
    /// 뷰포트 지면 좌표
    /// </summary>
    private bool TryGetViewportWorldOnGround(Vector2 viewportPos, out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        Ray ray = cam.ViewportPointToRay(new Vector3(viewportPos.x, viewportPos.y, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            worldPos = hit.point;
            return true;
        }

        if (!fallbackToGroundPlane)
            return false;

        float denom = Vector3.Dot(ray.direction, Vector3.up);
        if (Mathf.Abs(denom) < 0.0001f)
            return false;

        float t = (groundPlaneY - ray.origin.y) / denom;
        if (t < 0f)
            return false;

        worldPos = ray.origin + ray.direction * t;
        return true;
    }

    /// <summary>
    /// 지면 월드 좌표 조회
    /// </summary>
    private bool TryGetMouseWorldOnGround(Vector3 screenPos, out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            worldPos = hit.point;
            return true;
        }

        if (!fallbackToGroundPlane)
            return false;

        float denom = Vector3.Dot(ray.direction, Vector3.up);
        if (Mathf.Abs(denom) < 0.0001f)
            return false;

        float t = (groundPlaneY - ray.origin.y) / denom;
        if (t < 0f)
            return false;

        worldPos = ray.origin + ray.direction * t;
        return true;
    }
}
