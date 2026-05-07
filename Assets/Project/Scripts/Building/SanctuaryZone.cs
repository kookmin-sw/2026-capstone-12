using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SanctuaryZone : MonoBehaviour
{
    // 플레이어/어둠 판정에서 전역 조회할 활성 정화 구역 목록
    private static readonly List<SanctuaryZone> activeZones = new List<SanctuaryZone>();

    [Header("Sanctuary Zone")]
    // CommandTower 중심의 어둠 노출 차단 기본 반경
    public float radius = 25f;
    // 정화 구역별 어둠 차단 여부 제어 플래그
    public bool preventsDarknessExposure = true;

    // 외부 시스템용 활성 정화 구역 읽기 전용 목록
    public static IReadOnlyList<SanctuaryZone> ActiveZones => activeZones;

    // 지정 월드 좌표의 현재 정화 구역 포함 여부 확인
    public bool IsInside(Vector3 worldPosition)
    {
        return Vector3.Distance(transform.position, worldPosition) <= radius;
    }

    // 활성 정화 구역 중 하나 이상의 월드 좌표 보호 여부 확인
    public static bool IsInsideAnySanctuary(Vector3 worldPosition)
    {
        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            SanctuaryZone zone = activeZones[i];
            if (zone == null)
            {
                activeZones.RemoveAt(i);
                continue;
            }

            if (zone.preventsDarknessExposure && zone.IsInside(worldPosition))
                return true;
        }

        return false;
    }

    private void OnEnable()
    {
        // 동일 구역 중복 등록 방지
        if (!activeZones.Contains(this))
            activeZones.Add(this);
    }

    private void OnDisable()
    {
        activeZones.Remove(this);
    }

    private void OnValidate()
    {
        // 인스펙터 음수 반경 입력 방지
        radius = Mathf.Max(0f, radius);
    }

    private void OnDrawGizmos()
    {
        // Scene 뷰 정화 구역 범위 확인용 Gizmo 표시
        Gizmos.color = new Color(0.45f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
