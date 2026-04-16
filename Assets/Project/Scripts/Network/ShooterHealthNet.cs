using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ShooterHealthNet : MonoBehaviourPunCallbacks
{
    public static ShooterHealthNet Instance { get; private set; }

    [Header("Player HP")]
    [SerializeField] private float shooterMaxHp = 100;

    // 테스트 규칙: 마스터(슈터) HP만 관리
    private float shooterHp;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 마스터가 초기 HP를 정하고 모두에게 동기화
        if (PhotonNetwork.IsMasterClient)
        {
            shooterHp = shooterMaxHp;
            photonView.RPC(nameof(RpcSetShooterHp), RpcTarget.All, shooterHp, shooterMaxHp);
        }
    }

    // 마스터에서만 호출: 슈터에게 데미지 적용
    public void MasterApplyDamageToShooter(float damage)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        shooterHp = Mathf.Max(0, shooterHp - damage);
        photonView.RPC(nameof(RpcSetShooterHp), RpcTarget.All, shooterHp, shooterMaxHp);

        if (shooterHp <= 0)
        {
            photonView.RPC(nameof(RpcShooterDied), RpcTarget.All);
        }
    }

    // Support 아이템 체력 회복 요청
    public void RequestHealShooter(float healAmount)
    {
        if (healAmount <= 0f)
            return;

        if (!PhotonNetwork.InRoom)
        {
            ApplyHealLocal(healAmount);
            return;
        }

        photonView.RPC(nameof(RpcRequestHealShooter), RpcTarget.MasterClient, healAmount);
    }

    [PunRPC]
    // 마스터 기준 체력 회복 동기화
    private void RpcRequestHealShooter(float healAmount)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        shooterHp = Mathf.Min(shooterMaxHp, shooterHp + healAmount);
        photonView.RPC(nameof(RpcSetShooterHp), RpcTarget.All, shooterHp, shooterMaxHp);
    }

    [PunRPC]
    private void RpcSetShooterHp(float hp, float maxHp)
    {
        // 씬에 있는 Shooter 플레이어 오브젝트의 HealthManager를 찾아 반영
        var hm = FindShooterHealth();
        if (hm != null)
            hm.SetHpFromNetwork(hp, maxHp);
    }

    [PunRPC]
    private void RpcShooterDied()
    {
        var hm = FindShooterHealth();
        if (hm != null)
            hm.ForceDieFromNetwork();
    }

    // 오프라인 테스트용 로컬 체력 회복
    private void ApplyHealLocal(float healAmount)
    {
        HealthManager healthManager = FindShooterHealth();
        if (healthManager == null)
            return;

        float maxHp = healthManager.MaxHp; // 체력 상한 보정 기준
        float hp = Mathf.Min(maxHp, healthManager.CurrentHp + healAmount); // 회복 후 체력
        healthManager.SetHpFromNetwork(hp, maxHp);
    }

    private HealthManager FindShooterHealth()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return null;

        return playerObj.GetComponent<HealthManager>();
    }
}
