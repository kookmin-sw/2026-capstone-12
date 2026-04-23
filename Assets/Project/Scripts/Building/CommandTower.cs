using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(BuildingHealthNet))]
public class CommandTower : MonoBehaviourPun, IBuildingDestroyedListener
{
    [Header("Command Tower")]
    [SerializeField] private float maxHp = 1000f;
    [SerializeField] private Vector2Int footprint = new Vector2Int(3, 3);
    [SerializeField] private GridManager grid;

    private BuildingHealthNet health;
    private StructureSelectable selectable;
    private bool destructionHandled;
    private bool gridOccupied;

    public static CommandTower ActiveTower { get; private set; }
    public static Transform ActiveTarget => ActiveTower != null ? ActiveTower.transform : null;
    public static Vector3 ActiveTargetPosition { get; private set; }

    private void Awake()
    {
        health = GetComponent<BuildingHealthNet>();
        selectable = GetComponent<StructureSelectable>();

        if (health != null)
            health.Init(GetConfiguredMaxHp());
    }

    private void OnEnable()
    {
        RegisterActiveTower();
    }

    private void Start()
    {
        SnapToGridAndOccupy();
        ActiveTargetPosition = transform.position;
    }

    private void OnDisable()
    {
        if (ActiveTower == this)
            ActiveTower = null;
    }

    private void OnDestroy()
    {
        ReleaseGridOccupation();
    }

    public void OnBuildingDestroyedByMaster(BuildingHealthNet buildingHealth)
    {
        if (destructionHandled)
            return;

        destructionHandled = true;

        if (PhotonNetwork.InRoom && photonView != null)
        {
            photonView.RPC(nameof(RpcTriggerGameOver), RpcTarget.All);
            return;
        }

        TriggerGameOverLocal();
    }

    [PunRPC]
    private void RpcTriggerGameOver()
    {
        TriggerGameOverLocal();
    }

    private void TriggerGameOverLocal()
    {
        GameManager.Instance?.TriggerGameOver();
        InputLock.Lock();
    }

    private void RegisterActiveTower()
    {
        if (ActiveTower != null && ActiveTower != this)
            Debug.LogWarning($"{nameof(CommandTower)}: Multiple command towers are active. Enemies will target the latest enabled tower.", this);

        ActiveTower = this;
        ActiveTargetPosition = transform.position;
    }

    private float GetConfiguredMaxHp()
    {
        if (selectable != null && selectable.type != null && selectable.type.maxHp > 0f)
            return selectable.type.maxHp;

        return maxHp;
    }

    private Vector2Int GetConfiguredFootprint()
    {
        if (selectable != null && selectable.type != null && selectable.type.footprint.x > 0 && selectable.type.footprint.y > 0)
            return selectable.type.footprint;

        return footprint;
    }

    private void SnapToGridAndOccupy()
    {
        if (grid == null)
            grid = FindObjectOfType<GridManager>();

        if (grid == null)
            return;

        Vector2Int towerFootprint = GetConfiguredFootprint();
        if (towerFootprint.x <= 0 || towerFootprint.y <= 0)
            return;

        int snappedRotationY = GetSnappedRightAngle(transform.eulerAngles.y);
        Vector2Int centerCell = grid.WorldToGrid(transform.position);
        Vector2Int anchor = grid.CenterToAnchor(centerCell, towerFootprint, snappedRotationY);

        if (selectable != null)
            selectable.BindGrid(grid, anchor, towerFootprint, snappedRotationY);

        Vector3 snappedPosition = grid.AnchorToWorldCenter(anchor, towerFootprint, snappedRotationY);
        transform.SetPositionAndRotation(snappedPosition, Quaternion.Euler(0f, snappedRotationY, 0f));

        grid.SetAreaOccupied(anchor.x, anchor.y, towerFootprint, snappedRotationY, true);
        gridOccupied = true;
    }

    private void ReleaseGridOccupation()
    {
        if (!gridOccupied || grid == null)
            return;

        Vector2Int towerFootprint = GetConfiguredFootprint();
        if (towerFootprint.x <= 0 || towerFootprint.y <= 0)
            return;

        Vector2Int anchor;
        int rotationY;

        if (selectable != null)
        {
            anchor = selectable.anchor;
            rotationY = selectable.rotationY;
        }
        else
        {
            rotationY = GetSnappedRightAngle(transform.eulerAngles.y);
            anchor = grid.CenterToAnchor(grid.WorldToGrid(transform.position), towerFootprint, rotationY);
        }

        grid.SetAreaOccupied(anchor.x, anchor.y, towerFootprint, rotationY, false);
        gridOccupied = false;
    }

    private static int GetSnappedRightAngle(float yRotation)
    {
        int snapped = Mathf.RoundToInt(yRotation / 90f) * 90;
        snapped %= 360;

        if (snapped < 0)
            snapped += 360;

        return snapped;
    }
}
