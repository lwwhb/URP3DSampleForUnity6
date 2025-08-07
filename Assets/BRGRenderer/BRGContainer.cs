using System;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public class BRGContainer : MonoBehaviour
{
    public Mesh[] meshes;
    public Material[] materials;

    private bool UseConstantBuffer => BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;
    private BatchRendererGroup m_BRG;
    private BatchMeshID[] m_MeshesID;
    private BatchMaterialID[] m_MaterialsID;
    private GraphicsBuffer[] m_InstanceDataBuffers;
    private BatchID[] m_BatchesID;

    private void Start()
    {
        InitializeBRG();
    }

    private void OnDisable()
    {
        m_BRG.Dispose();
    }

    public unsafe JobHandle OnPerformCulling(
        BatchRendererGroup rendererGroup,
        BatchCullingContext cullingContext,
        BatchCullingOutput cullingOutput,
        IntPtr userContext)
    {
        // This simple example doesn't use jobs, so it can return an empty JobHandle.
        // Performance-sensitive applications should use Burst jobs to implement
        // culling and draw command output. In this case, this function would return a
        // handle that completes when the Burst jobs finish.
        return new JobHandle();
    }
    public void OnFinishedCulling(IntPtr customCullingResult)
    {
        
    }
    
    private void InitializeBRG()
    {
        m_BRG = new BatchRendererGroup(new BatchRendererGroupCreateInfo()
        {
            cullingCallback = OnPerformCulling,
            finishedCullingCallback = OnFinishedCulling,
            userContext = IntPtr.Zero
        });
        for (int i = 0; i < meshes.Length; i++)
            m_MeshesID[i] = m_BRG.RegisterMesh(meshes[i]);
        for (int i = 0; i < materials.Length; i++)
            m_MaterialsID[i] = m_BRG.RegisterMaterial(materials[i]);
        for (int i = 0; i < m_InstanceDataBuffers.Length; i++)
        {
            if (UseConstantBuffer)
            {
                m_InstanceDataBuffers[i] = new GraphicsBuffer(GraphicsBuffer.Target.Constant, 1000, 12);
            }
            else
            {
                m_InstanceDataBuffers[i] = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 1000, 12);
            }
        }
    }
}
