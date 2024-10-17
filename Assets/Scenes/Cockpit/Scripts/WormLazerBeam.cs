using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class WormLazerBeam : MonoBehaviour
{
    public bool hitting = false;

    private bool cachedActive;
    
    public GameObject beamHitObj;
    public GameObject beamMissObj;

    private Vector3 cachedBeamScale;
    private Vector3 beamScale;
    public Transform beamPivot;

    private void OnEnable()
    {
        RenderPipelineManager.beginContextRendering += OnBeginCamera;
        RenderPipelineManager.endContextRendering += EndCamera;
    }
    
    private void OnDisable()
    {
        RenderPipelineManager.beginContextRendering -= OnBeginCamera;
        RenderPipelineManager.endContextRendering -= EndCamera;
    }
    
    private void OnBeginCamera(ScriptableRenderContext arg1, List<Camera> arg2)
    {
        if(beamPivot == null || beamHitObj == null || beamMissObj == null) return;
        
        cachedBeamScale = beamPivot.localScale;
        cachedActive = beamHitObj.activeSelf;

        beamScale = cachedBeamScale;
        beamScale.y = -beamHitObj.transform.localPosition.z;
        beamPivot.localScale = beamScale;
        beamHitObj.SetActive(hitting);
        beamMissObj.SetActive(!hitting);
    }
    
    private void EndCamera(ScriptableRenderContext arg1, List<Camera> arg2)
    {
        if(beamPivot == null || beamHitObj == null || beamMissObj == null) return;
        
        beamPivot.localScale = cachedBeamScale;
        beamHitObj.SetActive(cachedActive);
        beamMissObj.SetActive(!cachedActive);
    }
}
