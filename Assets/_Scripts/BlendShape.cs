using Unity.Mathematics;
using UnityEngine;

public class BlendShape : MonoBehaviour
{

    [SerializeField]
    private SkinnedMeshRenderer m_skinnedMeshRenderer;

    float m_weight = 0.0f;

    float m_upWeight = 50.0f;

    float m_keepWeight = 0.0f;


    private void Start()
    {
        m_skinnedMeshRenderer.SetBlendShapeWeight(0, 0);
    }

    private void Update()
    {



        m_weight = Mathf.Clamp(m_weight, 0f, 100f);
        m_skinnedMeshRenderer.SetBlendShapeWeight(0, m_weight);
    }

    public void Sucsses()
    {
        m_weight += m_upWeight;
        m_weight = Mathf.Clamp(m_weight, 0f, 100f);
        m_skinnedMeshRenderer.SetBlendShapeWeight(0, m_weight);
    }

}
