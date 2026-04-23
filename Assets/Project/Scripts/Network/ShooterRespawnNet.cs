using Photon.Pun;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class ShooterRespawnNet : MonoBehaviourPun
{
    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 5f; // 사망 후 부활까지 대기 시간
    [SerializeField, Range(0.01f, 1f)] private float respawnHpRatio = 1f; // 부활 시 최대 체력 대비 회복 비율
    [SerializeField] private Transform respawnPoint; // 지정 시 해당 위치를 부활 지점으로 사용

    private ShooterHealthNet healthNet; // 리스폰 완료 시 HP 상태를 복구할 대상
    private Coroutine respawnCoroutine; // 중복 리스폰 타이머 실행 방지용
    private Vector3 fallbackRespawnPosition; // respawnPoint가 없을 때 사용할 시작 위치
    private bool hasFallbackRespawnPosition; // fallbackRespawnPosition이 유효한지 표시

    /// <summary>
    /// 같은 오브젝트의 ShooterHealthNet을 캐시해 리스폰 완료 시 HP 복구에 사용
    /// </summary>
    private void Awake()
    {
        healthNet = GetComponent<ShooterHealthNet>();
    }

    /// <summary>
    /// 명시적 리스폰 지점이 없는 씬을 위해 시작 배치 위치를 예비 부활 지점으로 저장
    /// </summary>
    private void Start()
    {
        CacheFallbackRespawnPosition();
    }

    /// <summary>
    /// 마스터에서 리스폰 타이머를 시작해 모든 클라이언트의 부활 시점을 하나로 맞춤
    /// </summary>
    public void BeginRespawn(ShooterHealthNet owner)
    {
        if (!HasMasterAuthority())
            return;

        healthNet = owner != null ? owner : healthNet;
        if (healthNet == null || respawnCoroutine != null)
            return;

        respawnCoroutine = StartCoroutine(RespawnShooterAfterDelay());
    }

    /// <summary>
    /// 설정된 대기 시간 후 부활 HP와 위치를 계산하고 RPC로 배포
    /// </summary>
    private IEnumerator RespawnShooterAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, respawnDelay));

        float effectiveMax = healthNet.EffectiveMaxHp;
        float respawnHp = Mathf.Clamp(effectiveMax * respawnHpRatio, 1f, effectiveMax);
        Vector3 position = ResolveRespawnPosition();

        healthNet.MasterCompleteRespawn(respawnHp);
        BroadcastShooterRespawn(position, respawnHp, effectiveMax);
        respawnCoroutine = null;
    }

    /// <summary>
    /// 지정된 리스폰 지점을 우선하고, 없으면 시작 위치나 현재 Player 위치를 사용
    /// </summary>
    private Vector3 ResolveRespawnPosition()
    {
        if (respawnPoint != null)
            return respawnPoint.position;

        if (hasFallbackRespawnPosition)
            return fallbackRespawnPosition;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        return playerObj != null ? playerObj.transform.position : Vector3.zero;
    }

    /// <summary>
    /// respawnPoint가 비어 있어도 안정적으로 돌아갈 수 있게 시작 위치를 저장
    /// </summary>
    private void CacheFallbackRespawnPosition()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
            return;

        fallbackRespawnPosition = playerObj.transform.position;
        hasFallbackRespawnPosition = true;
    }

    /// <summary>
    /// 모든 클라이언트에서 슈터 위치를 옮기고 HealthManager를 부활 상태로 복구
    /// </summary>
    [PunRPC]
    private void RpcRespawnShooter(Vector3 position, float hp, float maxHp)
    {
        MoveShooterToRespawn(position);

        if (healthNet == null)
            healthNet = GetComponent<ShooterHealthNet>();

        healthNet?.ApplyRespawnFromNetwork(hp, maxHp);
    }

    /// <summary>
    /// CharacterController 충돌 보정을 피하려고 잠시 끈 뒤 Player 루트 위치를 이동
    /// </summary>
    private void MoveShooterToRespawn(Vector3 position)
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
            return;

        CharacterController controller = playerObj.GetComponent<CharacterController>();
        bool wasControllerEnabled = controller != null && controller.enabled;
        if (controller != null)
            controller.enabled = false;

        playerObj.transform.position = position;

        if (controller != null)
            controller.enabled = wasControllerEnabled;
    }

    /// <summary>
    /// 네트워크 방 여부에 따라 리스폰 RPC 또는 로컬 부활 호출을 실행
    /// </summary>
    private void BroadcastShooterRespawn(Vector3 position, float hp, float maxHp)
    {
        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(RpcRespawnShooter), RpcTarget.All, position, hp, maxHp);
        else
            RpcRespawnShooter(position, hp, maxHp);
    }

    /// <summary>
    /// 오프라인 테스트와 멀티플레이에서 공통으로 쓸 마스터 권한 기준을 제공
    /// </summary>
    private bool HasMasterAuthority()
    {
        return !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
    }
}
