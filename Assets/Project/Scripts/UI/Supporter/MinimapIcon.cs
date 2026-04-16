using UnityEngine;

/// <summary>
/// 미니맵에 표시할 아이콘 마커
/// - 플레이어, 적, 스폰코어 등에 부착
/// - 미니맵 카메라만 렌더링하는 레이어에서 Quad/Sprite를 표시
/// </summary>
public class MinimapIcon : MonoBehaviour
{
    public enum IconType { Player, Enemy, SpawnCore }

    [Header("Settings")]
    [SerializeField] private IconType iconType = IconType.Enemy;
    [SerializeField] private Color iconColor = Color.red;
    [SerializeField] private float iconSize = 3f;
    [SerializeField] private float heightOffset = 50f;

    private GameObject iconObject;
    private static readonly int MinimapLayer = 10; // "Minimap" 레이어 (TagManager에서 slot 10에 등록됨)

    /// <summary>
    /// 런타임 AddComponent 후 초기화용
    /// </summary>
    public void Init(IconType type, Color color, float size)
    {
        iconType = type;
        iconColor = color;
        iconSize = size;
    }

    private void Start()
    {
        CreateIcon();
    }

    private void LateUpdate()
    {
        if (iconObject == null) return;

        // 부모 오브젝트 위치를 따라가되, 높이만 올림
        Vector3 pos = transform.position;
        iconObject.transform.position = new Vector3(pos.x, pos.y + heightOffset, pos.z);
    }

    private void CreateIcon()
    {
        iconObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        iconObject.name = $"MinimapIcon_{iconType}";
        iconObject.transform.SetParent(transform);
        iconObject.transform.localPosition = new Vector3(0f, heightOffset, 0f);
        iconObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        iconObject.transform.localScale = Vector3.one * iconSize;

        // 콜라이더 즉시 제거 (Rigidbody 하위 non-convex MeshCollider 에러 방지)
        Collider col = iconObject.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        // 레이어 설정
        iconObject.layer = MinimapLayer;

        // 머티리얼 설정 (Unlit 색상)
        Renderer rend = iconObject.GetComponent<Renderer>();
        if (rend != null)
        {
            // URP 호환 Unlit 셰이더 사용
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            rend.material = new Material(shader);
            rend.material.color = iconColor;
            // URP Unlit은 _BaseColor 프로퍼티 사용
            if (rend.material.HasProperty("_BaseColor"))
                rend.material.SetColor("_BaseColor", iconColor);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }
    }

    private void OnDestroy()
    {
        if (iconObject != null)
            Destroy(iconObject);
    }
}
