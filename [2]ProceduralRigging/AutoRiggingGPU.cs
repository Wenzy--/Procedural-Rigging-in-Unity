using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRiggingGPU : MonoBehaviour
{

    public ComputeShader RigCS;
    ComputeBuffer BoneBuffer; // 三维坐标
    ComputeBuffer PosBuffer; // 传入 mesh 顶点用于计算
    ComputeBuffer WeightBuffer; // 前 4 个 float 后 4 个 int 
    int kernel;

    public int BoneNum = 4;
    public Vector3 boneDirection;
    public float boneLength = 0.3f;

    Mesh mesh;
    SkinnedMeshRenderer skin;

    Transform[] bones;
    Matrix4x4[] bindPoses;
    public float Spread = 0.1f;

    void Start()
    {
        int maxBoneNum = 30;
        BoneBuffer = new ComputeBuffer(maxBoneNum, sizeof(float) * 3);
        int maxVerNum = 100000; // 假设最大的模型顶点数
        WeightBuffer = new ComputeBuffer(maxVerNum, sizeof(float) * 4 + sizeof(int) * 4);
        PosBuffer = new ComputeBuffer(maxVerNum, sizeof(float) * 3);
        bones = new Transform[BoneNum];
        bindPoses = new Matrix4x4[BoneNum];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            Generate();
            Debug.Log("count:" + mesh.vertexCount);
        }
    }

    public void Generate()
    {
        for (int i = 0; i < BoneNum; i++)
        {
            bones[i] = new GameObject("Bone" + i).transform;
            if (i == 0)
                bones[i].parent = transform;
            else
                bones[i].parent = bones[i - 1];

            bones[i].localRotation = Quaternion.identity;

            // 保证起始的端点在 mesh 的坐标零点
            if (i == 0)
                bones[i].localPosition = Vector3.zero;
            else
                bones[i].localPosition = boneDirection.normalized * boneLength;
            bindPoses[i] = bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
        }
        
        mesh = transform.GetComponent<MeshFilter>().mesh;
        skin = gameObject.AddComponent<SkinnedMeshRenderer>();
        
        RigCS.SetFloat("Spread", Spread);
        RigCS.SetInt("VertexNum", mesh.vertexCount);
        RigCS.SetInt("BoneNum", BoneNum);
        
        kernel = RigCS.FindKernel("Calculate");
        PosBuffer.SetData(mesh.vertices);
        RigCS.SetBuffer(kernel, "PosBuffer", PosBuffer);

        Vector3[] boneData = new Vector3[BoneNum];
        for (int i = 0; i < BoneNum; i++)
            boneData[i] = (bones[i].position  - transform.position)/ transform.localScale.x;
        BoneBuffer.SetData(boneData);
        RigCS.SetBuffer(kernel, "BoneBuffer", BoneBuffer);
        RigCS.SetBuffer(kernel, "WeightBuffer", WeightBuffer);
        RigCS.Dispatch(kernel, (int)Mathf.Ceil(mesh.vertexCount / 8f), 1, 1);

        BoneWeight[] weightData = new BoneWeight[mesh.vertexCount];
        WeightBuffer.GetData(weightData, 0, 0, mesh.vertexCount);

        mesh.boneWeights = weightData;
        mesh.bindposes = bindPoses;
        skin.bones = bones;
        skin.sharedMesh = mesh;

    }
}
