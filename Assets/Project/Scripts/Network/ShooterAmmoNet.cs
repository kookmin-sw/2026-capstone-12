using Photon.Pun;
using UnityEngine;

public class ShooterAmmoNet : MonoBehaviourPun
{
    public static ShooterAmmoNet Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SyncAmmoFromShooter(int currentAmmo, int reserveAmmo, int maxAmmo)
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom)
            return;

        photonView.RPC(nameof(RpcSetShooterAmmo), RpcTarget.All, currentAmmo, reserveAmmo, maxAmmo);
    }

    [PunRPC]
    private void RpcSetShooterAmmo(int currentAmmo, int reserveAmmo, int maxAmmo)
    {
        AmmoManager ammoManager = FindShooterAmmoManager();
        if (ammoManager == null)
            return;

        ammoManager.SetAmmoFromNetwork(currentAmmo, reserveAmmo, maxAmmo);
    }

    private AmmoManager FindShooterAmmoManager()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return null;

        return playerObj.GetComponent<AmmoManager>();
    }
}
