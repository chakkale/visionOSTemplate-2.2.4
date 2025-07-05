using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TransparentGrabPassFeature : ScriptableRendererFeature
{
    class GrabPassRenderPass : ScriptableRenderPass
    {
        private RTHandle tempTexture;
        private string profilerTag;
        private RTHandle source;
        private int downsample;
        private static readonly int _TransparentGrabTexture = Shader.PropertyToID("_TransparentGrabTexture");

        public GrabPassRenderPass(string profilerTag, int downsample)
        {
            this.profilerTag = profilerTag;
            this.downsample = downsample;
        }

        public void Setup(RTHandle source)
        {
            this.source = source;
        }

        [System.Obsolete]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            if (downsample > 1)
            {
                desc.width /= downsample;
                desc.height /= downsample;
            }
            RenderingUtils.ReAllocateHandleIfNeeded(ref tempTexture, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempTransparentGrabTexture");
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            // Use Blitter for RTHandle blit
            Blitter.BlitCameraTexture(cmd, source, tempTexture);

            cmd.SetGlobalTexture(_TransparentGrabTexture, tempTexture);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (tempTexture != null)
            {
                tempTexture.Release();
                tempTexture = null;
            }
        }
    }

    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        [Range(1, 4)] public int downsample = 1;
    }

    public Settings settings = new Settings();
    private GrabPassRenderPass grabPass;

    public override void Create()
    {
        grabPass = new GrabPassRenderPass("Transparent Grab Pass", settings.downsample)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        #pragma warning disable CS0618 // Type or member is obsolete
        grabPass.Setup(renderer.cameraColorTargetHandle);
        #pragma warning restore CS0618 // Type or member is obsolete
        renderer.EnqueuePass(grabPass);
    }
} 