using UnityEngine;
using Photon.Pun;

public class StructureSelectable : MonoBehaviour
{
    public BuildingTypeSO type;  // 이걸 기준으로 cost/maxHp 읽기
    public BuildingHealthNet health; 

    // Grid 관련
    public GridManager grid;
    public Vector2Int anchor;   // 점유 시작 그리드 좌표(baseGx, baseGz)
    public Vector2Int footprint;    // 원본 footprint
    public int rotationY;   // 설치 당시 회전

    public bool IsFullHp => health != null && health.IsFullHp;
    public bool CanRepair => type != null && type.canRepair;
    public bool CanSell => type != null && type.canSell;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<BuildingHealthNet>();
        if (type != null && health != null)
            health.Init(type.maxHp);
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
        if (!CanSell)
            return;

        PhotonView pv = GetComponent<PhotonView>();
        if (PhotonNetwork.InRoom && pv != null)
        {
            BuildNetManager.Instance.RequestSell(pv.ViewID);
            return;
        }
    }

    public void RequestRepair()
    {
        if (!CanRepair)
            return;

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
        if (!CanRepair) return false;
        if (IsFullHp) return false;

        int cost = type.repairCost;
        return ResourceManager.Instance == null || ResourceManager.Instance.CanAfford(cost);
    }
}
