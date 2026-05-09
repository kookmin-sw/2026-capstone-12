using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PurificationLightSource))]
public class ShooterPurification : MonoBehaviour
{
    [Header("Gauge")]
    [SerializeField] private float maxGauge = 100f; // Purification Gauge 최대값
    [SerializeField] private float graceDuration = 5f; // 게이지 획득 후 감소가 시작되기 전 유지 시간
    [SerializeField] private float decayPerSecond = 12f; // graceDuration 이후 초당 게이지 감소량

    [Header("Field")]
    [SerializeField] private float minFieldRadius = 4f; // 게이지가 0보다 클 때 Shooter Field 최소 반경
    [SerializeField] private float maxFieldRadius = 16f; // 게이지가 최대일 때 Shooter Field 최대 반경
    [SerializeField] private Light shooterFieldLight; // Shooter 주변 Purification Field 시각화용 Point Light

    [Header("Enemy Rewards")]
    [SerializeField] private float basicEnemyGain = 7f; // BasicEnemy 처치 시 게이지 획득량
    [SerializeField] private float fastEnemyGain = 9f; // FastEnemy 처치 시 게이지 획득량
    [SerializeField] private float tankEnemyGain = 18f; // TankEnemy 처치 시 게이지 획득량
    [SerializeField] private float sanctuaryKillGainMultiplier = 0.5f; // SanctuaryZone 내부 처치 보상 배율

    public event Action<float, float> OnGaugeChanged; // UI 표시용 게이지 변경 이벤트

    private PurificationLightSource fieldSource;
    private HealthManager healthManager;
    private float currentGauge;
    private float lastGainTime = -999f;
    private bool isSuppressedByDeath;

    public float CurrentGauge => currentGauge;
    public float MaxGauge => maxGauge;
    public float CurrentFieldRadius => CalculateFieldRadius();

    private void Awake()
    {
        fieldSource = GetComponent<PurificationLightSource>();
        healthManager = GetComponent<HealthManager>();
        ApplyGaugeState();
    }

    private void OnEnable()
    {
        if (healthManager == null)
            healthManager = GetComponent<HealthManager>();

        if (healthManager == null)
            return;

        healthManager.OnDied += HandleDied;
        healthManager.OnRevived += HandleRevived;
        isSuppressedByDeath = healthManager.IsDead;
        ApplyGaugeState();
    }

    private void OnDisable()
    {
        if (healthManager == null)
            return;

        healthManager.OnDied -= HandleDied;
        healthManager.OnRevived -= HandleRevived;
    }

    // 적 처치 시 Purification Gauge 보상 지급
    public bool AddPurificationEnergyForEnemy(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null)
            return false;

        float gain = GetEnemyPurificationGain(enemyHealth);
        if (SanctuaryZoneContains(enemyHealth.transform.position))
            gain *= sanctuaryKillGainMultiplier;

        return AddPurificationEnergy(gain);
    }

    // Purification Gauge 증가
    public bool AddPurificationEnergy(float amount)
    {
        if (amount <= 0f)
            return false;

        float previousGauge = currentGauge;
        currentGauge = Mathf.Clamp(currentGauge + amount, 0f, maxGauge);
        lastGainTime = Time.time;
        ApplyGaugeState();
        NotifyGaugeChanged();

        return !Mathf.Approximately(previousGauge, currentGauge);
    }

    // graceDuration 이후 Purification Gauge 감소 처리
    public bool TickGaugeDecay(float deltaTime)
    {
        if (currentGauge <= 0f || Time.time - lastGainTime < graceDuration)
            return false;

        float previousGauge = currentGauge;
        currentGauge = Mathf.Max(0f, currentGauge - decayPerSecond * deltaTime);
        ApplyGaugeState();
        NotifyGaugeChanged();

        return !Mathf.Approximately(previousGauge, currentGauge);
    }

    // 네트워크 수신 Purification Gauge 적용
    public void SetGaugeFromNet(float newGauge)
    {
        currentGauge = Mathf.Clamp(newGauge, 0f, maxGauge);
        ApplyGaugeState();
        NotifyGaugeChanged();
    }

    // 현재 Purification Gauge 이벤트 재발행
    public void NotifyGaugeChanged()
    {
        OnGaugeChanged?.Invoke(currentGauge, maxGauge);
    }

    // Purification Gauge 기반 fieldSource와 shooterFieldLight 상태 반영
    private void ApplyGaugeState()
    {
        if (fieldSource == null)
            fieldSource = GetComponent<PurificationLightSource>();

        float radius = CalculateFieldRadius();

        if (fieldSource != null)
        {
            fieldSource.sourceType = PurificationLightSourceType.ShooterField;
            fieldSource.radius = radius;
            fieldSource.strength = maxGauge > 0f ? currentGauge / maxGauge : 0f;
            fieldSource.preventsDarknessExposure = radius > 0f;
            fieldSource.isPermanent = false;
        }

        if (shooterFieldLight != null)
        {
            shooterFieldLight.range = radius;
            shooterFieldLight.intensity = Mathf.Lerp(0.6f, 2.5f, maxGauge > 0f ? currentGauge / maxGauge : 0f);
            shooterFieldLight.enabled = radius > 0f;
        }
    }

    // Purification Gauge 기반 Shooter Field 반경 계산
    private float CalculateFieldRadius()
    {
        if (isSuppressedByDeath || maxGauge <= 0f || currentGauge <= 0f)
            return 0f;

        float t = Mathf.Clamp01(currentGauge / maxGauge);
        return Mathf.Lerp(minFieldRadius, maxFieldRadius, t);
    }

    // 적 종류별 Purification Gauge 보상값 선택
    private void HandleDied()
    {
        isSuppressedByDeath = true;
        ApplyGaugeState();
    }

    private void HandleRevived()
    {
        isSuppressedByDeath = false;
        ApplyGaugeState();
    }

    private float GetEnemyPurificationGain(EnemyHealth enemyHealth)
    {
        string enemyName = enemyHealth.gameObject.name;
        if (enemyName.Contains("Tank"))
            return tankEnemyGain;

        if (enemyName.Contains("Fast"))
            return fastEnemyGain;

        return basicEnemyGain;
    }

    // SanctuaryZone 내부 위치 여부 확인
    private bool SanctuaryZoneContains(Vector3 worldPosition)
    {
        PurificationZoneRegistry registry = PurificationZoneRegistry.Instance;
        if (registry == null)
            return false;

        foreach (PurificationLightSource source in registry.Sources)
        {
            if (source == null || source.sourceType != PurificationLightSourceType.Sanctuary)
                continue;

            if (source.Contains(worldPosition))
                return true;
        }

        return false;
    }

    private void OnValidate()
    {
        // 인스펙터 음수 수치 입력 방지
        maxGauge = Mathf.Max(1f, maxGauge);
        graceDuration = Mathf.Max(0f, graceDuration);
        decayPerSecond = Mathf.Max(0f, decayPerSecond);
        minFieldRadius = Mathf.Max(0f, minFieldRadius);
        maxFieldRadius = Mathf.Max(minFieldRadius, maxFieldRadius);
        basicEnemyGain = Mathf.Max(0f, basicEnemyGain);
        fastEnemyGain = Mathf.Max(0f, fastEnemyGain);
        tankEnemyGain = Mathf.Max(0f, tankEnemyGain);
        sanctuaryKillGainMultiplier = Mathf.Max(0f, sanctuaryKillGainMultiplier);
    }
}
