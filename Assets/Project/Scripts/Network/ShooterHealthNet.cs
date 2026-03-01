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

    private HealthManager FindShooterHealth()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return null;

        return playerObj.GetComponent<HealthManager>();
    }
}