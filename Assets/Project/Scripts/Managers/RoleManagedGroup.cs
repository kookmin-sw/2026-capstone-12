using Photon.Pun;
using UnityEngine;

public enum RoleType { Shooter, Supporter }
public enum EnablePolicy
{
    // 해당 역할(로컬)에서만 켬. 다른 쪽은 꺼짐(존재 필요 없음)
    RoleOnlyObject,

    // 오브젝트는 항상 켜두되, 로컬에서만 특정 컴포넌트/카메라가 켜짐(원격에도 보여야 할 때)
    AlwaysObject_LocalComponentsOnly
}

public class RoleManagedGroup : MonoBehaviourPun
{
    [Header("Role")]
    public RoleType role;
    public EnablePolicy policy = EnablePolicy.RoleOnlyObject;

    [Header("Local-only components (enabled only when local has this role)")]
    [SerializeField] private Behaviour[] localOnlyBehaviours;
    [SerializeField] private Camera[] localOnlyCameras;

    public void Apply(RoleType localRole)
    {
        bool isLocalRole = (localRole == role);

        if (policy == EnablePolicy.RoleOnlyObject)
        {
            gameObject.SetActive(isLocalRole);
            return;
        }

        // AlwaysObject_LocalComponentsOnly
        // 오브젝트는 항상 켜두고, 입력/카메라만 로컬 역할일 때 켬
        SetBehaviours(localOnlyBehaviours, isLocalRole);
        SetCameras(localOnlyCameras, isLocalRole);
    }

    private static void SetBehaviours(Behaviour[] arr, bool enabled)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] != null) arr[i].enabled = enabled;
    }

    private static void SetCameras(Camera[] cams, bool enabled)
    {
        if (cams == null) return;
        for (int i = 0; i < cams.Length; i++)
        {
            if (cams[i] == null) continue;
            cams[i].enabled = enabled;

            var al = cams[i].GetComponent<AudioListener>();
            if (al != null) al.enabled = enabled;
        }
    }
}