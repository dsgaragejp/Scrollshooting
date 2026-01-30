using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
            private RenderTargetIdentifier source;
            private RenderTargetHandle tempTexture;

            public FullScreenShaderPass(Settings settings)
            {
                this.settings = settings;
                this.renderPassEvent = settings.renderPassEvent;
                tempTexture.Init("_TempTexture");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (settings.effectMaterial == null) return;

                CommandBuffer cmd = CommandBufferPool.Get("FullScreenShaderEffect");

                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;

                cmd.GetTemporaryRT(tempTexture.id, descriptor);

                // フルスクリーンにマテリアルを描画
                cmd.Blit(BuiltinRenderTextureType.None, tempTexture.Identifier(), settings.effectMaterial);
                cmd.Blit(tempTexture.Identifier(), renderingData.cameraData.renderer.cameraColorTargetHandle);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(tempTexture.id);
            }
        }
    }
}
