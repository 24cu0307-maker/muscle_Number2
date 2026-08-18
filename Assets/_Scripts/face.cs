

using System.Collections.Generic;
using UnityEngine;

public class Face : MonoBehaviour
{
    [Range(-2.0f, 2.0f)]
    [SerializeField]
    private float faceMinX = -0.25f;

    [Range(-2.0f, 2.0f)]
    [SerializeField]
    private float faceMaxX = 0.25f;

    [Range(-2.0f, 2.0f)]
    [SerializeField]
    private float faceMinY = 1.50f;

    [Range(-2.0f, 2.0f)]
    [SerializeField]
    private float faceMaxY = 1.72f;

    [Range(-2.0f, 2.0f)]
    [SerializeField]
    private float faceMinZ = 0.05f;

    // 人型モデルのSkinnedMeshRenderer
    [SerializeField] private SkinnedMeshRenderer renderer;

    // 顔に貼り付けるPNG画像
    [SerializeField] private Texture2D faceTexture;

    // 元の人型モデルのMesh
    private Mesh originalMesh;

    // 顔として判定された三角形の頂点番号を保存するリスト
    private List<int> faceTriangles = new List<int>();


    void Start()
    {
        renderer = GameObject.Find("Ch36").GetComponent<SkinnedMeshRenderer>();
        //faceTexture = GameObject.Find("ScreenShot").GetComponent<Texture2D>();

        // SkinnedMeshRendererが現在使用しているMeshを取得
        originalMesh = renderer.sharedMesh;

        // Meshの中から顔の三角形を探す
        FindFace();

        // 見つけた顔の三角形から顔専用Meshを作成する
        CreateFaceMesh();
    }


    //==================================================
    // 顔の三角形を探す
    //==================================================
    void FindFace()
    {
        // 元Meshに存在するすべての三角形を取得
        int[] triangles = originalMesh.triangles;

        // 元Meshのすべての頂点座標を取得
        Vector3[] vertices = originalMesh.vertices;


        // 三角形を1つずつ調べる
        // trianglesは3つの頂点番号で1つの三角形を表す
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // 三角形を構成している3頂点を取得
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];


            // 三角形の中心座標を計算
            // 3頂点の平均位置
            Vector3 center = (v0 + v1 + v2) / 3f;


            //==================================================
            // 顔かどうかを座標で判定
            //
            // x → 左右
            // y → 上下
            // z → 前後
            //
            // この範囲に入っている三角形を顔とする
            //==================================================
            bool isFace =
                center.x > faceMinX &&
                center.x < faceMaxX &&
                center.y > faceMinY &&
                center.y < faceMaxY &&
                center.z > faceMinZ;


            // 顔と判定された場合
            if (isFace)
            {
                // 三角形を構成する3つの頂点番号を保存
                faceTriangles.Add(triangles[i]);
                faceTriangles.Add(triangles[i + 1]);
                faceTriangles.Add(triangles[i + 2]);
            }
        }


        // 見つかった顔の三角形数を表示
        Debug.Log(
            "顔Triangle数 : " +
            faceTriangles.Count / 3
        );
    }


    //==================================================
    // 顔専用Meshを作成する
    //==================================================
    void CreateFaceMesh()
    {
        // 元Meshの頂点座標を取得
        Vector3[] originalVertices = originalMesh.vertices;

        // 元Meshの法線を取得
        // 法線は面がどちらを向いているかを表す
        Vector3[] originalNormals = originalMesh.normals;

        // 元MeshのBoneWeightを取得
        // RigによるMeshの変形に必要
        BoneWeight[] originalBoneWeights =
            originalMesh.boneWeights;


        //==================================================
        // 元Meshの頂点番号と
        // 新しいMeshの頂点番号を対応させるDictionary
        //==================================================
        Dictionary<int, int> vertexMap =
            new Dictionary<int, int>();


        // 新しいMeshに使用する頂点
        List<Vector3> vertices =
            new List<Vector3>();

        // 新しいMeshに使用する法線
        List<Vector3> normals =
            new List<Vector3>();

        // 新しいMeshに使用するBoneWeight
        List<BoneWeight> boneWeights =
            new List<BoneWeight>();

        // 新しいMeshの三角形情報
        List<int> triangles =
            new List<int>();


        //==================================================
        // 顔として判定した三角形を1つずつ処理
        //==================================================
        foreach (int originalIndex in faceTriangles)
        {
            // まだ新しいMeshに登録されていない頂点の場合
            if (!vertexMap.ContainsKey(originalIndex))
            {
                // 新しいMeshでの頂点番号
                int newIndex = vertices.Count;


                // 元Meshの頂点番号と
                // 新Meshの頂点番号を対応付ける
                vertexMap.Add(
                    originalIndex,
                    newIndex
                );


                // 元Meshから頂点座標をコピー
                vertices.Add(
                    originalVertices[originalIndex]
                );


                // 元Meshから法線をコピー
                normals.Add(
                    originalNormals[originalIndex]
                );


                // 元MeshからBoneWeightをコピー
                boneWeights.Add(
                    originalBoneWeights[originalIndex]
                );
            }


            // 新しいMeshで使用する頂点番号を追加
            triangles.Add(
                vertexMap[originalIndex]
            );
        }


        //==================================================
        // 新しい顔専用Meshを作成
        //==================================================
        Mesh faceMesh = new Mesh();

        // Meshの名前
        faceMesh.name = "FaceMesh";


        // 頂点を設定
        faceMesh.vertices =
            vertices.ToArray();


        // 法線を設定
        faceMesh.normals =
            normals.ToArray();


        // BoneWeightを設定
        // Rigによる顔の追従に必要
        faceMesh.boneWeights =
            boneWeights.ToArray();


        // 三角形を設定
        faceMesh.triangles =
            triangles.ToArray();


        //==================================================
        // 顔Meshの範囲を調べる
        //==================================================

        // 最初の頂点を最小・最大座標の初期値にする
        Vector3 min = vertices[0];
        Vector3 max = vertices[0];


        // 全頂点を調べて顔の範囲を求める
        foreach (Vector3 vertex in vertices)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }


        //==================================================
        // 顔用UVを作成
        //==================================================

        // 顔Meshの頂点数と同じ数のUV配列を作る
        Vector2[] uv =
            new Vector2[vertices.Count];


        // 各頂点にUV座標を設定
        for (int i = 0; i < vertices.Count; i++)
        {
            // X座標を0～1に変換
            // 左端 → 0
            // 右端 → 1
            float u = Mathf.InverseLerp(
                min.x,
                max.x,
                vertices[i].x
            );


            // Y座標を0～1に変換
            // 下端 → 0
            // 上端 → 1
            float v = Mathf.InverseLerp(
                min.y,
                max.y,
                vertices[i].y
            );


            // UVを設定
            uv[i] =
                new Vector2(u, v);
        }


        // 作成したUVをMeshに設定
        faceMesh.uv = uv;


        //==================================================
        // 元モデルと同じBone情報を使用
        //==================================================

        // RigのBoneとの対応情報を設定
        faceMesh.bindposes =
            originalMesh.bindposes;


        //==================================================
        // SkinnedMeshRendererに顔Meshを設定
        //==================================================

        // 今まで使用していた全身Meshを
        // 作成した顔専用Meshに変更
        renderer.sharedMesh =
            faceMesh;


        //==================================================
        // 顔用Materialを作成
        //==================================================

        // URP/Litシェーダーを使用したMaterialを作成
        Material faceMaterial =
            new Material(
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                )
            );


        // Materialのテクスチャに顔PNGを設定
        faceMaterial.mainTexture =
            faceTexture;


        // SkinnedMeshRendererに
        // 顔用Materialを設定
        renderer.sharedMaterial =
            faceMaterial;


        // 処理完了
        Debug.Log("顔Mesh作成完了");
    }
}




/*
public class Face : MonoBehaviour
{
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material faceMaterial;
    [SerializeField] private SkinnedMeshRenderer renderer;
    [SerializeField] private Texture2D faceTexture;
    [SerializeField] private Material bodyMaterial;

    private Vector3[] vertices;
    private int[] triangles;

    private List<int> faceTriangles = new List<int>();
    private List<int> bodyTriangles = new List<int>();

    void Start()
    {
        vertices = mesh.vertices;
        triangles = mesh.triangles;


        SetFaceMaterial();
        FindFace();
        CreateFaceSubMesh();
    }

    void FindFace()
    {
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            Vector3 center = (v0 + v1 + v2) / 3f;

            // 顔として判定できた条件
            bool isFace =
                center.x > -0.25f &&
                center.x < 0.25f &&
                center.y > 1.50f &&
                center.y < 1.72f &&
                center.z > 0.05f;

            if (isFace)
            {
                faceTriangles.Add(triangles[i]);
                faceTriangles.Add(triangles[i + 1]);
                faceTriangles.Add(triangles[i + 2]);
            }
            else
            {
                bodyTriangles.Add(triangles[i]);
                bodyTriangles.Add(triangles[i + 1]);
                bodyTriangles.Add(triangles[i + 2]);
            }
        }
    }

    void CreateFaceSubMesh()
    {
        // 元MeshのSubMeshを2つにする
        mesh.subMeshCount = 2;

        // SubMesh 0 = 体
        mesh.SetTriangles(bodyTriangles, 0);

        // SubMesh 1 = 顔
        mesh.SetTriangles(faceTriangles, 1);
        
        if (renderer == null)
        {
            Debug.LogError("SkinnedMeshRendererがありません");
            return;
        }

        // Materialを2つにする
        Material bodyMaterial = renderer.sharedMaterials[0];

        renderer.sharedMaterials = new Material[]
        {
            bodyMaterial,
            faceMaterial
        };

        Debug.Log("顔Materialを設定しました");
    }

    void SetFaceMaterial()
    {
     
        bodyMaterial = renderer.sharedMaterials[0];
     
        // PNGを設定
        faceMaterial.mainTexture = faceTexture;

        // Materialを2つにする
        renderer.sharedMaterials = new Material[]
        {
            bodyMaterial,
            faceMaterial
        };
    }
}
*/

/*
using UnityEngine;

public class face : MonoBehaviour
{
    [SerializeField] private Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //mesh = GetComponent<MeshFilter>().mesh;

        vertices = mesh.vertices;
        triangles = mesh.triangles;

        FindFace();
     
    }

    // Update is called once per frame
    void Update()
    {
    
       
    }


    void FindFace()
    {
        int faceCount = 0;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            Vector3 center = (v0 + v1 + v2) / 3f;

            // 顔の中心部分だけを取得
            bool position =
                center.x > -0.25f &&
                center.x < 0.25f &&
                center.y > 1.50f &&
                center.y < 1.72f &&
                center.z > 0.05f;

            if (position)
            {
                faceCount++;

                Debug.DrawLine(
                    transform.TransformPoint(v0),
                    transform.TransformPoint(v1),
                    Color.red,
                    100f
                );

                Debug.DrawLine(
                    transform.TransformPoint(v1),
                    transform.TransformPoint(v2),
                    Color.red,
                    100f
                );

                Debug.DrawLine(
                    transform.TransformPoint(v2),
                    transform.TransformPoint(v0),
                    Color.red,
                    100f
                );
            }
        }

        Debug.Log("顔候補 : " + faceCount);
    }
}
*/