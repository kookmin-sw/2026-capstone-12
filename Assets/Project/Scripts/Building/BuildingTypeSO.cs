using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/Building Type")]
public class BuildingTypeSO : ScriptableObject
{
    public GameObject prefab;

    public int typeId;
    public string photonPrefabPath; // Resources 하위 경로(예: "Prefabs/Buildings/Turret_MG")

    // 이 건물이 차지하는 칸 크기 (가로 x, 세로 z)
    public Vector2Int footprint = new Vector2Int(1, 1);

    public bool allowRotate = true;
    public int cost = 0;
    public float maxHp = 0;

    public int repairCost => (int)(cost * 0.1);
    public float repairAmount => (int)(maxHp * 0.5);
}
