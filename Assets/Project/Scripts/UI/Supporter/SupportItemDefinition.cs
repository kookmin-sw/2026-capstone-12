using UnityEngine;

public enum SupportItemKind
{
    AmmoPack, // 탄약 회복 아이템 구분값
    HealthPack // 체력 회복 아이템 구분값
}

[System.Serializable]
public class SupportItemDefinition
{
    public SupportItemKind kind; // 아이템 종류 식별값
    public string displayName; // UI 표시 이름
    public string description; // UI 설명 문구
    public int cost; // 배치 소모 골드
    public string prefabResourcePath; // Resources 기준 프리팹 경로
    public int ammoAmount; // 총 탄약 회복량
    public float healAmount; // 총 체력 회복량
    public float effectDuration = 2f; // 단계별 회복 지속 시간
}

public static class SupportItemCatalog
{
    private static readonly SupportItemDefinition[] Items = // Support 슬롯과 배치가 공유하는 고정 아이템 목록
    {
        new()
        {
            kind = SupportItemKind.AmmoPack,
            displayName = "Ammo Pack",
            description = "Slowly restores shooter ammo.",
            cost = 20,
            prefabResourcePath = "Prefabs/Support/AmmoPack",
            ammoAmount = 30,
            healAmount = 0f,
            effectDuration = 2f
        },
        new()
        {
            kind = SupportItemKind.HealthPack,
            displayName = "Health Pack",
            description = "Slowly restores shooter health.",
            cost = 25,
            prefabResourcePath = "Prefabs/Support/HealthPack",
            ammoAmount = 0,
            healAmount = 35f,
            effectDuration = 2.5f
        }
    };

    public static int Count => Items.Length; // Support 아이템 개수 조회

    // 슬롯 순서 기반 아이템 조회
    public static SupportItemDefinition Get(int index)
    {
        if (index < 0 || index >= Items.Length)
            return null;

        return Items[index];
    }

    // 아이템 종류 기반 아이템 조회
    public static SupportItemDefinition Get(SupportItemKind kind)
    {
        for (int i = 0; i < Items.Length; i++)
        {
            if (Items[i].kind == kind)
                return Items[i];
        }

        return null;
    }

    // 네트워크 전송용 아이템 인덱스 조회
    public static int GetIndex(SupportItemKind kind)
    {
        for (int i = 0; i < Items.Length; i++)
        {
            if (Items[i].kind == kind)
                return i;
        }

        return -1;
    }
}
