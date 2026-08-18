using UnityEngine;

public class FaceMaterial : MonoBehaviour
{
    [SerializeField] private Mesh mesh;
    [SerializeField] private Texture2D faceTexture;
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private SkinnedMeshRenderer renderer;


    private Vector3[] vertices;
    private int[] triangles;

    void Start()
    {
        vertices = mesh.vertices;
        triangles = mesh.triangles;

        SetFaceMaterial();
    }

    void FindFace()
    {
        // ‚±‚±‚Í¡AŠç‚ğæ“¾‚Å‚«‚Ä‚¢‚éğŒ‚ğ‚»‚Ì‚Ü‚Üg‚¤
    }

    void SetFaceMaterial()
    {
        /*
        SkinnedMeshRenderer renderer =
            GetComponent<SkinnedMeshRenderer>();
        */
        bodyMaterial = renderer.sharedMaterials[0];

        // Šç—pMaterial‚ğì¬
        Material faceMaterial = new Material(bodyMaterial);

        // PNG‚ğİ’è
        faceMaterial.mainTexture = faceTexture;

        // Material‚ğ2‚Â‚É‚·‚é
        renderer.sharedMaterials = new Material[]
        {
            bodyMaterial,
            faceMaterial
        };
    }
}
