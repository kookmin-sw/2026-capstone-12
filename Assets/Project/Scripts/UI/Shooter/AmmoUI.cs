using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 탄약 UI 표시
/// WeaponController에서 탄약 정보를 받아와서 텍스트로 표시
/// </summary>
public class AmmoUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Text ammoText;
    [SerializeField] private WeaponController weaponController;

    private AmmoManager ammoManager;

    void Start()
    {
        ResolveAmmoManager();

        if (ammoManager != null)
            ammoManager.OnAmmoChanged += HandleAmmoChanged;

        UpdateAmmoDisplay();
    }

    void OnDestroy()
    {
        if (ammoManager != null)
            ammoManager.OnAmmoChanged -= HandleAmmoChanged;
    }

    /// <summary>
    /// 탄약 표시 업데이트
    /// </summary>
    void UpdateAmmoDisplay()
    {
        if (ammoManager == null || ammoText == null)
            return;

        // WeaponController에서 탄약 정보 가져오기
        int current = ammoManager.CurrentAmmo;
        int reserve = ammoManager.ReserveAmmo;

        // 텍스트 업데이트
        ammoText.text = $"{current} / {reserve}";
    }

    void ResolveAmmoManager()
    {
        if (weaponController == null)
            return;

        ammoManager = weaponController.GetComponent<AmmoManager>();
    }

    void HandleAmmoChanged(int current, int reserve)
    {
        if (ammoText == null)
            return;

        ammoText.text = $"{current} / {reserve}";
    }
}
