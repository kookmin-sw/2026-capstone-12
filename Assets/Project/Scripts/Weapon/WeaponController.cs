using UnityEngine;
using Photon.Pun;
using UnityEngine.Serialization;

/// <summary>
/// Shooter 무기 입력, 발사, 재장전, 조준, 탄약 동기화 처리
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Weapon Stats")]
    [SerializeField] private float damage = 10f; // 발사 1회당 적용할 데미지
    private float bonusDamage = 0f;
    [FormerlySerializedAs("maxShootDistance")]
    [SerializeField] private float minEffectiveShootDistance = 25f; // 최소 유효 사거리
    [SerializeField] private float raycastDistance = 1000f; // Raycast 검사 거리
    [SerializeField] private float unpurifiedMinRangeDamageMultiplier = 0.1f; // 비가시 최소 사거리 피해 배율
    [SerializeField] private float fireRate = 0.1f; // 연사 간격

    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 30; // 탄창 최대 탄약 수
    [SerializeField] private int currentAmmo; // 시작 시 현재 탄약 수
    [SerializeField] private int reserveAmmo = 120; // 시작 시 예비 탄약 수
    [SerializeField] private float reloadTime = 2f; // 재장전 완료까지 걸리는 시간

    public int CurrentAmmo => ammoManager != null ? ammoManager.CurrentAmmo : currentAmmo;
    public int ReserveAmmo => ammoManager != null ? ammoManager.ReserveAmmo : reserveAmmo;
    public int MaxAmmo => ammoManager != null ? ammoManager.MaxAmmo : maxAmmo;

    [Header("FPS Arms")]
    [SerializeField] private Animator handsAnimator; // FPS 팔 애니메이션 제어 대상

    [Header("Muzzle Flash")]
    [SerializeField] private GameObject[] muzzleFlashPrefabs; // 발사 시 랜덤으로 보여줄 머즐 플래시 프리팹 목록
    [SerializeField] private Transform muzzlePoint; // 머즐 플래시 생성 기준 위치
    [SerializeField] private float muzzleForwardOffset = 0.3f; // 총구 앞쪽으로 밀어낼 머즐 플래시 거리

    [Header("Aiming")]
    [SerializeField] private float aimFOV = 40f; // 조준 시 목표 카메라 FOV
    private float normalFOV = 60f; // 비조준 상태 카메라 FOV

    private Camera playerCamera; // 로컬 Shooter가 사용하는 카메라
    private ShooterWeaponNet shooterWeaponNet; // 무기 피격 요청 네트워크 컴포넌트
    private ShooterAmmoNet shooterAmmoNet; // 탄약 UI 동기화 네트워크 컴포넌트
    private ShooterVisibilityFogController visibilityFogController; // 시야 판정 컨트롤러
    private PhotonView photonView; // 로컬 권한 판정용 PhotonView
    private AmmoManager ammoManager; // 탄약 상태 관리 컴포넌트

    private float nextTimeToFire = 0f; // 다음 발사 가능 시간
    private bool isReloading = false; // 재장전 입력 차단 상태
    private bool isAiming = false; // 현재 조준 상태

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        ammoManager = GetComponent<AmmoManager>();
        if (ammoManager == null)
            ammoManager = gameObject.AddComponent<AmmoManager>();

        ammoManager.ConfigureDefaults(maxAmmo, currentAmmo, reserveAmmo);
    }

    /// <summary>
    /// 씬 참조 자동 연결과 초기 탄약 상태 전파
    /// </summary>
    private void Start()
    {
        // Camera.main 대신 자식 카메라 직접 참조 (Supporter Camera 태그 충돌 방지)
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            normalFOV = playerCamera.fieldOfView;
            visibilityFogController = playerCamera.GetComponent<ShooterVisibilityFogController>();
        }

        shooterWeaponNet = GetComponent<ShooterWeaponNet>();
        shooterAmmoNet = ShooterAmmoNet.Instance;

        AutoWireReferences();
        BroadcastAmmoState();
    }

    /// <summary>
    /// Inspector 미할당 참조를 플레이어 하위 오브젝트에서 자동 연결
    /// </summary>
    private void AutoWireReferences()
    {
        if (handsAnimator == null && playerCamera != null)
        {
            Transform fpsArms = playerCamera.transform.Find("FPS_Arms"); // FPS 팔 Animator 탐색 기준
            if (fpsArms != null)
                handsAnimator = fpsArms.GetComponent<Animator>();
        }

        if (muzzlePoint == null && playerCamera != null)
        {
            Transform foundMuzzlePoint = playerCamera.transform.Find("FPS_Arms/MuzzlePoint"); // FPS 팔 하위 총구 위치
            if (foundMuzzlePoint != null)
                muzzlePoint = foundMuzzlePoint;
        }

    }

    private void Update()
    {
        if (!HasLocalAuthority())
            return;

        if (InputLock.IsLocked)
            return;

        if (isReloading)
            return;

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

    private void OnValidate()
    {
        minEffectiveShootDistance = Mathf.Max(0f, minEffectiveShootDistance);
        raycastDistance = Mathf.Max(minEffectiveShootDistance, raycastDistance);
        unpurifiedMinRangeDamageMultiplier = Mathf.Clamp01(unpurifiedMinRangeDamageMultiplier);
    }

    /// <summary>
    /// 우클릭 조준 상태와 카메라 FOV 보간 처리
    /// </summary>
    private void HandleAiming()
    {
        isAiming = Input.GetButton("Fire2");

        if (playerCamera == null)
            return;

        float targetFOV = isAiming ? aimFOV : normalFOV; // 조준 상태별 목표 FOV
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * 10f);
    }

    /// <summary>
    /// 이동과 조준 상태를 FPS 팔 Animator 파라미터에 반영
    /// </summary>
    private void DriveHandsAnimator()
    {
        if (handsAnimator == null)
            return;

        CharacterController characterController = GetComponent<CharacterController>(); // 이동 속도 조회 대상
        float horizontalSpeed = characterController != null
            ? new Vector3(characterController.velocity.x, 0f, characterController.velocity.z).magnitude
            : 0f;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift); // 달리기 애니메이션 판정 상태
        int maxSpeed = isSprinting ? 5 : 3; // Animator maxSpeed 파라미터 값

        handsAnimator.SetFloat("walkSpeed", horizontalSpeed);
        handsAnimator.SetInteger("maxSpeed", maxSpeed);
        handsAnimator.SetBool("aiming", isAiming);
    }

    /// <summary>
    /// 탄약 소비, 이펙트, 사운드, Raycast 피격 요청 처리
    /// </summary>
    private void Fire()
    {
        if (playerCamera == null || ammoManager == null || shooterWeaponNet == null)
            return;

        ammoManager.UseAmmo();
        BroadcastAmmoState();

        SpawnMuzzleFlash();
        PlayWeaponSound(GameSoundType.ShooterFire);

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)); // 화면 중앙 조준 Ray
        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
            return;

        if (TryRequestEnemyHit(hit))
            return;

        TryRequestStructureHit(hit);
    }

    /// <summary>
    /// Enemy 피격 여부 확인과 MasterClient 데미지 요청
    /// </summary>
    private bool TryRequestEnemyHit(RaycastHit hit)
    {
        if (!hit.collider.CompareTag("Enemy"))
            return false;

        PhotonView enemyPhotonView = hit.collider.GetComponentInParent<PhotonView>(); // 피격 Enemy 네트워크 식별자
        if (enemyPhotonView == null)
            return true;

        if (!TryGetDamageAtHit(hit, out float finalDamage))
            return true;

        shooterWeaponNet.RequestHitEnemy(enemyPhotonView.ViewID, finalDamage);
        return true;
    }

    /// <summary>
    /// Core와 Source 구조물 피격 여부 확인과 MasterClient 데미지 요청
    /// </summary>
    private void TryRequestStructureHit(RaycastHit hit)
    {
        PhotonView structurePhotonView = GetHitStructurePhotonView(hit.collider); // 피격 구조물 네트워크 식별자
        if (structurePhotonView == null)
            return;

        if (!TryGetDamageAtHit(hit, out float finalDamage))
            return;

        shooterWeaponNet.RequestHitStructure(structurePhotonView.ViewID, finalDamage);
    }

    /// <summary>
    /// 피격 거리와 현재 가시 영역 기준 최종 피해 계산
    /// </summary>
    private bool TryGetDamageAtHit(RaycastHit hit, out float finalDamage)
    {
        finalDamage = damage + bonusDamage;
        float distance = Vector3.Distance(GetShootOriginPosition(), hit.point); // 피격 거리
        bool isVisibleToShooter = IsVisibleToShooter(hit.point); // 현재 시야 포함 여부

        if (distance <= minEffectiveShootDistance)
        {
            if (!isVisibleToShooter)
                finalDamage *= unpurifiedMinRangeDamageMultiplier;

            return finalDamage > 0f;
        }

        return isVisibleToShooter && finalDamage > 0f;
    }

    /// <summary>
    /// 현재 Shooter가 피격 위치를 볼 수 있는지 확인
    /// </summary>
    private bool IsVisibleToShooter(Vector3 worldPosition)
    {
        if (visibilityFogController == null && playerCamera != null)
            visibilityFogController = playerCamera.GetComponent<ShooterVisibilityFogController>();

        if (visibilityFogController == null)
            return true;

        return visibilityFogController.ContainsVisiblePosition(worldPosition);
    }

    /// <summary>
    /// 사격 거리 계산 기준 위치 반환
    /// </summary>
    private Vector3 GetShootOriginPosition()
    {
        if (playerCamera != null)
            return playerCamera.transform.position;

        return transform.position;
    }

    /// <summary>
    /// 피격 Collider 부모에서 공격 가능한 구조물 PhotonView 조회
    /// </summary>
    private PhotonView GetHitStructurePhotonView(Collider hitCollider)
    {
        SpawnCore spawnCore = hitCollider.GetComponentInParent<SpawnCore>(); // Core 피격 대상
        if (spawnCore != null)
            return spawnCore.GetComponent<PhotonView>();

        EnemyNest enemyNest = hitCollider.GetComponentInParent<EnemyNest>(); // Nest 피격 대상
        if (enemyNest != null)
            return enemyNest.GetComponent<PhotonView>();

        return null;
    }

    /// <summary>
    /// 총구 위치에 짧은 머즐 플래시 이펙트 생성
    /// </summary>
    private void SpawnMuzzleFlash()
    {
        if (muzzleFlashPrefabs == null || muzzleFlashPrefabs.Length == 0 || muzzlePoint == null)
            return;

        int prefabIndex = Random.Range(0, muzzleFlashPrefabs.Length); // 머즐 플래시 랜덤 인덱스
        if (muzzleFlashPrefabs[prefabIndex] == null)
            return;

        Vector3 spawnPosition = muzzlePoint.position + muzzlePoint.forward * muzzleForwardOffset; // 총구 앞 생성 위치
        GameObject flash = Instantiate(
            muzzleFlashPrefabs[prefabIndex],
            spawnPosition,
            muzzlePoint.rotation * Quaternion.Euler(0f, 0f, 90f)
        );

        flash.transform.SetParent(muzzlePoint);
        Destroy(flash, 0.05f);
    }

    /// <summary>
    /// 재장전 가능 상태에서 재장전 시작
    /// </summary>
    private void StartReload()
    {
        if (ammoManager == null || !ammoManager.CanReload())
            return;

        isReloading = true;

        handsAnimator?.SetBool("reloading", true);
        PlayWeaponSound(GameSoundType.ShooterReload);

        Invoke(nameof(FinishReload), reloadTime);
    }

    /// <summary>
    /// 무기 사운드 위치 기반 재생 요청
    /// </summary>
    private void PlayWeaponSound(GameSoundType soundType)
    {
        Vector3 soundPosition = muzzlePoint != null ? muzzlePoint.position : transform.position; // 무기 사운드 기준 위치
        if (SoundNet.Instance != null)
        {
            SoundNet.Instance.RequestPlayAt(soundType, soundPosition);
            return;
        }

        GameEventSoundPlayer.Instance?.PlayAt(soundType, soundPosition);
    }

    /// <summary>
    /// 재장전 완료 후 탄약 상태와 애니메이션 갱신
    /// </summary>
    private void FinishReload()
    {
        ammoManager.Reload();
        BroadcastAmmoState();

        isReloading = false;
        handsAnimator?.SetBool("reloading", false);
    }

    /// <summary>
    /// 네트워크에서 받은 탄약 상태를 로컬 AmmoManager에 반영
    /// </summary>
    public void SetAmmoFromNetwork(int newCurrentAmmo, int newReserveAmmo, int newMaxAmmo)
    {
        if (ammoManager == null)
            return;

        ammoManager.SetAmmoFromNetwork(newCurrentAmmo, newReserveAmmo, newMaxAmmo);
    }

    /// <summary>
    /// 로컬 권한 Shooter의 탄약 상태를 UI 동기화 대상으로 전파
    /// </summary>
    private void BroadcastAmmoState()
    {
        if (!HasLocalAuthority())
            return;

        if (ammoManager == null)
            return;

        shooterAmmoNet?.SyncAmmoFromShooter(CurrentAmmo, ReserveAmmo, MaxAmmo);
    }

    /// <summary>
    /// Photon 연결 상태 기준 로컬 입력 처리 권한 판정
    /// </summary>
    private bool HasLocalAuthority()
    {
        if (!PhotonNetwork.IsConnected)
            return true;

        return photonView == null || photonView.IsMine;
    }

    public void SetBonusDamage(float bonus)
    {
        bonusDamage = bonus;
    }

    /// <summary>
    /// 외부 UI에서 무기 입력 활성 상태 제어
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
    }
}
