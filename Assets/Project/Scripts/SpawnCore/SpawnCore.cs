using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BuildingHealthNet))]
public class SpawnCore : MonoBehaviourPun, IBuildingDamageGate, IBuildingDestroyedListener
{
    private const string DefaultRemainsRootPath = "Prefabs/SpawnCore/Remains/";

    [Header("Spawn Core")]
    [SerializeField] private int coreOrder = 0; // Core 공략 순서
    [SerializeField] private int requiredDestroyedCoreCountToUnlock = 0; // 공격 가능 상태가 되기 위한 선행 파괴 Core 수
    [SerializeField] private string destroyedRemainsPrefabPath; // 파괴 후 생성할 잔해 Resources 경로

    private StructureSelectable selectable; // Grid 점유와 선택 처리를 위한 구조물 선택 컴포넌트
    private bool destructionHandled = false; // 파괴 처리 중복 방지 상태
    private bool gridOccupied = false; // Grid 점유 해제 필요 여부
    private bool blockGridOnDestroy = false; // 잔해 생성 후 Grid 차단 필요 여부

    public int CoreOrder => coreOrder;
    public int RequiredDestroyedCoreCountToUnlock => requiredDestroyedCoreCountToUnlock;
    public bool IsDestroyed => destructionHandled; // EnemyNest 생존 판정용 파괴 상태
    public bool IsUnlocked => SpawnCoreManager.Instance == null || SpawnCoreManager.Instance.IsCoreUnlocked(this);

    public void SetUnlockRequirement(int requiredCount)
    {
        requiredDestroyedCoreCountToUnlock = requiredCount;
    }

    private void Awake()
    {
        selectable = GetComponent<StructureSelectable>();
    }

    private void Start()
    {
        SnapToGridAndOccupy();
    }

    private void OnDestroy()
    {
        if (!gridOccupied || selectable == null || selectable.grid == null)
            return;

        if (selectable.footprint.x <= 0 || selectable.footprint.y <= 0)
            return;

        selectable.grid.SetAreaOccupied(selectable.anchor.x, selectable.anchor.y, selectable.footprint, selectable.rotationY, false);

        if (blockGridOnDestroy)
            selectable.grid.SetAreaBlocked(selectable.anchor.x, selectable.anchor.y, selectable.footprint, selectable.rotationY, true);
    }

    /// <summary>
    /// BuildingHealthNet 공격 가능 여부 판정
    /// </summary>
    public bool CanTakeDamage(BuildingHealthNet buildingHealth, float incomingDamage)
    {
        if (SpawnCoreManager.Instance == null || SpawnCoreManager.Instance.CanDamageCore(this))
            return true;

        SpawnCoreManager.Instance.NotifyLockedAttackAttempt();
        return false;
    }

    /// <summary>
    /// BuildingHealthNet 파괴 후 처리 연동
    /// </summary>
    public void OnBuildingDestroyedByMaster(BuildingHealthNet buildingHealth)
    {
        if (destructionHandled)
            return;

        destructionHandled = true;
        SpawnDestroyedRemains();

        blockGridOnDestroy = true;

        EnemyManager.Instance?.HandleSpawnCoreDestroyed(this);
        SpawnCoreManager.Instance?.HandleCoreDestroyed(this);
    }

    /// <summary>
    /// 파괴 직후 잔해 생성
    /// </summary>
    private void SpawnDestroyedRemains()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        string remainsPrefabPath = GetDestroyedRemainsPrefabPath(); // 잔해 프리팹 Resources 경로
        if (string.IsNullOrWhiteSpace(remainsPrefabPath))
            return;

        PhotonNetwork.InstantiateRoomObject(remainsPrefabPath, transform.position, transform.rotation);
    }

    /// <summary>
    /// 씬 배치 Core의 Grid 정렬과 점유 등록
    /// </summary>
    private void SnapToGridAndOccupy()
    {
        if (selectable == null || selectable.type == null)
            return;

        GridManager grid = selectable.grid != null ? selectable.grid : FindObjectOfType<GridManager>();
        if (grid == null)
            return;

        Vector2Int footprint = selectable.type.footprint;
        if (footprint.x <= 0 || footprint.y <= 0)
            return;

        int snappedRotationY = GetSnappedRightAngle(transform.eulerAngles.y);
        Vector2Int centerCell = grid.WorldToGrid(transform.position);
        Vector2Int anchor = grid.CenterToAnchor(centerCell, footprint, snappedRotationY);

        selectable.BindGrid(grid, anchor, footprint, snappedRotationY);

        Vector3 snappedPosition = grid.AnchorToWorldCenter(anchor, footprint, snappedRotationY);
        transform.SetPositionAndRotation(snappedPosition, Quaternion.Euler(0f, snappedRotationY, 0f));

        grid.SetAreaOccupied(anchor.x, anchor.y, footprint, snappedRotationY, true);
        gridOccupied = true;
    }

    /// <summary>
    /// Y축 회전값 90도 단위 정규화
    /// </summary>
    private static int GetSnappedRightAngle(float yRotation)
    {
        int snapped = Mathf.RoundToInt(yRotation / 90f) * 90;
        snapped %= 360;

        if (snapped < 0)
            snapped += 360;

        return snapped;
    }

    /// <summary>
    /// 잔해 프리팹 경로 결정
    /// </summary>
    private string GetDestroyedRemainsPrefabPath()
    {
        if (!string.IsNullOrWhiteSpace(destroyedRemainsPrefabPath))
            return destroyedRemainsPrefabPath;

        return DefaultRemainsRootPath + gameObject.name;
    }
}
