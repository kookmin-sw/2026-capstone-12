using UnityEngine;

public enum SupportItemKind
{
    AmmoPack,
    HealthPack
}

[CreateAssetMenu(menuName = "TowerDefense/Supporter Item")]
public class SupporterItemSO : ScriptableObject
{
    public SupportItemKind kind; // 아이템 종류 식별값
    public string displayName; // UI 표시 이름
    public string description; // UI 설명 문구
    public Sprite icon; // UI 슬롯 아이콘

    public int typeId; // 네트워크 전송용 고정 순서 ID
    public int cost; // 배치 소모 골드
    public GameObject prefab; // 배치/생성용 프리팹 직접 참조
    public string prefabResourcePath; // Resources 기준 프리팹 경로

    public int ammoAmount; // 총 탄약 회복량
    public float healAmount; // 총 체력 회복량
    public float effectDuration = 2f; // 단계별 회복 지속 시간
}
