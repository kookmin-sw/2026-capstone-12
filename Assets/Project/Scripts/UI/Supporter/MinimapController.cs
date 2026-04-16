using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 서포터 미니맵 컨트롤러
/// - RenderTexture 기반 미니맵 표시
/// - 플레이어/적 아이콘 표시 (MinimapIcon 연동)
/// - 미니맵 클릭 시 서포터 카메라를 해당 월드 위치로 이동
/// </summary>
public class MinimapController : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private TopDownCameraController topDownCamera;

    [Header("Minimap Camera Settings")]
    [SerializeField] private float minimapHeight = 120f;
    [SerializeField] private float minimapOrthoSize = 80f;

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

    private void LateUpdate()
    {
        if (minimapCamera == null || topDownCamera == null) return;

        // 서포터 카메라의 XZ 위치를 미니맵 카메라 중심으로 추적
        Vector3 supporterPos = topDownCamera.transform.position;
        minimapCamera.transform.position = new Vector3(supporterPos.x, minimapHeight, supporterPos.z);
    }

    private void SetupRenderTexture()
    {
        int size = Mathf.RoundToInt(minimapRect.rect.width);
        if (size <= 0) size = 256;

        renderTexture = new RenderTexture(size, size, 16);
        renderTexture.filterMode = FilterMode.Bilinear;

        minimapCamera.targetTexture = renderTexture;
        minimapImage.texture = renderTexture;
    }

    private void SetupMinimapCamera()
    {
        if (minimapCamera == null) return;

        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = minimapOrthoSize;
        minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        minimapCamera.transform.position = new Vector3(0f, minimapHeight, 0f);
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        minimapCamera.depth = -10;

        // 미니맵 카메라: 기본 레이어 + Minimap 레이어(10) 렌더링
        minimapCamera.cullingMask = minimapCamera.cullingMask | (1 << 10);

        // 메인 카메라에서 Minimap 레이어 제외
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam != minimapCamera)
            mainCam.cullingMask &= ~(1 << 10);
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
