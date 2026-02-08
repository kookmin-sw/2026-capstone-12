using UnityEngine;
using UnityEngine.EventSystems;

public class StructurePopupSpawner : MonoBehaviour
{
    public Camera cam;
    public LayerMask structureMask;
    public Canvas canvas;
    public StructurePopupUI popupPrefab;

    private StructurePopupUI current;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        // ESC 또는 바깥 클릭으로 닫기
        if (current != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                // UI 위 클릭이면 닫지 않음
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                    Close();
            }
            return;
        }

        // UI 위 클릭은 무시
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // 구조물 클릭으로 열기
        if (Input.GetMouseButtonDown(0))
            TryOpenAtMouse();
    }

    private void TryOpenAtMouse()
    {
        if (cam == null || popupPrefab == null || canvas == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 2000f, structureMask, QueryTriggerInteraction.Ignore))
            return;

        var target = hit.collider.GetComponentInParent<StructureSelectable>();
        if (target == null) return;

        Open(target, Input.mousePosition);
    }

    private void Open(StructureSelectable target, Vector2 screenPos)
    {
        InputLock.Lock();

        current = Instantiate(popupPrefab, canvas.transform);
        current.SetTarget(target);

        // 위치 보정 포함
        PositionPopup(current.GetComponent<RectTransform>(), screenPos);
    }

    private void Close()
    {
        if (current != null) Destroy(current.gameObject);
        current = null;
        InputLock.Unlock();
    }

    private void PositionPopup(RectTransform popupRect, Vector2 screenPos)
    {
        popupRect.position = screenPos;

        // 화면 밖으로 나가면 안쪽으로 밀기
        Vector3[] corners = new Vector3[4];
        popupRect.GetWorldCorners(corners);

        float left = corners[0].x;
        float right = corners[2].x;
        float bottom = corners[0].y;
        float top = corners[2].y;

        Vector3 offset = Vector3.zero;

        if (left < 0f) offset.x += -left;
        if (right > Screen.width) offset.x -= right - Screen.width;
        if (bottom < 0f) offset.y += -bottom;
        if (top > Screen.height) offset.y -= top - Screen.height;

        popupRect.position += offset;
    }
}