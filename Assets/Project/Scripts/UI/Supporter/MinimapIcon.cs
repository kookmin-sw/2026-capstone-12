using UnityEngine;

/// <summary>
/// 미니맵에 표시할 아이콘 마커
/// - 플레이어, 적, 스폰코어 등에 부착
/// - 미니맵 카메라만 렌더링하는 레이어에서 Quad/Sprite를 표시
/// </summary>
public class MinimapIcon : MonoBehaviour
{
    public enum IconType { Player, Enemy, SpawnCore }
    private const float PurificationCheckInterval = 0.25f; // 정화 영역 재확인 주기

    [Header("Settings")]
    [SerializeField] private IconType iconType = IconType.Enemy;
    [SerializeField] private Color iconColor = Color.red;
    [SerializeField] private float iconSize = 3f;
    [SerializeField] private float heightOffset = 50f;

    [Header("Enemy Darkness Signal")]
    [SerializeField] private float darkSignalMinInterval = 1.5f; // 어둠 신호 최소 표시 간격
    [SerializeField] private float darkSignalMaxInterval = 2.5f; // 어둠 신호 최대 표시 간격
    [SerializeField] private float darkSignalVisibleDuration = 0.55f; // 어둠 신호 유지 시간
    [SerializeField] private float darkSignalOffsetRadius = 6f; // 어둠 신호 위치 오차 반경
    [SerializeField] private Color darkSignalColor = new Color(1f, 0.1f, 0.05f, 0.75f); // 어둠 신호 색상

    private GameObject iconObject;
    private Renderer cachedRenderer;
    private PurificationZoneRegistry purificationRegistry; // 정화 영역 레지스트리 캐시
    private Vector3 darkSignalWorldPosition; // 현재 어둠 신호 월드 위치
    private float nextDarkSignalTime; // 다음 어둠 신호 표시 시각
    private float darkSignalHideTime; // 현재 어둠 신호 숨김 시각
    private float nextPurificationCheckTime; // 다음 정화 상태 확인 시각
    private bool useDarkSignal; // 캐시된 어둠 신호 사용 여부
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
        ScheduleNextDarkSignal(0f);
    }

    private void LateUpdate()
    {
        if (iconObject == null) return;

        // 부모 오브젝트 위치를 따라가되, 높이만 올림
        // 어둠 속 적 아이콘을 근사 신호로 대체하는 분기
        if (ShouldUseDarkSignal())
        {
            UpdateDarkEnemySignal();
            return;
        }

        // 정화 영역 안의 정확한 아이콘 표시 복원
        iconObject.SetActive(true);
        SetIconColor(iconColor);

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
            cachedRenderer = rend;
            SetIconColor(iconColor);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }
    }

    // 적 아이콘의 어둠 속 근사 신호 전환 여부
    private bool ShouldUseDarkSignal()
    {
        if (iconType != IconType.Enemy)
            return false;

        // 정화 상태 캐시 유효 시간 확인
        if (Time.time < nextPurificationCheckTime)
            return useDarkSignal;

        nextPurificationCheckTime = Time.time + PurificationCheckInterval;

        // 정화 레지스트리 지연 조회
        if (purificationRegistry == null)
            purificationRegistry = PurificationZoneRegistry.Instance;

        useDarkSignal = purificationRegistry != null && !purificationRegistry.IsPositionPurified(transform.position);
        return useDarkSignal;
    }

    // 어둠 속 적 위치를 오차가 있는 임시 신호로 표시하는 갱신
    private void UpdateDarkEnemySignal()
    {
        float now = Time.time;

        // 다음 신호 위치와 유지 시간 계산
        if (now >= nextDarkSignalTime)
        {
            Vector2 offset = Random.insideUnitCircle * Mathf.Max(0f, darkSignalOffsetRadius);
            Vector3 pos = transform.position;
            darkSignalWorldPosition = new Vector3(pos.x + offset.x, pos.y + heightOffset, pos.z + offset.y);
            darkSignalHideTime = now + Mathf.Max(0.05f, darkSignalVisibleDuration);
            ScheduleNextDarkSignal(now);
        }

        bool visible = now < darkSignalHideTime;
        iconObject.SetActive(visible);
        if (!visible)
            return;

        SetIconColor(darkSignalColor);
        iconObject.transform.position = darkSignalWorldPosition;
    }

    private void SetIconColor(Color color)
    {
        if (cachedRenderer == null || cachedRenderer.material == null)
            return;

        cachedRenderer.material.color = color;
        // URP Unlit은 _BaseColor 프로퍼티 사용
        if (cachedRenderer.material.HasProperty("_BaseColor"))
            cachedRenderer.material.SetColor("_BaseColor", color);
    }

    // 다음 어둠 신호 표시 시각 예약
    private void ScheduleNextDarkSignal(float fromTime)
    {
        float minInterval = Mathf.Max(0.05f, darkSignalMinInterval);
        float maxInterval = Mathf.Max(minInterval, darkSignalMaxInterval);
        nextDarkSignalTime = fromTime + Random.Range(minInterval, maxInterval);
    }

    private void OnDestroy()
    {
        if (iconObject != null)
            Destroy(iconObject);
    }
}
