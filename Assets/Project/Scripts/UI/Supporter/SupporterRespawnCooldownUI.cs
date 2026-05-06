using TMPro;
using UnityEngine;

public class SupporterRespawnCooldownUI : MonoBehaviour
{
    [SerializeField] private GameObject respawnCooldown;
    [SerializeField] private TextMeshProUGUI cooldownText;

    private ShooterHealthNet shooterHealthNet;
    private ShooterRespawnNet shooterRespawnNet;

    private void Awake()
    {
        AutoWireReferences();
        SetVisible(false);
    }

    private void Update()
    {
        EnsureNetworkReferences();

        bool shouldShow = IsShooterDeadOrRespawning();
        SetVisible(shouldShow);

        if (shouldShow && cooldownText != null)
            cooldownText.text = Mathf.CeilToInt(GetRemainingRespawnTime()).ToString();
    }

    // 인스펙터 참조가 비어 있어도 지정된 UI 경로의 하위 오브젝트를 찾아 연결
    private void AutoWireReferences()
    {
        if (respawnCooldown == null)
        {
            Transform found = transform.Find("RespawnCooldown");
            if (found != null)
                respawnCooldown = found.gameObject;
        }

        if (cooldownText == null && respawnCooldown != null)
            cooldownText = respawnCooldown.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    // 런타임에 생성되는 Shooter 네트워크 컴포넌트 참조 보강
    private void EnsureNetworkReferences()
    {
        if (shooterHealthNet == null)
            shooterHealthNet = ShooterHealthNet.Instance;

        if (shooterRespawnNet == null)
            shooterRespawnNet = ShooterRespawnNet.Instance;
    }

    // 사망 상태와 리스폰 카운트다운을 함께 고려한 표시 여부 판정
    private bool IsShooterDeadOrRespawning()
    {
        bool isDead = shooterHealthNet != null && shooterHealthNet.IsShooterDead;
        bool isRespawning = shooterRespawnNet != null && shooterRespawnNet.IsRespawning;
        return isDead || isRespawning;
    }

    // ShooterRespawnNet이 제공하는 공유 기준 남은 시간 조회
    private float GetRemainingRespawnTime()
    {
        return shooterRespawnNet != null ? shooterRespawnNet.RemainingRespawnTime : 0f;
    }

    // 카운트다운 UI 활성 상태 반영
    private void SetVisible(bool visible)
    {
        if (respawnCooldown != null && respawnCooldown.activeSelf != visible)
            respawnCooldown.SetActive(visible);
    }
}
