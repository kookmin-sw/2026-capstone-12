using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BuildingHealthNet))]
[RequireComponent(typeof(StructureSelectable))]
public class EnemyNest : MonoBehaviourPun, IBuildingDestroyedListener
{
    [Header("Ownership")]
    [SerializeField] private SpawnCore ownerCore; // 소속 Core 참조

    [Header("Destroyed Remains")]
    [SerializeField] private string destroyedRemainsPrefabPath; // 파괴 후 생성할 잔해 Resources 경로

    [Header("Spawn")]
    [SerializeField] private bool allowSpawn = true; // Nest별 스폰 허용 여부
    [SerializeField] private float spawnRadius = 1.5f; // Nest 주변 랜덤 스폰 반경
    [SerializeField] private float spawnHeightOffset = 0.5f; // 지면 겹침 방지용 스폰 높이
    [SerializeField] private float weight = 1f; // 같은 그룹 안에서 Nest 선택 확률 가중치

    [Header("Fallback Health")]
    [SerializeField] private float fallbackMaxHp = 60f; // BuildingType 미초기화 시 보정 체력

    private BuildingHealthNet health; // 파괴 가능한 구조물 체력 참조
    private StructureSelectable selectable; // Grid 스냅과 점유 처리를 위한 선택 정보
    private EnemyNestGroup group; // 그룹 단위 스폰 주기 참조
    private bool destructionHandled; // 중복 파괴 처리 방지 상태
    private bool gridOccupied; // Grid 점유 해제 필요 여부
    private bool blockGridOnDestroy; // 잔해 생성 후 Grid 차단 필요 여부

    public SpawnCore OwnerCore => ownerCore; // 소속 Core 조회용 속성
    public EnemyNestGroup Group => group; // Manager의 그룹별 스폰 스케줄용 속성
    public bool CanSpawnEnemies => allowSpawn && !destructionHandled && IsOwnerCoreAlive(); // 스폰 후보 포함 가능 상태
    public float Weight => Mathf.Max(0f, weight); // 음수 가중치 방지용 속성

    // 체력 참조와 부모 그룹 기반 소속 Core 자동 연결
    private void Awake()
    {
        health = GetComponent<BuildingHealthNet>();
        selectable = GetComponent<StructureSelectable>();
        group = GetComponentInParent<EnemyNestGroup>();

        if (ownerCore == null && group != null)
            ownerCore = group.OwnerCore;

        if (health != null && health.MaxHp <= 0f)
            health.Init(fallbackMaxHp);
    }

    // Manager 생성 순서 차이를 흡수하기 위한 등록 처리
    private void OnEnable()
    {
        EnemyManager.Instance?.RegisterSpawnNest(this);
    }

    // 씬 배치 Nest의 Grid 스냅과 Manager 등록 처리
    private void Start()
    {
        SnapToGridAndOccupy();
        EnemyManager.Instance?.RegisterSpawnNest(this);
    }

    // Nest 제거 시 Grid 점유 해제와 잔해 차단 반영
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

    // 비활성 Nest가 스폰 후보에 남지 않기 위한 해제 처리
    private void OnDisable()
    {
        EnemyManager.Instance?.UnregisterSpawnNest(this);
    }

    // Nest 주변 반경 안에서 Enemy 생성 위치 제공
    public Vector3 GetSpawnPosition()
    {
        Vector2 random = Random.insideUnitCircle * Mathf.Max(0f, spawnRadius); // 수평 랜덤 위치
        Vector3 offset = new Vector3(random.x, 0f, random.y); // 월드 좌표 보정값
        return transform.position + offset + Vector3.up * spawnHeightOffset;
    }

    // 소속 Core 파괴 시 Nest를 함께 제거하기 위한 처리
    public void DestroyByOwnerCore()
    {
        if (destructionHandled)
            return;

        HandleDestroyedByMaster();

        if (!PhotonNetwork.InRoom)
        {
            Destroy(gameObject);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }

    // 직접 피격으로 파괴될 때 잔해와 Grid 상태를 반영하기 위한 콜백
    public void OnBuildingDestroyedByMaster(BuildingHealthNet buildingHealth)
    {
        if (destructionHandled)
            return;

        HandleDestroyedByMaster();
    }

    // 모든 파괴 경로에서 공유하는 Nest 파괴 전처리
    private void HandleDestroyedByMaster()
    {
        destructionHandled = true;
        EnemyManager.Instance?.UnregisterSpawnNest(this);
        SpawnDestroyedRemains();
        blockGridOnDestroy = true;
    }

    // Nest 파괴 후 네트워크 동기화 잔해 생성
    private void SpawnDestroyedRemains()
    {
        string remainsPrefabPath = GetDestroyedRemainsPrefabPath(); // Resources 기준 잔해 경로
        if (string.IsNullOrWhiteSpace(remainsPrefabPath))
            return;

        GameObject remainsPrefab = Resources.Load<GameObject>(remainsPrefabPath); // Photon 생성 전 존재 검증용 프리팹
        if (remainsPrefab == null)
        {
            Debug.LogWarning($"{nameof(EnemyNest)}: Destroyed remains prefab not found at Resources/{remainsPrefabPath}.", this);
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.InstantiateRoomObject(remainsPrefabPath, transform.position, transform.rotation);

            return;
        }

        Instantiate(remainsPrefab, transform.position, transform.rotation);
    }

    // 씬에 직접 배치된 Nest를 Grid 기준 위치로 보정하고 점유 등록
    private void SnapToGridAndOccupy()
    {
        if (selectable == null || selectable.type == null)
            return;

        GridManager grid = selectable.grid != null ? selectable.grid : FindObjectOfType<GridManager>(); // 배치 기준 Grid 참조
        if (grid == null)
            return;

        Vector2Int footprint = selectable.type.footprint; // Nest가 차지할 Grid 크기
        if (footprint.x <= 0 || footprint.y <= 0)
            return;

        int snappedRotationY = GetSnappedRightAngle(transform.eulerAngles.y); // Grid 회전 정렬값
        Vector2Int centerCell = grid.WorldToGrid(transform.position); // 현재 위치 기준 중심 셀
        Vector2Int anchor = grid.CenterToAnchor(centerCell, footprint, snappedRotationY); // 점유 시작 셀

        selectable.BindGrid(grid, anchor, footprint, snappedRotationY);

        Vector3 snappedPosition = grid.AnchorToWorldCenter(anchor, footprint, snappedRotationY); // Grid 표면 기준 보정 위치
        transform.SetPositionAndRotation(snappedPosition, Quaternion.Euler(0f, snappedRotationY, 0f));

        grid.SetAreaOccupied(anchor.x, anchor.y, footprint, snappedRotationY, true);
        gridOccupied = true;
    }

    // Grid 배치 규칙에 맞는 직각 회전값 계산
    private static int GetSnappedRightAngle(float yRotation)
    {
        int snapped = Mathf.RoundToInt(yRotation / 90f) * 90; // 90도 단위 보정 회전값
        snapped %= 360;

        if (snapped < 0)
            snapped += 360;

        return snapped;
    }

    // Nest별 명시 잔해 경로 반환
    private string GetDestroyedRemainsPrefabPath()
    {
        return destroyedRemainsPrefabPath;
    }

    // 소속 Core 파괴 여부 기반 Nest 활성 판정
    private bool IsOwnerCoreAlive()
    {
        if (ownerCore == null)
            return true;

        return !ownerCore.IsDestroyed;
    }
}
