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
    [SerializeField] private float spawnRadiusMin = 4f;
    [SerializeField] private float spawnRadiusMax = 8f;

    private bool destructionHandled = false;

    public int CoreOrder => coreOrder;
    public int RequiredDestroyedCoreCountToUnlock => requiredDestroyedCoreCountToUnlock;
    public bool AllowEnemySpawn => allowEnemySpawn;
    public float SpawnRadiusMin => Mathf.Max(0f, spawnRadiusMin);
    public float SpawnRadiusMax => Mathf.Max(SpawnRadiusMin, spawnRadiusMax);
    // 현재 코어 피격 가능 상태
    public bool IsUnlocked => SpawnCoreManager.Instance == null || SpawnCoreManager.Instance.IsCoreUnlocked(this);

    // BuildingHealthNet 피격 차단 연동 목적
    public bool CanTakeDamage(BuildingHealthNet buildingHealth, float incomingDamage)
    {
        // 잠금 상태 기반 피격 허용 판정
        return SpawnCoreManager.Instance == null || SpawnCoreManager.Instance.CanDamageCore(this);
    }

    // BuildingHealthNet 파괴 후처리 연동 목적
    public void OnBuildingDestroyedByMaster(BuildingHealthNet buildingHealth)
    {
        if (destructionHandled)
            return;

        destructionHandled = true;
        // 잔해 생성 처리
        SpawnDestroyedRemains();

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
