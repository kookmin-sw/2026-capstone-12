using System;
using UnityEngine;

public class AmmoManager : MonoBehaviour
{
    public event Action<int, int> OnAmmoChanged;

    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo;
    [SerializeField] private int reserveAmmo = 120;

    private bool initialized;

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public int MaxAmmo => maxAmmo;

    public void ConfigureDefaults(int defaultMaxAmmo, int defaultCurrentAmmo, int defaultReserveAmmo)
    {
        if (initialized)
            return;

        maxAmmo = defaultMaxAmmo;
        reserveAmmo = defaultReserveAmmo;
        currentAmmo = defaultCurrentAmmo > 0 ? defaultCurrentAmmo : defaultMaxAmmo;
        initialized = true;
        NotifyAmmoChanged();
    }

    public bool CanReload()
    {
        return reserveAmmo > 0 && currentAmmo < maxAmmo;
    }

    public void UseAmmo()
    {
        if (currentAmmo <= 0)
            return;

        currentAmmo--;
        NotifyAmmoChanged();
    }

    public void Reload()
    {
        int ammoNeeded = maxAmmo - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);
        currentAmmo += ammoToReload;
        reserveAmmo -= ammoToReload;
        NotifyAmmoChanged();
    }

    // Support 아이템 예비 탄약 회복
    public void AddReserveAmmo(int amount)
    {
        if (amount <= 0)
            return;

        reserveAmmo += amount;
        NotifyAmmoChanged();
    }

    public void SetAmmoFromNetwork(int newCurrentAmmo, int newReserveAmmo, int newMaxAmmo)
    {
        maxAmmo = newMaxAmmo;
        currentAmmo = Mathf.Clamp(newCurrentAmmo, 0, maxAmmo);
        reserveAmmo = Mathf.Max(0, newReserveAmmo);
        initialized = true;
        NotifyAmmoChanged();
    }

    private void NotifyAmmoChanged()
    {
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }
}
