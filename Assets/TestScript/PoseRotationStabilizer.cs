using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MediaPipeなどがUpdateで設定したボーン回転をLateUpdateで平滑化します。
/// MediaPipe本体や受信処理を変更せず、モデルの細かな震えだけを抑えるための後処理です。
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class PoseRotationStabilizer : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("安定化するモデルのHumanoidSkeleton。")]
    [SerializeField] private HumanoidSkeleton m_skeleton;

    [SerializeField] private bool m_stabilizeSpine = true;
    [SerializeField] private bool m_stabilizeArms = true;
    [SerializeField] private bool m_stabilizeLegs = true;
    [SerializeField] private bool m_stabilizeHands = false;

    [Tooltip("Skeletonに含まれない追加ボーン。")]
    [SerializeField] private Transform[] m_additionalBones;

    [Header("Stabilization")]
    [Tooltip("この角度未満の変化をノイズとして無視します。大きすぎるとゆっくりした動きも止まります。")]
    [Range(0.0f, 5.0f)]
    [SerializeField] private float m_deadZoneDegrees = 0.35f;

    [Tooltip("値が半分まで追従する時間（秒）。大きいほど滑らかですが遅延が増えます。")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float m_smoothingHalfLife = 0.06f;

    [Tooltip("1秒間に回転できる最大角度。瞬間的な誤検出を抑制します。")]
    [Range(30.0f, 1440.0f)]
    [SerializeField] private float m_maxDegreesPerSecond = 720.0f;

    [Tooltip("Time.timeScaleの影響を受けずに平滑化します。")]
    [SerializeField] private bool m_useUnscaledTime = true;

    private Transform[] m_bones;
    private Quaternion[] m_smoothedRotations;
    private bool m_initialized;

    private void Awake()
    {
        RebuildBoneList();
    }

    private void OnEnable()
    {
        ResetFilter();
    }

    private void LateUpdate()
    {
        if (m_bones == null || m_bones.Length == 0) { return; }

        float deltaTime = m_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (deltaTime <= 0.0f) { return; }

        if (!m_initialized)
        {
            CaptureCurrentRotations();
            m_initialized = true;
            return;
        }

        float halfLife = Mathf.Max(0.0001f, m_smoothingHalfLife);
        float blend = 1.0f - Mathf.Exp(-Mathf.Log(2.0f) * deltaTime / halfLife);
        float maxStep = Mathf.Max(0.0f, m_maxDegreesPerSecond) * deltaTime;

        for (int i = 0; i < m_bones.Length; i++)
        {
            Transform bone = m_bones[i];
            if (bone == null) { continue; }

            Quaternion previous = m_smoothedRotations[i];
            Quaternion measured = bone.rotation;
            float change = Quaternion.Angle(previous, measured);

            if (change <= m_deadZoneDegrees)
            {
                bone.rotation = previous;
                continue;
            }

            Quaternion smoothed = Quaternion.Slerp(previous, measured, blend);
            smoothed = Quaternion.RotateTowards(previous, smoothed, maxStep);

            m_smoothedRotations[i] = smoothed;
            bone.rotation = smoothed;
        }
    }

    /// <summary>
    /// 現在の姿勢を基準にし直します。モデル切替やワープ後に呼び出してください。
    /// </summary>
    public void ResetFilter()
    {
        m_initialized = false;
    }

    /// <summary>
    /// Inspector上の対象設定を変更した場合などにボーン一覧を作り直します。
    /// </summary>
    public void RebuildBoneList()
    {
        List<Transform> bones = new List<Transform>();
        HashSet<Transform> registered = new HashSet<Transform>();

        if (m_skeleton != null)
        {
            if (m_stabilizeSpine) { AddBones(m_skeleton.playerSpline, bones, registered); }

            if (m_stabilizeArms)
            {
                AddBones(m_skeleton.playerLeftArm, bones, registered);
                AddBones(m_skeleton.playerRightArm, bones, registered);
            }

            if (m_stabilizeLegs)
            {
                AddBones(m_skeleton.playerLeftLeg, bones, registered);
                AddBones(m_skeleton.playerRightLeg, bones, registered);
            }

            if (m_stabilizeHands)
            {
                AddBones(m_skeleton.playerLeftHand, bones, registered);
                AddBones(m_skeleton.playerRightHand, bones, registered);
            }
        }

        AddBones(m_additionalBones, bones, registered);

        m_bones = bones.ToArray();
        m_smoothedRotations = new Quaternion[m_bones.Length];
        ResetFilter();
    }

    private void CaptureCurrentRotations()
    {
        for (int i = 0; i < m_bones.Length; i++)
        {
            if (m_bones[i] != null)
            {
                m_smoothedRotations[i] = m_bones[i].rotation;
            }
        }
    }

    private static void AddBones(
        Transform[] _source,
        List<Transform> _destination,
        HashSet<Transform> _registered)
    {
        if (_source == null) { return; }

        foreach (Transform bone in _source)
        {
            if (bone != null && _registered.Add(bone))
            {
                _destination.Add(bone);
            }
        }
    }
}
