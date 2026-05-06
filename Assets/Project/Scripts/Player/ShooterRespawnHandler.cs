using UnityEngine;

public class ShooterRespawnHandler : MonoBehaviour
{
    [SerializeField] private HealthManager healthManager;
    [SerializeField] private GameObject reviveEffectPrefab; // 부활 시 생성할 이펙트
    [SerializeField] private float reviveEffectLifetime = 1f; // 부활 이펙트 자동 제거 시간
    [SerializeField] private Transform reviveEffectPoint;

    private void Awake()
    {
        if (healthManager == null)
            healthManager = GetComponent<HealthManager>();

        if (reviveEffectPoint == null)
            reviveEffectPoint = transform.Find("ShooterEffectPoint");
    }

    private void OnEnable()
    {
        if (healthManager != null)
            healthManager.OnRevived += HandleRevived;
    }

    private void OnDisable()
    {
        if (healthManager != null)
            healthManager.OnRevived -= HandleRevived;
    }

    // 부활 이벤트에서 Shooter 전용 피드백 실행
    private void HandleRevived()
    {
        PlayReviveEffect();
        PlayReviveSound();
    }

    // ShooterEffectPoint 기준 부활 이펙트 생성
    private void PlayReviveEffect()
    {
        if (reviveEffectPrefab == null)
            return;

        Transform pivot = reviveEffectPoint != null ? reviveEffectPoint : transform;
        GameObject effect = Instantiate(reviveEffectPrefab, pivot.position, Quaternion.identity);

        if (reviveEffectLifetime > 0f)
            Destroy(effect, reviveEffectLifetime);
    }

    // 위치와 무관한 부활 사운드 로컬 재생
    private void PlayReviveSound()
    {
        if (SoundNet.Instance != null)
        {
            SoundNet.Instance.PlayLocal(GameSoundType.ShooterRespawn);
            return;
        }

        GameEventSoundPlayer.Instance?.Play(GameSoundType.ShooterRespawn);
    }
}
