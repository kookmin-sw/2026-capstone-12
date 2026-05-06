using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MinimapSpaceWorldPing : MonoBehaviour
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Camera minimapCamera;
    private RectTransform minimapRect;
    private Vector3 worldPosition;
    private float visibleUntil = -1f;
    private float destroyAt = -1f;

    public void Initialize(Vector3 targetWorldPosition, Camera mapCamera, RectTransform mapRect, float visibleDuration, float fadeDuration)
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetOrCreateCanvasGroup();
        minimapCamera = mapCamera;
        minimapRect = mapRect;
        worldPosition = targetWorldPosition;

        if (visibleDuration > 0f && fadeDuration > 0f)
        {
            visibleUntil = Time.time + visibleDuration;
            destroyAt = visibleUntil + fadeDuration;
        }

        UpdateVisual();
    }

    private void Update()
    {
        if (destroyAt > 0f && Time.time >= destroyAt)
        {
            Destroy(gameObject);
            return;
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (minimapCamera == null || rectTransform == null || minimapRect == null)
            return;

        Vector3 viewportPos = minimapCamera.WorldToViewportPoint(worldPosition);
        bool outOfView = viewportPos.z <= 0f || viewportPos.x < 0f || viewportPos.x > 1f || viewportPos.y < 0f || viewportPos.y > 1f;

        canvasGroup.alpha = outOfView ? 0f : GetAlpha();
        canvasGroup.blocksRaycasts = false;

        if (!outOfView)
        {
            Rect rect = minimapRect.rect;
            float localX = (viewportPos.x - 0.5f) * rect.width;
            float localY = (viewportPos.y - 0.5f) * rect.height;
            rectTransform.localPosition = new Vector3(localX, localY, 0f);
        }
    }

    private float GetAlpha()
    {
        if (visibleUntil < 0f || destroyAt < 0f || Time.time <= visibleUntil)
            return 1f;

        return Mathf.Clamp01((destroyAt - Time.time) / (destroyAt - visibleUntil));
    }

    private CanvasGroup GetOrCreateCanvasGroup()
    {
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();
        return group;
    }
}
