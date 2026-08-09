using UnityEditor;
using UnityEngine;

/// <summary>
/// Audience Prefab上で左右のペンライト生成位置と向きを視覚調整します。
/// Cyanが左手、Magentaが右手です。
/// </summary>
[CustomEditor(typeof(AudiencePenlight))]
public sealed class AudiencePenlightEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Create / Repair Hand Anchorsで左右の基準点を作成し、"
            + "Prefabビューの移動・回転ハンドルで手元へ合わせてください。",
            MessageType.Info);

        if (GUILayout.Button("Create / Repair Hand Anchors"))
        {
            CreateOrRepairAnchors((AudiencePenlight)target);
        }
    }

    private void OnSceneGUI()
    {
        AudiencePenlight penlight = (AudiencePenlight)target;
        DrawAnchorHandle(penlight.LeftHandAnchor, Color.cyan, "Left Penlight");
        DrawAnchorHandle(penlight.RightHandAnchor, Color.magenta, "Right Penlight");
    }

    /// <summary>Anchorの位置と向きをSceneビュー上のHandleで編集します。</summary>
    private static void DrawAnchorHandle(
        Transform _anchor,
        Color _color,
        string _label)
    {
        if (_anchor == null)return;
        Handles.color = _color;
        Handles.Label(_anchor.position, _label);

        EditorGUI.BeginChangeCheck();
        Vector3 position = Handles.PositionHandle(_anchor.position, _anchor.rotation);
        Quaternion rotation = Handles.RotationHandle(_anchor.rotation, position);
        if (!EditorGUI.EndChangeCheck())return;

        Undo.RecordObject(_anchor, "Move Penlight Hand Anchor");
        _anchor.SetPositionAndRotation(position, rotation);
        EditorUtility.SetDirty(_anchor);
    }

    /// <summary>
    /// 未設定側だけを観客Boundsの左右上部へ作成し、既存の手動調整は維持します。
    /// </summary>
    private static void CreateOrRepairAnchors(AudiencePenlight _penlight)
    {
        Renderer[] renderers = _penlight.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = renderers.Length > 0
            ? renderers[0].bounds
            : new Bounds(_penlight.transform.position + Vector3.up, Vector3.one * 2.0f);
        for (int i = 1; i < renderers.Length; ++i)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Transform left = _penlight.LeftHandAnchor;
        Transform right = _penlight.RightHandAnchor;
        if (left == null)
        {
            left = CreateAnchor(
                _penlight.transform,
                "PenlightLeftHandAnchor",
                bounds.center - _penlight.transform.right * bounds.extents.x * 0.72f
                    + _penlight.transform.up * bounds.extents.y * 0.25f);
        }
        if (right == null)
        {
            right = CreateAnchor(
                _penlight.transform,
                "PenlightRightHandAnchor",
                bounds.center + _penlight.transform.right * bounds.extents.x * 0.72f
                    + _penlight.transform.up * bounds.extents.y * 0.25f);
        }

        Undo.RecordObject(_penlight, "Assign Penlight Hand Anchors");
        _penlight.SetHandAnchors(left, right);
        EditorUtility.SetDirty(_penlight);
        PrefabUtility.RecordPrefabInstancePropertyModifications(_penlight);
    }

    private static Transform CreateAnchor(
        Transform _parent,
        string _name,
        Vector3 _worldPosition)
    {
        GameObject anchorObject = new GameObject(_name);
        Undo.RegisterCreatedObjectUndo(anchorObject, "Create Penlight Hand Anchor");
        Transform anchor = anchorObject.transform;
        anchor.SetParent(_parent, true);
        anchor.position = _worldPosition;
        anchor.rotation = Quaternion.LookRotation(_parent.up, _parent.forward);
        return anchor;
    }
}
