using UnityEngine;

public class StructureSelectable : MonoBehaviour
{
    public BuildingTypeSO type;  // 이걸 기준으로 cost/maxHp 읽기
    public int hp;

    public int Cost => type != null ? type.cost : 0;
    public int MaxHp => type != null ? type.maxHp : 0;
    public bool IsFullHp => hp >= MaxHp;

    // Grid 관련
    public GridManager grid;
    public Vector2Int anchor;   // 점유 시작 그리드 좌표(baseGx, baseGz)
    public Vector2Int footprint;    // 원본 footprint
    public int rotationY;   // 설치 당시 회전

	private void Start()
	{
		hp = MaxHp;
	}

    public void BindGrid(GridManager gridManager, Vector2Int anchor, Vector2Int footprint, int rotationY)
    {
        grid = gridManager;
        this.anchor = anchor;
        this.footprint = footprint;
        this.rotationY = rotationY;
    }

	public void Sell()
    {
        if (grid != null)
            grid.SetAreaOccupied(anchor.x, anchor.y, footprint, rotationY, false);

        Destroy(gameObject);
    }

    public bool CanRepair(int cost)
    {
        return ResourceManager.Instance == null || ResourceManager.Instance.CanAfford(cost);
    }

    public void Repair()
    {
        hp = MaxHp;
    }
}
