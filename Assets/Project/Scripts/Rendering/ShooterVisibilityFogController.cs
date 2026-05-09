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
    private static readonly int ZoneCountId = Shader.PropertyToID("_ShooterVisibilityZoneCount"); // 시야 구역 수 ID
    private static readonly int ZoneCentersId = Shader.PropertyToID("_ShooterVisibilityZoneCenters"); // 시야 구역 중심 배열 ID
    private static readonly int ZoneRadiiId = Shader.PropertyToID("_ShooterVisibilityZoneRadii"); // 시야 구역 반경 배열 ID
    private static readonly List<ShooterVisibilityFogController> ActiveControllers = new List<ShooterVisibilityFogController>(); // 활성 컨트롤러 목록
    private const int MaxShaderZoneCount = 32; // 셰이더 시야 구역 최대 수
    private static readonly Vector4[] ShaderZoneCenters = new Vector4[MaxShaderZoneCount]; // 셰이더 전달 중심 배열
    private static readonly float[] ShaderZoneRadii = new float[MaxShaderZoneCount]; // 셰이더 전달 반경 배열

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
    private readonly List<PurificationLightSource> connectedZones = new List<PurificationLightSource>(); // 연결 정화 구역 목록
    private readonly List<PurificationLightSource> searchQueue = new List<PurificationLightSource>(); // 연결 탐색 대기 목록

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
        int zoneCount = 1; // 셰이더 구역 수

        if (useCurrentPurificationZone && TryGetConnectedPurificationZones(origin, connectedZones))
        {
            PurificationLightSource primaryZone = connectedZones[0]; // 대표 정화 구역
            center = primaryZone.transform.position;
            radius = primaryZone.radius + connectedZoneExtraRadius;
            zoneCount = WriteShaderZones(connectedZones);
        }
        else
        {
            ShaderZoneCenters[0] = origin;
            ShaderZoneRadii[0] = fallbackRadius;
        }

        Shader.SetGlobalFloat(EnabledId, 1f);
        Shader.SetGlobalVector(CenterId, center);
        Shader.SetGlobalFloat(RadiusId, radius);
        Shader.SetGlobalFloat(SoftEdgeId, softEdge);
        Shader.SetGlobalColor(FogColorId, fogColor);
        Shader.SetGlobalFloat(FogOpacityId, fogOpacity);
        Shader.SetGlobalFloat(ZoneCountId, zoneCount);
        Shader.SetGlobalVectorArray(ZoneCentersId, ShaderZoneCenters);
        Shader.SetGlobalFloatArray(ZoneRadiiId, ShaderZoneRadii);
    }

    // 월드 위치가 현재 카메라 가시 영역에 포함되는지 확인
    public bool ContainsVisiblePosition(Vector3 worldPosition)
    {
        Vector3 origin = visibilityOrigin != null ? visibilityOrigin.position : transform.position; // 현재 기준 위치
        if (!useCurrentPurificationZone)
            return Vector3.Distance(origin, worldPosition) <= fallbackRadius;

        if (!TryGetConnectedPurificationZones(origin, connectedZones))
            return Vector3.Distance(origin, worldPosition) <= fallbackRadius;

        for (int i = 0; i < connectedZones.Count; i++)
        {
            PurificationLightSource zone = connectedZones[i]; // 연결 정화 구역
            if (zone != null && Vector3.Distance(zone.transform.position, worldPosition) <= zone.radius + connectedZoneExtraRadius)
                return true;
        }

        return false;
    }

    // 현재 위치에서 이어진 정화 구역 목록 조회
    private bool TryGetConnectedPurificationZones(Vector3 origin, List<PurificationLightSource> results)
    {
        results.Clear();
        searchQueue.Clear();
        PurificationZoneRegistry registry = PurificationZoneRegistry.Instance;
        if (registry == null)
            return false;

        foreach (PurificationLightSource source in registry.Sources)
        {
            if (source == null || !source.preventsDarknessExposure || !source.Contains(origin))
                continue;

            AddConnectedZone(source, results, searchQueue);
        }

        for (int queueIndex = 0; queueIndex < searchQueue.Count; queueIndex++)
        {
            PurificationLightSource current = searchQueue[queueIndex]; // 탐색 기준 정화 구역
            if (current == null)
                continue;

            foreach (PurificationLightSource candidate in registry.Sources)
            {
                if (candidate == null || !candidate.preventsDarknessExposure || results.Contains(candidate))
                    continue;

                if (!AreZonesConnected(current, candidate))
                    continue;

                AddConnectedZone(candidate, results, searchQueue);
            }
        }

        return results.Count > 0;
    }

    // 연결 정화 구역 목록에 중복 없이 추가
    private void AddConnectedZone(PurificationLightSource source, List<PurificationLightSource> results, List<PurificationLightSource> queue)
    {
        if (source == null || results.Contains(source))
            return;

        results.Add(source);
        queue.Add(source);
    }

    // 두 정화 구역의 반경 연결 여부 확인
    private bool AreZonesConnected(PurificationLightSource a, PurificationLightSource b)
    {
        float connectionDistance = a.radius + b.radius; // 연결 기준 거리
        return Vector3.Distance(a.transform.position, b.transform.position) <= connectionDistance;
    }

    // 연결 구역을 셰이더 배열로 변환
    private int WriteShaderZones(List<PurificationLightSource> zones)
    {
        int count = Mathf.Min(zones.Count, MaxShaderZoneCount); // 전달 구역 수
        for (int i = 0; i < count; i++)
        {
            PurificationLightSource zone = zones[i]; // 전달 정화 구역
            ShaderZoneCenters[i] = zone != null ? zone.transform.position : Vector3.zero;
            ShaderZoneRadii[i] = zone != null ? zone.radius + connectedZoneExtraRadius : 0f;
        }

        return Mathf.Max(1, count);
    }
}
