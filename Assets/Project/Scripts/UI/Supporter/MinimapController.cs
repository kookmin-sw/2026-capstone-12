using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 서포터 미니맵 컨트롤러
/// - RenderTexture 기반 미니맵 표시
/// - 맵 전체를 고정으로 표시 (ground 오브젝트 바운드 기반)
/// - 미니맵 클릭 시 서포터 카메라를 해당 월드 위치로 이동
/// </summary>
public class MinimapController : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private TopDownCameraController topDownCamera;

    [Header("Minimap Camera Settings")]
    [SerializeField] private float minimapHeight = 200f;
    [SerializeField] private float padding = 10f;

    [Header("Ground Objects (맵 바닥)")]
    [SerializeField] private string[] groundNames = { "ground_1", "ground_1_1", "ground_1_2", "ground_1_3" };

    private RenderTexture renderTexture;
    private RectTransform minimapRect;

    private void Awake()
    {
        minimapRect = minimapImage.GetComponent<RectTransform>();
    }

    private void Start()
    {
        SetupRenderTexture();
        SetupMinimapCamera();
    }

    private void SetupRenderTexture()
    {
        int width = Mathf.RoundToInt(minimapRect.rect.width);
        int height = Mathf.RoundToInt(minimapRect.rect.height);
        if (width <= 0) width = 256;
        if (height <= 0) height = 256;

        renderTexture = new RenderTexture(width, height, 16);
        renderTexture.filterMode = FilterMode.Bilinear;

        minimapCamera.targetTexture = renderTexture;
        minimapImage.texture = renderTexture;
    }

    private void SetupMinimapCamera()
    {
        if (minimapCamera == null) return;

        minimapCamera.orthographic = true;
        minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        minimapCamera.depth = -10;

        minimapCamera.cullingMask = minimapCamera.cullingMask | (1 << 10);

        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam != minimapCamera)
            mainCam.cullingMask &= ~(1 << 10);

        FitCameraToMap();
    }

    private void FitCameraToMap()
    {
        Bounds mapBounds = CalculateMapBounds();
        Vector3 center = mapBounds.center;
        minimapCamera.transform.position = new Vector3(center.x, minimapHeight, center.z);

        float mapWidth = mapBounds.size.x + padding * 2f;
        float mapDepth = mapBounds.size.z + padding * 2f;

        float aspect = minimapCamera.aspect;
        float orthoSizeForDepth = mapDepth * 0.5f;
        float orthoSizeForWidth = mapWidth * 0.5f / aspect;
        minimapCamera.orthographicSize = Mathf.Max(orthoSizeForDepth, orthoSizeForWidth);
    }

    private Bounds CalculateMapBounds()
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool initialized = false;

        foreach (string groundName in groundNames)
        {
            GameObject go = GameObject.Find(groundName);
            if (go == null) continue;

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in renderers)
            {
                if (rend.gameObject.layer == 10) continue;
                if (!initialized)
                {
                    bounds = rend.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(rend.bounds);
                }
            }
        }

        if (!initialized)
            bounds = new Bounds(Vector3.zero, new Vector3(200f, 0f, 200f));

        return bounds;
    }

    /// <summary>
    /// 미니맵 클릭 시 서포터 카메라를 해당 월드 좌표로 이동
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (topDownCamera == null || minimapCamera == null) return;

        // 클릭 위치를 미니맵 RectTransform 내 로컬 좌표로 변환
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                minimapRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return;

        // 로컬 좌표를 0~1 비율로 변환
        Rect rect = minimapRect.rect;
        float normalizedX = (localPoint.x - rect.x) / rect.width;
        float normalizedY = (localPoint.y - rect.y) / rect.height;

        // 미니맵 카메라의 뷰포트 → 월드 좌표 변환
        Ray ray = minimapCamera.ViewportPointToRay(new Vector3(normalizedX, normalizedY, 0f));

        // 지면(Y=0) 과 교차점 계산
        float denom = Vector3.Dot(ray.direction, Vector3.up);
        if (Mathf.Abs(denom) < 0.0001f) return;

        float t = (0f - ray.origin.y) / denom;
        if (t < 0f) return;

        Vector3 worldPoint = ray.origin + ray.direction * t;

        // 서포터 카메라의 현재 높이를 유지하면서 XZ만 이동
        Vector3 camPos = topDownCamera.transform.position;
        topDownCamera.transform.position = new Vector3(worldPoint.x, camPos.y, worldPoint.z);
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
