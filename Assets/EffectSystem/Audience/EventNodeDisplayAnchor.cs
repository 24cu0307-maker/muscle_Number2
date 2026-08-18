/*━━━━━━━━━*
*@file EventNodeDisplayAnchor.cs*
*@brief Event Node用World Space Canvasの手動配置位置を表示する*
*@author 24cu0312 久場洸太*
*@date 2026/08/03*
*最終更新日 2026/08/03*
*@remarks Scene View上で左・中央・右の位置調整に使用する*
*━━━━━━━━━*/

using UnityEngine;

/// <summary>
/// 特殊Event Nodeを表示する手動配置Anchorです。
/// </summary>
public sealed class EventNodeDisplayAnchor : MonoBehaviour
{
    [SerializeField] private int m_nodeIndex; //左0・中央1・右2
    [SerializeField] private float m_gizmoSize = 1.2f; //Scene表示寸法

    /// <summary>
    /// Scene ViewへNode配置位置と種類色を表示します。
    /// </summary>
    private void OnDrawGizmos()
    {
        Color[] colors =
        {
            new Color(0.1f, 0.8f, 1.0f, 0.9f),
            new Color(1.0f, 0.85f, 0.1f, 0.9f),
            new Color(1.0f, 0.2f, 0.75f, 0.9f)
        }; //三種類の確認色
        int index = Mathf.Clamp(m_nodeIndex, 0, colors.Length - 1); //安全な種類番号
        Gizmos.color = colors[index];
        Gizmos.DrawWireCube(
            transform.position,
            Vector3.one * Mathf.Max(0.1f, m_gizmoSize));
        Gizmos.DrawLine(
            transform.position,
            transform.position + Vector3.up * m_gizmoSize);
    }
}
