using System;
using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
public class DarknessExposureController : MonoBehaviour
{
    [Header("Exposure")]
    [SerializeField] private float maxExposure = 100f; // Darkness Exposure 최대값
    [SerializeField] private float exposureIncreasePerSecond = 22f; // 정화 영역 밖 초당 노출도 증가량
    [SerializeField] private float exposureRecoveryPerSecond = 45f; // 정화 영역 안 초당 노출도 회복량
    [SerializeField] private float damageThreshold = 50f; // HP 피해가 시작되는 노출도
    [SerializeField] private float maxDamagePerSecond = 12f; // 최대 노출도에서 초당 HP 피해량
    [SerializeField] private float warningDelay = 1f; // 정화 영역 이탈 후 경고가 표시되기까지의 지연 시간
    [SerializeField] private float damageApplyInterval = 0.25f; // 지속 피해 네트워크 적용 간격

    public event Action<float, float, bool> OnExposureChanged;

    private HealthManager healthManager;
    private float currentExposure;
    private float pendingDamage;
    private float damageApplyTimer;
    private float darknessDuration;
    private bool wasPurified = true;
    private bool warningVisible;

    public float CurrentExposure => currentExposure;
    public float MaxExposure => maxExposure;
    public float DamageThreshold => damageThreshold;
    public bool IsInDarkness => !wasPurified;
    public bool ShouldShowWarning => warningVisible;

    private void Awake()
    {
        healthManager = GetComponent<HealthManager>();
    }

    private void OnEnable()
    {
        NotifyExposureChanged();
    }

    private void Update()
    {
        if (healthManager != null && healthManager.IsDead)
        {
            SetExposure(0f, true);
            pendingDamage = 0f;
            damageApplyTimer = 0f;
            darknessDuration = 0f;
            return;
        }

        bool isPurified = IsCurrentPositionPurified();
        float delta = isPurified
            ? -exposureRecoveryPerSecond * Time.deltaTime
            : exposureIncreasePerSecond * Time.deltaTime;

        darknessDuration = isPurified ? 0f : darknessDuration + Time.deltaTime;
        SetExposure(Mathf.Clamp(currentExposure + delta, 0f, maxExposure), isPurified);
        UpdateDarknessDamage();
    }

    // 현재 위치가 등록된 정화 영역 안인지 확인
    private bool IsCurrentPositionPurified()
    {
        PurificationZoneRegistry registry = PurificationZoneRegistry.Instance;
        return registry != null && registry.IsPositionPurified(transform.position);
    }

    // 노출도가 기준치를 넘었을 때 MasterClient 기준으로 Shooter HP 피해 적용
    private void UpdateDarknessDamage()
    {
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            return;

        if (wasPurified || currentExposure <= damageThreshold)
        {
            pendingDamage = 0f;
            damageApplyTimer = 0f;
            return;
        }

        float denominator = Mathf.Max(0.01f, maxExposure - damageThreshold);
        float t = Mathf.Clamp01((currentExposure - damageThreshold) / denominator);
        float damagePerSecond = Mathf.Lerp(0f, maxDamagePerSecond, t);

        pendingDamage += damagePerSecond * Time.deltaTime;
        damageApplyTimer += Time.deltaTime;

        if (damageApplyTimer < damageApplyInterval)
            return;

        if (ShooterHealthNet.Instance != null && pendingDamage > 0f)
            ShooterHealthNet.Instance.MasterApplyDamageToShooter(pendingDamage);

        pendingDamage = 0f;
        damageApplyTimer = 0f;
    }

    // Darkness Exposure 상태 변경과 UI 이벤트 발행
    private void SetExposure(float newExposure, bool isPurified)
    {
        bool changed = !Mathf.Approximately(currentExposure, newExposure) || wasPurified != isPurified;
        currentExposure = newExposure;
        wasPurified = isPurified;

        bool previousWarningVisible = warningVisible;
        if (!isPurified && darknessDuration >= warningDelay)
            warningVisible = true;
        else if (currentExposure <= 0.01f)
            warningVisible = false;

        changed = changed || previousWarningVisible != warningVisible;

        if (changed)
            NotifyExposureChanged();
    }

    private void NotifyExposureChanged()
    {
        OnExposureChanged?.Invoke(currentExposure, maxExposure, ShouldShowWarning);
    }

    private void OnValidate()
    {
        maxExposure = Mathf.Max(1f, maxExposure);
        exposureIncreasePerSecond = Mathf.Max(0f, exposureIncreasePerSecond);
        exposureRecoveryPerSecond = Mathf.Max(0f, exposureRecoveryPerSecond);
        damageThreshold = Mathf.Clamp(damageThreshold, 0f, maxExposure);
        maxDamagePerSecond = Mathf.Max(0f, maxDamagePerSecond);
        warningDelay = Mathf.Max(0f, warningDelay);
        damageApplyInterval = Mathf.Max(0.05f, damageApplyInterval);
    }
}
