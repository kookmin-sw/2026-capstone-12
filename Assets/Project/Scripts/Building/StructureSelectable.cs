using UnityEngine;
using Photon.Pun;

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

	public void RequestSell()
    {
        PhotonView pv = GetComponent<PhotonView>();
        if (PhotonNetwork.InRoom && pv != null)
        {
            BuildNetManager.Instance.RequestSell(pv.ViewID);
            return;
        }
    }

    public void RequestRepair()
    {
        PhotonView pv = GetComponent<PhotonView>();
        if (PhotonNetwork.InRoom && pv != null)
        {
            BuildNetManager.Instance.RequestRepair(pv.ViewID);
            return;
        }
    }

    public bool CanRepairLocal()
    {
        if (type == null) return false;
        if (hp >= type.maxHp) return false;

        int cost = type.repairCost;
        return ResourceManager.Instance == null || ResourceManager.Instance.CanAfford(cost);
    }
}