#if !UNITY_6000_0_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IIGR
{
    public class GrassBendingRTPrePass : ScriptableRendererFeature
    {
        private CustomRenderPass m_ScriptablePass;

        public override void Create()
        {
            m_ScriptablePass = new CustomRenderPass();
            m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }

        private class CustomRenderPass : ScriptableRenderPass
        {
            static readonly int _GrassBendingRT_pid = Shader.PropertyToID("_GrassBendingRT");
            static readonly RenderTargetIdentifier _GrassBendingRT_rti = new RenderTargetIdentifier(_GrassBendingRT_pid);
            ShaderTagId GrassBending_stid = new ShaderTagId("GrassBending");

#if UNITY_6_OR_NEWER
            [System.Obsolete]
#endif
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                cmd.GetTemporaryRT(_GrassBendingRT_pid, new RenderTextureDescriptor(512, 512, RenderTextureFormat.R8, 0));
                ConfigureTarget(_GrassBendingRT_rti);
                ConfigureClear(ClearFlag.All, Color.white);
            }

#if UNITY_6_OR_NEWER
            [System.Obsolete]
#endif
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (InstancedIndirectGrassRenderer.Instance == null)
                    return;

                CommandBuffer cmd = CommandBufferPool.Get("GrassBendingRT");
                Matrix4x4 viewMatrix = Matrix4x4.TRS(InstancedIndirectGrassRenderer.Instance.transform.position + new Vector3(0, 1, 0), Quaternion.LookRotation(-Vector3.up), new Vector3(1, 1, -1)).inverse;
                float sizeX = InstancedIndirectGrassRenderer.Instance.transform.localScale.x;
                float sizeZ = InstancedIndirectGrassRenderer.Instance.transform.localScale.z;
                Matrix4x4 projectionMatrix = Matrix4x4.Ortho(-sizeX, sizeX, -sizeZ, sizeZ, 0.5f, 1.5f);

                cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                context.ExecuteCommandBuffer(cmd);

                var drawSetting = CreateDrawingSettings(GrassBending_stid, ref renderingData, SortingCriteria.CommonTransparent);
                var filterSetting = new FilteringSettings(RenderQueueRange.all);
                context.DrawRenderers(renderingData.cullResults, ref drawSetting, ref filterSetting);

                cmd.Clear();
                cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);
                cmd.SetGlobalTexture(_GrassBendingRT_pid, new RenderTargetIdentifier(_GrassBendingRT_pid));

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(_GrassBendingRT_pid);
            }
        }
    }
}
#endif