using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ShooterVisibilityFogFeature : ScriptableRendererFeature
{
    [System.Serializable]
    private class Settings
    {
        public Material material; // 안개 합성 재질
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing; // 실행 시점
    }

    [SerializeField] private Settings settings = new Settings(); // 렌더 패스 설정

    private ShooterVisibilityFogPass pass; // 렌더 패스

    // 렌더 패스 생성
    public override void Create()
    {
        pass = new ShooterVisibilityFogPass(settings.material, settings.renderPassEvent);
    }

    // 게임 카메라 렌더 패스 등록
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null || settings.material == null || renderingData.cameraData.cameraType != CameraType.Game)
            return;

        renderer.EnqueuePass(pass);
    }

    // 카메라 컬러 타겟 연결
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (pass == null || settings.material == null || renderingData.cameraData.cameraType != CameraType.Game)
            return;

        pass.SetTarget(renderer.cameraColorTargetHandle);
    }

    private class ShooterVisibilityFogPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler ProfilingSampler = new ProfilingSampler("Shooter Visibility Fog"); // 프로파일링 표식

        private readonly Material material; // 안개 합성 재질
        private RTHandle source; // 원본 컬러 타겟
        private RTHandle tempColor; // 임시 컬러 타겟

        // 렌더 패스 초기화
        public ShooterVisibilityFogPass(Material material, RenderPassEvent renderPassEvent)
        {
            this.material = material;
            this.renderPassEvent = renderPassEvent;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        // 원본 컬러 타겟 설정
        public void SetTarget(RTHandle colorTarget)
        {
            source = colorTarget;
        }

        // 임시 컬러 타겟 준비
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor; // 카메라 타겟 설명
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref tempColor, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_ShooterVisibilityFogTemp");
        }

        // 안개 합성 실행
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || source == null || tempColor == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(); // 렌더 명령 버퍼
            using (new ProfilingScope(cmd, ProfilingSampler))
            {
                Blitter.BlitCameraTexture(cmd, source, tempColor, material, 0);
                Blitter.BlitCameraTexture(cmd, tempColor, source);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // 임시 타겟 해제
        public void Dispose()
        {
            tempColor?.Release();
        }
    }

    // 렌더 feature 해제
    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
    }
}
