using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ShooterOwnership : MonoBehaviourPun
{
    public void EnsureLocalOwnership()
    {
        Debug.Log("Check in EnsureLocalOwnership, before if");
        if (photonView.IsMine) return;
        Debug.Log("Check in EnsureLocalOwnership, after if");
        // photonView ownership을 takeover로 해둬야 함.
        photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
    }
}