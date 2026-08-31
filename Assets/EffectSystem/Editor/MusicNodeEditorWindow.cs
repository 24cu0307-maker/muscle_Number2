/*━━━━━━━━━*
*@file MusicNodeEditorWindow.cs*
*@brief BGM波形上でPose Nodeの時間とIDを視覚編集する*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks PoseTimeFlow.csvへ書き出して既存Gameと接続*
*━━━━━━━━━*/

using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

/// <summary>
/// Audio波形上へPose Nodeを配置、移動、編集します。
/// </summary>
public sealed class MusicNodeEditorWindow : EditorWindow
{
    private enum ETimelinePlacementMode
    {
        PoseNode,
        ConditionalEffect
    }

    private const string ECsvPath = "Assets/Resources/PoseTimeFlow.csv"; //既存CSV
    private const string EUnifiedCsvPath =
        "Assets/Resources/MusicTimeline.csv"; //Poseと条件付き演出の統一CSV
    private const float ETimelineHeight = 180.0f; //波形領域高さ
    private const float ENodeWidth = 2.0f; //Node線の表示幅
    private const float ENodeHitWidth = 10.0f; //Nodeを選択できる幅
    private const float ENodeListHeight = 190.0f; //Node一覧の高さ
    private const float ENodeSelectColumnWidth = 28.0f; //選択Button列幅
    private const float ENodeNumberColumnWidth = 55.0f; //番号列幅
    private const float ENodeTimeColumnWidth = 75.0f; //時間列幅
    private const float ENodePoseColumnWidth = 65.0f; //Pose ID列幅
    private const float ESuccessEffectColumnWidth = 420.0f; //成功演出列幅
    private const float ENodeDeleteColumnWidth = 28.0f; //削除Button列幅
    private const float ECompactWindowHeight = 82.0f; //簡易表示時の高さ
    private const float EMinimumRestoreHeight = 520.0f; //通常表示へ戻す最低高さ
    private const float EMinimumDuration = 1.0f; //最短表示秒数
    private const int EWaveSampleCount = 512; //波形描画Sample数

    [SerializeField] private MusicNodeSequence m_sequence; //編集中Data
    [SerializeField] private bool b_m_compactView; //簡易表示中か
    [SerializeField] private float m_restoreWindowHeight =
        EMinimumRestoreHeight; //通常表示時の高さ
    private int m_selectedIndex = -1; //選択Node番号
    private Vector2 m_mainScrollPosition; //編集領域全体のScroll位置
    private Vector2 m_nodeListScrollPosition; //Node一覧Scroll位置
    private bool b_m_nodeListFoldout = true; //Node一覧の展開状態
    private AudioClip m_cachedWaveformClip; //波形Cache対象
    private float[] m_waveformSamples; //WAVから取得した波形Cache
    private double m_previewPositionSeconds; //Editor試聴位置
    private bool b_m_previewPlaying; //Editor試聴中か
    private bool b_m_previewPaused; //Editor試聴を一時停止中か
    private readonly List<int> m_eventSelectedIndicesList = new List<int>(); //Event別選択Node
    private readonly List<Vector2> m_eventScrollPositionsList = new List<Vector2>(); //Event別一覧Scroll
    private readonly List<bool> b_m_eventFoldoutsList = new List<bool>(); //Event別展開状態
    private bool b_m_conditionalEffectsFoldout = true; //条件付き演出一覧の展開状態
    private readonly Dictionary<ConditionalEffectEvent, bool>
        b_m_conditionalEventFoldouts =
            new Dictionary<ConditionalEffectEvent, bool>(); //条件Event別展開状態
    private readonly Dictionary<ConditionalEffectEntry, bool>
        b_m_conditionalEntryFoldouts =
            new Dictionary<ConditionalEffectEntry, bool>(); //Effect別展開状態
    private ETimelinePlacementMode m_timelinePlacementMode; //Timeline配置対象
    private int m_selectedConditionalEffectIndex = -1; //選択中の条件付き演出

    /// <summary>
    /// Music Node Editorを開きます。
    /// </summary>
    [MenuItem("Tools/Effect System/Music Node Editor", priority = 150)]
    private static void OpenWindow()
    {
        GetWindow<MusicNodeEditorWindow>("Music Node Editor");
    }

    /// <summary>
    /// Editor試聴中の再描画処理を登録します。
    /// </summary>
    private void OnEnable()
    {
        EditorApplication.update += UpdatePreview;
    }

    /// <summary>
    /// Windowを閉じる際に試聴を停止します。
    /// </summary>
    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;
        StopPreview();
    }

    /// <summary>
    /// Data選択、波形、Node編集UIを表示します。
    /// </summary>
    private void OnGUI()
    {
        DrawWindowModeControls();
        m_sequence = EditorGUILayout.ObjectField(
            "Sequence",
            m_sequence,
            typeof(MusicNodeSequence),
            false) as MusicNodeSequence;
        if (m_sequence == null)
        {
            EditorGUILayout.HelpBox(
                "Projectで Create > Effect System > Music Node Sequence "
                + "を作成して指定してください。",
                MessageType.Info);
            return;
        }

        if (b_m_compactView)
        {
            EditorGUILayout.LabelField(
                $"Preview Time: {m_previewPositionSeconds:F2}s");
            return;
        }

        m_mainScrollPosition = EditorGUILayout.BeginScrollView(
            m_mainScrollPosition,
            false,
            true); //複数Event展開時も最下部まで操作できる全体Scroll

        AudioClip clip = EditorGUILayout.ObjectField(
            "BGM",
            m_sequence.BgmClip,
            typeof(AudioClip),
            false) as AudioClip;
        if (clip != m_sequence.BgmClip)
        {
            Undo.RecordObject(m_sequence, "Change BGM");
            m_sequence.BgmClip = clip;
            EditorUtility.SetDirty(m_sequence);
        }

        if (m_sequence.BgmClip == null)
        {
            EditorGUI.BeginChangeCheck();
            float manualDuration = EditorGUILayout.FloatField(
                "Timeline Duration (Seconds)",
                m_sequence.ManualTimelineDuration);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_sequence, "Change Manual Timeline Duration");
                m_sequence.ManualTimelineDuration = manualDuration;
                EditorUtility.SetDirty(m_sequence);
            }

            EditorGUILayout.HelpBox(
                "No BGM is assigned. This duration is shared by the normal and event timelines.",
                MessageType.Info);
        }

        DrawPreviewControls();
        m_timelinePlacementMode = (ETimelinePlacementMode)GUILayout.Toolbar(
            (int)m_timelinePlacementMode,
            new[] { "Pose Node", "Conditional Effect" });
        Rect timelineRect = GUILayoutUtility.GetRect(
            100.0f,
            ETimelineHeight,
            GUILayout.ExpandWidth(true)); //波形表示範囲
        DrawTimeline(timelineRect);
        HandleTimelineInput(timelineRect);
        DrawConditionalEffectList();
        DrawNodeList();
        DrawEventSceneList();
        DrawDataControls();
        EditorGUILayout.EndScrollView();
    }

    /// <summary>並び替えとCSV入出力を用途別に整列して表示します。</summary>
    private void DrawDataControls()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Data Controls", EditorStyles.boldLabel);
        if (GUILayout.Button("Sort By Time"))
        {
            SortEvents();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("MusicTimeline.csv (Pose + Effect)");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Import MusicTimeline.csv"))
        {
            ImportUnifiedCsv();
        }

        if (GUILayout.Button("Export MusicTimeline.csv"))
        {
            ExportUnifiedCsv();
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("PoseTimeFlow.csv (Legacy Pose Only)");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Import PoseTimeFlow.csv"))
        {
            ImportCsv();
        }

        if (GUILayout.Button("Export PoseTimeFlow.csv"))
        {
            ExportCsv();
        }

        GUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    /// <summary>Poseと条件付き演出をBGM絶対時刻の統一CSVへ保存します。</summary>
    private void ExportUnifiedCsv()
    {
        SortEvents();
        StringBuilder csv = new StringBuilder();
        csv.AppendLine(
            "Type,Group,Time,NodeNumber,PoseID,EventName,SuccessEffects,"
            + "Condition,TriggerOnce,RepeatInterval,EffectName,Delay,"
            + "OverridePosition,PositionX,PositionY,PositionZ");
        for (int i = 0; i < m_sequence.EventsList.Count; ++i)
        {
            SMusicNodeEvent node = m_sequence.EventsList[i];
            csv.Append("Pose,,");
            csv.Append(node.m_time.ToString("0.###", CultureInfo.InvariantCulture));
            csv.Append($",{node.m_nodeNumber},{node.m_poseId},");
            csv.Append(EscapeCsv(node.m_eventName));
            csv.Append(',');
            csv.Append(EscapeCsv(node.m_successEffectNames));
            csv.AppendLine(",,,,,,,,,");
        }

        for (int i = 0; i < m_sequence.ConditionalEffectsList.Count; ++i)
        {
            ConditionalEffectEvent effectEvent =
                m_sequence.ConditionalEffectsList[i];
            if (effectEvent == null)continue;
            int entryCount = Mathf.Max(1, effectEvent.m_effectsList.Count);
            for (int j = 0; j < entryCount; ++j)
            {
                ConditionalEffectEntry entry = j < effectEvent.m_effectsList.Count
                    ? effectEvent.m_effectsList[j]
                    : null;
                csv.Append($"Effect,{i},");
                csv.Append(effectEvent.m_timelineTime.ToString(
                    "0.###", CultureInfo.InvariantCulture));
                csv.Append(",,,");
                csv.Append(EscapeCsv(effectEvent.m_eventName));
                csv.Append(",,");
                csv.Append(EscapeCsv(effectEvent.m_conditionExpression));
                csv.Append(effectEvent.b_m_triggerOnce ? ",1," : ",0,");
                csv.Append(effectEvent.m_repeatIntervalSeconds.ToString(
                    "0.###", CultureInfo.InvariantCulture));
                csv.Append(',');
                csv.Append(EscapeCsv(entry?.m_effectName));
                csv.Append(',');
                csv.Append((entry?.m_delaySeconds ?? 0.0f).ToString(
                    "0.###", CultureInfo.InvariantCulture));
                csv.Append(entry != null && entry.b_m_overridePosition
                    ? ",1," : ",0,");
                Vector3 position = entry?.m_position ?? Vector3.zero;
                csv.Append(position.x.ToString("0.###", CultureInfo.InvariantCulture));
                csv.Append(',');
                csv.Append(position.y.ToString("0.###", CultureInfo.InvariantCulture));
                csv.Append(',');
                csv.AppendLine(position.z.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        File.WriteAllText(EUnifiedCsvPath, csv.ToString(), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(EUnifiedCsvPath);
        Debug.Log($"統一Timelineを{EUnifiedCsvPath}へ書き出しました。");
    }

    /// <summary>統一CSVからPoseと条件付き演出をSequenceへ読み込みます。</summary>
    private void ImportUnifiedCsv()
    {
        if (!File.Exists(EUnifiedCsvPath))
        {
            Debug.LogWarning($"{EUnifiedCsvPath}が見つかりません。");
            return;
        }

        List<SMusicNodeEvent> nodes = new List<SMusicNodeEvent>();
        Dictionary<string, ConditionalEffectEvent> effects =
            new Dictionary<string, ConditionalEffectEvent>();
        string[] lines = File.ReadAllLines(EUnifiedCsvPath);
        for (int i = 1; i < lines.Length; ++i)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))continue;
            List<string> columns = ParseCsvLine(lines[i]);
            if (columns.Count < 16)continue;
            if (string.Equals(columns[0], "Pose", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseFloat(columns[2], out float time)
                    || !int.TryParse(columns[3], out int number)
                    || !int.TryParse(columns[4], out int poseId))continue;
                nodes.Add(new SMusicNodeEvent
                {
                    m_time = Mathf.Max(0.0f, time),
                    m_nodeNumber = Mathf.Max(1, number),
                    m_poseId = poseId,
                    m_eventName = columns[5],
                    m_successEffectNames = columns[6]
                });
                continue;
            }
            if (!string.Equals(columns[0], "Effect", StringComparison.OrdinalIgnoreCase))continue;

            string group = columns[1];
            if (!effects.TryGetValue(group, out ConditionalEffectEvent effectEvent))
            {
                TryParseFloat(columns[2], out float time);
                TryParseFloat(columns[9], out float repeat);
                effectEvent = new ConditionalEffectEvent
                {
                    m_eventName = columns[5],
                    b_m_enabled = true,
                    b_m_useTimelineTime = true,
                    m_timelineTime = Mathf.Max(0.0f, time),
                    m_conditionExpression = string.IsNullOrWhiteSpace(columns[7])
                        ? "time >= 0" : columns[7],
                    b_m_triggerOnce = columns[8] != "0",
                    m_repeatIntervalSeconds = Mathf.Max(0.0f, repeat)
                };
                effects.Add(group, effectEvent);
            }
            if (string.IsNullOrWhiteSpace(columns[10]))continue;
            TryParseFloat(columns[11], out float delay);
            TryParseFloat(columns[13], out float x);
            TryParseFloat(columns[14], out float y);
            TryParseFloat(columns[15], out float z);
            effectEvent.m_effectsList.Add(new ConditionalEffectEntry
            {
                m_effectName = columns[10],
                m_delaySeconds = Mathf.Max(0.0f, delay),
                b_m_overridePosition = columns[12] != "0",
                m_position = new Vector3(x, y, z)
            });
        }

        Undo.RecordObject(m_sequence, "Import Unified Music Timeline");
        m_sequence.EventsList.Clear();
        m_sequence.EventsList.AddRange(nodes);
        m_sequence.ConditionalEffectsList.Clear();
        m_sequence.ConditionalEffectsList.AddRange(effects.Values);
        SortEvents();
        EditorUtility.SetDirty(m_sequence);
    }

    private static bool TryParseFloat(string _value, out float _result)
    {
        return float.TryParse(
            _value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out _result);
    }

    /// <summary>
    /// 条件式、複数Effect、個別の待ち時間とWorld座標をまとめて編集します。
    /// </summary>
    private void DrawConditionalEffectList()
    {
        EditorGUILayout.Space();
        b_m_conditionalEffectsFoldout = EditorGUILayout.Foldout(
            b_m_conditionalEffectsFoldout,
            $"Conditional Effects ({m_sequence.ConditionalEffectsList.Count})",
            true);
        if (!b_m_conditionalEffectsFoldout)return;

        EditorGUILayout.HelpBox(
            "Conditional Effectsはポーズ成功とは無関係に、毎フレーム条件を評価します。\n"
            + "上のConditional Effectモードを選び、Timelineをクリックして配置できます。\n"
            + "使用可能: >, >=, <, <=, ==, !=, &, |, ( )\n"
            + "例: (time > 50 | score > 10000) & voltage > 50\n"
            + "time / score / voltage のほか、管理クラスへ登録した任意の値も使用できます。",
            MessageType.Info);

        int deleteEventIndex = -1;
        for (int i = 0; i < m_sequence.ConditionalEffectsList.Count; ++i)
        {
            ConditionalEffectEvent effectEvent =
                m_sequence.ConditionalEffectsList[i]; //編集対象条件
            if (effectEvent == null)continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            Color previousBackgroundColor = GUI.backgroundColor;
            if (i == m_selectedConditionalEffectIndex)
            {
                GUI.backgroundColor = Color.yellow;
            }
            if (GUILayout.Button("●", GUILayout.Width(28.0f)))
            {
                m_selectedConditionalEffectIndex = i;
                m_timelinePlacementMode = ETimelinePlacementMode.ConditionalEffect;
            }
            GUI.backgroundColor = previousBackgroundColor;
            bool b_eventFoldout = GetConditionalEventFoldout(effectEvent);
            b_eventFoldout = EditorGUILayout.Foldout(
                b_eventFoldout,
                string.IsNullOrWhiteSpace(effectEvent.m_eventName)
                    ? $"Conditional Effect {i + 1}"
                    : effectEvent.m_eventName,
                true);
            b_m_conditionalEventFoldouts[effectEvent] = b_eventFoldout;
            EditorGUI.BeginChangeCheck();
            effectEvent.b_m_enabled = EditorGUILayout.Toggle(
                effectEvent.b_m_enabled,
                GUILayout.Width(20.0f));
            if (GUILayout.Button("×", GUILayout.Width(28.0f)))
            {
                deleteEventIndex = i;
            }
            GUILayout.EndHorizontal();

            if (b_eventFoldout)
            {
                effectEvent.m_eventName = EditorGUILayout.TextField(
                    "Event Name",
                    effectEvent.m_eventName);
                effectEvent.b_m_useTimelineTime = EditorGUILayout.Toggle(
                    "Use Timeline Time",
                    effectEvent.b_m_useTimelineTime);
                using (new EditorGUI.DisabledScope(!effectEvent.b_m_useTimelineTime))
                {
                    effectEvent.m_timelineTime = Mathf.Max(
                        0.0f,
                        EditorGUILayout.FloatField(
                            "Timeline Time",
                            effectEvent.m_timelineTime));
                }
                effectEvent.m_conditionExpression = EditorGUILayout.TextField(
                    "Condition",
                    effectEvent.m_conditionExpression);
                effectEvent.b_m_triggerOnce = EditorGUILayout.Toggle(
                    "Trigger Once",
                    effectEvent.b_m_triggerOnce);
                using (new EditorGUI.DisabledScope(effectEvent.b_m_triggerOnce))
                {
                    effectEvent.m_repeatIntervalSeconds = Mathf.Max(
                        0.0f,
                        EditorGUILayout.FloatField(
                            "Repeat Interval",
                            effectEvent.m_repeatIntervalSeconds));
                }

                DrawConditionalEffectEntries(effectEvent);
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_sequence, "Edit Conditional Effect");
                EditorUtility.SetDirty(m_sequence);
            }
            EditorGUILayout.EndVertical();
        }

        if (deleteEventIndex >= 0)
        {
            Undo.RecordObject(m_sequence, "Delete Conditional Effect");
            m_sequence.ConditionalEffectsList.RemoveAt(deleteEventIndex);
            if (m_selectedConditionalEffectIndex == deleteEventIndex)
            {
                m_selectedConditionalEffectIndex = -1;
            }
            else if (m_selectedConditionalEffectIndex > deleteEventIndex)
            {
                --m_selectedConditionalEffectIndex;
            }
            EditorUtility.SetDirty(m_sequence);
        }

        if (GUILayout.Button("Add Conditional Effect"))
        {
            Undo.RecordObject(m_sequence, "Add Conditional Effect");
            ConditionalEffectEvent effectEvent = new ConditionalEffectEvent();
            effectEvent.m_effectsList.Add(new ConditionalEffectEntry());
            m_sequence.ConditionalEffectsList.Add(effectEvent);
            EditorUtility.SetDirty(m_sequence);
        }
    }

    /// <summary>一つの条件に紐づく複数Effectを編集します。</summary>
    private void DrawConditionalEffectEntries(ConditionalEffectEvent _effectEvent)
    {
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
        int deleteEffectIndex = -1;
        for (int i = 0; i < _effectEvent.m_effectsList.Count; ++i)
        {
            ConditionalEffectEntry entry = _effectEvent.m_effectsList[i];
            if (entry == null)continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            bool b_entryFoldout = GetConditionalEntryFoldout(entry);
            string effectLabel = string.IsNullOrWhiteSpace(entry.m_effectName)
                ? $"Effect {i + 1}"
                : $"Effect {i + 1}: {entry.m_effectName}";
            b_entryFoldout = EditorGUILayout.Foldout(
                b_entryFoldout,
                effectLabel,
                true);
            b_m_conditionalEntryFoldouts[entry] = b_entryFoldout;
            if (GUILayout.Button("×", GUILayout.Width(28.0f)))
            {
                deleteEffectIndex = i;
            }
            GUILayout.EndHorizontal();

            if (b_entryFoldout)
            {
                int effectIndex = i;
                DrawSuccessEffectSelector(
                    entry.m_effectName,
                    selectedEffectName =>
                    {
                        Undo.RecordObject(m_sequence, "Select Conditional Effect");
                        _effectEvent.m_effectsList[effectIndex].m_effectName =
                            selectedEffectName;
                        EditorUtility.SetDirty(m_sequence);
                    });

                entry.m_delaySeconds = Mathf.Max(
                    0.0f,
                    EditorGUILayout.FloatField(
                        "Delay Seconds",
                        entry.m_delaySeconds));
                entry.b_m_overridePosition = EditorGUILayout.Toggle(
                    "Override Position",
                    entry.b_m_overridePosition);
                using (new EditorGUI.DisabledScope(!entry.b_m_overridePosition))
                {
                    entry.m_position = EditorGUILayout.Vector3Field(
                        "World Position",
                        entry.m_position);
                }
            }
            EditorGUILayout.EndVertical();
        }

        if (deleteEffectIndex >= 0)
        {
            Undo.RecordObject(m_sequence, "Delete Conditional Effect Entry");
            _effectEvent.m_effectsList.RemoveAt(deleteEffectIndex);
            EditorUtility.SetDirty(m_sequence);
        }
        if (GUILayout.Button("Add Effect", GUILayout.Width(120.0f)))
        {
            Undo.RecordObject(m_sequence, "Add Conditional Effect Entry");
            _effectEvent.m_effectsList.Add(new ConditionalEffectEntry());
            EditorUtility.SetDirty(m_sequence);
        }
    }

    /// <summary>条件Eventの展開状態を取得します。</summary>
    private bool GetConditionalEventFoldout(ConditionalEffectEvent _effectEvent)
    {
        if (b_m_conditionalEventFoldouts.TryGetValue(
            _effectEvent,
            out bool b_foldout))return b_foldout;

        b_m_conditionalEventFoldouts.Add(_effectEvent, true);
        return true;
    }

    /// <summary>個別Effectの展開状態を取得します。</summary>
    private bool GetConditionalEntryFoldout(ConditionalEffectEntry _entry)
    {
        if (b_m_conditionalEntryFoldouts.TryGetValue(
            _entry,
            out bool b_foldout))return b_foldout;

        b_m_conditionalEntryFoldouts.Add(_entry, true);
        return true;
    }

    /// <summary>
    /// Node Editorの簡易表示と通常表示を切り替えます。
    /// </summary>
    private void DrawWindowModeControls()
    {
        string buttonLabel = b_m_compactView
            ? "Restore Editor"
            : "Compact Editor"; //切替Button名
        if (!GUILayout.Button(buttonLabel))return;

        Rect windowPosition = position; //現在のWindow位置と大きさ
        if (!b_m_compactView)
        {
            m_restoreWindowHeight = Mathf.Max(
                EMinimumRestoreHeight,
                windowPosition.height);
            b_m_compactView = true;
            windowPosition.height = ECompactWindowHeight;
        }
        else
        {
            b_m_compactView = false;
            windowPosition.height = Mathf.Max(
                EMinimumRestoreHeight,
                m_restoreWindowHeight);
        }

        position = windowPosition;
        Repaint();
    }

    /// <summary>
    /// BGMの再生、一時停止、停止、再生位置操作を表示します。
    /// </summary>
    private void DrawPreviewControls()
    {
        AudioClip clip = m_sequence.BgmClip; //試聴対象
        EditorGUI.BeginDisabledGroup(clip == null);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Play"))
        {
            PlayPreview(clip);
        }

        if (GUILayout.Button("Pause / Resume"))
        {
            TogglePausePreview();
        }

        if (GUILayout.Button("Stop"))
        {
            StopPreview();
        }

        GUILayout.EndHorizontal();
        double duration = clip != null
            ? clip.length
            : EMinimumDuration; //試聴可能時間
        double selectedPosition = EditorGUILayout.Slider(
            "Preview Time",
            (float)m_previewPositionSeconds,
            0.0f,
            (float)duration);
        if (!Mathf.Approximately(
            (float)selectedPosition,
            (float)m_previewPositionSeconds))
        {
            m_previewPositionSeconds = selectedPosition;
            SetPreviewPosition(clip, m_previewPositionSeconds);
        }

        EditorGUI.EndDisabledGroup();
    }

    /// <summary>
    /// Unity EditorのAudio Preview機能でBGMを再生します。
    /// </summary>
    private void PlayPreview(AudioClip _clip)
    {
        if (_clip == null)return;

        double startPositionSeconds = m_previewPositionSeconds; //再生開始位置
        InvokeAudioUtil("StopAllPreviewClips", null);
        m_previewPositionSeconds = startPositionSeconds;
        int startSample = Mathf.Clamp(
            Mathf.RoundToInt(
                (float)m_previewPositionSeconds * _clip.frequency),
            0,
            Mathf.Max(0, _clip.samples - 1)); //開始Sample
        InvokeAudioUtil(
            "PlayPreviewClip",
            new object[] {_clip, startSample, false});
        b_m_previewPlaying = true;
        b_m_previewPaused = false;
    }

    /// <summary>
    /// Editor試聴を一時停止または再開します。
    /// </summary>
    private void TogglePausePreview()
    {
        AudioClip clip = m_sequence != null
            ? m_sequence.BgmClip
            : null; //試聴対象
        if (clip == null)return;

        if (b_m_previewPaused)
        {
            PlayPreview(clip);
            return;
        }

        if (!b_m_previewPlaying)
        {
            PlayPreview(clip);
            return;
        }

        UpdatePreviewPosition();
        InvokeAudioUtil("StopAllPreviewClips", null);
        b_m_previewPlaying = false;
        b_m_previewPaused = true;
        Repaint();
    }

    /// <summary>
    /// Editor試聴を停止して再生位置を先頭へ戻します。
    /// </summary>
    private void StopPreview()
    {
        InvokeAudioUtil("StopAllPreviewClips", null);
        m_previewPositionSeconds = 0.0d;
        b_m_previewPlaying = false;
        b_m_previewPaused = false;
    }

    /// <summary>
    /// Slider位置へEditor試聴位置を移動します。
    /// </summary>
    private static void SetPreviewPosition(
        AudioClip _clip,
        double _positionseconds)
    {
        if (_clip == null)return;

        int samplePosition = Mathf.Clamp(
            Mathf.RoundToInt((float)_positionseconds * _clip.frequency),
            0,
            Mathf.Max(0, _clip.samples - 1)); //移動先Sample
        InvokeAudioUtil(
            "SetPreviewClipSamplePosition",
            new object[] {_clip, samplePosition});
    }

    /// <summary>
    /// 試聴中の位置を取得してPlayheadを更新します。
    /// </summary>
    private void UpdatePreview()
    {
        if (!b_m_previewPlaying)return;

        UpdatePreviewPosition();
        Repaint();
    }

    /// <summary>
    /// Unity Editorから現在の試聴位置を取得します。
    /// </summary>
    private void UpdatePreviewPosition()
    {
        object position = InvokeAudioUtil(
            "GetPreviewClipPosition",
            null); //現在の試聴秒数
        if (position is float floatPosition)
        {
            m_previewPositionSeconds = floatPosition;
        }
    }

    /// <summary>
    /// UnityEditor内部のAudio Preview関数を安全に呼び出します。
    /// </summary>
    private static object InvokeAudioUtil(
        string _methodname,
        object[] _arguments)
    {
        System.Type audioUtilType =
            typeof(AudioImporter).Assembly.GetType(
                "UnityEditor.AudioUtil"); //Editor Audio Utility型
        if (audioUtilType == null)return null;

        MethodInfo[] methods = audioUtilType.GetMethods(
            BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic); //利用可能関数一覧
        int argumentCount = 0;
        if (_arguments != null)
        {
            argumentCount = _arguments.Length;
        }
        for (int i = 0; i < methods.Length; ++i)
        {
            if (methods[i].Name != _methodname)continue;
            if (methods[i].GetParameters().Length != argumentCount)continue;

            return methods[i].Invoke(null, _arguments);
        }

        return null;
    }

    /// <summary>
    /// BGM波形と時間Nodeを描画します。
    /// </summary>
    private void DrawTimeline(Rect _rect)
    {
        EditorGUI.DrawRect(_rect, new Color(0.09f, 0.09f, 0.11f));
        float duration = GetDuration(); //表示秒数
        DrawWaveform(_rect);
        for (int i = 0; i < m_sequence.EventsList.Count; ++i)
        {
            SMusicNodeEvent node = m_sequence.EventsList[i]; //描画Node
            float x = _rect.x
                + Mathf.Clamp01(node.m_time / duration) * _rect.width;
            Color nodeColor = i == m_selectedIndex
                ? Color.yellow
                : new Color(0.2f, 0.85f, 1.0f); //Node色
            EditorGUI.DrawRect(
                new Rect(
                    x - ENodeWidth * 0.5f,
                    _rect.y,
                    ENodeWidth,
                    _rect.height),
                nodeColor);
            GUI.Label(
                new Rect(x + 4.0f, _rect.y + 4.0f, 120.0f, 20.0f),
                $"{node.m_poseId}:{node.m_eventName}");
        }

        DrawEventSceneMarkers(_rect, duration);
        DrawConditionalEffectMarkers(_rect, duration);

        GUI.Label(
            new Rect(_rect.x + 5.0f, _rect.yMax - 22.0f, 150.0f, 20.0f),
            $"0.00s  -  {duration:F2}s");

        float playheadX = _rect.x
            + Mathf.Clamp01((float)m_previewPositionSeconds / duration)
            * _rect.width; //試聴位置
        EditorGUI.DrawRect(
            new Rect(playheadX - 1.0f, _rect.y, 2.0f, _rect.height),
            Color.white);
    }

    /// <summary>条件付き演出の配置時刻をTimeline上へ表示します。</summary>
    private void DrawConditionalEffectMarkers(Rect _rect, float _duration)
    {
        for (int i = 0; i < m_sequence.ConditionalEffectsList.Count; ++i)
        {
            ConditionalEffectEvent effectEvent =
                m_sequence.ConditionalEffectsList[i];
            if (effectEvent == null || !effectEvent.b_m_useTimelineTime)continue;

            float x = _rect.x
                + Mathf.Clamp01(effectEvent.m_timelineTime / _duration)
                * _rect.width;
            Color markerColor = i == m_selectedConditionalEffectIndex
                ? Color.yellow
                : new Color(1.0f, 0.45f, 0.1f);
            EditorGUI.DrawRect(
                new Rect(x - ENodeWidth, _rect.y, ENodeWidth * 2.0f, _rect.height),
                markerColor);
            GUI.Label(
                new Rect(x + 4.0f, _rect.y + 44.0f, 180.0f, 20.0f),
                $"EFFECT: {effectEvent.m_eventName}");
        }
    }

    /// <summary>
    /// Event Sceneへ移動する通常NodeをTimeline上へ表示します。
    /// </summary>
    private void DrawEventSceneMarkers(
        Rect _rect,
        float _duration)
    {
        for (int i = 0; i < m_sequence.EventScenesList.Count; ++i)
        {
            MusicEventSceneData eventData =
                m_sequence.EventScenesList[i]; //現在Event設定
            if (eventData == null || !eventData.b_m_enabled)continue;

            for (int j = 0; j < m_sequence.EventsList.Count; ++j)
            {
                SMusicNodeEvent node = m_sequence.EventsList[j]; //通常Node
                if (node.m_nodeNumber
                    != eventData.m_triggerNodeNumber)continue;

                float x = _rect.x
                    + Mathf.Clamp01(node.m_time / _duration) * _rect.width;
                EditorGUI.DrawRect(
                    new Rect(x - 2.0f, _rect.y, 4.0f, _rect.height),
                    new Color(1.0f, 0.2f, 0.75f));
                GUI.Label(
                    new Rect(x + 5.0f, _rect.y + 24.0f, 180.0f, 20.0f),
                    $"EVENT: {eventData.m_eventName}");
                break;
            }
        }
    }

    /// <summary>
    /// AudioClip Sampleから簡易波形を描画します。
    /// </summary>
    private void DrawWaveform(Rect _rect)
    {
        AudioClip clip = m_sequence.BgmClip; //描画対象BGM
        if (clip == null)return;

        float[] samples = GetWaveformSamples(clip); //Editor表示用波形
        if (samples == null || samples.Length < 2)return;

        Handles.BeginGUI();
        Handles.color = new Color(0.35f, 0.75f, 0.45f);
        Vector3 previousPoint =
            new Vector3(_rect.x, _rect.center.y, 0.0f); //直前描画点
        for (int i = 0; i < samples.Length; ++i)
        {
            float amplitude = samples[i]; //描画振幅
            Vector3 point = new Vector3(
                _rect.x + (float)i / (samples.Length - 1) * _rect.width,
                _rect.center.y - amplitude * _rect.height * 0.45f,
                0.0f); //今回描画点
            Handles.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        Handles.EndGUI();
    }

    /// <summary>
    /// Streaming設定を変えずに元WAVから表示用Sampleを取得します。
    /// </summary>
    private float[] GetWaveformSamples(AudioClip _clip)
    {
        if (m_cachedWaveformClip == _clip
            && m_waveformSamples != null)return m_waveformSamples;

        m_cachedWaveformClip = _clip;
        m_waveformSamples = null;
        string assetPath = AssetDatabase.GetAssetPath(_clip); //Audio Asset Path
        if (!assetPath.EndsWith(
            ".wav",
            System.StringComparison.OrdinalIgnoreCase))return null;

        string fullPath = Path.GetFullPath(assetPath); //WAV絶対Path
        try
        {
            m_waveformSamples = ReadWavSamples(fullPath);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                $"波形読込に失敗しました: {exception.Message}");
        }

        return m_waveformSamples;
    }

    /// <summary>
    /// PCMまたはFloat WAVから間引いた波形Sampleを読み込みます。
    /// </summary>
    private static float[] ReadWavSamples(string _fullpath)
    {
        using (FileStream stream = File.OpenRead(_fullpath))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            string riff = new string(reader.ReadChars(4)); //RIFF Header
            reader.ReadInt32();
            string wave = new string(reader.ReadChars(4)); //WAVE Header
            if (riff != "RIFF" || wave != "WAVE")return null;

            ushort audioFormat = 0; //PCM形式番号
            ushort channels = 0; //Channel数
            ushort blockAlign = 0; //一FrameのByte数
            ushort bitsPerSample = 0; //一SampleのBit数
            long dataPosition = 0; //音声Data開始位置
            int dataSize = 0; //音声Data Byte数
            while (stream.Position + 8 <= stream.Length)
            {
                string chunkId = new string(reader.ReadChars(4)); //Chunk名
                int chunkSize = reader.ReadInt32(); //Chunk Byte数
                long nextChunkPosition =
                    stream.Position + chunkSize + chunkSize % 2; //次Chunk位置
                if (chunkId == "fmt ")
                {
                    audioFormat = reader.ReadUInt16();
                    channels = reader.ReadUInt16();
                    reader.ReadInt32();
                    reader.ReadInt32();
                    blockAlign = reader.ReadUInt16();
                    bitsPerSample = reader.ReadUInt16();
                }
                else if (chunkId == "data")
                {
                    dataPosition = stream.Position;
                    dataSize = chunkSize;
                }

                stream.Position = System.Math.Min(
                    nextChunkPosition,
                    stream.Length);
            }

            if (dataPosition <= 0
                || dataSize <= 0
                || channels == 0
                || blockAlign == 0)return null;

            int frameCount = dataSize / blockAlign; //WAV Frame数
            int sampleCount = Mathf.Min(EWaveSampleCount, frameCount); //表示数
            int stride = Mathf.Max(1, frameCount / sampleCount); //間引き幅
            float[] samples = new float[sampleCount]; //表示用波形
            for (int i = 0; i < sampleCount; ++i)
            {
                int frameIndex = Mathf.Min(i * stride, frameCount - 1); //読込Frame
                stream.Position = dataPosition + (long)frameIndex * blockAlign;
                samples[i] = ReadWavSample(
                    reader,
                    audioFormat,
                    bitsPerSample);
            }

            return samples;
        }
    }

    /// <summary>
    /// WAVの先頭Channelを正規化した振幅として読み込みます。
    /// </summary>
    private static float ReadWavSample(
        BinaryReader _reader,
        ushort _audioformat,
        ushort _bitspersample)
    {
        if (_audioformat == 3 && _bitspersample == 32)
        {
            return Mathf.Clamp(_reader.ReadSingle(), -1.0f, 1.0f);
        }

        switch (_bitspersample)
        {
            case 8:
                return (_reader.ReadByte() - 128.0f) / 128.0f;
            case 16:
                return _reader.ReadInt16() / 32768.0f;
            case 24:
                int sample =
                    _reader.ReadByte()
                    | _reader.ReadByte() << 8
                    | _reader.ReadByte() << 16; //24bit PCM
                if ((sample & 0x800000) != 0)
                {
                    sample |= unchecked((int)0xFF000000);
                }

                return sample / 8388608.0f;
            case 32:
                return _reader.ReadInt32() / 2147483648.0f;
            default:
                return 0.0f;
        }
    }

    /// <summary>
    /// Clickで追加、既存NodeのDragで時間を変更します。
    /// </summary>
    private void HandleTimelineInput(Rect _rect)
    {
        Event currentEvent = Event.current; //現在Editor入力
        if (!_rect.Contains(currentEvent.mousePosition))return;
        if (currentEvent.button != 0)return;

        float duration = GetDuration(); //表示秒数
        float time = Mathf.Clamp01(
            (currentEvent.mousePosition.x - _rect.x) / _rect.width) * duration;
        if (currentEvent.type == EventType.MouseDown)
        {
            if (m_timelinePlacementMode == ETimelinePlacementMode.ConditionalEffect)
            {
                HandleConditionalEffectMouseDown(time, duration);
                currentEvent.Use();
                return;
            }

            m_selectedIndex = FindNearestNode(time, duration);
            if (m_selectedIndex < 0)
            {
                Undo.RecordObject(m_sequence, "Add Music Node");
                m_sequence.EventsList.Add(new SMusicNodeEvent
                {
                    m_nodeNumber = GetNextNodeNumber(),
                    m_time = time,
                    m_poseId = 0,
                    m_eventName = "Event"
                });
                m_selectedIndex = m_sequence.EventsList.Count - 1;
                EditorUtility.SetDirty(m_sequence);
            }

            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseDrag
            && m_timelinePlacementMode == ETimelinePlacementMode.PoseNode
            && m_selectedIndex >= 0)
        {
            Undo.RecordObject(m_sequence, "Move Music Node");
            SMusicNodeEvent node =
                m_sequence.EventsList[m_selectedIndex]; //移動Node
            node.m_time = time;
            m_sequence.EventsList[m_selectedIndex] = node;
            EditorUtility.SetDirty(m_sequence);
            Repaint();
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseDrag
            && m_timelinePlacementMode == ETimelinePlacementMode.ConditionalEffect
            && m_selectedConditionalEffectIndex >= 0)
        {
            Undo.RecordObject(m_sequence, "Move Conditional Effect");
            ConditionalEffectEvent effectEvent =
                m_sequence.ConditionalEffectsList[m_selectedConditionalEffectIndex];
            effectEvent.m_timelineTime = time;
            EditorUtility.SetDirty(m_sequence);
            Repaint();
            currentEvent.Use();
        }
    }

    /// <summary>既存Markerを選択し、空き位置なら条件付き演出を追加します。</summary>
    private void HandleConditionalEffectMouseDown(float _time, float _duration)
    {
        m_selectedConditionalEffectIndex = FindNearestConditionalEffect(
            _time,
            _duration);
        if (m_selectedConditionalEffectIndex >= 0)return;

        Undo.RecordObject(m_sequence, "Add Conditional Effect On Timeline");
        ConditionalEffectEvent effectEvent = new ConditionalEffectEvent
        {
            m_eventName = "Effect Event",
            b_m_useTimelineTime = true,
            m_timelineTime = _time,
            m_conditionExpression = "time >= 0"
        };
        effectEvent.m_effectsList.Add(new ConditionalEffectEntry());
        m_sequence.ConditionalEffectsList.Add(effectEvent);
        m_selectedConditionalEffectIndex =
            m_sequence.ConditionalEffectsList.Count - 1;
        EditorUtility.SetDirty(m_sequence);
        Repaint();
    }

    /// <summary>指定時刻の近くにある条件付き演出Markerを検索します。</summary>
    private int FindNearestConditionalEffect(float _time, float _duration)
    {
        float hitSeconds = ENodeHitWidth / Mathf.Max(1.0f, position.width)
            * _duration;
        int nearestIndex = -1;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < m_sequence.ConditionalEffectsList.Count; ++i)
        {
            ConditionalEffectEvent effectEvent =
                m_sequence.ConditionalEffectsList[i];
            if (effectEvent == null || !effectEvent.b_m_useTimelineTime)continue;

            float distance = Mathf.Abs(effectEvent.m_timelineTime - _time);
            if (distance > hitSeconds || distance >= nearestDistance)continue;
            nearestIndex = i;
            nearestDistance = distance;
        }

        return nearestIndex;
    }

    /// <summary>
    /// 現在設定されている全Nodeを一覧表示して直接編集します。
    /// </summary>
    private void DrawNodeList()
    {
        EditorGUILayout.Space();
        b_m_nodeListFoldout = EditorGUILayout.Foldout(
            b_m_nodeListFoldout,
            $"Nodes ({m_sequence.EventsList.Count})",
            true);
        if (!b_m_nodeListFoldout)return;

        GUILayout.BeginHorizontal();
        GUILayout.Label(string.Empty, GUILayout.Width(ENodeSelectColumnWidth));
        GUILayout.Label("No.", GUILayout.Width(ENodeNumberColumnWidth));
        GUILayout.Label("Time", GUILayout.Width(ENodeTimeColumnWidth));
        GUILayout.Label("Pose ID", GUILayout.Width(ENodePoseColumnWidth));
        GUILayout.Label("Event Name");
        GUILayout.Label(
            "Success Effect",
            GUILayout.Width(ESuccessEffectColumnWidth));
        GUILayout.Label(string.Empty, GUILayout.Width(ENodeDeleteColumnWidth));
        GUILayout.EndHorizontal();

        m_nodeListScrollPosition = EditorGUILayout.BeginScrollView(
            m_nodeListScrollPosition,
            GUILayout.Height(ENodeListHeight));
        int deleteIndex = -1; //削除対象のNode番号
        for (int i = 0; i < m_sequence.EventsList.Count; ++i)
        {
            int nodeIndex = i; //Dropdown選択後も維持するNode番号
            SMusicNodeEvent node = m_sequence.EventsList[i]; //現在行のNode
            Color previousBackgroundColor = GUI.backgroundColor; //元のButton色
            if (i == m_selectedIndex)
            {
                GUI.backgroundColor = Color.yellow;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(
                "●",
                GUILayout.Width(ENodeSelectColumnWidth)))
            {
                m_selectedIndex = i;
            }

            GUI.backgroundColor = previousBackgroundColor;
            EditorGUI.BeginChangeCheck();
            node.m_nodeNumber = Mathf.Max(
                1,
                EditorGUILayout.IntField(
                    node.m_nodeNumber,
                    GUILayout.Width(ENodeNumberColumnWidth)));
            node.m_time = Mathf.Max(
                0.0f,
                EditorGUILayout.FloatField(
                    node.m_time,
                    GUILayout.Width(ENodeTimeColumnWidth)));
            node.m_poseId = EditorGUILayout.IntField(
                node.m_poseId,
                GUILayout.Width(ENodePoseColumnWidth));
            node.m_eventName = EditorGUILayout.TextField(node.m_eventName);
            DrawSuccessEffectSelector(
                node.m_successEffectNames,
                _effectName =>
                {
                    Undo.RecordObject(m_sequence, "Select Success Effect");
                    SMusicNodeEvent editedNode =
                        m_sequence.EventsList[nodeIndex];
                    editedNode.m_successEffectNames = ToggleEffectName(
                        editedNode.m_successEffectNames,
                        _effectName);
                    m_sequence.EventsList[nodeIndex] = editedNode;
                    EditorUtility.SetDirty(m_sequence);
                });
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_sequence, "Edit Music Node");
                m_sequence.EventsList[i] = node;
                EditorUtility.SetDirty(m_sequence);
            }

            if (GUILayout.Button(
                "×",
                GUILayout.Width(ENodeDeleteColumnWidth)))
            {
                deleteIndex = i;
            }

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        if (deleteIndex >= 0)
        {
            Undo.RecordObject(m_sequence, "Delete Music Node");
            m_sequence.EventsList.RemoveAt(deleteIndex);
            if (m_selectedIndex == deleteIndex)
            {
                m_selectedIndex = -1;
            }
            else if (m_selectedIndex > deleteIndex)
            {
                --m_selectedIndex;
            }

            EditorUtility.SetDirty(m_sequence);
        }
    }

    /// <summary>
    /// 通常Nodeとは別に複数のEvent Sceneと専用Node一覧を編集します。
    /// </summary>
    private void DrawEventSceneList()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            $"Event Scenes ({m_sequence.EventScenesList.Count})",
            EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Trigger Node成功時にSceneへ移動します。Event Nodesは移動先Scene専用で、"
            + "通常のPoseTimeFlow.csvには出力されません。",
            MessageType.Info);

        EnsureEventEditorStates();
        for (int i = 0; i < m_sequence.EventScenesList.Count; ++i)
        {
            DrawEventEditor(i);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Event Scene"))
        {
            AddEventSceneDirect();
        }

        if (GUILayout.Button("Add 3 Event Slots"))
        {
            for (int i = 0; i < 3; ++i)
            {
                AddEventSceneDirect();
            }
        }

        using (new EditorGUI.DisabledScope(m_sequence.EventScenesList.Count == 0))
        {
            if (GUILayout.Button("Remove Last Event"))
            {
                Undo.RecordObject(m_sequence, "Remove Event Scene");
                m_sequence.EventScenesList.RemoveAt(
                    m_sequence.EventScenesList.Count - 1);
                EditorUtility.SetDirty(m_sequence);
            }
        }

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Event一件分の設定、専用グラフ、専用Node一覧を表示します。
    /// </summary>
    private void DrawEventEditor(int _eventIndex)
    {
        MusicEventSceneData eventData =
            m_sequence.EventScenesList[_eventIndex]; //編集対象Event
        if (eventData == null)return;

        b_m_eventFoldoutsList[_eventIndex] = EditorGUILayout.Foldout(
            b_m_eventFoldoutsList[_eventIndex],
            $"Event {_eventIndex + 1}: {eventData.m_eventName}",
            true);
        if (!b_m_eventFoldoutsList[_eventIndex])return;

        EditorGUI.indentLevel++;
        EditorGUI.BeginChangeCheck();
        eventData.b_m_enabled = EditorGUILayout.Toggle(
            "Enabled",
            eventData.b_m_enabled);
        eventData.m_eventName = EditorGUILayout.TextField(
            "Event Name",
            eventData.m_eventName);
        eventData.m_triggerNodeNumber = EditorGUILayout.IntField(
            "Trigger Node Number",
            eventData.m_triggerNodeNumber);
        if (eventData.m_eventType == EMusicEventType.AudienceChoice)
        {
            EditorGUILayout.HelpBox(
                "Use Event Name 'Decision' for the selection start time and 'End' for the failure/end time. The first three nodes excluding these control nodes are pose candidates.",
                MessageType.Info);
        }
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(m_sequence, "Edit Event Settings");
            EditorUtility.SetDirty(m_sequence);
        }

        EditorGUILayout.LabelField(
            $"Event Timeline ({eventData.m_eventNodesList.Count} Nodes)",
            EditorStyles.boldLabel);
        Rect timelineRect = GUILayoutUtility.GetRect(
            100.0f,
            ETimelineHeight,
            GUILayout.ExpandWidth(true)); //Event専用波形領域
        DrawEventTimeline(timelineRect, eventData, _eventIndex);
        HandleEventTimelineInput(timelineRect, eventData, _eventIndex);
        DrawEventNodeList(eventData, _eventIndex);
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(8.0f);
    }

    /// <summary>
    /// Event専用の波形とNode線を描画します。
    /// </summary>
    private void DrawEventTimeline(
        Rect _rect,
        MusicEventSceneData _eventData,
        int _eventIndex)
    {
        EditorGUI.DrawRect(_rect, new Color(0.08f, 0.07f, 0.12f));
        float duration = GetEventDuration(_eventData); //Event表示秒数
        DrawWaveform(_rect);
        DrawEventTriggerMarker(_rect, _eventData, duration);
        for (int i = 0; i < _eventData.m_eventNodesList.Count; ++i)
        {
            SMusicNodeEvent node = _eventData.m_eventNodesList[i]; //描画Node
            float x = _rect.x
                + Mathf.Clamp01(node.m_time / duration) * _rect.width;
            Color color = i == m_eventSelectedIndicesList[_eventIndex]
                ? Color.yellow
                : new Color(1.0f, 0.25f, 0.75f); //Event Node色
            EditorGUI.DrawRect(
                new Rect(x - ENodeWidth * 0.5f, _rect.y, ENodeWidth, _rect.height),
                color);
            GUI.Label(
                new Rect(x + 4.0f, _rect.y + 4.0f, 150.0f, 20.0f),
                $"{node.m_poseId}:{node.m_eventName}");
        }

        GUI.Label(
            new Rect(_rect.x + 5.0f, _rect.yMax - 22.0f, 180.0f, 20.0f),
            $"BGM 0.00s - {duration:F2}s");

        float playheadX = _rect.x
            + Mathf.Clamp01((float)m_previewPositionSeconds / duration)
            * _rect.width;
        EditorGUI.DrawRect(
            new Rect(playheadX - 1.0f, _rect.y, 2.0f, _rect.height),
            Color.white);
    }

    private void DrawEventTriggerMarker(
        Rect _rect,
        MusicEventSceneData _eventData,
        float _duration)
    {
        for (int i = 0; i < m_sequence.EventsList.Count; ++i)
        {
            SMusicNodeEvent node = m_sequence.EventsList[i];
            if (node.m_nodeNumber != _eventData.m_triggerNodeNumber)continue;

            float x = _rect.x
                + Mathf.Clamp01(node.m_time / _duration) * _rect.width;
            EditorGUI.DrawRect(
                new Rect(x - 2.0f, _rect.y, 4.0f, _rect.height),
                new Color(1.0f, 0.45f, 0.05f));
            GUI.Label(
                new Rect(x + 5.0f, _rect.y + 24.0f, 180.0f, 20.0f),
                $"TRIGGER {node.m_time:F2}s");
            return;
        }
    }

    /// <summary>
    /// Event専用グラフのClick追加とDrag移動を処理します。
    /// </summary>
    private void HandleEventTimelineInput(
        Rect _rect,
        MusicEventSceneData _eventData,
        int _eventIndex)
    {
        Event currentEvent = Event.current; //現在入力
        if (!_rect.Contains(currentEvent.mousePosition)
            || currentEvent.button != 0)return;

        float duration = GetEventDuration(_eventData); //Event表示秒数
        float time = Mathf.Clamp01(
            (currentEvent.mousePosition.x - _rect.x) / _rect.width) * duration;
        if (currentEvent.type == EventType.MouseDown)
        {
            int selectedIndex = FindNearestEventNode(
                _eventData.m_eventNodesList,
                time,
                duration); //選択Node
            if (selectedIndex < 0)
            {
                Undo.RecordObject(m_sequence, "Add Event Node");
                _eventData.m_eventNodesList.Add(new SMusicNodeEvent
                {
                    m_nodeNumber = GetNextEventNodeNumber(_eventData),
                    m_time = time,
                    m_poseId = 0,
                    m_eventName = "Event Node"
                });
                selectedIndex = _eventData.m_eventNodesList.Count - 1;
                EditorUtility.SetDirty(m_sequence);
            }

            m_eventSelectedIndicesList[_eventIndex] = selectedIndex;
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseDrag
            && m_eventSelectedIndicesList[_eventIndex] >= 0)
        {
            int selectedIndex = m_eventSelectedIndicesList[_eventIndex]; //移動対象
            Undo.RecordObject(m_sequence, "Move Event Node");
            SMusicNodeEvent node = _eventData.m_eventNodesList[selectedIndex];
            node.m_time = time;
            _eventData.m_eventNodesList[selectedIndex] = node;
            EditorUtility.SetDirty(m_sequence);
            Repaint();
            currentEvent.Use();
        }
    }

    /// <summary>
    /// Event専用Nodeを一覧で直接編集します。
    /// </summary>
    private void DrawEventNodeList(
        MusicEventSceneData _eventData,
        int _eventIndex)
    {
        m_eventScrollPositionsList[_eventIndex] = EditorGUILayout.BeginScrollView(
            m_eventScrollPositionsList[_eventIndex],
            GUILayout.Height(ENodeListHeight));
        int deleteIndex = -1; //削除対象
        for (int i = 0; i < _eventData.m_eventNodesList.Count; ++i)
        {
            int nodeIndex = i; //Dropdown選択後も維持するNode番号
            SMusicNodeEvent node = _eventData.m_eventNodesList[i]; //編集Node
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("●", GUILayout.Width(ENodeSelectColumnWidth)))
            {
                m_eventSelectedIndicesList[_eventIndex] = i;
            }

            EditorGUI.BeginChangeCheck();
            node.m_nodeNumber = EditorGUILayout.IntField(
                node.m_nodeNumber,
                GUILayout.Width(ENodeNumberColumnWidth));
            node.m_time = Mathf.Max(
                0.0f,
                EditorGUILayout.FloatField(
                    node.m_time,
                    GUILayout.Width(ENodeTimeColumnWidth)));
            node.m_poseId = EditorGUILayout.IntField(
                node.m_poseId,
                GUILayout.Width(ENodePoseColumnWidth));
            node.m_eventName = EditorGUILayout.TextField(node.m_eventName);
            DrawSuccessEffectSelector(
                node.m_successEffectNames,
                _effectName =>
                {
                    Undo.RecordObject(m_sequence, "Select Event Success Effect");
                    SMusicNodeEvent editedNode =
                        _eventData.m_eventNodesList[nodeIndex];
                    editedNode.m_successEffectNames = ToggleEffectName(
                        editedNode.m_successEffectNames,
                        _effectName);
                    _eventData.m_eventNodesList[nodeIndex] = editedNode;
                    EditorUtility.SetDirty(m_sequence);
                });
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_sequence, "Edit Event Node");
                _eventData.m_eventNodesList[i] = node;
                EditorUtility.SetDirty(m_sequence);
            }

            if (GUILayout.Button("×", GUILayout.Width(ENodeDeleteColumnWidth)))
            {
                deleteIndex = i;
            }

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        if (deleteIndex < 0)return;

        Undo.RecordObject(m_sequence, "Delete Event Node");
        _eventData.m_eventNodesList.RemoveAt(deleteIndex);
        m_eventSelectedIndicesList[_eventIndex] = -1;
        EditorUtility.SetDirty(m_sequence);
    }

    /// <summary>
    /// Event数に合わせてEditor表示状態を確保します。
    /// </summary>
    private void EnsureEventEditorStates()
    {
        while (m_eventSelectedIndicesList.Count < m_sequence.EventScenesList.Count)
        {
            m_eventSelectedIndicesList.Add(-1);
            m_eventScrollPositionsList.Add(Vector2.zero);
            b_m_eventFoldoutsList.Add(true);
        }
    }

    /// <summary>
    /// Event専用グラフの表示時間を返します。
    /// </summary>
    private float GetEventDuration(MusicEventSceneData _eventData)
    {
        if (m_sequence.BgmClip != null)
        {
            return Mathf.Max(EMinimumDuration, m_sequence.BgmClip.length);
        }

        return Mathf.Max(
            EMinimumDuration,
            m_sequence.ManualTimelineDuration);
    }

    /// <summary>
    /// Click時刻に近いEvent Node番号を返します。
    /// </summary>
    private static int FindNearestEventNode(
        List<SMusicNodeEvent> _nodesList,
        float _time,
        float _duration)
    {
        float hitSeconds = ENodeHitWidth / 1000.0f * _duration; //選択許容秒数
        for (int i = 0; i < _nodesList.Count; ++i)
        {
            if (Mathf.Abs(_nodesList[i].m_time - _time) <= hitSeconds)return i;
        }

        return -1;
    }

    /// <summary>
    /// Event内で未使用の次Node番号を返します。
    /// </summary>
    private static int GetNextEventNodeNumber(MusicEventSceneData _eventData)
    {
        int maximumNumber = 0; //現在最大番号
        for (int i = 0; i < _eventData.m_eventNodesList.Count; ++i)
        {
            maximumNumber = Mathf.Max(
                maximumNumber,
                _eventData.m_eventNodesList[i].m_nodeNumber);
        }

        return maximumNumber + 1;
    }

    /// <summary>
    /// Event設定を一件追加します。
    /// </summary>
    private void AddEventSceneDirect()
    {
        Undo.RecordObject(m_sequence, "Add Event Scene");
        m_sequence.EventScenesList.Add(new MusicEventSceneData
        {
            b_m_enabled = true,
            m_eventName = $"Event {m_sequence.EventScenesList.Count + 1}",
            m_triggerNodeNumber = 1
        });
        EnsureEventEditorStates();
        EditorUtility.SetDirty(m_sequence);
    }

    /// <summary>
    /// 指定時間付近のNode番号を取得します。
    /// </summary>
    private int FindNearestNode(float _time, float _duration)
    {
        float tolerance =
            _duration * ENodeHitWidth / Mathf.Max(1.0f, position.width);
        for (int i = 0; i < m_sequence.EventsList.Count; ++i)
        {
            if (Mathf.Abs(m_sequence.EventsList[i].m_time - _time)
                <= tolerance)return i;
        }

        return -1;
    }

    /// <summary>
    /// BGM長またはNode末尾から表示秒数を取得します。
    /// </summary>
    private float GetDuration()
    {
        if (m_sequence.BgmClip != null)
        {
            return Mathf.Max(EMinimumDuration, m_sequence.BgmClip.length);
        }

        if (m_sequence.ManualTimelineDuration > 0.0f)
        {
            return Mathf.Max(
                EMinimumDuration,
                m_sequence.ManualTimelineDuration);
        }

        float duration = EMinimumDuration; //Nodeのみの場合の表示秒数
        for (int i = 0; i < m_sequence.EventsList.Count; ++i)
        {
            duration = Mathf.Max(duration, m_sequence.EventsList[i].m_time);
        }

        return duration;
    }

    /// <summary>
    /// Nodeを時間順へ並べます。
    /// </summary>
    private void SortEvents()
    {
        Undo.RecordObject(m_sequence, "Sort Music Nodes");
        m_sequence.EventsList.Sort(
            (_left, _right) => _left.m_time.CompareTo(_right.m_time));
        m_selectedIndex = -1;
        EditorUtility.SetDirty(m_sequence);
    }

    /// <summary>
    /// 既存Loader互換のPoseTimeFlow.csvを生成します。
    /// </summary>
    private void ExportCsv()
    {
        SortEvents();
        StringBuilder csv = new StringBuilder(); //CSV内容
        csv.AppendLine("PoseFlow,PoseID,EventName,time,SuccessEffectNames");
        float previousTime = 0.0f; //直前Nodeの絶対時間
        for (int i = 0; i < m_sequence.EventsList.Count; ++i)
        {
            SMusicNodeEvent node = m_sequence.EventsList[i]; //出力Node
            float duration =
                Mathf.Max(0.0f, node.m_time - previousTime); //既存Game用の区間秒数
            csv.Append(node.m_nodeNumber);
            csv.Append(',');
            csv.Append(node.m_poseId);
            csv.Append(',');
            csv.Append(EscapeCsv(node.m_eventName));
            csv.Append(',');
            csv.Append(duration.ToString(
                "0.###",
                CultureInfo.InvariantCulture));
            csv.Append(',');
            csv.AppendLine(EscapeCsv(node.m_successEffectNames));
            previousTime = node.m_time;
        }

        File.WriteAllText(ECsvPath, csv.ToString(), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(ECsvPath);
        Debug.Log($"Music Nodeを{ECsvPath}へ書き出しました。");
    }

    /// <summary>
    /// 外部編集された既存CSVをSequenceへ読み込みます。
    /// </summary>
    private void ImportCsv()
    {
        if (!File.Exists(ECsvPath))
        {
            Debug.LogWarning($"{ECsvPath}が見つかりません。");
            return;
        }

        string[] lines = File.ReadAllLines(ECsvPath); //CSV全行
        List<SMusicNodeEvent> importedEventsList =
            new List<SMusicNodeEvent>(); //読込Node一覧
        float absoluteTime = 0.0f; //BGM開始からの累積秒数
        for (int i = 1; i < lines.Length; ++i)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))continue;

            List<string> columnsList = ParseCsvLine(lines[i]); //分割した列
            if (columnsList.Count < 4)continue;
            if (!int.TryParse(columnsList[0], out int nodeNumber))continue;
            if (!int.TryParse(columnsList[1], out int poseId))continue;
            if (!float.TryParse(
                columnsList[3],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float duration))continue;

            absoluteTime += Mathf.Max(0.0f, duration);
            importedEventsList.Add(new SMusicNodeEvent
            {
                m_nodeNumber = Mathf.Max(1, nodeNumber),
                m_time = absoluteTime,
                m_poseId = poseId,
                m_eventName = columnsList[2],
                m_successEffectNames = columnsList.Count >= 5
                    ? columnsList[4]
                    : string.Empty
            });
        }

        Undo.RecordObject(m_sequence, "Import Music Nodes");
        m_sequence.EventsList.Clear();
        m_sequence.EventsList.AddRange(importedEventsList);
        m_selectedIndex = -1;
        EditorUtility.SetDirty(m_sequence);
        Debug.Log(
            $"{ECsvPath}からNodeを{importedEventsList.Count}件読み込みました。");
    }

    /// <summary>
    /// QuoteとCommaを考慮してCSV一行を分割します。
    /// </summary>
    private static List<string> ParseCsvLine(string _line)
    {
        List<string> columnsList = new List<string>(); //分割結果
        StringBuilder value = new StringBuilder(); //現在列
        bool b_insideQuote = false; //Quote内部か
        for (int i = 0; i < _line.Length; ++i)
        {
            char character = _line[i]; //現在文字
            if (character == '"')
            {
                if (b_insideQuote
                    && i + 1 < _line.Length
                    && _line[i + 1] == '"')
                {
                    value.Append('"');
                    ++i;
                    continue;
                }

                b_insideQuote = !b_insideQuote;
                continue;
            }

            if (character == ',' && !b_insideQuote)
            {
                columnsList.Add(value.ToString());
                value.Clear();
                continue;
            }

            value.Append(character);
        }

        columnsList.Add(value.ToString());
        return columnsList;
    }

    /// <summary>
    /// 現在未使用の次のNode番号を返します。
    /// </summary>
    private int GetNextNodeNumber()
    {
        int maximumNumber = 0; //現在の最大Node番号
        for (int i = 0; i < m_sequence.EventsList.Count; ++i)
        {
            maximumNumber = Mathf.Max(
                maximumNumber,
                m_sequence.EventsList[i].m_nodeNumber);
        }

        return maximumNumber + 1;
    }

    /// <summary>
    /// CSV文字列のQuoteを処理します。
    /// </summary>
    private static string EscapeCsv(string _value)
    {
        if (string.IsNullOrEmpty(_value))return string.Empty;
        if (!_value.Contains(",") && !_value.Contains("\""))return _value;

        return $"\"{_value.Replace("\"", "\"\"")}\"";
    }

    /// <summary>
    /// Scene内EffectListを検索元にした、検索可能な成功演出メニューを表示します。
    /// </summary>
    private void DrawSuccessEffectSelector(
        string _currentEffectName,
        Action<string> _onSelected)
    {
        string label = string.IsNullOrWhiteSpace(_currentEffectName)
            ? "(Random)"
            : _currentEffectName;
        if (!GUILayout.Button(
            label,
            EditorStyles.popup,
            GUILayout.Width(ESuccessEffectColumnWidth)))return;

        EffectList effectList = UnityEngine.Object.FindFirstObjectByType<EffectList>(
            FindObjectsInactive.Include);
        if (effectList == null
            || effectList.Effects == null
            || effectList.Effects.Length == 0)
        {
            ShowNotification(new GUIContent(
                "EffectListがScene内にありません。Gameplayシーンを開いてください。"));
            return;
        }

        List<string> effectNames = new List<string>();
        for (int i = 0; i < effectList.Effects.Length; ++i)
        {
            string effectName = effectList.Effects[i].EffectName?.Trim();
            if (string.IsNullOrEmpty(effectName)
                || effectNames.Contains(effectName))continue;
            effectNames.Add(effectName);
        }
        effectNames.Sort(StringComparer.OrdinalIgnoreCase);

        SearchableEffectDropdown dropdown = new SearchableEffectDropdown(
            new AdvancedDropdownState(),
            effectNames,
            _onSelected);
        dropdown.Show(GUILayoutUtility.GetLastRect());
    }

    /// <summary>
    /// |区切りの演出一覧へクリックされた名前を追加し、登録済みなら解除します。
    /// Random選択時は全指定を解除します。
    /// </summary>
    private static string ToggleEffectName(
        string _currentEffectNames,
        string _selectedEffectName)
    {
        if (string.IsNullOrWhiteSpace(_selectedEffectName))return string.Empty;

        List<string> selectedNames = new List<string>();
        if (!string.IsNullOrWhiteSpace(_currentEffectNames))
        {
            string[] currentNames = _currentEffectNames.Split('|');
            for (int i = 0; i < currentNames.Length; ++i)
            {
                string currentName = currentNames[i].Trim();
                if (!string.IsNullOrEmpty(currentName)
                    && !selectedNames.Contains(currentName))
                {
                    selectedNames.Add(currentName);
                }
            }
        }

        if (selectedNames.Contains(_selectedEffectName))
        {
            selectedNames.Remove(_selectedEffectName);
        }
        else
        {
            selectedNames.Add(_selectedEffectName);
        }
        return string.Join("|", selectedNames);
    }

    /// <summary>AdvancedDropdown標準の検索欄を使用するEffect選択メニューです。</summary>
    private sealed class SearchableEffectDropdown : AdvancedDropdown
    {
        private const string ERandomItemName = "(Random / None)";
        private readonly IReadOnlyList<string> m_effectNames;
        private readonly Action<string> m_onSelected;

        public SearchableEffectDropdown(
            AdvancedDropdownState _state,
            IReadOnlyList<string> _effectNames,
            Action<string> _onSelected)
            : base(_state)
        {
            m_effectNames = _effectNames;
            m_onSelected = _onSelected;
            minimumSize = new Vector2(360.0f, 320.0f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("Effects");
            root.AddChild(new AdvancedDropdownItem(ERandomItemName));
            for (int i = 0; i < m_effectNames.Count; ++i)
            {
                root.AddChild(new AdvancedDropdownItem(m_effectNames[i]));
            }
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem _item)
        {
            m_onSelected?.Invoke(
                _item.name == ERandomItemName ? string.Empty : _item.name);
        }
    }
}
