/*━━━━━━━━━*
*@file MusicNodeEditorWindow.cs*
*@brief BGM波形上でPose Nodeの時間とIDを視覚編集する*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks PoseTimeFlow.csvへ書き出して既存Gameと接続*
*━━━━━━━━━*/

using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Audio波形上へPose Nodeを配置、移動、編集します。
/// </summary>
public sealed class MusicNodeEditorWindow : EditorWindow
{
    private const string ECsvPath = "Assets/Resources/PoseTimeFlow.csv"; //既存CSV
    private const float ETimelineHeight = 180.0f; //波形領域高さ
    private const float ENodeWidth = 2.0f; //Node線の表示幅
    private const float ENodeHitWidth = 10.0f; //Nodeを選択できる幅
    private const float ENodeListHeight = 190.0f; //Node一覧の高さ
    private const float ENodeSelectColumnWidth = 28.0f; //選択Button列幅
    private const float ENodeNumberColumnWidth = 55.0f; //番号列幅
    private const float ENodeTimeColumnWidth = 75.0f; //時間列幅
    private const float ENodePoseColumnWidth = 65.0f; //Pose ID列幅
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
    private AudioClip m_cachedWaveformClip; //波形Cache対象
    private float[] m_waveformSamples; //WAVから取得した波形Cache
    private double m_previewPositionSeconds; //Editor試聴位置
    private bool b_m_previewPlaying; //Editor試聴中か
    private bool b_m_previewPaused; //Editor試聴を一時停止中か
    private readonly List<int> m_eventSelectedIndicesList = new List<int>(); //Event別選択Node
    private readonly List<Vector2> m_eventScrollPositionsList = new List<Vector2>(); //Event別一覧Scroll
    private readonly List<bool> b_m_eventFoldoutsList = new List<bool>(); //Event別展開状態

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
        Rect timelineRect = GUILayoutUtility.GetRect(
            100.0f,
            ETimelineHeight,
            GUILayout.ExpandWidth(true)); //波形表示範囲
        DrawTimeline(timelineRect);
        HandleTimelineInput(timelineRect);
        DrawNodeList();
        DrawEventSceneList();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Sort By Time"))
        {
            SortEvents();
        }

        if (GUILayout.Button("Import PoseTimeFlow.csv"))
        {
            ImportCsv();
        }

        if (GUILayout.Button("Export PoseTimeFlow.csv"))
        {
            ExportCsv();
        }

        GUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
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
    }

    /// <summary>
    /// 現在設定されている全Nodeを一覧表示して直接編集します。
    /// </summary>
    private void DrawNodeList()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            $"Nodes ({m_sequence.EventsList.Count})",
            EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.Label(string.Empty, GUILayout.Width(ENodeSelectColumnWidth));
        GUILayout.Label("No.", GUILayout.Width(ENodeNumberColumnWidth));
        GUILayout.Label("Time", GUILayout.Width(ENodeTimeColumnWidth));
        GUILayout.Label("Pose ID", GUILayout.Width(ENodePoseColumnWidth));
        GUILayout.Label("Event Name");
        GUILayout.Label(string.Empty, GUILayout.Width(ENodeDeleteColumnWidth));
        GUILayout.EndHorizontal();

        m_nodeListScrollPosition = EditorGUILayout.BeginScrollView(
            m_nodeListScrollPosition,
            GUILayout.Height(ENodeListHeight));
        int deleteIndex = -1; //削除対象のNode番号
        for (int i = 0; i < m_sequence.EventsList.Count; ++i)
        {
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
        csv.AppendLine("PoseFlow,PoseID,EventName,time");
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
            csv.AppendLine(duration.ToString(
                "0.###",
                CultureInfo.InvariantCulture));
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
                m_eventName = columnsList[2]
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
}
