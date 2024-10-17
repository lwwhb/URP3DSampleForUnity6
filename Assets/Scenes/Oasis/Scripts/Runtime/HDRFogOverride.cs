using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class HDRFogOverride : MonoBehaviour
{
    // private var for previous fog color
    private Color _previousFogColor;
    // public var for fog color
    [ColorUsage(false, true)]public Color FogColor = Color.white;
    
    private void OnEnable()
    {
        RenderPipelineManager.beginContextRendering += BeginFrame;
        RenderPipelineManager.endContextRendering += EndFrame;
    }
    
    private void OnDisable()
    {
        RenderPipelineManager.beginContextRendering -= BeginFrame;
        RenderPipelineManager.endContextRendering -= EndFrame;
    }

    private void BeginFrame(ScriptableRenderContext arg1, List<Camera> arg2)
    {
        // get current fog color
        _previousFogColor = RenderSettings.fogColor;
        // set fog color to our override color
        RenderSettings.fogColor = FogColor;
    }
    
    private void EndFrame(ScriptableRenderContext arg1, List<Camera> arg2)
    {
        // revert fog color to previous color
        RenderSettings.fogColor = _previousFogColor;
    }
}
