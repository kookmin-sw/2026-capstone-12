using UnityEngine;
using Photon.Pun;

/// <summary>
/// 무기 컨트롤러
/// - 마우스 좌클릭 발사
/// - Raycast 히트스캔 방식
/// - 탄약 관리
/// - 재장전
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Weapon Stats")]
    [SerializeField] private float damage = 25f;              // 데미지
    [SerializeField] private float range = 100f;              // 사거리
    [SerializeField] private float fireRate = 0.1f;           // 연사 속도 (초)

    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 30;                // 탄창 크기
    [SerializeField] private int currentAmmo;                 // 현재 탄창
    [SerializeField] private int reserveAmmo = 120;           // 예비 탄약
    [SerializeField] private float reloadTime = 2f;           // 재장전 시간

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public int MaxAmmo => maxAmmo;

    // Components
    private Camera playerCamera;

    // State
    private float nextTimeToFire = 0f;
    private bool isReloading = false;

    void Start()
    {
        // 카메라 참조
        playerCamera = Camera.main;

        // 시작 시 탄창 가득 채우기
        currentAmmo = maxAmmo;
    }

    void Update()
    {
        // 재장전 중이면 다른 동작 안 함
        if (isReloading)
            return;

        // 재장전 입력 (R키)
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartReload();
            return;
        }

        // 자동 재장전 (탄창이 비었을 때)
        if (currentAmmo <= 0)
        {
            StartReload();
            return;
        }

        // 발사 입력 (마우스 좌클릭)
        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + fireRate;
            Fire();
        }
    }

    /// <summary>
    /// 총 발사
    /// </summary>
    void Fire()
    {

        // 탄약 소모
        currentAmmo--;

        // 화면 중앙에서 Raycast 발사
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range))
        {
            // 맞은 오브젝트 콘솔에 출력 (테스트용)
            Debug.Log("Hit: " + hit.collider.name);

            // 맞은 위치에 시각적 피드백 (Scene 뷰에서만 보임)
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red, 1f);

            // TODO: 나중에 데미지 처리 추가
            // 예: hit.collider.GetComponent<Enemy>()?.TakeDamage(damage);
            if (hit.collider.CompareTag("Enemy"))
            {
                PhotonView enemyPv = hit.collider.GetComponentInParent<PhotonView>();
                EnemyHealthNet.Instance.MasterApplyDamage(enemyPv.ViewID, damage);
            }
        }
        else
        {
            // 허공에 쏜 경우
            Debug.DrawRay(ray.origin, ray.direction * range, Color.yellow, 1f);
        }

        // 콘솔에 탄약 상태 출력
        Debug.Log($"Fire! Ammo: {currentAmmo}/{maxAmmo} | Reserve: {reserveAmmo}");
    }

    /// <summary>
    /// 재장전 시작
    /// </summary>
    void StartReload()
    {
        // 예비 탄약이 없으면 재장전 불가
        if (reserveAmmo <= 0)
        {
            Debug.Log("No reserve ammo!");
            return;
        }

        // 이미 탄창이 가득 차있으면 재장전 불필요
        if (currentAmmo == maxAmmo)
        {
            Debug.Log("Magazine is full!");
            return;
        }

        Debug.Log("Reloading...");
        isReloading = true;

        // 재장전 시간 후 완료
        Invoke(nameof(FinishReload), reloadTime);
    }

    /// <summary>
    /// 재장전 완료
    /// </summary>
    void FinishReload()
    {
        // 필요한 탄약 계산
        int ammoNeeded = maxAmmo - currentAmmo;

        // 예비 탄약에서 가져오기
        int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToReload;
        reserveAmmo -= ammoToReload;

        isReloading = false;

        Debug.Log($"Reload Complete! Ammo: {currentAmmo}/{maxAmmo} | Reserve: {reserveAmmo}");
    }
    /// <summary>
    /// 컨트롤러 활성화/비활성화 (사망용)
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
    }
}