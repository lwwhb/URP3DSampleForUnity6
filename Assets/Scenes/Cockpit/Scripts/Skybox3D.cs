using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class _3DSkybox : MonoBehaviour
{
    private _3DSkyboxPass pass;
    
    public LayerMask mask;

    private FilteringSettings filterOpaqueSettings = FilteringSettings.defaultValue;
    private FilteringSettings filterTransparentSettings = FilteringSettings.defaultValue;
    
    private RenderStateBlock renderStateBlock;

    public int stencilRef;
    
    private StencilState stencilState;

    public Material depthClearMat;

    public float scale = 64;
    
    private void OnEnable()
    {
        pass ??= new _3DSkyboxPass();

        // injection point
        pass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

        // setup 3D skybox stuff
        filterOpaqueSettings = new FilteringSettings(RenderQueueRange.opaque, mask.value);
        filterTransparentSettings = new FilteringSettings(RenderQueueRange.transparent, mask.value);
        renderStateBlock = new RenderStateBlock(RenderStateMask.Stencil);
        
        stencilState = StencilState.defaultValue;
        stencilState.enabled = true;
        stencilState.SetCompareFunction(CompareFunction.Equal);
        
        renderStateBlock.stencilReference = stencilRef;
        renderStateBlock.stencilState = stencilState;
        
        // setup callback
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += EndCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= EndCamera;
    }

    private void OnBeginCamera(ScriptableRenderContext context, Camera cam)
    {
        //Avoid rendering while in terminal
        if (SceneTransitionManager.IsAvailable() && SceneTransitionManager.IsInTerminal() && cam.CompareTag("MainCamera"))
        {
            return;
        }
        
        if (pass == null) return;
        
        if (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView) return;
        
        pass.filterOpaqueSettings = filterOpaqueSettings;
        pass.filterTransparentSettings = filterTransparentSettings;
        pass.renderStateBlock = renderStateBlock;

        pass.depthClearMat = depthClearMat;

        // Do transform
        TransformSkybox(cam);

        // inject pass
        cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(pass);
    }
    
    private void EndCamera(ScriptableRenderContext arg1, Camera arg2)
    {
        transform.position = Vector3.zero;
        transform.localScale = Vector3.one;
    }

    private void TransformSkybox(Camera cam)
    {
        if (cam.cameraType == CameraType.SceneView) return;
        
        var offset = cam.transform.position * (1 - (1/scale));

        transform.position = offset;
        transform.localScale = Vector3.one * (1/scale); 
    }

    private class _3DSkyboxPass : ScriptableRenderPass
    {
        public FilteringSettings filterOpaqueSettings;
        public FilteringSettings filterTransparentSettings;
        
        public RenderStateBlock renderStateBlock;

        public Material depthClearMat;

        readonly List<ShaderTagId> shaderTags = new List<ShaderTagId>
        {
            new("SRPDefaultUnlit"), new("UniversalForward"), new("UniversalForwardOnly")
        };
        
        public _3DSkyboxPass()
        {
            profilingSampler = new ProfilingSampler(nameof(_3DSkyboxPass));
        }

#pragma warning disable CS0618
#pragma warning disable CS0672
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("3D Skybox");

            DrawSkyboxObjects(ref renderingData, context, ref cmd);

            // Clear the depth values
            if (depthClearMat != null)
            {
                CoreUtils.DrawFullScreen(cmd, depthClearMat);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        static ShaderTagId[] s_ShaderTagValues = new ShaderTagId[1];
        static RenderStateBlock[] s_RenderStateBlocks = new RenderStateBlock[1];

        private void DrawSkyboxObjects(ref RenderingData renderingData, ScriptableRenderContext context, ref CommandBuffer cmd)
        {
            var drawSettings =
                RenderingUtils.CreateDrawingSettings(shaderTags, ref renderingData, SortingCriteria.CommonOpaque);

            s_RenderStateBlocks[0] = renderStateBlock;
            s_ShaderTagValues[0] = ShaderTagId.none;

            var param = new RendererListParams(renderingData.cullResults, drawSettings, filterOpaqueSettings);
            param.stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
            param.tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
            param.isPassTagName = false;
            var renderersListHandle = context.CreateRendererList(ref param);
            cmd.DrawRendererList(renderersListHandle);

            param = new RendererListParams(renderingData.cullResults, drawSettings, filterTransparentSettings);
            param.stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
            param.tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
            param.isPassTagName = false;
            renderersListHandle = context.CreateRendererList(ref param);
            cmd.DrawRendererList(renderersListHandle);
        }

        private RendererListHandle InitRendererLists(ContextContainer frameData, RenderGraph renderGraph, FilteringSettings filterSettings, RenderStateBlock renderStateBlock)
        {
            // Access the relevant frame data from the Universal Render Pipeline
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            var sortFlags = cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings( shaderTags, universalRenderingData, cameraData, lightData, sortFlags);

            var param = new RendererListParams(universalRenderingData.cullResults, drawSettings, filterSettings);
            s_RenderStateBlocks[0] = renderStateBlock;
            s_ShaderTagValues[0] = ShaderTagId.none;
            param.stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
            param.tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
            param.isPassTagName = false;

            
            return renderGraph.CreateRendererList(param);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var camData = frameData.Get<UniversalCameraData>();
            var cam = camData.camera;

            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalResourceData cameraData = frameData.Get<UniversalResourceData>();

            using (var builder = renderGraph.AddRasterRenderPass("3D Skybox", out _3DSkyboxPassData passData))
            {
                passData.opaqueRendererListHandle = InitRendererLists(frameData, renderGraph, filterOpaqueSettings, renderStateBlock);
                passData.transparentRendererListHandle= InitRendererLists(frameData, renderGraph, filterTransparentSettings, renderStateBlock);
                passData.cameraData = camData;

                // We declare the RendererList we just created as an input dependency to this pass, via UseRendererList()
                builder.UseRendererList(passData.opaqueRendererListHandle);
                builder.UseRendererList(passData.transparentRendererListHandle);

                // Setup as a render target via SetRenderAttachment and SetRenderAttachmentDepth, which are the equivalent of using the old cmd.SetRenderTarget(color,depth)
                builder.SetRenderAttachment(cameraData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(cameraData.activeDepthTexture);

                if (camData.xr.enabled)
                {
                    builder.EnableFoveatedRasterization(camData.xr.supportsFoveatedRendering);
                }
                
                builder.SetRenderFunc((_3DSkyboxPassData data, RasterGraphContext context) => ExecuteRenderGraph(data, context));
            }
        }

        private void ExecuteRenderGraph( _3DSkyboxPassData passData, RasterGraphContext rgContext )
        {
            var cmd = rgContext.cmd;
            
            var cameraData = passData.cameraData;
            
            if (cameraData.xr.enabled && cameraData.xr.singlePassEnabled) //If skybox is rendering in VR, we have to modify matrices to eliminate unwanted parallax
            {

                var leftEyeProjection = cameraData.GetProjectionMatrix(0);
                var leftEyeView = cameraData.camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
                
                var rightEyeProjection = cameraData.GetProjectionMatrix(1);
                var rightEyeView = cameraData.camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

                Vector4 leftOrigin = leftEyeView.GetColumn(3);
                Vector4 rightOrigin = rightEyeView.GetColumn(3);

                Vector4 centerOrigin = (leftOrigin + rightOrigin) * 0.5f;

                var centerView = leftEyeView;
                centerView.SetColumn(3, centerOrigin);
                
                XRBuiltinShaderConstants.UpdateBuiltinShaderConstants(centerView, leftEyeProjection, false, 0);
                XRBuiltinShaderConstants.UpdateBuiltinShaderConstants(centerView, rightEyeProjection, false, 1);
                XRBuiltinShaderConstants.SetBuiltinShaderConstants(cmd);

                cmd.DrawRendererList(passData.opaqueRendererListHandle);
                cmd.DrawRendererList(passData.transparentRendererListHandle);
                
                XRBuiltinShaderConstants.UpdateBuiltinShaderConstants(leftEyeView, leftEyeProjection, false, 0);
                XRBuiltinShaderConstants.UpdateBuiltinShaderConstants(rightEyeView, rightEyeProjection, false, 1);
                XRBuiltinShaderConstants.SetBuiltinShaderConstants(cmd);
            }
            else
            {
                cmd.DrawRendererList(passData.opaqueRendererListHandle);
                cmd.DrawRendererList(passData.transparentRendererListHandle);
            }
            
            
            // Clear the depth values
            // cmd.ClearRenderTarget(true, false, Color.black);

            if (depthClearMat != null)
            {
                cmd.DrawProcedural(Matrix4x4.identity, depthClearMat, 0, MeshTopology.Triangles, 3, 1, null);
            }

        }

        class _3DSkyboxPassData
        {
            public CullingResults cullingResults;
            public RendererListHandle opaqueRendererListHandle;
            public RendererListHandle transparentRendererListHandle;
            public UniversalCameraData cameraData;
        }
    }

    private Camera cam;

    private void OnDrawGizmosSelected()
    {
        if (cam == null) cam = Camera.main;

        if (cam == null) return;

        Gizmos.matrix = cam.transform.localToWorldMatrix;

        var c = Color.red;
        c.a = 0.2f;
        Gizmos.color = c;
        Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, cam.farClipPlane * (1f/scale), cam.nearClipPlane * (1f/scale), cam.aspect);
    }
}
