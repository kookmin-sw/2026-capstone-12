using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PurificationLightSource))]
public class LightPylonPurificationZone : MonoBehaviour
{
    [Header("Light Pylon")]
    [SerializeField] private float purificationRadius = 16f; // 정화 반경
    [SerializeField] private Light pylonLight; // 시각 조명

    private PurificationLightSource lightSource; // 정화 광원

    private void Awake()
    {
        lightSource = GetComponent<PurificationLightSource>();
        ApplyPurificationState();
    }

    private void OnEnable()
    {
        ApplyPurificationState();
    }

    private void OnValidate()
    {
        purificationRadius = Mathf.Max(0f, purificationRadius);

        if (pylonLight == null)
            pylonLight = GetComponentInChildren<Light>(true);

        ApplyPurificationState();
    }

    // 정화 영역과 조명 설정 적용
    private void ApplyPurificationState()
    {
        if (lightSource == null)
            lightSource = GetComponent<PurificationLightSource>();

        if (lightSource != null)
        {
            lightSource.sourceType = PurificationLightSourceType.LightPylon;
            lightSource.radius = purificationRadius;
            lightSource.strength = 1f;
            lightSource.preventsDarknessExposure = purificationRadius > 0f;
            lightSource.isPermanent = false;
        }

        if (pylonLight != null)
        {
            pylonLight.type = LightType.Point;
            pylonLight.range = purificationRadius;
            pylonLight.intensity = 2.4f;
            pylonLight.color = new Color(0.72f, 0.9f, 1f, 1f);
            pylonLight.shadows = LightShadows.None;
            pylonLight.enabled = purificationRadius > 0f;
        }
    }
}
