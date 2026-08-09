/*============================================================
*@file LightEffectBase.cs*
*@brief Composable Lightへ登録可能な全Light Effectの共通親Class*
*@author 24CU0312 久場洸太*
*@date 2026/08/07*
*============================================================*/

using UnityEngine;

/// <summary>
/// Light Effect Composerへ登録できるComponentの共通親です。
/// 新しいLight演出はこのClassを継承することで、Composerの登録対象になります。
/// </summary>
public abstract class LightEffectBase : MonoBehaviour
{
    private Light m_attachedLight; //このEffectを所有している親Prefabの実Light

    /// <summary>このEffectが参照する親の実Lightです。</summary>
    protected Light AttachedLight => m_attachedLight;

    /// <summary>
    /// ComposerがEffectを生成した直後に、所有元の実Lightを渡します。
    /// 派生Classで追加処理が必要な場合はoverrideし、baseも呼び出してください。
    /// </summary>
    public virtual void AttachToLight(Light _light)
    {
        m_attachedLight = _light;
    }
}
