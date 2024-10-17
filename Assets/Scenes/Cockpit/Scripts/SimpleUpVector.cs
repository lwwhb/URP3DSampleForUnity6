using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class SimpleUpvector : MonoBehaviour
{
    private Transform parent;
    public Transform upVectorGoal;

    private Quaternion cachedRotation;

    // Start is called before the first frame update
    private void OnEnable()
    {
        parent = transform.parent;
        
        if (parent == null || upVectorGoal == null) return;
        
        RenderPipelineManager.beginContextRendering += BeginFrame;
        RenderPipelineManager.endContextRendering += EndFrame;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginContextRendering -= BeginFrame;
        RenderPipelineManager.endContextRendering -= EndFrame;
    }
    
    private void BeginFrame(ScriptableRenderContext context, List<Camera> cams)
    {
        cachedRotation = transform.rotation;
        
        transform.rotation = Quaternion.LookRotation(parent.forward, upVectorGoal.position - transform.position);
    }
    
    private void EndFrame(ScriptableRenderContext context, List<Camera> cams)
    {
        if (Application.isPlaying) return;
        
        transform.rotation = cachedRotation;
    }
}
