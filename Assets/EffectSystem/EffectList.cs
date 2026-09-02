using UnityEngine;

/// <summary>
/// Gameplayシーンで使用できる個別エフェクトを一元管理します。
/// MusicNodeEditorの成功演出候補にもこの一覧を使用します。
/// </summary>
public sealed class EffectList : MonoBehaviour
{
    [SerializeField] private SEffectData[] m_effects = new SEffectData[0];

    public SEffectData[] Effects => m_effects;

    public bool TryGetEffect(string _effectName, out SEffectData _effect)
    {
        if (m_effects != null && !string.IsNullOrWhiteSpace(_effectName))
        {
            for (int i = 0; i < m_effects.Length; ++i)
            {
                if (string.Equals(m_effects[i].EffectName, _effectName))
                {
                    _effect = m_effects[i];
                    return true;
                }
            }
        }

        _effect = default;
        return false;
    }
}
