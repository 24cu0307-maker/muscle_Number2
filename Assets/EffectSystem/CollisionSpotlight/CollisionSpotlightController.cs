/*━━━━━━━━━*
*@file CollisionSpotlightController.cs*
*@brief 衝突地点まで伸びるスポットライトを制御する*
*@author 24cu0312 久場洸太*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks 既存SpotlightConeとは独立したライト用*
*━━━━━━━━━*/

using UnityEngine;

/// <summary>
/// 開始点から+Z方向へRaycastし、最初の衝突地点でコーンを停止します。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpotlightConeMesh))]
public sealed class CollisionSpotlightController : MonoBehaviour
{
    private const float EMinimumDistance = 0.01f;      //表示可能な最短距離
    private const float EDefaultSurfaceOffset = 0.02f; //表面へのめり込み防止距離
    private const float ERadiansToDegrees = 2.0f * Mathf.Rad2Deg; //半角から全角へ変換する係数

    [SerializeField] private LayerMask m_collisionLayers = -1; //衝突判定対象Layer
    [Min(0.0f)]
    [SerializeField] private float m_surfaceOffset = EDefaultSurfaceOffset; //表面直前で止める距離
    [SerializeField] private QueryTriggerInteraction m_triggerInteraction =
        QueryTriggerInteraction.Ignore;                   //Triggerとの衝突方法
    [SerializeField] private Light m_spotLight;            //任意の実ライト
    [SerializeField] private bool b_m_updateInEditMode = true; //編集モードでも追従するか
    [SerializeField] private bool b_m_drawRay = true;      //Scene上に判定線を表示するか

    private SpotlightConeMesh m_coneMesh;                  //長さを変更するコーン
    private RaycastHit m_lastHit;                          //最後に検出した衝突情報
    private bool b_m_hasHit;                               //現在衝突しているか

    public bool HasHit
    {
        get
        {
            return b_m_hasHit;
        }
    }

    public Vector3 HitPoint
    {
        get
        {
            if (b_m_hasHit)return m_lastHit.point;

            return transform.position + transform.forward * GetMaximumDistance();
        }
    }

    /// <summary>
    /// 有効化時に参照を取得して長さを更新します。
    /// </summary>
    private void OnEnable()
    {
        CacheReferences();
        RefreshCollision();
    }

    /// <summary>
    /// 無効化時にコーンを元の長さへ戻します。
    /// </summary>
    private void OnDisable()
    {
        if (m_coneMesh == null)return;

        m_coneMesh.ClearRuntimeLength();
    }

    /// <summary>
    /// Inspector変更時に衝突状態を更新します。
    /// </summary>
    private void OnValidate()
    {
        CacheReferences();
        RefreshCollision();
    }

    /// <summary>
    /// 再生中、または編集モード追従が有効な場合に毎フレーム更新します。
    /// </summary>
    private void LateUpdate()
    {
        if (!Application.isPlaying && !b_m_updateInEditMode)return;

        RefreshCollision();
    }

    /// <summary>
    /// 現在の位置と向きから衝突距離を再計算します。
    /// </summary>
    [ContextMenu("Refresh Collision")]
    public void RefreshCollision()
    {
        CacheReferences();
        if (m_coneMesh == null)return;
        if (!gameObject.scene.IsValid())return;

        float maximumDistance = GetMaximumDistance();      //衝突しない場合の最大距離
        b_m_hasHit = Physics.Raycast(
            transform.position,
            transform.forward,
            out m_lastHit,
            maximumDistance,
            m_collisionLayers,
            m_triggerInteraction);

        float displayDistance = maximumDistance;           //実際に表示するコーン長
        if (b_m_hasHit)
        {
            displayDistance = Mathf.Max(
                EMinimumDistance,
                m_lastHit.distance - m_surfaceOffset);
        }

        m_coneMesh.SetRuntimeLength(displayDistance);
        UpdateSpotLight(displayDistance);
    }

    /// <summary>
    /// SceneビューへRaycastと衝突地点を描画します。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!b_m_drawRay)return;

        Gizmos.color = b_m_hasHit ? Color.green : Color.yellow;
        Vector3 endPosition = b_m_hasHit
            ? m_lastHit.point
            : transform.position + transform.forward * GetMaximumDistance(); //判定線の終点
        Gizmos.DrawLine(transform.position, endPosition);

        if (!b_m_hasHit)return;

        Gizmos.DrawSphere(m_lastHit.point, EDefaultSurfaceOffset);
    }

    /// <summary>
    /// 使用するコンポーネント参照を取得します。
    /// </summary>
    private void CacheReferences()
    {
        if (m_coneMesh == null)
        {
            m_coneMesh = GetComponent<SpotlightConeMesh>();
        }

        if (m_spotLight == null)
        {
            m_spotLight = GetComponent<Light>();
        }
    }

    /// <summary>
    /// コーンに設定されている最大照射距離を取得します。
    /// </summary>
    private float GetMaximumDistance()
    {
        if (m_coneMesh == null)return EMinimumDistance;

        return Mathf.Max(EMinimumDistance, m_coneMesh.ConfiguredLength);
    }

    /// <summary>
    /// 実Lightが設定されている場合に距離と角度をコーンへ同期します。
    /// </summary>
    private void UpdateSpotLight(float _distance)
    {
        if (m_spotLight == null)return;

        float configuredLength =
            Mathf.Max(EMinimumDistance, m_coneMesh.ConfiguredLength); //元のコーン長
        float halfAngleRadians =
            Mathf.Atan(m_coneMesh.ConfiguredEndRadius / configuredLength); //コーンの半角

        m_spotLight.type = LightType.Spot;
        m_spotLight.range = _distance;
        m_spotLight.spotAngle = halfAngleRadians * ERadiansToDegrees;
    }
}
