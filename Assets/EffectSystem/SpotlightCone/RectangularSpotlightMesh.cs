/*━━━━━━━━━*
*@file RectangularSpotlightMesh.cs*
*@brief 四角形スポットライト用の四角錐メッシュを生成する*
*@remarks 円形用SpotlightConeMeshとは独立して+Z方向へ生成する*
*━━━━━━━━━*/

using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class RectangularSpotlightMesh : MonoBehaviour
{
    private const float EMinimumSize = 0.01f;
    private const int ESideCount = 4;
    private const int EVerticesPerCorner = 2;
    private const int EIndicesPerSide = 6;

    [SerializeField, Min(EMinimumSize)] private float m_length = 6.0f;
    [SerializeField, Min(EMinimumSize)] private float m_endWidth = 4.0f;
    [SerializeField, Min(EMinimumSize)] private float m_endHeight = 2.5f;

    private Mesh m_mesh;
    private float m_runtimeLength = -1.0f;

    public float ConfiguredLength => m_length;
    public float ConfiguredEndRadius =>
        Mathf.Max(m_endWidth, m_endHeight) * 0.5f;
    public Vector2 ConfiguredEndSize => new Vector2(m_endWidth, m_endHeight);

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        Rebuild();
    }

    private void OnDisable()
    {
        if (m_mesh == null)return;
        if (Application.isPlaying)Destroy(m_mesh);
        else DestroyImmediate(m_mesh);
        m_mesh = null;
    }

    [ContextMenu("Rebuild Rectangular Spotlight")]
    public void Rebuild()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)return;
        PrepareMesh();

        float currentLength = GetCurrentLength();
        float lengthScale = currentLength / Mathf.Max(m_length, EMinimumSize);
        float halfWidth = Mathf.Max(m_endWidth, EMinimumSize) * 0.5f * lengthScale;
        float halfHeight = Mathf.Max(m_endHeight, EMinimumSize) * 0.5f * lengthScale;
        Vector3[] corners =
        {
            new Vector3(-halfWidth, -halfHeight, currentLength),
            new Vector3( halfWidth, -halfHeight, currentLength),
            new Vector3( halfWidth,  halfHeight, currentLength),
            new Vector3(-halfWidth,  halfHeight, currentLength),
            new Vector3(-halfWidth, -halfHeight, currentLength)
        };
        Vector3[] vertices = new Vector3[(ESideCount + 1) * EVerticesPerCorner];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[ESideCount * EIndicesPerSide];

        for (int sideIndex = 0; sideIndex <= ESideCount; ++sideIndex)
        {
            int vertexIndex = sideIndex * EVerticesPerCorner;
            float perimeterPosition = sideIndex / (float)ESideCount;
            vertices[vertexIndex] = Vector3.zero;
            vertices[vertexIndex + 1] = corners[sideIndex];
            uvs[vertexIndex] = new Vector2(perimeterPosition, 0.0f);
            uvs[vertexIndex + 1] = new Vector2(perimeterPosition, 1.0f);

            if (sideIndex == ESideCount)continue;
            int triangleIndex = sideIndex * EIndicesPerSide;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex + 3;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 3;
            triangles[triangleIndex + 5] = vertexIndex + 2;
        }

        m_mesh.vertices = vertices;
        m_mesh.uv = uvs;
        m_mesh.triangles = triangles;
        m_mesh.RecalculateNormals();
        m_mesh.RecalculateBounds();
        meshFilter.sharedMesh = m_mesh;
    }

    public void SetRuntimeLength(float _length)
    {
        m_runtimeLength = Mathf.Max(EMinimumSize, _length);
        Rebuild();
    }

    public void ClearRuntimeLength()
    {
        m_runtimeLength = -1.0f;
        Rebuild();
    }

    private void PrepareMesh()
    {
        if (m_mesh == null)
        {
            m_mesh = new Mesh
            {
                name = "Rectangular Spotlight Generated",
                hideFlags = HideFlags.DontSave
            };
            return;
        }
        m_mesh.Clear();
    }

    private float GetCurrentLength()
    {
        return m_runtimeLength >= EMinimumSize ? m_runtimeLength : m_length;
    }
}
