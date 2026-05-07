using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PurificationLightSource))]
public class SanctuaryZone : MonoBehaviour
{
    [Header("Sanctuary Zone")]
    // CommandTower 중심의 어둠 노출 차단 기본 반경
    public float radius = 25f;
    // 본진 안전 구역의 어둠 노출 차단 여부
    public bool preventsDarknessExposure = true;

    private PurificationLightSource lightSource;

    // 지정 월드 좌표의 본진 안전 구역 포함 여부 확인
    public bool IsInside(Vector3 worldPosition)
    {
        return Vector3.Distance(transform.position, worldPosition) <= radius;
    }

    private void Awake()
    {
        // 본진 안전 구역용 정화 광원 참조 확보
        CacheLightSource();
    }

    private void OnEnable()
    {
        // 본진 안전 구역 설정의 공통 정화 광원 반영
        ApplyToLightSource();
    }

    private void OnValidate()
    {
        // 인스펙터 음수 반경 입력 방지
        radius = Mathf.Max(0f, radius);
        CacheLightSource();
        ApplyToLightSource();
    }

    private void OnDrawGizmos()
    {
        // Scene 뷰 본진 안전 구역 범위 확인용 Gizmo 표시
        Gizmos.color = new Color(0.45f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void CacheLightSource()
    {
        if (lightSource == null)
            lightSource = GetComponent<PurificationLightSource>();
    }

    private void ApplyToLightSource()
    {
        if (lightSource == null)
            return;

        lightSource.sourceType = PurificationLightSourceType.Sanctuary;
        lightSource.radius = radius;
        lightSource.strength = 1f;
        lightSource.preventsDarknessExposure = preventsDarknessExposure;
        lightSource.isPermanent = true;
    }
}
