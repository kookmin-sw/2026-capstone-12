using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ShooterHealthNet : MonoBehaviourPunCallbacks
{
    public static ShooterHealthNet Instance { get; private set; }

    [Header("Player HP")]
    [SerializeField] private float shooterMaxHp = 100;
    private float bonusHp = 0f;

    // 테스트 규칙: 마스터(슈터) HP만 관리
    private float shooterHp;
    private bool isHpInitialized = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        TryInitializeHp();
    }

    public override void OnJoinedRoom()
    {
        TryInitializeHp();
    }

    // 마스터에서만 호출: 슈터에게 데미지 적용
    public void MasterApplyDamageToShooter(float damage)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        TryInitializeHp();

        shooterHp = Mathf.Max(0, shooterHp - damage);
        float effectiveMax = shooterMaxHp + bonusHp;
        photonView.RPC(nameof(RpcSetShooterHp), RpcTarget.All, shooterHp, effectiveMax);

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
    private void RpcRequestHealShooter(float healAmount)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        TryInitializeHp();

        float effectiveMax = shooterMaxHp + bonusHp;
        shooterHp = Mathf.Min(effectiveMax, shooterHp + healAmount);
        photonView.RPC(nameof(RpcSetShooterHp), RpcTarget.All, shooterHp, effectiveMax);
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

    /// <summary>
    /// 레벨업에 의한 최대 체력 보너스 설정 (마스터에서만 실제 적용)
    /// </summary>
    public void SetBonusHp(float bonus)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        TryInitializeHp();

        float oldMax = shooterMaxHp + bonusHp;
        bonusHp = bonus;
        float newMax = shooterMaxHp + bonusHp;

        // 최대 체력 증가분만큼 현재 체력도 회복
        float hpGain = newMax - oldMax;
        if (hpGain > 0)
            shooterHp = Mathf.Min(shooterHp + hpGain, newMax);

        photonView.RPC(nameof(RpcSetShooterHp), RpcTarget.All, shooterHp, newMax);
    }

    private void TryInitializeHp()
    {
        if (isHpInitialized)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        float effectiveMax = shooterMaxHp + bonusHp;
        shooterHp = effectiveMax;
        isHpInitialized = true;
        photonView.RPC(nameof(RpcSetShooterHp), RpcTarget.All, shooterHp, effectiveMax);
    }

    // 오프라인 테스트용 로컬 체력 회복
    private void ApplyHealLocal(float healAmount)
    {
        HealthManager healthManager = FindShooterHealth();
        if (healthManager == null)
            return;

        float maxHp = healthManager.MaxHp;
        float hp = Mathf.Min(maxHp, healthManager.CurrentHp + healAmount);
        healthManager.SetHpFromNetwork(hp, maxHp);
    }

    private HealthManager FindShooterHealth()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return null;

        return playerObj.GetComponent<HealthManager>();
    }
}
