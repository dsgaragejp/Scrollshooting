using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Game.Background
{
    /// <summary>
    /// URPでフルスクリーンシェーダーエフェクトを適用するためのRendererFeature
    /// カメラ背景としてXorDev風シェーダーを描画
    /// </summary>
    public class FullScreenShaderEffect : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            public Material effectMaterial;
        }

        public Settings settings = new Settings();
        private FullScreenShaderPass shaderPass;

        public override void Create()
        {
            shaderPass = new FullScreenShaderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.effectMaterial == null) return;
            renderer.EnqueuePass(shaderPass);
        }

        class FullScreenShaderPass : ScriptableRenderPass
        {
            private Settings settings;
            private RTHandle tempTextureHandle;
            private static readonly int TempTextureId = Shader.PropertyToID("_TempTexture");

            public FullScreenShaderPass(Settings settings)
            {
                this.settings = settings;
                this.renderPassEvent = settings.renderPassEvent;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                RenderingUtils.ReAllocateHandleIfNeeded(ref tempTextureHandle, descriptor, name: "_TempTexture");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (settings.effectMaterial == null) return;

                CommandBuffer cmd = CommandBufferPool.Get("FullScreenShaderEffect");

                var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // フルスクリーンにマテリアルを描画
                Blitter.BlitCameraTexture(cmd, cameraColorTarget, tempTextureHandle, settings.effectMaterial, 0);
                Blitter.BlitCameraTexture(cmd, tempTextureHandle, cameraColorTarget);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                // RTHandle is managed by RenderingUtils.ReAllocateHandleIfNeeded
            }

            public void Dispose()
            {
                tempTextureHandle?.Release();
            }
        }
    }
}
