using System.Collections;
using Photon.Pun;
using UnityEngine;

public class SupportPickupItem : MonoBehaviour
{
    [SerializeField] private SupportItemKind kind;
    [SerializeField] private SupporterItemSO item;
    [SerializeField] private GameObject healEffectPrefab;
    [SerializeField] private GameObject ammoEffectPrefab;
    [SerializeField] private float floatAmplitude = 0.18f;
    [SerializeField] private float floatSpeed = 1.6f;
    [SerializeField] private float rotateSpeed = 45f;

    private int supportId = -1;
    private bool consuming;
    private bool applySoundPlayed;
    private Vector3 basePosition;

    public int SupportId => supportId;

    private void Awake()
    {
        basePosition = transform.position;
        EnsurePickupPhysics();
    }

    private void Update()
    {
        if (consuming)
            return;

        float offset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = basePosition + Vector3.up * offset;
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (consuming || !other.CompareTag("Player"))
            return;

        if (!HasLocalShooterAuthority(other.gameObject))
            return;

        if (BuildNetManager.Instance != null && supportId >= 0)
        {
            BuildNetManager.Instance.RequestConsumeSupport(supportId);
        }
        else
        {
            StartConsuming();
        }
    }

    public void Configure(int id, SupportItemKind itemKind)
    {
        supportId = id;
        kind = itemKind;
        item = null;
        basePosition = transform.position;
        EnsurePickupPhysics();
    }

    public void Configure(int id, SupporterItemSO itemDefinition)
    {
        supportId = id;
        item = itemDefinition;
        if (itemDefinition != null)
            kind = itemDefinition.kind;

        basePosition = transform.position;
        EnsurePickupPhysics();
    }

    public void StartConsuming()
    {
        if (consuming)
            return;

        StartCoroutine(ConsumeRoutine());
    }

    private IEnumerator ConsumeRoutine()
    {
        consuming = true;
        PlayGetItemSound();
        PlayApplySoundOnce();

        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.enabled = false;

        float duration = GetApplyDuration();
        GameObject shooterEffect = CreateShooterEffect();
        int steps = Mathf.Max(1, Mathf.CeilToInt(duration / 0.2f));
        float stepDelay = duration / steps;

        for (int i = 0; i < steps; i++)
        {
            ApplyStepEffect(item, steps);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 1f / (steps - i));
            yield return new WaitForSeconds(stepDelay);
        }

        if (shooterEffect != null)
            Destroy(shooterEffect);

        Destroy(gameObject);
    }

    private void ApplyStepEffect(SupporterItemSO itemDefinition, int steps)
    {
        if (!IsLocalShooter() || itemDefinition == null)
            return;

        HealthPackSupportEffectSO healthEffect = itemDefinition.effect as HealthPackSupportEffectSO;
        if (healthEffect != null && healthEffect.healAmount > 0f)
            ApplyHealth(healthEffect.healAmount / steps);

        AmmoPackSupportEffectSO ammoEffect = itemDefinition.effect as AmmoPackSupportEffectSO;
        if (ammoEffect != null && ammoEffect.ammoAmount > 0)
            ApplyAmmo(Mathf.CeilToInt((float)ammoEffect.ammoAmount / steps));
    }

    private void ApplyHealth(float amount)
    {
        if (ShooterHealthNet.Instance != null)
        {
            ShooterHealthNet.Instance.RequestHealShooter(amount);
            return;
        }

        HealthManager healthManager = FindShooterHealthManager();
        if (healthManager == null)
            return;

        float maxHp = healthManager.MaxHp;
        float hp = Mathf.Min(maxHp, healthManager.CurrentHp + amount);
        healthManager.SetHpFromNetwork(hp, maxHp);
    }

    private void ApplyAmmo(int amount)
    {
        AmmoManager ammoManager = FindShooterAmmoManager();
        if (ammoManager == null)
            return;

        ammoManager.AddReserveAmmo(amount);
        ShooterAmmoNet.Instance?.SyncAmmoFromShooter(ammoManager.CurrentAmmo, ammoManager.ReserveAmmo, ammoManager.MaxAmmo);
    }

    private void PlayGetItemSound()
    {
        if (SoundNet.Instance != null)
        {
            SoundNet.Instance.PlayLocalAt(GameSoundType.GetItem, transform.position);
            return;
        }

        GameEventSoundPlayer.Instance?.PlayAt(GameSoundType.GetItem, transform.position);
    }

    private void PlayApplySoundOnce()
    {
        if (applySoundPlayed || !IsLocalShooter() || item == null)
            return;

        applySoundPlayed = true;
        if (item.effect is HealthPackSupportEffectSO)
        {
            PlayLocalGameSound(GameSoundType.ApplyHealthPack);
            return;
        }

        if (item.effect is AmmoPackSupportEffectSO)
            PlayLocalGameSound(GameSoundType.ApplyAmmoPack);
    }

    private void PlayLocalGameSound(GameSoundType soundType)
    {
        if (SoundNet.Instance != null)
        {
            SoundNet.Instance.PlayLocal(soundType);
            return;
        }

        GameEventSoundPlayer.Instance?.Play(soundType);
    }

    private GameObject CreateShooterEffect()
    {
        bool isAmmoEffect = IsAmmoEffect();
        GameObject effectPrefab = isAmmoEffect ? ammoEffectPrefab : healEffectPrefab;
        if (effectPrefab == null)
            return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        Transform pivot = player.transform;
        if (isAmmoEffect)
        {
            Transform effectPoint = player.transform.Find("ShooterEffectPoint");
            if (effectPoint != null)
                pivot = effectPoint;
        }

        return Instantiate(effectPrefab, pivot.position, Quaternion.identity, pivot);
    }

    private bool IsAmmoEffect()
    {
        if (item != null)
        {
            if (item.effect is AmmoPackSupportEffectSO)
                return true;

            if (item.effect is HealthPackSupportEffectSO)
                return false;
        }

        return kind == SupportItemKind.AmmoPack;
    }

    private float GetApplyDuration()
    {
        if (item == null)
            return 1f;

        if (item.effect is AmmoPackSupportEffectSO ammoEffect)
            return Mathf.Max(0.1f, ammoEffect.applyDuration);

        if (item.effect is HealthPackSupportEffectSO healthEffect)
            return Mathf.Max(0.1f, healthEffect.applyDuration);

        return 1f;
    }

    private bool HasLocalShooterAuthority(GameObject player)
    {
        if (!PhotonNetwork.IsConnected)
            return true;

        PhotonView photonView = player.GetComponent<PhotonView>();
        return photonView == null || photonView.IsMine;
    }

    private bool IsLocalShooter()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null && HasLocalShooterAuthority(player);
    }

    private AmmoManager FindShooterAmmoManager()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        return player.GetComponent<AmmoManager>();
    }

    private HealthManager FindShooterHealthManager()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        return player.GetComponent<HealthManager>();
    }

    private void EnsurePickupPhysics()
    {
        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.isTrigger = true;

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = gameObject.AddComponent<Rigidbody>();

        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }
}
