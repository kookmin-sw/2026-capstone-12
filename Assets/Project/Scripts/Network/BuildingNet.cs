using Photon.Pun;
using UnityEngine;

public class BuildingNet : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    private int typeId, ax, az, rotY;
    public StructureSelectable selectable; // 이미 쓰는 컴포넌트
    public GridManager grid;

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = photonView.InstantiationData;
        typeId = (int)data[0];
        ax = (int)data[1];
        az = (int)data[2];
        rotY = (int)data[3];

        grid = FindObjectOfType<GridManager>();

        if(BuildNetManager.Instance == null) return;
        BuildingTypeSO type = BuildNetManager.Instance.types[typeId];

        if (selectable == null)
            selectable = GetComponent<StructureSelectable>();

        // 기존 PlaceBuilding이 하던 세팅을 여기서
        selectable.type = type;
        //selectable.hp = type.maxHp;
        selectable.BindGrid(grid, new Vector2Int(ax, az), type.footprint, rotY);

        grid.SetAreaOccupied(ax, az, type.footprint, rotY, true);
    }


    private void OnDestroy()
    {
        if (grid == null) return;
        if(BuildNetManager.Instance == null) return;
        BuildingTypeSO type = BuildNetManager.Instance.types[typeId];

        if (type == null) return;
        grid.SetAreaOccupied(ax, az, type.footprint, rotY, false);
    }
}
