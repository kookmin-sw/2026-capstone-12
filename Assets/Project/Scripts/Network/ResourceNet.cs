using Photon.Pun;
using UnityEngine;

public class ResourceNet : MonoBehaviourPunCallbacks
{
    public static ResourceNet Instance { get; private set; }

    [Header("Initial Values (Master sets)")]
    [SerializeField] private int startMoney = 0;
    [SerializeField] private int startScore = 0;
    [SerializeField] private int startKills = 0;

    private int money;
    private int score;
    private int kills;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnJoinedRoom()
    {
        // 마스터가 초기값 확정 후 전체 동기화
        if (PhotonNetwork.IsMasterClient)
        {
            InitResource();
        }
    }

    // ----------------------------
    // Master APIs (게임 로직에서 호출)
    // ----------------------------
    public void InitResource()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        money = startMoney;
        score = startScore;
        kills = startKills;
        BroadcastAll();
    }

    public void MasterAddMoney(int amount)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        money += amount;
        photonView.RPC(nameof(RpcSetMoney), RpcTarget.All, money);
    }

    public bool MasterTrySpendMoney(int cost)
    {
        if (!PhotonNetwork.IsMasterClient) return false;
        if (money < cost) return false;

        money -= cost;
        photonView.RPC(nameof(RpcSetMoney), RpcTarget.All, money);
        return true;
    }

    public void MasterAddScore(int amount)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        score += amount;
        photonView.RPC(nameof(RpcSetScore), RpcTarget.All, score);
    }

    public void MasterAddKills(int amount)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        kills += amount;
        photonView.RPC(nameof(RpcSetKills), RpcTarget.All, kills);
    }

    // ----------------------------
    // RPC (All clients apply)
    // ----------------------------
    private void BroadcastAll()
    {
        photonView.RPC(nameof(RpcSetMoney), RpcTarget.All, money);
        photonView.RPC(nameof(RpcSetScore), RpcTarget.All, score);
        photonView.RPC(nameof(RpcSetKills), RpcTarget.All, kills);
    }

    [PunRPC]
    private void RpcSetMoney(int newMoney)
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.SetMoneyFromNet(newMoney);
    }

    [PunRPC]
    private void RpcSetScore(int newScore)
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.SetScoreFromNet(newScore);
    }

    [PunRPC]
    private void RpcSetKills(int newKills)
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.SetKillsFromNet(newKills);
    }
}