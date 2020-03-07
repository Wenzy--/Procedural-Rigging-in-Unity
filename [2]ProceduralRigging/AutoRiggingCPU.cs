using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRiggingCPU : MonoBehaviour
{
    public int boneNum = 4;
    public Vector3 boneDirection;
    public float boneLength = 0.3f;

    Mesh mesh;
    SkinnedMeshRenderer skin;

    Transform[] bones;
    Matrix4x4[] bindPoses;
    public float spread = 0.1f;


    void Start()
    {
        bones = new Transform[boneNum];
        bindPoses = new Matrix4x4[boneNum];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            Generate();
            Debug.Log("count:" + mesh.vertexCount);
        }
    }


    void CaculateWeight()
    {
        int vertexNum = mesh.vertexCount;
        BoneWeight[] weights = new BoneWeight[vertexNum];
        // 先做粗略的，假设每个点都可以受最近的 4 个骨骼影响.这里要注意，骨骼的位置与 mesh 的顶点位置需要转换，否则参考系不同，计算得出的权重就会出问题
        for (int i = 0; i < vertexNum; i++)
        {
            List<Vector2> distList = new List<Vector2>();
            for (int j = 0; j < bones.Length; j++)
            {
                Vector2 tempDist = new Vector2();
                tempDist.x = j;
                // 考虑到缩放比率，要除以一个系数
                tempDist.y = Vector3.Distance(mesh.vertices[i] * transform.localScale.x + transform.position, bones[j].position);
                distList.Add(tempDist);
            }
            distList.Sort((a, b) => a.y.CompareTo(b.y)); // 基于 y 从小到大排序

            // 计算权重比例
            List<float> percent = new List<float>();
            float sum = 0;
            // 经过排序后，就只需要使用前 4 个
            for (int j = 0; j < 4; j++)
                sum += distList[j].y;

            for (int j = 0; j < 4; j++)
                percent.Add(distList[j].y / sum);

            // 将 percent 取反
            List<float> weightRatio = new List<float>();
            for (int j = 0; j < 4; j++)
                weightRatio.Add(percent[3 - j]);


            weights[i].boneIndex0 = (int)distList[0].x;
            weights[i].boneIndex1 = (int)distList[1].x;
            weights[i].boneIndex2 = (int)distList[2].x;
            weights[i].boneIndex3 = (int)distList[3].x;

            //// 一种逐级递减分配权重的方式
            float weightLeft = 1f;
            float finalWeight;
            for (int j = 0; j < 4; j++)
            {
                finalWeight = Mathf.Lerp(1, weightRatio[j], spread);
                if (weightLeft - finalWeight > 0)
                {
                    weightLeft -= finalWeight;
                }
                else
                {
                    finalWeight = weightLeft;
                    weightLeft = 0;
                }
                if (j == 0)
                    weights[i].weight0 = finalWeight;
                else if (j == 1)
                    weights[i].weight1 = finalWeight;
                else if (j == 2)
                    weights[i].weight2 = finalWeight;
                else if (j == 3)
                    weights[i].weight3 = finalWeight;

            }
        }
        mesh.boneWeights = weights;
        mesh.bindposes = bindPoses;
        skin.bones = bones;
        skin.sharedMesh = mesh;
    }

    public void Generate()
    {
        for (int i = 0; i < boneNum; i++)
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
        CaculateWeight();
    }
}
