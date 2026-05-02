using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;

public class BuildNetManager : MonoBehaviourPun
{
    public static BuildNetManager Instance { get; private set; }
    public GridManager grid;
    public BuildSystem buildSystem;

    // BuildingTypeSO 배열(모든 클라 동일 순서 보장 필요)
    public BuildingTypeSO[] types;

    private readonly Dictionary<int, SupportPickupItem> supportItems = new(); // 생성된 Support 아이템 조회 테이블
    private int nextSupportId = 1; // Support 아이템 소비 동기화용 고유 ID 발급값

    private void Awake()
    {
        Instance = this;
        if (grid == null) grid = FindObjectOfType<GridManager>();
        if (buildSystem == null) buildSystem = FindObjectOfType<BuildSystem>();
    }

    // 클라(서포터)가 호출: 설치 요청
    public void RequestPlace(int typeId, int anchorX, int anchorZ, int rotY)
    {
        photonView.RPC(nameof(RpcRequestPlace), RpcTarget.MasterClient, typeId, anchorX, anchorZ, rotY, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    // MasterClient 기준 구조물 배치 검증 및 생성
    private void RpcRequestPlace(int typeId, int anchorX, int anchorZ, int rotY, int requesterActor)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        BuildingTypeSO type = types[typeId];
        if (type == null) return;

        // 배치 가능 검사
        if (!grid.IsAreaFree(anchorX, anchorZ, type.footprint, rotY))
            return;

        // 자원 차감
        if (ResourceNet.Instance != null && !ResourceNet.Instance.MasterTrySpendMoney(type.cost))
            return;

        // 스폰 위치 계산
        Vector3 pos = grid.AnchorToWorldCenter(new Vector2Int(anchorX, anchorZ), type.footprint, rotY);
        Quaternion rot = Quaternion.Euler(0f, rotY, 0f);
        
        // 오브젝트 생성
        object[] instData = new object[] { typeId, anchorX, anchorZ, rotY };
        PhotonNetwork.Instantiate(type.photonPrefabPath, pos, rot, 0, instData);
        Player requester = PhotonNetwork.CurrentRoom?.GetPlayer(requesterActor);
        if (requester != null)
            photonView.RPC(nameof(RpcPlayBuildStructureSound), requester);
    }

    [PunRPC]
    // 건설 성공 요청자에게 구조물 배치 사운드 재생
    private void RpcPlayBuildStructureSound()
    {
        SupporterUISoundManager.Instance?.Play(SupporterUISoundType.BuildStructure);
    }
    
    // Support 아이템 배치 요청
    public void RequestPlaceSupport(int supportTypeIndex, Vector3 position)
    {
        photonView.RPC(nameof(RpcRequestPlaceSupport), RpcTarget.MasterClient, supportTypeIndex, position);
    }

    // 마스터 기준 Support 배치 검증과 골드 차감
    [PunRPC]
    private void RpcRequestPlaceSupport(int supportTypeIndex, Vector3 position)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        SupportItemDefinition item = SupportItemCatalog.Get(supportTypeIndex);
        if (item == null) return;

        if (ResourceNet.Instance != null && !ResourceNet.Instance.MasterTrySpendMoney(item.cost))
            return;

        int supportId = nextSupportId++; // 클라이언트별 소비 대상 매칭용 ID
        photonView.RPC(nameof(RpcSpawnSupport), RpcTarget.All, supportTypeIndex, position, supportId);
    }

    // Support 아이템 소비 요청
    public void RequestConsumeSupport(int supportId)
    {
        photonView.RPC(nameof(RpcRequestConsumeSupport), RpcTarget.MasterClient, supportId);
    }

    // 마스터 기준 Support 소비 승인
    [PunRPC]
    private void RpcRequestConsumeSupport(int supportId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC(nameof(RpcConsumeSupport), RpcTarget.All, supportId);
    }

    // 전체 클라이언트 Support 아이템 생성
    [PunRPC]
    // Support 아이템 생성과 배치 성공 사운드 반영
    private void RpcSpawnSupport(int supportTypeIndex, Vector3 position, int supportId)
    {
        SupportItemDefinition item = SupportItemCatalog.Get(supportTypeIndex);
        if (item == null) return;

        GameObject prefab = Resources.Load<GameObject>(item.prefabResourcePath);
        if (prefab == null) return;

        GameObject supportObject = Instantiate(prefab, position, Quaternion.identity);
        SupportPickupItem pickupItem = supportObject.GetComponent<SupportPickupItem>();
        if (pickupItem == null)
            pickupItem = supportObject.AddComponent<SupportPickupItem>();

        pickupItem.Configure(supportId, item.kind);
        supportItems[supportId] = pickupItem;
        SupporterUISoundManager.Instance?.Play(SupporterUISoundType.SupplyItem);
    }

    // 전체 클라이언트 Support 아이템 소비 처리
    [PunRPC]
    private void RpcConsumeSupport(int supportId)
    {
        if (!supportItems.TryGetValue(supportId, out SupportPickupItem pickupItem) || pickupItem == null)
        {
            SupportPickupItem[] allItems = FindObjectsOfType<SupportPickupItem>(); // 딕셔너리 누락 보정용 씬 검색
            for (int i = 0; i < allItems.Length; i++)
            {
                if (allItems[i].SupportId == supportId)
                {
                    pickupItem = allItems[i];
                    break;
                }
            }
        }

        if (pickupItem == null)
            return;

        supportItems.Remove(supportId);
        pickupItem.StartConsuming();
    }

    // 판매 요청
    public void RequestSell(int viewId)
    {
        photonView.RPC(nameof(RpcRequestSell), RpcTarget.MasterClient, viewId);
    }

    [PunRPC]
    private void RpcRequestSell(int viewId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView pv = PhotonView.Find(viewId);
        if (pv == null) return;

        var selectable = pv.GetComponent<StructureSelectable>();
        if (selectable != null && selectable.type != null)
        {
            if (!selectable.CanSell)
                return;

            int refund = Mathf.RoundToInt(selectable.type.cost * 0.3f);
            ResourceNet.Instance?.MasterAddMoney(refund);
        }

        // 네트워크 오브젝트 삭제
        PhotonNetwork.Destroy(pv.gameObject);
    }

    // 수리 요청(예: viewId, cost, healAmount)
    public void RequestRepair(int viewId)
    {
        photonView.RPC(nameof(RpcRequestRepair), RpcTarget.MasterClient, viewId);
    }

    [PunRPC]
    private void RpcRequestRepair(int viewId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView pv = PhotonView.Find(viewId);
        if (pv == null) return;

        var s = pv.GetComponent<StructureSelectable>();
        if (s == null || s.type == null) return;
        if (!s.CanRepair) return;

        var health = pv.GetComponent<BuildingHealthNet>();
        if (health == null) return;

        float maxHp = health.MaxHp;
        if (health.CurrentHp >= maxHp) return;

        int repairCost = s.type.repairCost;
        float healAmount = s.type.repairAmount;

        if (ResourceNet.Instance == null) return;
        if (!ResourceNet.Instance.MasterTrySpendMoney(repairCost)) return;

        health.MasterRepair(healAmount);
    }
}
