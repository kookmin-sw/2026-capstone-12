using Photon.Pun;
using UnityEngine;

public class RoleManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private ShooterOwnership shooterOwnership;

    [Header("Role Vision")]
    [SerializeField] private bool applyRoleVisionSettings = true; // 로컬 역할별 시야 체감 설정 적용 여부
    [SerializeField] private Color shooterAmbientSkyColor = new Color(0.018f, 0.02f, 0.028f, 1f); // Shooter 하늘 Ambient 색상
    [SerializeField] private Color shooterAmbientEquatorColor = new Color(0.012f, 0.013f, 0.018f, 1f); // Shooter 수평 Ambient 색상
    [SerializeField] private Color shooterAmbientGroundColor = new Color(0.006f, 0.006f, 0.008f, 1f); // Shooter 지면 Ambient 색상
    [SerializeField] private float shooterAmbientIntensity = 0.25f; // Shooter Ambient 세기
    [SerializeField] private Color shooterFogColor = new Color(0.01f, 0.012f, 0.018f, 1f); // Shooter 안개 색상
    [SerializeField] private float shooterFogDensity = 0.035f; // Shooter 안개 밀도

    [SerializeField] private Color supporterAmbientSkyColor = new Color(0.085f, 0.09f, 0.105f, 1f); // Supporter 하늘 Ambient 색상
    [SerializeField] private Color supporterAmbientEquatorColor = new Color(0.055f, 0.06f, 0.072f, 1f); // Supporter 수평 Ambient 색상
    [SerializeField] private Color supporterAmbientGroundColor = new Color(0.03f, 0.032f, 0.04f, 1f); // Supporter 지면 Ambient 색상
    [SerializeField] private float supporterAmbientIntensity = 0.65f; // Supporter Ambient 세기
    [SerializeField] private Color supporterFogColor = new Color(0.035f, 0.04f, 0.055f, 1f); // Supporter 안개 색상
    [SerializeField] private float supporterFogDensity = 0.012f; // Supporter 안개 밀도

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
        ApplyRoleVisionSettings(localRole);

        var groups = FindObjectsOfType<RoleManagedGroup>(true);
        for (int i = 0; i < groups.Length; i++)
            groups[i].Apply(localRole);
    }

    // 로컬 클라이언트 역할에 맞춰 씬 조명 체감만 분리
    private void ApplyRoleVisionSettings(RoleType localRole)
    {
        if (!applyRoleVisionSettings)
            return;

        RenderSettings.fog = true;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.skybox = null;

        if (localRole == RoleType.Supporter)
        {
            ApplyVisionColors(
                supporterAmbientSkyColor,
                supporterAmbientEquatorColor,
                supporterAmbientGroundColor,
                supporterAmbientIntensity,
                supporterFogColor,
                supporterFogDensity);
            return;
        }

        ApplyVisionColors(
            shooterAmbientSkyColor,
            shooterAmbientEquatorColor,
            shooterAmbientGroundColor,
            shooterAmbientIntensity,
            shooterFogColor,
            shooterFogDensity);
    }

    // RenderSettings 색상과 안개 밀도 일괄 적용
    private void ApplyVisionColors(
        Color ambientSky,
        Color ambientEquator,
        Color ambientGround,
        float ambientIntensity,
        Color fogColor,
        float fogDensity)
    {
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;
        RenderSettings.ambientIntensity = Mathf.Max(0f, ambientIntensity);
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = Mathf.Max(0f, fogDensity);
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
