/*━━━━━━━━━*
*@file LaserBeamMesh.cs*
*@brief レーザーライト用の交差平面メッシュを生成する*
*@author 24cu0312 久場洸太*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks +Z方向へレーザーを生成*
*━━━━━━━━━*/

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// どの方向から見てもレーザーに見える交差平面メッシュを生成します。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class LaserBeamMesh : MonoBehaviour
{
    private const float EMinimumSize = 0.01f;              //形状サイズの最小値
    private const float EDefaultLength = 8.0f;             //標準のレーザー長
    private const float EDefaultRadius = 0.08f;            //標準のレーザー半径
    private const int EMinimumPlanes = 2;                  //交差平面数の最小値
    private const int EMaximumPlanes = 8;                  //交差平面数の最大値
    private const int EDefaultPlanes = 3;                  //標準の交差平面数
    private const int EVerticesPerPlane = 4;               //一平面あたりの頂点数
    private const int ETrianglesPerPlane = 2;              //一平面あたりの三角形数
    private const int EIndicesPerTriangle = 3;             //一三角形あたりの頂点番号数
    private const float EHalfTurnDegrees = 180.0f;         //半周分の角度

    [Min(EMinimumSize)]
    [FormerlySerializedAs("m_length")]
    [SerializeField] private float m_length = EDefaultLength; //レーザーの長さ
    [Min(EMinimumSize)]
    [FormerlySerializedAs("m_radius")]
    [SerializeField] private float m_radius = EDefaultRadius; //レーザーの半径
    [Range(EMinimumPlanes, EMaximumPlanes)]
    [FormerlySerializedAs("m_planes")]
    [SerializeField] private int m_planeCount = EDefaultPlanes; //交差平面の数

    private Mesh m_mesh;                                   //実行時に生成するメッシュ

    /// <summary>
    /// 有効化されたときにレーザーメッシュを生成します。
    /// </summary>
    private void OnEnable()
    {
        Rebuild();
    }

    /// <summary>
    /// Inspectorの値が変わったときに再生成します。
    /// </summary>
    private void OnValidate()
    {
        Rebuild();
    }

    /// <summary>
    /// 生成した一時メッシュを破棄します。
    /// </summary>
    private void OnDisable()
    {
        if (m_mesh == null)return;

        if (Application.isPlaying)
        {
            Destroy(m_mesh);
        }
        else
        {
            DestroyImmediate(m_mesh);
        }

        m_mesh = null;
    }

    /// <summary>
    /// 現在の設定でレーザーメッシュを再生成します。
    /// </summary>
    [ContextMenu("Rebuild Laser")]
    public void Rebuild()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>(); //生成メッシュの設定先
        if (meshFilter == null)return;

        PrepareMesh();

        int planeCount = Mathf.Clamp(
            m_planeCount,
            EMinimumPlanes,
            EMaximumPlanes);                                //実際に使用する平面数
        Vector3[] vertices =
            new Vector3[planeCount * EVerticesPerPlane];     //頂点座標群
        Vector2[] uvs =
            new Vector2[vertices.Length];                    //UV座標群
        int[] triangles = new int[
            planeCount * ETrianglesPerPlane * EIndicesPerTriangle]; //三角形頂点番号群

        for (int i = 0; i < planeCount; ++i)
        {
            BuildPlane(
                i,
                planeCount,
                vertices,
                uvs,
                triangles);
        }

        m_mesh.vertices = vertices;
        m_mesh.uv = uvs;
        m_mesh.triangles = triangles;
        m_mesh.RecalculateNormals();
        m_mesh.RecalculateBounds();
        meshFilter.sharedMesh = m_mesh;
    }

    /// <summary>
    /// LaserBeamの形状生成処理を、Prefab以外の短い派生Effectからも再利用します。
    /// ペンライト側で同じMesh生成コードを複製しないための共通設定口です。
    /// </summary>
    public void Configure(float _length, float _radius, int _planeCount)
    {
        m_length = Mathf.Max(EMinimumSize, _length);
        m_radius = Mathf.Max(EMinimumSize, _radius);
        m_planeCount = Mathf.Clamp(
            _planeCount,
            EMinimumPlanes,
            EMaximumPlanes);
        Rebuild();
    }

    /// <summary>
    /// メッシュを新規作成または初期化します。
    /// </summary>
    private void PrepareMesh()
    {
        if (m_mesh == null)
        {
            m_mesh = new Mesh();
            m_mesh.name = "Laser Beam Generated";
            m_mesh.hideFlags = HideFlags.DontSave;
            return;
        }

        m_mesh.Clear();
    }

    /// <summary>
    /// 指定された角度のレーザー平面を構築します。
    /// </summary>
    private void BuildPlane(
        int _planeindex,
        int _planecount,
        Vector3[] _vertices,
        Vector2[] _uvs,
        int[] _triangles)
    {
        float angleDegrees =
            EHalfTurnDegrees * _planeindex / _planecount;     //平面の回転角度
        Quaternion rotation =
            Quaternion.AngleAxis(angleDegrees, Vector3.forward); //平面の回転
        Vector3 widthDirection = rotation * Vector3.right * m_radius; //レーザー幅方向
        int vertexIndex = _planeindex * EVerticesPerPlane;     //平面頂点の開始番号
        int triangleIndex =
            _planeindex * ETrianglesPerPlane * EIndicesPerTriangle; //三角形の開始番号

        _vertices[vertexIndex] = -widthDirection;
        _vertices[vertexIndex + 1] = widthDirection;
        _vertices[vertexIndex + 2] = -widthDirection + Vector3.forward * m_length;
        _vertices[vertexIndex + 3] = widthDirection + Vector3.forward * m_length;

        _uvs[vertexIndex] = new Vector2(0.0f, 0.0f);
        _uvs[vertexIndex + 1] = new Vector2(1.0f, 0.0f);
        _uvs[vertexIndex + 2] = new Vector2(0.0f, 1.0f);
        _uvs[vertexIndex + 3] = new Vector2(1.0f, 1.0f);

        _triangles[triangleIndex] = vertexIndex;
        _triangles[triangleIndex + 1] = vertexIndex + 2;
        _triangles[triangleIndex + 2] = vertexIndex + 1;
        _triangles[triangleIndex + 3] = vertexIndex + 1;
        _triangles[triangleIndex + 4] = vertexIndex + 2;
        _triangles[triangleIndex + 5] = vertexIndex + 3;
    }
}
