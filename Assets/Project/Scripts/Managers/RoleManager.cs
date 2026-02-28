using Photon.Pun;
using UnityEngine;

public class RoleManager : MonoBehaviourPunCallbacks
{
    private bool applied;
    private void OnEnable()
    {
        // 씬 로드될 때마다 초기화
        applied = false;
    }

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

        // CustomProperties에서 역할 읽기 ← 수정!
        RoleType localRole = RoleType.Shooter; // 기본값

        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
        {
            string roleStr = (string)PhotonNetwork.LocalPlayer.CustomProperties["Role"];

            if (roleStr == "Shooter")
                localRole = RoleType.Shooter;
            else if (roleStr == "Supporter")
                localRole = RoleType.Supporter;

            Debug.Log($"Role from CustomProperties: {roleStr} → {localRole}");
        }
        else
        {
            Debug.LogWarning("No role in CustomProperties! Using default: Shooter");
        }

        var groups = FindObjectsOfType<RoleManagedGroup>(true);
        for (int i = 0; i < groups.Length; i++)
            groups[i].Apply(localRole);
    }
}