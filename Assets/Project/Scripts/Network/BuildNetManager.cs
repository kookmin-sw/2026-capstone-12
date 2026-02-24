using Photon.Pun;
using UnityEngine;

public class BuildNetManager : MonoBehaviourPun
{
    public static BuildNetManager Instance { get; private set; }
    public GridManager grid;
    public BuildSystem buildSystem;

    // BuildingTypeSO 배열(모든 클라 동일 순서 보장 필요)
    public BuildingTypeSO[] types;

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

        int maxHp = s.type.maxHp;
        if (s.hp >= maxHp) return;

        int repairCost = s.type.repairCost;
        int healAmount = s.type.repairAmount;

        if (ResourceNet.Instance == null) return;
        if (!ResourceNet.Instance.MasterTrySpendMoney(repairCost)) return;

        int newHp = Mathf.Min(maxHp, s.hp + healAmount);

        // HP 결과를 전체에 브로드캐스트
        photonView.RPC(nameof(RpcApplyStructureHp), RpcTarget.All, viewId, newHp);
    }

    // Damage 관련
    public void RequestDamage(int viewId, int damage)
    {
        photonView.RPC(nameof(RpcRequestDamage), RpcTarget.MasterClient, viewId, damage);
    }

    [PunRPC]
    private void RpcRequestDamage(int viewId, int damage)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView pv = PhotonView.Find(viewId);
        if (pv == null) return;

        StructureSelectable s = pv.GetComponent<StructureSelectable>();
        if (s == null || s.type == null) return;

        int newHp = Mathf.Max(0, s.hp - damage);
        pv.RPC(nameof(RpcApplyStructureHp), RpcTarget.All, viewId, newHp);
    }

    // hp 관련 요청
    [PunRPC]
    private void RpcApplyStructureHp(int viewId, int newHp)
    {
        PhotonView pv = PhotonView.Find(viewId);
        if (pv == null) return;

        var s = pv.GetComponent<StructureSelectable>();
        if (s == null) return;

        s.hp = newHp;
    }
}
