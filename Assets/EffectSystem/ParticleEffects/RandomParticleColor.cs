/*━━━━━━━━━*
*@file RandomParticleColor.cs*
*@brief Particleごとにランダムな色を設定する*
*@author 24CU0000 Name*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks 紙吹雪用*
*━━━━━━━━━*/

using UnityEngine;

/// <summary>
/// 登録されたカラーパレットからParticleごとに一色を選択します。
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public sealed class RandomParticleColor : MonoBehaviour
{
    private const int EEmptyColorCount = 0;               //色が未登録の状態

    [SerializeField] private Color[] m_colors =
    {
        new Color(1.0f, 0.05f, 0.08f, 1.0f),
        new Color(1.0f, 0.35f, 0.02f, 1.0f),
        new Color(1.0f, 0.92f, 0.02f, 1.0f),
        new Color(0.08f, 1.0f, 0.22f, 1.0f),
        new Color(0.02f, 1.0f, 1.0f, 1.0f),
        new Color(0.04f, 0.25f, 1.0f, 1.0f),
        new Color(0.62f, 0.04f, 1.0f, 1.0f),
        new Color(1.0f, 0.03f, 0.58f, 1.0f)
    }; //紙吹雪へ使用する色群

    private ParticleSystem m_particles;                   //操作対象ParticleSystem
    private ParticleSystem.Particle[] m_particleBuffer;   //Particle取得用バッファ

    /// <summary>
    /// 有効化時に必要な参照とバッファを準備します。
    /// </summary>
    private void OnEnable()
    {
        PrepareBuffer();
    }

    /// <summary>
    /// 表示中の各Particleへ固定されたランダム色を設定します。
    /// </summary>
    private void LateUpdate()
    {
        if (m_colors == null || m_colors.Length == EEmptyColorCount)return;

        PrepareBuffer();
        if (m_particles == null || m_particleBuffer == null)return;

        int particleCount = m_particles.GetParticles(m_particleBuffer); //取得したParticle数
        for (int i = 0; i < particleCount; ++i)
        {
            int colorIndex = (int)(m_particleBuffer[i].randomSeed % m_colors.Length); //Particle固有の色番号
            m_particleBuffer[i].startColor = m_colors[colorIndex];
        }

        m_particles.SetParticles(m_particleBuffer, particleCount);
    }

    /// <summary>
    /// Editor生成処理から使用するカラーパレットを設定します。
    /// </summary>
    public void SetColors(Color[] _colors)
    {
        m_colors = _colors;
    }

    /// <summary>
    /// ParticleSystem参照と再利用バッファを準備します。
    /// </summary>
    private void PrepareBuffer()
    {
        if (m_particles == null)
        {
            m_particles = GetComponent<ParticleSystem>();
        }

        if (m_particles == null)return;

        int requiredCount = m_particles.main.maxParticles; //必要なバッファ数
        if (m_particleBuffer != null && m_particleBuffer.Length == requiredCount)return;

        m_particleBuffer = new ParticleSystem.Particle[requiredCount];
    }
}
