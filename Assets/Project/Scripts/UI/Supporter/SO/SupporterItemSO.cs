using UnityEngine;

public enum SupportItemKind
{
    AmmoPack,
    HealthPack,
    PurificationBeacon
}

[CreateAssetMenu(menuName = "TowerDefense/Supporter Item")]
public class SupporterItemSO : ScriptableObject
{
    public SupportItemKind kind;
    public string displayName;
    public string description;
    public Sprite icon;
    public int typeId;
    public int cost;
    public GameObject prefab;
    public string prefabResourcePath;

    [Header("Support Policy")]
    public float cooldown;
    public int maxActiveCount;
    public SupportItemEffectSO effect;
}
