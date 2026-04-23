using Photon.Pun;
using UnityEngine;

public class PlayerDeathHandler : MonoBehaviourPun
{
    [SerializeField] private HealthManager healthManager;
    [Tooltip("사망 중 로컬 슈터의 입력/전투 기능을 멈추기 위해 비활성화할 컴포넌트")]
    [SerializeField] private Behaviour[] disableOnDeath; // 죽을 때 비활성화할 로컬 입력/전투 컴포넌트
    [Tooltip("사망 중 피격, 이동, 아이템 획득 같은 충돌 처리를 막기 위해 비활성화할 콜라이더")]
    [SerializeField] private Collider[] disableCollidersOnDeath; // 사망 중 피격/픽업 충돌을 막고 싶을 때 끌 콜라이더
    [Tooltip("사망 중 모델, 이펙트 등 통째로 숨기거나 멈출 하위 오브젝트")]
    [SerializeField] private GameObject[] disableObjectsOnDeath; // 사망 중 통째로 끌 하위 오브젝트

    /// <summary>
    /// 인스펙터 참조가 비어 있어도 같은 Player 오브젝트의 HealthManager를 사용
    /// </summary>
    private void Awake()
    {
        if (healthManager == null) healthManager = GetComponent<HealthManager>();
    }

    /// <summary>
    /// HealthManager 사망/부활 이벤트를 받아 Player 상태 전환을 처리하도록 구독
    /// </summary>
    private void OnEnable()
    {
        if (healthManager != null) healthManager.OnDied += HandleDied;
        if (healthManager != null) healthManager.OnRevived += HandleRevived;
    }

    /// <summary>
    /// 비활성화 시 이벤트 구독을 해제해 중복 호출과 파괴 후 참조를 방지
    /// </summary>
    private void OnDisable()
    {
        if (healthManager != null) healthManager.OnDied -= HandleDied;
        if (healthManager != null) healthManager.OnRevived -= HandleRevived;
    }

    /// <summary>
    /// 사망 이벤트를 입력/표시 비활성화 상태로 변환
    /// </summary>
    private void HandleDied()
    {
        SetDeathState(true);
        Debug.Log("Player Died. Waiting for respawn...");
    }

    /// <summary>
    /// 부활 이벤트를 입력/표시 복구 상태로 변환
    /// </summary>
    private void HandleRevived()
    {
        SetDeathState(false);
        Debug.Log("Player Revived.");
    }

    /// <summary>
    /// Player 루트는 유지하고 조작, 충돌, 표시 오브젝트만 사망 상태에 맞게 전환
    /// </summary>
    private void SetDeathState(bool isDead)
    {
        if (photonView.IsMine)
        {
            for (int i = 0; i < disableOnDeath.Length; i++)
                if (disableOnDeath[i] != null) disableOnDeath[i].enabled = !isDead;
        }

        SetColliders(!isDead);
        SetObjects(!isDead);
    }

    /// <summary>
    /// 사망 중 불필요한 충돌을 막고 부활 시 다시 켬
    /// </summary>
    private void SetColliders(bool enabled)
    {
        if (disableCollidersOnDeath == null)
            return;

        for (int i = 0; i < disableCollidersOnDeath.Length; i++)
            if (disableCollidersOnDeath[i] != null) disableCollidersOnDeath[i].enabled = enabled;
    }

    /// <summary>
    /// 사망 상태와 함께 꺼야 하는 하위 오브젝트를 일괄 전환
    /// </summary>
    private void SetObjects(bool active)
    {
        if (disableObjectsOnDeath == null)
            return;

        for (int i = 0; i < disableObjectsOnDeath.Length; i++)
            if (disableObjectsOnDeath[i] != null) disableObjectsOnDeath[i].SetActive(active);
    }
}
