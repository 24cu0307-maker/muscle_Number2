/*━━━━━━━━━*
*@file SpotlightConeMesh.cs*
*@brief スポットライト用コーンメッシュを生成する*
*@author 24cu0312 久場洸太*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks +Z方向へコーンを生成*
*━━━━━━━━━*/

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// スポットライトの光量を表現するコーンメッシュを生成します。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class SpotlightConeMesh : MonoBehaviour
{
    private const float EMinimumSize = 0.01f;             //形状サイズの最小値
    private const float EDefaultLength = 5.0f;            //標準のコーン長
    private const float EDefaultEndRadius = 2.0f;         //標準の終端半径
    private const int EDefaultSegments = 32;              //標準の分割数
    private const int EMinimumSegments = 3;               //分割数の最小値
    private const int EMaximumSegments = 64;              //分割数の最大値
    private const int ESideVerticesPerSegment = 2;        //一分割あたりの側面頂点数
    private const int ETriangleIndices = 3;               //一三角形の頂点番号数
    private const int ESideTrianglesPerSegment = 2;       //一分割あたりの側面三角形数
    private const float EFullCircleRadians = Mathf.PI * 2.0f; //一周分のラジアン
    private const float EUvCenter = 0.5f;                 //UVの中心座標

    [Min(EMinimumSize)]
    [FormerlySerializedAs("m_length")]
    [SerializeField] private float m_length = EDefaultLength; //コーンの長さ
    [Min(EMinimumSize)]
    [FormerlySerializedAs("m_endRadius")]
    [SerializeField] private float m_endRadius = EDefaultEndRadius; //コーン終端の半径
    [Range(EMinimumSegments, EMaximumSegments)]
    [FormerlySerializedAs("m_segments")]
    [SerializeField] private int m_segments = EDefaultSegments; //円周方向の分割数
    [FormerlySerializedAs("m_capEnd")]
    [SerializeField] private bool b_m_capEnd = true;      //コーン終端を閉じるか

    private Mesh m_mesh;                                  //実行時に生成するメッシュ
    private float m_runtimeLength = -1.0f;                 //衝突対応ライト用の一時的な長さ

    public float ConfiguredLength
    {
        get
        {
            return m_length;
        }
    }

    public float ConfiguredEndRadius
    {
        get
        {
            return m_endRadius;
        }
    }

    /// <summary>
    /// 有効化されたときにメッシュを生成します。
    /// </summary>
    private void OnEnable()
    {
        Rebuild();
    }

    /// <summary>
    /// Inspectorの値が変わったときにメッシュを再生成します。
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
    /// 現在の設定でコーンメッシュを再生成します。
    /// </summary>
    [ContextMenu("Rebuild Cone")]
    public void Rebuild()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();              //生成メッシュの設定先
        if (meshFilter == null)return;

        PrepareMesh();

        int segmentCount = Mathf.Clamp(
            m_segments,
            EMinimumSegments,
            EMaximumSegments);                                          //実際に使用する分割数
        int sideVertexCount = (segmentCount + 1) * ESideVerticesPerSegment; //側面の頂点数
        int capVertexCount = b_m_capEnd ? segmentCount + 2 : 0;          //終端面の頂点数
        int sideIndexCount =
            segmentCount * ESideTrianglesPerSegment * ETriangleIndices;  //側面の頂点番号数
        int capIndexCount = b_m_capEnd ? segmentCount * ETriangleIndices : 0; //終端面の頂点番号数

        Vector3[] vertices = new Vector3[sideVertexCount + capVertexCount]; //頂点座標群
        Vector2[] uvs = new Vector2[vertices.Length];                       //UV座標群
        int[] triangles = new int[sideIndexCount + capIndexCount];          //三角形頂点番号群

        BuildSides(
            segmentCount,
            vertices,
            uvs,
            triangles);

        if (b_m_capEnd)
        {
            BuildCap(
                segmentCount,
                sideVertexCount,
                sideIndexCount,
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
    /// Asset設定を変更せず、実行中のコーン長だけを変更します。
    /// </summary>
    public void SetRuntimeLength(float _length)
    {
        m_runtimeLength = Mathf.Max(EMinimumSize, _length);
        Rebuild();
    }

    /// <summary>
    /// 実行中の長さ変更を解除して設定値へ戻します。
    /// </summary>
    public void ClearRuntimeLength()
    {
        m_runtimeLength = -1.0f;
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
            m_mesh.name = "Spotlight Cone Generated";
            m_mesh.hideFlags = HideFlags.DontSave;
            return;
        }

        m_mesh.Clear();
    }

    /// <summary>
    /// コーンの側面を構築します。
    /// </summary>
    private void BuildSides(
        int _segmentcount,
        Vector3[] _vertices,
        Vector2[] _uvs,
        int[] _triangles)
    {
        float coneLength = GetCurrentLength();                        //現在描画するコーン長
        float radiusScale = coneLength / Mathf.Max(m_length, EMinimumSize); //元形状に対する長さ比率
        float coneRadius = m_endRadius * radiusScale;                 //角度を維持した終端半径

        for (int i = 0; i <= _segmentcount; ++i)
        {
            float normalizedPosition = i / (float)_segmentcount;       //円周上の正規化位置
            float angleRadians = normalizedPosition * EFullCircleRadians; //円周上の角度
            Vector3 rimPosition = new Vector3(
                Mathf.Cos(angleRadians) * coneRadius,
                Mathf.Sin(angleRadians) * coneRadius,
                coneLength);                                          //終端円周上の頂点
            int vertexIndex = i * ESideVerticesPerSegment;             //側面頂点の開始番号

            _vertices[vertexIndex] = Vector3.zero;
            _vertices[vertexIndex + 1] = rimPosition;
            _uvs[vertexIndex] = new Vector2(normalizedPosition, 0.0f);
            _uvs[vertexIndex + 1] = new Vector2(normalizedPosition, 1.0f);

            if (i == _segmentcount)continue;

            int triangleIndex =
                i * ESideTrianglesPerSegment * ETriangleIndices;       //側面三角形の開始番号
            _triangles[triangleIndex] = vertexIndex;
            _triangles[triangleIndex + 1] = vertexIndex + 1;
            _triangles[triangleIndex + 2] = vertexIndex + 3;
            _triangles[triangleIndex + 3] = vertexIndex;
            _triangles[triangleIndex + 4] = vertexIndex + 3;
            _triangles[triangleIndex + 5] = vertexIndex + 2;
        }
    }

    /// <summary>
    /// コーンの終端面を構築します。
    /// </summary>
    private void BuildCap(
        int _segmentcount,
        int _capstart,
        int _trianglestart,
        Vector3[] _vertices,
        Vector2[] _uvs,
        int[] _triangles)
    {
        float coneLength = GetCurrentLength();                        //現在描画するコーン長
        float radiusScale = coneLength / Mathf.Max(m_length, EMinimumSize); //元形状に対する長さ比率
        float coneRadius = m_endRadius * radiusScale;                 //角度を維持した終端半径

        _vertices[_capstart] = new Vector3(0.0f, 0.0f, coneLength);
        _uvs[_capstart] = new Vector2(EUvCenter, EUvCenter);

        for (int i = 0; i <= _segmentcount; ++i)
        {
            float normalizedPosition = i / (float)_segmentcount;       //円周上の正規化位置
            float angleRadians = normalizedPosition * EFullCircleRadians; //円周上の角度
            float cosine = Mathf.Cos(angleRadians);                    //X方向の円周位置
            float sine = Mathf.Sin(angleRadians);                      //Y方向の円周位置

            _vertices[_capstart + 1 + i] =
                new Vector3(cosine * coneRadius, sine * coneRadius, coneLength);
            _uvs[_capstart + 1 + i] =
                new Vector2(cosine * EUvCenter + EUvCenter, sine * EUvCenter + EUvCenter);

            if (i == _segmentcount)continue;

            int triangleIndex = _trianglestart + i * ETriangleIndices; //終端三角形の開始番号
            _triangles[triangleIndex] = _capstart;
            _triangles[triangleIndex + 1] = _capstart + i + 2;
            _triangles[triangleIndex + 2] = _capstart + i + 1;
        }
    }

    /// <summary>
    /// Runtime指定がある場合はその長さを、ない場合は設定値を返します。
    /// </summary>
    private float GetCurrentLength()
    {
        if (m_runtimeLength >= EMinimumSize)return m_runtimeLength;

        return m_length;
    }
}
