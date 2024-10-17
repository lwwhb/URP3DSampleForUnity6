using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CockpitOutline : FullscreenEffectBase<CockpitOutlinePass>
{
}

public class CockpitOutlinePass : FullscreenPassBase<FullscreenPassDataBase>
{
    bool IsOutlineEnabled()
    {
        var volumeComponent = VolumeManager.instance.stack.GetComponent<OutlineVolumeComponent>();

        return volumeComponent.Enabled.value;
    }

#pragma warning disable CS0618
#pragma warning disable CS0672
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (IsOutlineEnabled())
            base.Execute(context, ref renderingData);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (IsOutlineEnabled())
            base.RecordRenderGraph(renderGraph, frameData);
    }
}