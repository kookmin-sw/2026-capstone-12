using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class ShooterVisibilityFogController : MonoBehaviour
{
    private static readonly int EnabledId = Shader.PropertyToID("_ShooterVisibilityFogEnabled"); // 안개 활성 ID
    private static readonly int CenterId = Shader.PropertyToID("_ShooterVisibilityCenter"); // 시야 중심 ID
    private static readonly int RadiusId = Shader.PropertyToID("_ShooterVisibilityRadius"); // 시야 반경 ID
    private static readonly int SoftEdgeId = Shader.PropertyToID("_ShooterVisibilitySoftEdge"); // 경계 완화 ID
    private static readonly int FogColorId = Shader.PropertyToID("_ShooterVisibilityFogColor"); // 안개 색상 ID
    private static readonly int FogOpacityId = Shader.PropertyToID("_ShooterVisibilityFogOpacity"); // 안개 불투명도 ID
    private static readonly List<ShooterVisibilityFogController> ActiveControllers = new List<ShooterVisibilityFogController>(); // 활성 컨트롤러 목록

    [Header("Visibility")]
    [SerializeField] private Transform visibilityOrigin; // 시야 기준
    [SerializeField] private bool useCurrentPurificationZone = true; // 현재 정화 구역 사용 여부
    [SerializeField] private float fallbackRadius = 8f; // 정화 밖 기본 반경
    [SerializeField] private float connectedZoneExtraRadius = 3f; // 연결 구역 추가 반경
    [SerializeField] private float softEdge = 7f; // 경계 완화 폭

    [Header("Fog")]
    [SerializeField] private Color fogColor = new Color(0.005f, 0.01f, 0.012f, 1f); // 오염 안개 색상
    [SerializeField] private float fogOpacity = 0.96f; // 안개 불투명도

    private Camera targetCamera; // 적용 카메라

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        if (visibilityOrigin == null)
            visibilityOrigin = transform;
    }

    private void OnEnable()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        targetCamera.depthTextureMode |= DepthTextureMode.Depth;
        if (!ActiveControllers.Contains(this))
            ActiveControllers.Add(this);

        if (ActiveControllers.Count == 1)
            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
    }

    private void OnDisable()
    {
        ActiveControllers.Remove(this);
        if (ActiveControllers.Count == 0)
        {
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
            Shader.SetGlobalFloat(EnabledId, 0f);
        }
    }

    private void OnValidate()
    {
        fallbackRadius = Mathf.Max(0f, fallbackRadius);
        connectedZoneExtraRadius = Mathf.Max(0f, connectedZoneExtraRadius);
        softEdge = Mathf.Max(0.01f, softEdge);
        fogOpacity = Mathf.Clamp01(fogOpacity);
    }

    // 카메라별 안개 상태 초기화
    private static void HandleBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
    {
        Shader.SetGlobalFloat(EnabledId, 0f);

        for (int i = 0; i < ActiveControllers.Count; i++)
        {
            ShooterVisibilityFogController controller = ActiveControllers[i]; // 후보 컨트롤러
            if (controller == null || !controller.isActiveAndEnabled || renderingCamera != controller.targetCamera)
                continue;

            controller.ApplyShaderState();
            return;
        }
    }

    // 현재 카메라 시야 안개 값 적용
    private void ApplyShaderState()
    {
        Vector3 origin = visibilityOrigin != null ? visibilityOrigin.position : transform.position; // 현재 기준 위치
        Vector3 center = origin; // 시야 중심
        float radius = fallbackRadius; // 시야 반경

        if (useCurrentPurificationZone && TryGetCurrentPurificationZone(origin, out PurificationLightSource zone))
        {
            center = zone.transform.position;
            radius = zone.radius + connectedZoneExtraRadius;
        }

        Shader.SetGlobalFloat(EnabledId, 1f);
        Shader.SetGlobalVector(CenterId, center);
        Shader.SetGlobalFloat(RadiusId, radius);
        Shader.SetGlobalFloat(SoftEdgeId, softEdge);
        Shader.SetGlobalColor(FogColorId, fogColor);
        Shader.SetGlobalFloat(FogOpacityId, fogOpacity);
    }

    // 현재 위치가 포함된 정화 구역 조회
    private bool TryGetCurrentPurificationZone(Vector3 origin, out PurificationLightSource currentZone)
    {
        currentZone = null;

        PurificationZoneRegistry registry = PurificationZoneRegistry.Instance;
        if (registry == null)
            return false;

        float bestRadius = -1f; // 선택 반경
        foreach (PurificationLightSource source in registry.Sources)
        {
            if (source == null || !source.preventsDarknessExposure || !source.Contains(origin))
                continue;

            if (source.radius <= bestRadius)
                continue;

            currentZone = source;
            bestRadius = source.radius;
        }

        return currentZone != null;
    }
}
