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
    private bool isShooterDead = false; // 사망 중 중복 데미지/회복/리스폰 요청을 막는 상태 플래그
    private ShooterRespawnNet respawnNet; // HP 동기화와 분리된 리스폰 규칙 처리 컴포넌트

    public bool IsShooterDead => isShooterDead;
    public float EffectiveMaxHp => shooterMaxHp + bonusHp;

    /// <summary>
    /// 싱글톤과 리스폰 컴포넌트를 준비해 HP 관리와 리스폰 규칙을 연결
    /// </summary>
    private void Awake()
    {
        Instance = this;
        respawnNet = GetComponent<ShooterRespawnNet>();
        if (respawnNet == null)
            respawnNet = gameObject.AddComponent<ShooterRespawnNet>();
    }

    /// <summary>
    /// 씬 시작 시 마스터 기준 초기 HP를 동기화
    /// </summary>
    private void Start()
    {
        TryInitializeHp();
    }

    /// <summary>
    /// 방 입장 이후 생성 순서 차이로 초기화가 누락되지 않도록 HP 초기화를 재시도
    /// </summary>
    public override void OnJoinedRoom()
    {
        TryInitializeHp();
    }

    // 마스터에서만 호출해 슈터 데미지를 확정하고 사망 시 리스폰 흐름으로 넘김
    public void MasterApplyDamageToShooter(float damage)
    {
        if (!HasMasterAuthority()) return;
        if (isShooterDead) return;

        TryInitializeHp();

        shooterHp = Mathf.Max(0, shooterHp - damage);
        BroadcastShooterHp(shooterHp, EffectiveMaxHp);

        if (shooterHp <= 0)
            HandleShooterDeathByMaster();
    }

    // Support 아이템 회복을 마스터에 요청해 모든 클라이언트의 HP를 같은 값으로 맞춤
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

    /// <summary>
    /// Supporter 회복 요청을 마스터에서 검증하고 슈터 HP에 반영
    /// </summary>
    [PunRPC]
    private void RpcRequestHealShooter(float healAmount)
    {
        if (!HasMasterAuthority()) return;
        if (isShooterDead) return;

        TryInitializeHp();

        shooterHp = Mathf.Min(EffectiveMaxHp, shooterHp + healAmount);
        BroadcastShooterHp(shooterHp, EffectiveMaxHp);
    }

    /// <summary>
    /// 마스터가 확정한 슈터 HP를 각 클라이언트의 씬 Player HealthManager에 반영
    /// </summary>
    [PunRPC]
    private void RpcSetShooterHp(float hp, float maxHp)
    {
        if (hp > 0f)
            isShooterDead = false;

        var hm = FindShooterHealth();
        if (hm != null)
            hm.SetHpFromNetwork(hp, maxHp);
    }

    /// <summary>
    /// 모든 클라이언트에서 슈터를 사망 상태로 전환해 입력/어그로 처리 기준을 맞춤
    /// </summary>
    [PunRPC]
    // 슈터 사망 상태와 Supporter 알림음 반영
    private void RpcShooterDied()
    {
        isShooterDead = true;

        var hm = FindShooterHealth();
        if (hm != null)
            hm.ForceDieFromNetwork();

        if (IsLocalSupporter())
            SupporterUISoundManager.Instance?.Play(SupporterUISoundType.ShooterDeath);
    }

    /// <summary>
    /// 레벨업에 의한 최대 체력 보너스 설정 (마스터에서만 실제 적용)
    /// </summary>
    public void SetBonusHp(float bonus)
    {
        if (!HasMasterAuthority()) return;
        TryInitializeHp();

        float oldMax = EffectiveMaxHp;
        bonusHp = bonus;
        float newMax = EffectiveMaxHp;

        // 최대 체력 증가분만큼 현재 체력도 회복
        float hpGain = newMax - oldMax;
        if (!isShooterDead && hpGain > 0)
            shooterHp = Mathf.Min(shooterHp + hpGain, newMax);

        BroadcastShooterHp(shooterHp, newMax);
    }

    /// <summary>
    /// 리스폰 완료 시 마스터의 HP 상태를 먼저 복구해 이후 데미지/회복 처리가 가능하게 함
    /// </summary>
    public void MasterCompleteRespawn(float hp)
    {
        if (!HasMasterAuthority()) return;

        shooterHp = Mathf.Clamp(hp, 1f, EffectiveMaxHp);
        isShooterDead = false;
    }

    /// <summary>
    /// 리스폰 RPC를 받은 클라이언트에서 HealthManager 부활 이벤트를 발생시킴
    /// </summary>
    public void ApplyRespawnFromNetwork(float hp, float maxHp)
    {
        isShooterDead = false;

        var hm = FindShooterHealth();
        if (hm != null)
            hm.ReviveFromNetwork(hp, maxHp);
    }

    /// <summary>
    /// 마스터만 최초 HP를 확정해 방 전체에 같은 체력 상태를 배포
    /// </summary>
    private void TryInitializeHp()
    {
        if (isHpInitialized)
            return;

        if (!HasMasterAuthority())
            return;

        shooterHp = EffectiveMaxHp;
        isHpInitialized = true;
        isShooterDead = false;
        BroadcastShooterHp(shooterHp, EffectiveMaxHp);
    }

    /// <summary>
    /// 슈터 사망을 한 번만 확정하고 실제 리스폰 타이머는 ShooterRespawnNet에 위임
    /// </summary>
    private void HandleShooterDeathByMaster()
    {
        if (isShooterDead)
            return;

        isShooterDead = true;
        BroadcastShooterDied();
        respawnNet?.BeginRespawn(this);
    }

    /// <summary>
    /// 네트워크 방 여부에 따라 RPC 또는 로컬 호출로 HP 변경을 전파
    /// </summary>
    private void BroadcastShooterHp(float hp, float maxHp)
    {
        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(RpcSetShooterHp), RpcTarget.All, hp, maxHp);
        else
            RpcSetShooterHp(hp, maxHp);
    }

    /// <summary>
    /// 슈터 사망 이벤트를 모든 클라이언트에 동일하게 전달
    /// </summary>
    private void BroadcastShooterDied()
    {
        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(RpcShooterDied), RpcTarget.All);
        else
            RpcShooterDied();
    }

    /// <summary>
    /// 오프라인 테스트와 멀티플레이에서 공통으로 쓸 마스터 권한 기준을 제공
    /// </summary>
    private bool HasMasterAuthority()
    {
        return !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
    }

    // 로컬 플레이어의 Supporter 역할 여부 확인
    private bool IsLocalSupporter()
    {
        if (PhotonNetwork.LocalPlayer == null)
            return false;

        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Role", out object roleValue))
            return false;

        return roleValue as string == "Supporter";
    }

    /// <summary>
    /// 네트워크 방 밖 테스트에서 Support 회복 효과를 로컬 HealthManager에 직접 적용
    /// </summary>
    private void ApplyHealLocal(float healAmount)
    {
        if (isShooterDead)
            return;

        HealthManager healthManager = FindShooterHealth();
        if (healthManager == null)
            return;

        float maxHp = healthManager.MaxHp;
        float hp = Mathf.Min(maxHp, healthManager.CurrentHp + healAmount);
        healthManager.SetHpFromNetwork(hp, maxHp);
    }

    /// <summary>
    /// 씬에 유지되는 Player 오브젝트에서 슈터 체력 컴포넌트를 찾음
    /// </summary>
    private HealthManager FindShooterHealth()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return null;

        return playerObj.GetComponent<HealthManager>();
    }
}
