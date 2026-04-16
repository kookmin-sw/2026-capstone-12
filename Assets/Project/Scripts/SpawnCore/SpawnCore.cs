using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BuildingHealthNet))]
public class SpawnCore : MonoBehaviourPun, IBuildingDamageGate, IBuildingDestroyedListener
{
    // 잔해 프리팹 기본 경로
    private const string DefaultRemainsRootPath = "Prefabs/SpawnCore/Remains/";

    [Header("Spawn Core")]
    // 코어 파괴 순서값
    [SerializeField] private int coreOrder = 0;
    // 잠금 해제 필요 선행 파괴 수
    [SerializeField] private int requiredDestroyedCoreCountToUnlock = 0;
    // 개별 잔해 프리팹 경로
    [SerializeField] private string destroyedRemainsPrefabPath;

    [Header("Core Spawn Settings")]
    [SerializeField] private bool allowEnemySpawn = true;
    [SerializeField] private bool startsSpawnActivated = false;
    [SerializeField] private int requiredActivatedCoreCountToActivate = 0;
    [SerializeField] private float spawnRadiusMin = 4f;
    [SerializeField] private float spawnRadiusMax = 8f;

    private StructureSelectable selectable;
    private bool spawnActivated = false;
    private bool destructionHandled = false;
    private bool gridOccupied = false;
    private bool blockGridOnDestroy = false;

    public int CoreOrder => coreOrder;
    public int RequiredDestroyedCoreCountToUnlock => requiredDestroyedCoreCountToUnlock;
    public bool AllowEnemySpawn => allowEnemySpawn;
    public bool IsSpawnActivated => spawnActivated;
    public bool CanSpawnEnemies => allowEnemySpawn && spawnActivated;
    public int RequiredActivatedCoreCountToActivate => Mathf.Max(0, requiredActivatedCoreCountToActivate);
    public float SpawnRadiusMin => Mathf.Max(0f, spawnRadiusMin);
    public float SpawnRadiusMax => Mathf.Max(SpawnRadiusMin, spawnRadiusMax);
    // 현재 코어 피격 가능 상태
    public bool IsUnlocked => SpawnCoreManager.Instance == null || SpawnCoreManager.Instance.IsCoreUnlocked(this);

    private void Awake()
    {
        selectable = GetComponent<StructureSelectable>();
        spawnActivated = startsSpawnActivated;
    }

    // 액티베이터 기반 스폰 가능 상태 변경 목적
    public void SetSpawnActivated(bool active)
    {
        spawnActivated = active;
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

        // 파괴 후 잔해 타일 차단 처리
        if (blockGridOnDestroy)
            selectable.grid.SetAreaBlocked(selectable.anchor.x, selectable.anchor.y, selectable.footprint, selectable.rotationY, true);
    }

    // BuildingHealthNet 공격 차단 연동 목적
    public bool CanTakeDamage(BuildingHealthNet buildingHealth, float incomingDamage)
    {
        // 잠금 상태 기반 피격 허용 판정
        return IsSpawnActivated && (SpawnCoreManager.Instance == null || SpawnCoreManager.Instance.CanDamageCore(this));
    }

    // BuildingHealthNet 파괴 전처리 연동 목적
    public void OnBuildingDestroyedByMaster(BuildingHealthNet buildingHealth)
    {
        if (destructionHandled)
            return;

        destructionHandled = true;
        // 잔해 생성 처리
        SpawnDestroyedRemains();

        // 파괴 후 잔해 타일 유지 플래그
        blockGridOnDestroy = true;

        // 진행 상태 갱신 처리
        SpawnCoreManager.Instance?.HandleCoreDestroyed(this);
    }

    // 파괴 직후 잔해 생성 목적
    private void SpawnDestroyedRemains()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        // 잔해 프리팹 경로 확인
        string remainsPrefabPath = GetDestroyedRemainsPrefabPath();
        if (string.IsNullOrWhiteSpace(remainsPrefabPath))
            return;

        // 룸 오브젝트 잔해 생성
        PhotonNetwork.InstantiateRoomObject(remainsPrefabPath, transform.position, transform.rotation);
    }

    // 씬 배치 코어 그리드 정렬 및 점유 등록 목적
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

    // Y축 회전값 90도 단위 정규화 목적
    private static int GetSnappedRightAngle(float yRotation)
    {
        int snapped = Mathf.RoundToInt(yRotation / 90f) * 90;
        snapped %= 360;

        if (snapped < 0)
            snapped += 360;

        return snapped;
    }

    // 잔해 프리팹 경로 결정 목적
    private string GetDestroyedRemainsPrefabPath()
    {
        // 개별 지정 경로 우선
        if (!string.IsNullOrWhiteSpace(destroyedRemainsPrefabPath))
            return destroyedRemainsPrefabPath;

        // 프리팹 이름 기반 기본 경로
        return DefaultRemainsRootPath + gameObject.name;
    }
}
