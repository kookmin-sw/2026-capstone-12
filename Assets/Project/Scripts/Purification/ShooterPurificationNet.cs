using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ShooterPurification))]
public class ShooterPurificationNet : MonoBehaviourPun
{
    [Header("Sync")]
    [SerializeField] private float syncInterval = 0.1f; // 게이지 감소 중 RPC 전송 간격

    public static ShooterPurificationNet Instance { get; private set; }

    private ShooterPurification purification;
    private float lastSyncTime = -999f;

    private void Awake()
    {
        Instance = this;
        purification = GetComponent<ShooterPurification>();
    }

    private void Update()
    {
        if (!HasGaugeAuthority() || purification == null)
            return;

        if (purification.TickGaugeDecay(Time.deltaTime))
            SyncGaugeState(false);
    }

    // MasterClient 기준 적 처치 Purification Gauge 보상 동기화
    public void MasterAddPurificationEnergyForEnemy(EnemyHealth enemyHealth)
    {
        if (!HasGaugeAuthority() || purification == null)
            return;

        if (purification.AddPurificationEnergyForEnemy(enemyHealth))
            SyncGaugeState(true);
    }

    // MasterClient 기준 Purification Gauge 증가 동기화
    public void MasterAddPurificationEnergy(float amount)
    {
        if (!HasGaugeAuthority() || purification == null)
            return;

        if (purification.AddPurificationEnergy(amount))
            SyncGaugeState(true);
    }

    // Purification Gauge 네트워크 동기화
    private void SyncGaugeState(bool force)
    {
        if (!force && Time.time - lastSyncTime < syncInterval)
            return;

        lastSyncTime = Time.time;

        if (PhotonNetwork.InRoom && photonView != null)
        {
            photonView.RPC(nameof(RpcSetPurificationGauge), RpcTarget.All, purification.CurrentGauge);
            return;
        }

        purification.SetGaugeFromNet(purification.CurrentGauge);
    }

    [PunRPC]
    private void RpcSetPurificationGauge(float newGauge)
    {
        if (purification == null)
            purification = GetComponent<ShooterPurification>();

        purification?.SetGaugeFromNet(newGauge);
    }

    // Purification Gauge 계산 권위 확인
    private bool HasGaugeAuthority()
    {
        return !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
    }

    private void OnValidate()
    {
        // 인스펙터 음수 수치 입력 방지
        syncInterval = Mathf.Max(0.02f, syncInterval);
    }
}
