using UnityEngine;
using Photon.Pun;

/// <summary>
/// 무기 컨트롤러
/// - 마우스 좌클릭 발사 (레이캐스트 히트스캔)
/// - Easy FPS 팔 애니메이터 구동
/// - 머즐 플래시 이펙트
/// - 발사/재장전 사운드
/// - 조준(우클릭) FOV 변경
/// - 탄약 관리 및 재장전
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Weapon Stats")]
    [SerializeField] private float damage = 25f;
    private float bonusDamage = 0f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 0.1f;

    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo;
    [SerializeField] private int reserveAmmo = 120;
    [SerializeField] private float reloadTime = 2f;

    public int CurrentAmmo => ammoManager != null ? ammoManager.CurrentAmmo : currentAmmo;
    public int ReserveAmmo => ammoManager != null ? ammoManager.ReserveAmmo : reserveAmmo;
    public int MaxAmmo => ammoManager != null ? ammoManager.MaxAmmo : maxAmmo;

    [Header("FPS Arms")]
    [Tooltip("FPS_Character_prefab 오브젝트의 Animator")]
    [SerializeField] private Animator handsAnimator;

    [Header("Muzzle Flash")]
    [Tooltip("Easy FPS의 muzzelFlash 01~05 프리팹 5개 등록")]
    [SerializeField] private GameObject[] muzzleFlashPrefabs;
    [Tooltip("총구 위치 (FPS_Character 하위 총구 끝 오브젝트)")]
    [SerializeField] private Transform muzzlePoint;
    [Tooltip("총구 방향으로 머즐 플래시 오프셋 (앞으로 이동)")]
    [SerializeField] private float muzzleForwardOffset = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioSource shootSoundSource;
    [SerializeField] private AudioSource reloadSoundSource;

    [Header("Aiming")]
    [SerializeField] private float aimFOV = 40f;
    private float normalFOV = 60f;

    // Components
    private Camera playerCamera;
    private ShooterWeaponNet shooterWeaponNet;
    private ShooterAmmoNet shooterAmmoNet;
    private PhotonView photonView;
    private AmmoManager ammoManager;

    // State
    private float nextTimeToFire = 0f;
    private bool isReloading = false;
    private bool isAiming = false;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        ammoManager = GetComponent<AmmoManager>();
        if (ammoManager == null)
            ammoManager = gameObject.AddComponent<AmmoManager>();

        ammoManager.ConfigureDefaults(maxAmmo, currentAmmo, reserveAmmo);
    }

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera != null)
            normalFOV = playerCamera.fieldOfView;

        shooterWeaponNet = GetComponent<ShooterWeaponNet>();
        shooterAmmoNet = ShooterAmmoNet.Instance;

        AutoWireReferences();
        BroadcastAmmoState();
    }

    /// <summary>
    /// Inspector에 할당되지 않은 참조를 씬에서 자동으로 찾아 연결
    /// </summary>
    void AutoWireReferences()
    {
        // FPS Arms Animator - Main Camera 자식 "FPS_Arms"에서 찾기
        if (handsAnimator == null)
        {
            if (playerCamera != null)
            {
                Transform fpsArms = playerCamera.transform.Find("FPS_Arms");
                if (fpsArms != null)
                    handsAnimator = fpsArms.GetComponent<Animator>();
            }
        }

        // MuzzlePoint - FPS_Arms 하위에서 찾기
        if (muzzlePoint == null)
        {
            if (playerCamera != null)
            {
                Transform mp = playerCamera.transform.Find("FPS_Arms/MuzzlePoint");
                if (mp != null)
                    muzzlePoint = mp;
            }
        }

        // AudioSources - Player에 있는 AudioSource 순서대로 shoot / reload
        if (shootSoundSource == null || reloadSoundSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length >= 1 && shootSoundSource == null)
                shootSoundSource = sources[0];
            if (sources.Length >= 2 && reloadSoundSource == null)
                reloadSoundSource = sources[1];
        }

        // muzzleFlashPrefabs는 Inspector에서 직접 할당 필요
        // (Assets/Easy FPS/MuzzelFlash/muzzelFlash 01~05.prefab 5개 드래그)
    }

    void Update()
    {
        if (!HasLocalAuthority()) return;
        if (isReloading) return;

        HandleAiming();
        DriveHandsAnimator();

        if (Input.GetKeyDown(KeyCode.R))
        {
            StartReload();
            return;
        }

        if (CurrentAmmo <= 0)
        {
            StartReload();
            return;
        }

        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + fireRate;
            Fire();
        }
    }

    /// <summary>
    /// 우클릭 조준 처리 (FOV 변경 + 애니메이션)
    /// </summary>
    void HandleAiming()
    {
        isAiming = Input.GetButton("Fire2");

        if (playerCamera != null)
        {
            float targetFOV = isAiming ? aimFOV : normalFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * 10f);
        }
    }

    /// <summary>
    /// FPS 팔 애니메이터 파라미터 구동
    /// GunAnimator.controller 파라미터: walkSpeed(float), maxSpeed(int), aiming(bool), reloading(bool)
    /// </summary>
    void DriveHandsAnimator()
    {
        if (handsAnimator == null) return;

        // 이동 속도 계산 (CharacterController velocity 기준)
        var cc = GetComponent<CharacterController>();
        float horizontalSpeed = cc != null
            ? new Vector3(cc.velocity.x, 0, cc.velocity.z).magnitude
            : 0f;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        int maxSpeed = isSprinting ? 5 : 3;

        handsAnimator.SetFloat("walkSpeed", horizontalSpeed);
        handsAnimator.SetInteger("maxSpeed", maxSpeed);
        handsAnimator.SetBool("aiming", isAiming);
    }

    /// <summary>
    /// 총 발사: 레이캐스트 + 머즐 플래시 + 사운드 + 애니메이션
    /// </summary>
    void Fire()
    {
        ammoManager.UseAmmo();
        BroadcastAmmoState();

        // 머즐 플래시
        SpawnMuzzleFlash();

        // 발사 사운드
        shootSoundSource?.Play();

        // 발사 애니메이션 (walkSpeed 파라미터로 자동 처리됨, 별도 트리거 없음)

        // 레이캐스트 히트스캔
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                PhotonView enemyPv = hit.collider.GetComponentInParent<PhotonView>();
                if (enemyPv != null)
                    shooterWeaponNet.RequestHitEnemy(enemyPv.ViewID, damage + bonusDamage);
                return;
            }

            SpawnCore spawnCore = hit.collider.GetComponentInParent<SpawnCore>();
            if (spawnCore == null)
                return;

            PhotonView structurePv = spawnCore.GetComponent<PhotonView>();
            if (structurePv != null)
                shooterWeaponNet.RequestHitStructure(structurePv.ViewID, damage + bonusDamage);
        }
    }

    /// <summary>
    /// 머즐 플래시 랜덤 스폰
    /// </summary>
    void SpawnMuzzleFlash()
    {
        if (muzzleFlashPrefabs == null || muzzleFlashPrefabs.Length == 0 || muzzlePoint == null)
            return;

        int idx = Random.Range(0, muzzleFlashPrefabs.Length);
        if (muzzleFlashPrefabs[idx] == null) return;

        Vector3 spawnPos = muzzlePoint.position + muzzlePoint.forward * muzzleForwardOffset;
        GameObject flash = Instantiate(
            muzzleFlashPrefabs[idx],
            spawnPos,
            muzzlePoint.rotation * Quaternion.Euler(0, 0, 90)
        );
        flash.transform.SetParent(muzzlePoint);
        Destroy(flash, 0.05f);
    }

    void StartReload()
    {
        if (ammoManager == null || !ammoManager.CanReload()) return;

        isReloading = true;

        handsAnimator?.SetBool("reloading", true);
        reloadSoundSource?.Play();

        Invoke(nameof(FinishReload), reloadTime);
    }

    void FinishReload()
    {
        ammoManager.Reload();
        BroadcastAmmoState();

        isReloading = false;
        handsAnimator?.SetBool("reloading", false);
    }

    public void SetAmmoFromNetwork(int newCurrentAmmo, int newReserveAmmo, int newMaxAmmo)
    {
        if (ammoManager == null)
            return;

        ammoManager.SetAmmoFromNetwork(newCurrentAmmo, newReserveAmmo, newMaxAmmo);
    }

    void BroadcastAmmoState()
    {
        if (!HasLocalAuthority()) return;
        if (ammoManager == null) return;

        shooterAmmoNet?.SyncAmmoFromShooter(CurrentAmmo, ReserveAmmo, MaxAmmo);
    }

    bool HasLocalAuthority()
    {
        if (!PhotonNetwork.IsConnected)
            return true;

        return photonView == null || photonView.IsMine;
    }

    public void SetBonusDamage(float bonus)
    {
        bonusDamage = bonus;
    }

    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
    }
}
