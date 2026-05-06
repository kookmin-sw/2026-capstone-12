using Photon.Pun;
using UnityEngine;

public class RoleManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private ShooterOwnership shooterOwnership;
    private bool applied;
    
    public override void OnEnable()
    {
        base.OnEnable();
        // 씬 로드될 때마다 초기화
        applied = false;
    }

    public void Start()
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

        // CustomProperties에서 역할 읽기 ← 수정!
        RoleType localRole;

        if (TryGetLocalRole(out localRole))
        {
            if (localRole == RoleType.Shooter)
                shooterOwnership.EnsureLocalOwnership();

            Debug.Log($"Role from CustomProperties: {localRole}");
        }
        else
        {
            if (ShouldWaitForNetworkRole())
            {
                Debug.LogWarning("No role in CustomProperties! Waiting for role assignment.");
                return;
            }

            Debug.LogWarning("No role in CustomProperties! Using default: Shooter for offline scene test.");
            localRole = RoleType.Shooter;
        }

        applied = true;

        var groups = FindObjectsOfType<RoleManagedGroup>(true);
        for (int i = 0; i < groups.Length; i++)
            groups[i].Apply(localRole);
    }

    // Photon Role 문자열을 로컬 역할 enum으로 변환
    private bool TryGetLocalRole(out RoleType localRole)
    {
        localRole = RoleType.Shooter;

        if (PhotonNetwork.LocalPlayer == null)
            return false;

        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Role", out object roleValue))
            return false;

        string roleStr = roleValue as string;
        if (roleStr == "Shooter")
        {
            localRole = RoleType.Shooter;
            return true;
        }

        if (roleStr == "Supporter")
        {
            localRole = RoleType.Supporter;
            return true;
        }

        return false;
    }

    // QuickConnect 직접 실행에서는 OnJoinedRoom 이후 Role 주입까지 대기
    private bool ShouldWaitForNetworkRole()
    {
        if (PhotonNetwork.IsConnected || PhotonNetwork.InRoom)
            return true;

        QuickConnectPun quickConnect = FindObjectOfType<QuickConnectPun>();
        return quickConnect != null && quickConnect.UsesEditorDirectPlaySettings;
    }
}
