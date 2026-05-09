using UnityEngine;

public enum PurificationLightSourceType
{
    Sanctuary,
    ShooterField,
    Beacon,
    PermanentPurifiedZone,
    LightPylon // LightPylon 정화 광원
}

[DisallowMultipleComponent]
public class PurificationLightSource : MonoBehaviour
{
    [Header("Purification Light Source")]
    // 정화 광원 종류 구분값
    public PurificationLightSourceType sourceType = PurificationLightSourceType.Sanctuary;
    // 현재 광원이 보호하거나 정화하는 월드 반경
    public float radius = 10f;
    // 여러 정화 광원이 겹칠 때 사용할 정화 강도값
    public float strength = 1f;
    // 어둠 노출 차단 여부
    public bool preventsDarknessExposure = true;
    // SpawnCore 파괴 후 생성되는 영구 정화 구역 구분값
    public bool isPermanent;

    // 지정 월드 좌표의 현재 정화 광원 포함 여부 확인
    public bool Contains(Vector3 worldPosition)
    {
        return Vector3.Distance(transform.position, worldPosition) <= radius;
    }

    private void OnEnable()
    {
        // 활성 정화 광원 등록
        PurificationZoneRegistry.Register(this);
    }

    private void OnDisable()
    {
        // 비활성 정화 광원 등록 해제
        PurificationZoneRegistry.Unregister(this);
    }

    private void OnValidate()
    {
        // 인스펙터 음수 수치 입력 방지
        radius = Mathf.Max(0f, radius);
        strength = Mathf.Max(0f, strength);
    }

    private void OnDrawGizmosSelected()
    {
        // 선택된 정화 광원 반경 확인용 Gizmo 표시
        Gizmos.color = new Color(1f, 0.92f, 0.55f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
