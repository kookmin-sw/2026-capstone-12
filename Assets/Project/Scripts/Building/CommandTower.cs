using Photon.Pun;
using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(BuildingHealthNet))]
public class CommandTower : MonoBehaviourPun, IBuildingDestroyedListener, IBuildingDamagedListener
{
    [Header("Command Tower")]
    [SerializeField] private float maxHp = 1000f;
    [SerializeField] private Vector2Int footprint = new Vector2Int(3, 3);
    [SerializeField] private GridManager grid;
    [SerializeField] private AudioClip commandTowerHitWarningClip;
    [SerializeField] private float commandTowerHitWarningCooldown = 5f;
    [SerializeField] [Range(0f, 1f)] private float commandTowerHitWarningVolume = 1f;

    private BuildingHealthNet health;
    private StructureSelectable selectable;
    private AudioSource audioSource;
    private bool destructionHandled;
    private bool gridOccupied;
    private float nextCommandTowerHitWarningTime;

    public static CommandTower ActiveTower { get; private set; }
    public static event Action<CommandTower> ActiveTowerChanged;
    // Enemy가 씬 전체 탐색 없이 CommandTower를 주 목표로 참조하기 위한 캐시
    public static Transform ActiveTarget => ActiveTower != null ? ActiveTower.transform : null;
    public static Vector3 ActiveTargetPosition { get; private set; }

    private void Awake()
    {
        health = GetComponent<BuildingHealthNet>();
        selectable = GetComponent<StructureSelectable>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        if (health != null)
            health.Init(GetConfiguredMaxHp());
    }

    private void OnEnable()
    {
        RegisterActiveTower();
        ActiveTowerChanged?.Invoke(ActiveTower);
    }

    private void Start()
    {
        SnapToGridAndOccupy();
        ActiveTargetPosition = transform.position;
    }

    private void OnDisable()
    {
        if (ActiveTower == this)
        {
            ActiveTower = null;
            ActiveTowerChanged?.Invoke(null);
        }
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

        // 목표 건물 파괴는 모든 클라이언트에서 동일하게 게임오버 처리
        if (PhotonNetwork.InRoom && photonView != null)
        {
            photonView.RPC(nameof(RpcTriggerGameOver), RpcTarget.All);
            return;
        }

        TriggerGameOverLocal();
    }

    public void OnBuildingDamagedByMaster(BuildingHealthNet buildingHealth, float damage)
    {
        if (Time.time < nextCommandTowerHitWarningTime)
            return;

        nextCommandTowerHitWarningTime = Time.time + Mathf.Max(0f, commandTowerHitWarningCooldown);

        if (PhotonNetwork.InRoom && photonView != null)
        {
            photonView.RPC(nameof(RpcPlayCommandTowerHitWarning), RpcTarget.All);
            return;
        }

        PlayCommandTowerHitWarning();
    }

    [PunRPC]
    private void RpcPlayCommandTowerHitWarning()
    {
        PlayCommandTowerHitWarning();
    }

    private void PlayCommandTowerHitWarning()
    {
        if (commandTowerHitWarningClip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(commandTowerHitWarningClip, commandTowerHitWarningVolume);
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
        // 전용 BuildingTypeSO가 있으면 에셋의 밸런스 값을 우선 사용
        if (selectable != null && selectable.type != null && selectable.type.maxHp > 0f)
            return selectable.type.maxHp;

        return maxHp;
    }

    private Vector2Int GetConfiguredFootprint()
    {
        // 전용 BuildingTypeSO가 있으면 에셋의 footprint 값을 우선 사용
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

        // 씬에 미리 배치된 구조물도 설치 시스템과 같은 Grid 점유 정보를 갖게 함
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

        // 파괴 또는 씬 전환 시 남은 점유 정보가 후속 설치를 막지 않도록 해제
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
