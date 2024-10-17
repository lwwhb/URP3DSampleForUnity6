using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Empty class to be used in scenes and doesn't implement any additional overrides
public class FullscreenEffect : FullscreenEffectBase<FullscreenPassBase<FullscreenPassDataBase>>
{
}

[ExecuteAlways]
public class FullscreenEffectBase<T> : MonoBehaviour where T:FullscreenPassBase<FullscreenPassDataBase>, new()
{
    private T _pass;

    [SerializeField]
    private string _passName = "Fullscreen Pass";

    [SerializeField]
    private Material _material;

    [SerializeField]
    private RenderPassEvent _injectionPoint = RenderPassEvent.BeforeRenderingTransparents;
    [SerializeField]
    private int _injectionPointOffset = 0;
    [SerializeField]
    private ScriptableRenderPassInput _inputRequirements = ScriptableRenderPassInput.Color;
    [SerializeField]
    private CameraType _cameraType = CameraType.Game | CameraType.SceneView;


    private void OnEnable()
    {
        SetupPass();

        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
    }

    public virtual void SetupPass()
    {

        _pass ??= new T();

        // pass setup
        _pass.renderPassEvent = _injectionPoint + _injectionPointOffset;
        _pass.material = _material;
        if (_material != null)
        {
            _pass.hasYFlipKeyword = _material.shader.keywordSpace.keywordNames.Contains("_FLIPY");

            if (_pass.hasYFlipKeyword)
                _pass.yFlipKeyword = new LocalKeyword(_material.shader, "_FLIPY");
        }
        _pass.passName = _passName;

        _pass.inputRequirements = _inputRequirements;
        _pass.ConfigureInput(_inputRequirements);
    }

    public virtual void OnBeginCamera( ScriptableRenderContext ctx, Camera cam )
    {
        // Skip if pass wasn't initialized or if material is empty
        if (_pass == null || _material == null)
            return;

        // Only draw for selected camera types
        if ( (cam.cameraType & _cameraType) == 0) return;

        // injection pass
        cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass( _pass );
    }

    private void OnValidate()
    {
        SetupPass();
    }
}

public class FullscreenPassBase<T> : ScriptableRenderPass where T : FullscreenPassDataBase, new()
{
    public Material material;

    public bool hasYFlipKeyword;
    public LocalKeyword yFlipKeyword;
    new public string passName = "Fullscreen Pass";
    public ScriptableRenderPassInput inputRequirements;

    #region Legacy
#pragma warning disable CS0618
#pragma warning disable CS0672
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (hasYFlipKeyword)
            material.SetKeyword(
                yFlipKeyword,
                renderingData.cameraData.IsRenderTargetProjectionMatrixFlipped(renderingData.cameraData.renderer.cameraColorTargetHandle)
                );

        var cmd = CommandBufferPool.Get(passName);

        CoreUtils.DrawFullScreen(cmd, material);

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }
    #endregion

    #region RenderGraph
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var cameraData = frameData.Get<UniversalCameraData>();

        using (var builder = renderGraph.AddRasterRenderPass(passName, out T passData))
        {
            passData.cameraData = cameraData;
            passData.hasYFlipKeyword = hasYFlipKeyword;

            // Set buffers access.
            if (inputRequirements.HasFlag(ScriptableRenderPassInput.Color))
            {
                passData.textureHandle = resourceData.activeColorTexture;
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            }
            if (inputRequirements.HasFlag(ScriptableRenderPassInput.Depth))
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
            if (inputRequirements.HasFlag(ScriptableRenderPassInput.Normal))
                builder.UseTexture(resourceData.cameraNormalsTexture, AccessFlags.Read);
            if (inputRequirements.HasFlag(ScriptableRenderPassInput.Motion))
                builder.UseTexture(resourceData.motionVectorColor, AccessFlags.Read);

            builder.SetRenderFunc((T data, RasterGraphContext context) => ExecuteRenderGraph(data, context));
        }
    }

    public virtual void ExecuteRenderGraph(T passData, RasterGraphContext rgContext)
    {
        var cmd = rgContext.cmd;

        if (hasYFlipKeyword && passData.textureHandle.IsValid() )
            material.SetKeyword(
                yFlipKeyword,
                passData.cameraData.IsRenderTargetProjectionMatrixFlipped(passData.textureHandle)
                );

        cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1, null);
    }
    #endregion
}

public class FullscreenPassDataBase
{
    public UniversalCameraData cameraData;
    public TextureHandle textureHandle;
    public bool hasYFlipKeyword;

    public FullscreenPassDataBase()
    { }
}
