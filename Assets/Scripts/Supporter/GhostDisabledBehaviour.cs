using UnityEngine;

public abstract class GhostDisabledBehaviour : MonoBehaviour
{
    protected bool IsPreview => GetComponentInParent<GhostMarker>() != null;

    protected virtual void Start()
    {
        if (IsPreview)
            enabled = false; // 고스트면 아예 비활성화
    }

    protected virtual void OnEnable()
    {
        if (IsPreview)
            enabled = false; // 고스트면 아예 비활성화
    }
}
