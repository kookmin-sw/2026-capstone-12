using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ScreenSpaceWorldPing : MonoBehaviour
{
    // UI/투영 참조
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Camera targetCamera;

    // 월드 좌표와 수명 상태
    private Vector3 worldPosition;
    private float visibleUntil = -1f;
    private float destroyAt = -1f;

    /// <summary>
    /// 수명 제한 없이 월드 좌표 추적 UI 초기화
    /// </summary>
    public void Initialize(Vector3 targetWorldPosition, Camera worldCamera)
    {
        Initialize(targetWorldPosition, worldCamera, -1f, -1f);
    }

    /// <summary>
    /// 월드 좌표, 카메라, 표시 시간을 받아 핑 UI 초기화
    /// </summary>
    public void Initialize(Vector3 targetWorldPosition, Camera worldCamera, float visibleDuration, float fadeDuration)
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetOrCreateCanvasGroup();
        targetCamera = worldCamera;
        worldPosition = targetWorldPosition;

        // 셀렉터는 음수 시간을 넘겨서 컨트롤러가 직접 지울 때까지 유지되게 한다.
        if (visibleDuration > 0f && fadeDuration > 0f)
        {
            visibleUntil = Time.time + visibleDuration;
            destroyAt = visibleUntil + fadeDuration;
        }
        else
        {
            visibleUntil = -1f;
            destroyAt = -1f;
        }

        UpdateVisual();
    }

    /// <summary>
    /// 필요한 UI 컴포넌트 참조 준비
    /// </summary>
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetOrCreateCanvasGroup();
    }

    /// <summary>
    /// 매 프레임 위치와 알파 갱신 후 수명 종료 시 제거
    /// </summary>
    private void Update()
    {
        if (destroyAt > 0f && Time.time >= destroyAt)
        {
            Destroy(gameObject);
            return;
        }

        UpdateVisual();
    }

    /// <summary>
    /// 월드 좌표를 화면 좌표로 투영하고 현재 알파 적용
    /// </summary>
    private void UpdateVisual()
    {
        Camera cam = ResolveCamera();
        if (cam == null || rectTransform == null || canvasGroup == null)
            return;

        Vector3 screenPoint = cam.WorldToScreenPoint(worldPosition);
        bool behindCamera = screenPoint.z <= 0f;

        canvasGroup.alpha = behindCamera ? 0f : GetAlpha();
        canvasGroup.blocksRaycasts = false;

        if (!behindCamera)
            rectTransform.position = screenPoint;
    }

    /// <summary>
    /// 현재 시간 기준으로 핑의 투명도 계산
    /// </summary>
    private float GetAlpha()
    {
        if (visibleUntil < 0f || destroyAt < 0f || Time.time <= visibleUntil)
            return 1f;

        return Mathf.Clamp01((destroyAt - Time.time) / (destroyAt - visibleUntil));
    }

    /// <summary>
    /// CanvasGroup이 없으면 추가 후 반환
    /// </summary>
    private CanvasGroup GetOrCreateCanvasGroup()
    {
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        return group;
    }

    /// <summary>
    /// 월드 좌표를 화면에 투영할 카메라 탐색
    /// </summary>
    private Camera ResolveCamera()
    {
        if (targetCamera != null && targetCamera.isActiveAndEnabled)
            return targetCamera;

        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            targetCamera = Camera.main;

        return targetCamera;
    }
}
