/*━━━━━━━━━*
*@file EventAudienceCanvasController.cs*
*@brief Event Camera上で観客頭上へ三種類のNodeをCanvas表示する*
*@author 24cu0312 久場洸太*
*@date 2026/08/03*
*最終更新日 2026/08/03*
*@remarks Screen Space Camera Canvas上で観客のWorld座標を追従する*
*━━━━━━━━━*/

using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Event Cameraから見た観客の頭上へ大きな三種類のNodeを表示します。
/// </summary>
[RequireComponent(typeof(Canvas))]
public sealed class EventAudienceCanvasController : MonoBehaviour
{
    private const int EPreferenceCount = 3; //Node種類数
    private const float EMinimumNodeScale = 0.75f; //最低Node倍率
    private const float EMaximumNodeScale = 1.35f; //最大Node倍率
    private const float EMinimumNodePixelSize = 600.0f; //大Node最低Pixel寸法
    private const string ENodeRootName = "EventAudienceNode"; //表示Object名
    private static readonly float[] ETargetViewportXs =
    {
        0.08f,
        0.5f,
        0.92f
    }; //左・中央・右の表示基準

    [SerializeField] private Canvas m_canvas; //Event表示Canvas
    [SerializeField] private Camera m_eventCamera; //World座標変換Camera
    [SerializeField] private AudienceAreaSpawner m_audienceSpawner; //観客生成元
    [SerializeField] private AudiencePreferenceSystem m_preferenceSystem; //観客好み管理
    [SerializeField] private Sprite[] m_nodeSprites = new Sprite[EPreferenceCount]; //三種類のNode画像
    [SerializeField] private Color[] m_nodeColors =
    {
        new Color(0.1f, 0.8f, 1.0f, 0.95f),
        new Color(1.0f, 0.85f, 0.1f, 0.95f),
        new Color(1.0f, 0.2f, 0.75f, 0.95f)
    }; //画像未設定時の色
    [SerializeField] private Vector3 m_worldHeadOffset = new Vector3(0.0f, 2.3f, 0.0f); //頭上位置補正
    [SerializeField] private Vector2 m_nodeSize = new Vector2(650.0f, 650.0f); //Node基本寸法
    [Header("Speech Bubble")]
    [SerializeField] private Sprite m_speechBubbleSprite; //Pose画像の背面に表示する吹き出し
    [SerializeField] private Vector2 m_speechBubbleSize =
        new Vector2(820.0f, 760.0f); //吹き出し寸法
    [SerializeField] private Vector2 m_speechBubbleOffset =
        new Vector2(0.0f, 20.0f); //Pose画像からの表示位置補正
    [SerializeField] private Color m_speechBubbleColor = Color.white; //吹き出し色
    [SerializeField] private bool b_m_showPercentage; //Node内に好み割合を表示するか
    [SerializeField] private Color m_nodeOutlineColor =
        new Color(1.0f, 0.92f, 0.05f, 1.0f); //三Node共通の強調枠色
    [SerializeField] private Vector2 m_nodeOutlineDistance =
        new Vector2(8.0f, -8.0f); //枠の太さ
    [SerializeField] private float m_worldCanvasScale = 0.005f; //World Space Canvas倍率
    [SerializeField] private bool b_m_faceEventCamera = true; //Cameraへ正面を向けるか
    [SerializeField] private float m_minimumWorldSeparation = 18.0f; //三地点間の最低World距離
    [SerializeField] private Transform[] m_nodeAnchors =
        new Transform[EPreferenceCount]; //手動配置する左・中央・右Anchor
    [SerializeField] private bool b_m_showOnStart; //単体でScene開始時に表示するか

    private readonly List<SDisplayedAudienceNodes> m_displayedNodesList =
        new List<SDisplayedAudienceNodes>(); //表示中Node群
    private readonly List<Button> m_candidateButtons =
        new List<Button>(); //Audience Choice候補Button
    private readonly List<Vector3> m_candidateBaseScales =
        new List<Vector3>();
    private readonly List<GameObject> m_decisionCues =
        new List<GameObject>();
    private bool b_m_selectionEnabled;

    /// <summary>
    /// 一人分の観客参照とCanvas表示を保持します。
    /// </summary>
    private struct SDisplayedAudienceNodes
    {
        public AudienceReaction m_audience; //追従対象観客
        public RectTransform m_root; //三Node親Transform
    }

    /// <summary>
    /// 参照を補完し、指定されていればNode表示を開始します。
    /// </summary>
    private void Start()
    {
        FindReferences();
        ConfigureCanvas();
        if (b_m_showOnStart)
        {
            StartCoroutine(ShowNodesAfterAudienceSpawn());
        }
    }

    /// <summary>
    /// AudienceAreaSpawnerのStart処理後にCanvas Nodeを表示します。
    /// </summary>
    private IEnumerator ShowNodesAfterAudienceSpawn()
    {
        while (m_audienceSpawner != null && !m_audienceSpawner.IsSpawnComplete)
        {
            yield return null;
        }
        ShowNodes();
    }

    /// <summary>
    /// World Space Canvasの位置を固定したままEvent Cameraへ正面を向けます。
    /// </summary>
    private void LateUpdate()
    {
        if (m_eventCamera == null || m_canvas == null)return;

        for (int i = 0; i < m_displayedNodesList.Count; ++i)
        {
            SDisplayedAudienceNodes displayed = m_displayedNodesList[i]; //対象表示
            if (displayed.m_root == null)continue;
            if (!b_m_faceEventCamera)continue;

            displayed.m_root.rotation = m_eventCamera.transform.rotation;
        }

        float pulse = 1.0f + Mathf.Sin(Time.unscaledTime * 8.0f) * 0.08f;
        for (int i = 0; i < m_candidateButtons.Count; ++i)
        {
            Button button = m_candidateButtons[i];
            if (button == null || i >= m_candidateBaseScales.Count)continue;
            float scaleMultiplier = 1.0f;
            if (b_m_selectionEnabled)
            {
                scaleMultiplier = pulse;
            }
            button.transform.localScale =
                m_candidateBaseScales[i] * scaleMultiplier;
        }
    }

    /// <summary>
    /// 画面内の左・中央・右に近い観客へNodeを一つずつ作成します。
    /// </summary>
    [ContextMenu("Show Event Audience Nodes")]
    public void ShowNodes()
    {
        FindReferences();
        ConfigureCanvas();
        ClearNodes();
        if (m_audienceSpawner == null || m_preferenceSystem == null)return;

        IReadOnlyList<AudienceReaction> audiences =
            m_audienceSpawner.Audiences; //生成済み観客
        if (audiences.Count == 0)return;

        HashSet<AudienceReaction> selectedAudiences =
            new HashSet<AudienceReaction>(); //三地点で選択済みの観客
        for (int i = 0; i < EPreferenceCount; ++i)
        {
            AudienceReaction audience = FindAudienceNearViewportPoint(
                audiences,
                ETargetViewportXs[i],
                selectedAudiences); //左・中央・右の代表観客
            if (audience != null)
            {
                selectedAudiences.Add(audience);
            }

            Vector3 preferences = Vector3.one / EPreferenceCount; //未取得時の均等好み
            if (audience != null)
            {
                if (m_preferenceSystem.TryGetPreferences(
                    audience,
                    out Vector3 audiencePreferences))
                {
                    preferences = audiencePreferences;
                }
            }

            CreateAudienceNode(
                audience,
                i,
                GetPreference(preferences, i),
                GetNodeAnchor(i),
                GetCandidatePoseId(i),
                GetCandidatePoseName(i));
        }
    }

    /// <summary>
    /// 生成済みCanvas Nodeを全て削除します。
    /// </summary>
    [ContextMenu("Clear Event Audience Nodes")]
    public void ClearNodes()
    {
        for (int i = 0; i < m_displayedNodesList.Count; ++i)
        {
            if (m_displayedNodesList[i].m_root != null)
            {
                Destroy(m_displayedNodesList[i].m_root.gameObject);
            }
        }

        m_displayedNodesList.Clear();
        m_candidateButtons.Clear();
        m_candidateBaseScales.Clear();
        m_decisionCues.Clear();
        b_m_selectionEnabled = false;
    }

    /// <summary>
    /// 候補を強調表示しますが、クリック入力は受け付けません。
    /// 実際のプレイヤーポーズで候補を決定する場合に使用します。
    /// </summary>
    public void SetPoseDetectionEnabled(bool _enabled)
    {
        b_m_selectionEnabled = _enabled;
        for (int i = 0; i < m_candidateButtons.Count; ++i)
        {
            Button button = m_candidateButtons[i];
            if (button == null)continue;

            button.interactable = false;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = _enabled
                    ? Color.white
                    : new Color(0.35f, 0.35f, 0.35f, 0.8f);
            }

            Outline outline = button.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = _enabled
                    ? new Color(0.15f, 1.0f, 0.35f, 1.0f)
                    : m_nodeOutlineColor;
                outline.effectDistance = _enabled
                    ? new Vector2(14.0f, -14.0f)
                    : m_nodeOutlineDistance;
            }
        }

        for (int i = 0; i < m_decisionCues.Count; ++i)
        {
            if (m_decisionCues[i] != null)
            {
                m_decisionCues[i].SetActive(_enabled);
            }
        }
    }

    /// <summary>
    /// 指定観客の頭上へ指定種類のNodeを一つ生成します。
    /// </summary>
    private void CreateAudienceNode(
        AudienceReaction _audience,
        int _preferenceIndex,
        float _preference,
        Transform _anchor,
        int _poseId,
        string _poseName)
    {
        GameObject rootObject = new GameObject(
            $"{ENodeRootName}_{_preferenceIndex + 1}",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(GraphicRaycaster)); //観客頭上World Space Canvas
        RectTransform root = rootObject.GetComponent<RectTransform>(); //親Rect
        root.sizeDelta = m_nodeSize * EMaximumNodeScale;
        if (_anchor != null)
        {
            root.SetParent(_anchor, false);
            root.localPosition = Vector3.zero;
        }
        else if (_audience != null)
        {
            root.position = _audience.transform.position + m_worldHeadOffset;
        }

        root.localScale = Vector3.one * Mathf.Max(0.0001f, m_worldCanvasScale);
        root.rotation = m_eventCamera.transform.rotation;

        Canvas worldCanvas = rootObject.GetComponent<Canvas>(); //World表示Canvas
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = m_eventCamera;
        worldCanvas.overrideSorting = true;
        worldCanvas.sortingOrder = 50;
        CreateSpeechBubble(root);
        CreateNode(
            root,
            _preferenceIndex,
            _preference,
            _poseId,
            _poseName);

        m_displayedNodesList.Add(new SDisplayedAudienceNodes
        {
            m_audience = _audience,
            m_root = root
        });
    }

    /// <summary>Inspector設定を使用してPose画像の背面へ吹き出しを生成します。</summary>
    private void CreateSpeechBubble(RectTransform _parent)
    {
        if (m_speechBubbleSprite == null)return;

        GameObject bubbleObject = new GameObject(
            "SpeechBubble",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform bubbleRect = bubbleObject.GetComponent<RectTransform>();
        bubbleRect.SetParent(_parent, false);
        bubbleRect.sizeDelta = m_speechBubbleSize;
        bubbleRect.anchoredPosition = m_speechBubbleOffset;

        Image bubbleImage = bubbleObject.GetComponent<Image>();
        bubbleImage.sprite = m_speechBubbleSprite;
        bubbleImage.color = m_speechBubbleColor;
        bubbleImage.preserveAspect = true;
        bubbleImage.raycastTarget = false;
        bubbleRect.SetAsFirstSibling();
    }

    /// <summary>
    /// 指定種類に登録された手動配置Anchorを取得します。
    /// </summary>
    private Transform GetNodeAnchor(int _index)
    {
        if (m_nodeAnchors == null
            || _index < 0
            || _index >= m_nodeAnchors.Length)return null;

        return m_nodeAnchors[_index];
    }

    /// <summary>
    /// 一種類のNode Image、Button、Percentage Textを作成します。
    /// </summary>
    private void CreateNode(
        RectTransform _parent,
        int _index,
        float _preference,
        int _poseId,
        string _poseName)
    {
        GameObject nodeObject = new GameObject(
            $"EventNode_{_index + 1}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)); //Node UI
        RectTransform nodeRect = nodeObject.GetComponent<RectTransform>(); //Node Rect
        nodeRect.SetParent(_parent, false);
        nodeRect.sizeDelta = new Vector2(
            Mathf.Max(EMinimumNodePixelSize, m_nodeSize.x),
            Mathf.Max(EMinimumNodePixelSize, m_nodeSize.y));
        nodeRect.anchoredPosition = Vector2.zero;
        float scale = Mathf.Lerp(
            EMinimumNodeScale,
            EMaximumNodeScale,
            _preference); //好みに応じた倍率
        nodeRect.localScale = Vector3.one * scale;

        Image image = nodeObject.GetComponent<Image>(); //Node画像
        image.sprite = m_nodeSprites != null
            && _poseId >= 0
            && _poseId < m_nodeSprites.Length
            ? m_nodeSprites[_poseId]
            : null;
        image.color = image.sprite != null
            ? Color.white
            : m_nodeColors != null && _index < m_nodeColors.Length
                ? m_nodeColors[_index]
                : Color.white;

        Outline outline = nodeObject.AddComponent<Outline>(); //三Node共通強調枠
        outline.effectColor = m_nodeOutlineColor;
        outline.effectDistance = m_nodeOutlineDistance;
        outline.useGraphicAlpha = true;

        Button button = nodeObject.GetComponent<Button>(); //選択Button
        m_candidateButtons.Add(button);
        m_candidateBaseScales.Add(nodeRect.localScale);
        CreatePoseNameText(nodeRect, _poseName);
        m_decisionCues.Add(CreateDecisionCueText(nodeRect));
        if (b_m_showPercentage)
        {
            CreatePercentageText(nodeRect, _preference);
        }
    }

    private static GameObject CreateDecisionCueText(RectTransform _parent)
    {
        GameObject textObject = new GameObject(
            "DecisionCue",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(_parent, false);
        textRect.anchorMin = new Vector2(0.0f, 1.0f);
        textRect.anchorMax = new Vector2(1.0f, 1.0f);
        textRect.pivot = new Vector2(0.5f, 0.0f);
        textRect.anchoredPosition = new Vector2(0.0f, 24.0f);
        textRect.sizeDelta = new Vector2(0.0f, 90.0f);

        Text text = textObject.GetComponent<Text>();
        text.text = "POSE NOW!";
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 42;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(0.2f, 1.0f, 0.35f, 1.0f);
        text.raycastTarget = false;
        textObject.SetActive(false);
        return textObject;
    }

    private static int GetCandidatePoseId(int _index)
    {
        MusicEventSceneData eventData = EventNodeRuntimeContext.CurrentEvent;
        if (eventData == null
            || !eventData.TryGetAudienceChoiceCandidate(
                _index,
                out SMusicNodeEvent candidate))return _index;

        return candidate.m_poseId;
    }

    private static string GetCandidatePoseName(int _index)
    {
        MusicEventSceneData eventData = EventNodeRuntimeContext.CurrentEvent;
        if (eventData == null
            || !eventData.TryGetAudienceChoiceCandidate(
                _index,
                out SMusicNodeEvent candidate))
        {
            return $"Pose {_index + 1}";
        }

        string poseName = candidate.m_eventName;
        return string.IsNullOrWhiteSpace(poseName)
            ? $"Pose {_index + 1}"
            : poseName;
    }

    private static void CreatePoseNameText(
        RectTransform _parent,
        string _poseName)
    {
        GameObject textObject = new GameObject(
            "PoseName",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(_parent, false);
        textRect.anchorMin = new Vector2(0.0f, 0.0f);
        textRect.anchorMax = new Vector2(1.0f, 0.0f);
        textRect.pivot = new Vector2(0.5f, 1.0f);
        textRect.anchoredPosition = new Vector2(0.0f, -18.0f);
        textRect.sizeDelta = new Vector2(0.0f, 70.0f);

        Text text = textObject.GetComponent<Text>();
        text.text = _poseName;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    /// <summary>
    /// 指定Viewport横位置と観客席上部に最も近い画面内観客を取得します。
    /// </summary>
    private AudienceReaction FindAudienceNearViewportPoint(
        IReadOnlyList<AudienceReaction> _audiences,
        float _targetViewportX,
        HashSet<AudienceReaction> _excludedAudiences)
    {
        const float ETargetViewportY = 0.68f; //頭上Node表示の基準高さ
        AudienceReaction nearestAudience = null; //最も近い観客
        float nearestDistance = float.MaxValue; //最小Viewport距離
        for (int i = 0; i < _audiences.Count; ++i)
        {
            AudienceReaction audience = _audiences[i]; //候補観客
            if (audience == null || _excludedAudiences.Contains(audience))continue;
            if (!HasEnoughWorldSeparation(
                audience,
                _excludedAudiences))continue;

            Vector3 viewportPosition = m_eventCamera.WorldToViewportPoint(
                audience.transform.position + m_worldHeadOffset); //画面内位置
            if (viewportPosition.z <= 0.0f
                || viewportPosition.x < 0.0f
                || viewportPosition.x > 1.0f
                || viewportPosition.y < 0.0f
                || viewportPosition.y > 1.0f)continue;

            float horizontalDistance = Mathf.Abs(
                viewportPosition.x - _targetViewportX); //横方向距離
            float verticalDistance = Mathf.Abs(
                viewportPosition.y - ETargetViewportY); //縦方向距離
            float depthDistance = viewportPosition.z * 0.006f; //手前観客優先距離
            float distance = horizontalDistance
                + verticalDistance * 0.35f
                + depthDistance; //位置と奥行きを含む選択距離
            if (distance >= nearestDistance)continue;

            nearestDistance = distance;
            nearestAudience = audience;
        }

        return nearestAudience;
    }

    /// <summary>
    /// 選択済み観客から十分離れたWorld位置にいるか判定します。
    /// </summary>
    private bool HasEnoughWorldSeparation(
        AudienceReaction _audience,
        HashSet<AudienceReaction> _selectedAudiences)
    {
        foreach (AudienceReaction selectedAudience in _selectedAudiences)
        {
            if (selectedAudience == null)continue;

            Vector3 difference = _audience.transform.position
                - selectedAudience.transform.position; //観客間World距離
            difference.y = 0.0f;
            if (difference.magnitude < m_minimumWorldSeparation)return false;
        }

        return true;
    }

    /// <summary>
    /// Node中央へ好みPercentageを表示します。
    /// </summary>
    private static void CreatePercentageText(
        RectTransform _parent,
        float _preference)
    {
        GameObject textObject = new GameObject(
            "PreferencePercent",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text)); //Percentage表示
        RectTransform textRect = textObject.GetComponent<RectTransform>(); //Text Rect
        textRect.SetParent(_parent, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>(); //Percentage Text
        text.text = $"{_preference:P0}";
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    /// <summary>
    /// Screen Space Camera表示へCanvasを設定します。
    /// </summary>
    private void ConfigureCanvas()
    {
        if (m_canvas == null)return;

        m_canvas.renderMode = RenderMode.ScreenSpaceCamera;
        m_canvas.worldCamera = m_eventCamera;
        m_canvas.planeDistance = 1.0f;
        CanvasScaler scaler = m_canvas.GetComponent<CanvasScaler>(); //画面Scale調整
        if (scaler == null)
        {
            scaler = m_canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.matchWidthOrHeight = 0.5f;
        if (m_canvas.GetComponent<GraphicRaycaster>() == null)
        {
            m_canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    /// <summary>
    /// 好みVectorから指定番号の値を取得します。
    /// </summary>
    private static float GetPreference(
        Vector3 _preferences,
        int _index)
    {
        switch (_index)
        {
            case 0:
                return _preferences.x;
            case 1:
                return _preferences.y;
            default:
                return _preferences.z;
        }
    }

    /// <summary>
    /// 未設定のScene参照を補完します。
    /// </summary>
    private void FindReferences()
    {
        if (m_canvas == null)
        {
            m_canvas = GetComponent<Canvas>();
        }

        CinemachineBrain brain =
            FindFirstObjectByType<CinemachineBrain>(); //実際に描画するBrain
        if (brain != null && brain.GetComponent<Camera>() != null)
        {
            m_eventCamera = brain.GetComponent<Camera>();
        }
        else if (m_eventCamera == null)
        {
            m_eventCamera = Camera.main;
        }

        if (m_audienceSpawner == null)
        {
            m_audienceSpawner = FindFirstObjectByType<AudienceAreaSpawner>();
        }

        if (m_preferenceSystem == null)
        {
            m_preferenceSystem = FindFirstObjectByType<AudiencePreferenceSystem>();
        }
    }
}
