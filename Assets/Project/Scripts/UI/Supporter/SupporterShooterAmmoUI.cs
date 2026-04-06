using TMPro;
using UnityEngine;

/// <summary>
/// 서포터 화면에서 슈터의 탄약 UI를 표시한다.
/// </summary>
public class SupporterShooterAmmoUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private WeaponController shooterWeaponController;

    private AmmoManager ammoManager;
    private bool subscribed;

    private void Start()
    {
        ResolveAmmoManager();

        if (ammoManager == null)
        {
            Debug.LogWarning($"{nameof(SupporterShooterAmmoUI)} is missing references on {name}.");
            enabled = false;
            return;
        }

        ammoManager.OnAmmoChanged += HandleAmmoChanged;
        subscribed = true;
        UpdateAmmoDisplay();
    }

    private void OnDestroy()
    {
        if (subscribed && ammoManager != null)
            ammoManager.OnAmmoChanged -= HandleAmmoChanged;
    }

    private void ResolveAmmoManager()
    {
        if (shooterWeaponController == null)
            return;

        ammoManager = shooterWeaponController.GetComponent<AmmoManager>();
    }

    private void HandleAmmoChanged(int currentAmmo, int reserveAmmo)
    {
        if (ammoText != null)
            ammoText.text = $"{currentAmmo} / {reserveAmmo}";
    }

    private void UpdateAmmoDisplay()
    {
        if (ammoManager == null)
            return;

        HandleAmmoChanged(ammoManager.CurrentAmmo, ammoManager.ReserveAmmo);
    }
}
