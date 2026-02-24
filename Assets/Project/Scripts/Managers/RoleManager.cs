using Photon.Pun;
using UnityEngine;

public class RoleManager : MonoBehaviourPunCallbacks
{
    private bool applied;

    public override void OnJoinedRoom()
    {
        ApplyOnce();
    }

    private void Update()
    {
        if (!applied && PhotonNetwork.InRoom)
            ApplyOnce();
    }

    private void ApplyOnce()
    {
        if (applied) return;
        applied = true;

        // 마스터=슈터, 비마스터=서포터 (임시)
        RoleType localRole = PhotonNetwork.IsMasterClient ? RoleType.Shooter : RoleType.Supporter;

        var groups = FindObjectsOfType<RoleManagedGroup>(true);
        for (int i = 0; i < groups.Length; i++)
            groups[i].Apply(localRole);
    }
}