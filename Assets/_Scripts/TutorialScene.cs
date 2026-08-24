using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 3種類のポーズを成功するまで練習し、全て成功したらGameplayへ遷移します。
/// </summary>
public sealed class TutorialScene : MonoBehaviour
{
    private const string EMediaPipeSceneName = "Holistic";
    private const int EPoseCount = 3;

    [Header("Tutorial Assets")]
    [SerializeField] private GameObject m_characterPrefab;
    [SerializeField] private RectTransform m_poseCanvasPrefab;

    [Header("Attempt Timing")]
    [SerializeField, Min(0.5f)] private float m_attemptSeconds = 3.0f;
    [SerializeField, Min(0.0f)] private float m_resultDisplaySeconds = 1.0f;
    [SerializeField, Min(0.0f)] private float m_requiredHoldSeconds = 0.3f;
    [SerializeField, Range(1.0f, 2.0f)] private float m_approachingStartScale = 1.4f;

    [Header("Character Placement")]
    [SerializeField] private Vector3 m_characterPosition = new Vector3(0.0f, -4.6f, 100.0f);
    [SerializeField] private Vector3 m_characterEulerAngles = new Vector3(0.0f, 180.0f, 0.0f);
    [SerializeField] private float m_characterScale = 5.0f;

    [Header("Pose Frame Placement")]
    [SerializeField, Range(1.0f, 1.5f)] private float m_framePadding = 1.12f;
    [SerializeField] private Vector2 m_frameScreenOffset = Vector2.zero;

    private readonly string[] m_posePrefixes = { "Front", "Most", "Side" };
    private List<CSVPoseData> m_poseDatas;
    private GameObject m_poseCanvasInstance;
    private GameObject m_characterInstance;
    private Camera m_tutorialCamera;
    private Text m_statusText;
    private Image m_successFlash;
    private Text m_successText;
    private readonly List<Image> m_successRays = new List<Image>();
    [SerializeField] private ArmController m_armController;
    [SerializeField] private BodyController m_bodyController;
    private int m_currentPoseIndex;
    private float m_attemptElapsed;
    private float m_poseHoldElapsed;
    private bool b_m_attemptRunning;
    private bool b_m_resolvingAttempt;

    private GameObject m_approachingFrame;
    private GameObject m_waitingFrame;
    private GameObject m_successFrame;
    private GameObject m_failureFrame;
    private Vector3 m_waitingScale;

    private IEnumerator Start()
    {
        PrepareTutorialPresentation();
        LoadPoseData();
        ShowCurrentPoseFrames();
        SetStatus("カメラの前に全身が映るように立ってください…");

        Scene mediaPipeScene = SceneManager.GetSceneByName(EMediaPipeSceneName);
        if (!mediaPipeScene.isLoaded)
        {
            yield return SceneManager.LoadSceneAsync(EMediaPipeSceneName, LoadSceneMode.Additive);
        }

        while (!HasTrackingData())
        {
            SetStatus("カメラの前に全身が映るように立ってください…");
            yield return null;
        }

        m_armController.enabled = true;
        m_bodyController.enabled = true;
        BeginAttempt();
    }

    private void Update()
    {
        if (!b_m_attemptRunning || b_m_resolvingAttempt)return;

        m_attemptElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(m_attemptElapsed / m_attemptSeconds);
        if (m_approachingFrame != null)
        {
            Vector3 startScale = m_waitingScale * m_approachingStartScale;
            m_approachingFrame.transform.localScale = Vector3.Lerp(startScale, m_waitingScale, progress);
        }

        bool b_poseMatched = IsCurrentPoseMatched();
        bool b_inJudgeWindow = progress >= 0.75f;
        if (b_inJudgeWindow && b_poseMatched)
        {
            m_poseHoldElapsed += Time.deltaTime;
            if (m_poseHoldElapsed >= m_requiredHoldSeconds)
            {
                StartCoroutine(ResolveAttempt(true));
                return;
            }
        }
        else
        {
            m_poseHoldElapsed = 0.0f;
        }

        if (m_attemptElapsed >= m_attemptSeconds)
        {
            StartCoroutine(ResolveAttempt(false));
        }
    }

    private void PrepareTutorialPresentation()
    {
        SetNamedObjectActive("Panel", false);
        SetNamedObjectActive("PLAY Button", false);
        CreateStatusText();
        CreateSuccessPresentation();

        if (m_poseCanvasPrefab != null)
        {
            RectTransform poseCanvasTransform = Instantiate(m_poseCanvasPrefab);
            m_poseCanvasInstance = poseCanvasTransform.gameObject;
            Canvas poseCanvas = m_poseCanvasInstance.GetComponent<Canvas>();
            if (poseCanvas != null)
            {
                poseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                poseCanvas.worldCamera = null;
                poseCanvas.sortingOrder = 10;
            }
            poseCanvasTransform.anchorMin = Vector2.zero;
            poseCanvasTransform.anchorMax = Vector2.one;
            poseCanvasTransform.offsetMin = Vector2.zero;
            poseCanvasTransform.offsetMax = Vector2.zero;
            poseCanvasTransform.localScale = Vector3.one;
            HideAllPoseFrames();
        }

        GameObject character = GameObject.Find("Tutorial Character");
        if (character == null && m_characterPrefab != null)
        {
            character = Instantiate(
                m_characterPrefab,
                m_characterPosition,
                Quaternion.Euler(m_characterEulerAngles));
            character.name = "Tutorial Character";
            character.transform.localScale = Vector3.one * m_characterScale;
        }
        if (character == null)return;

        m_characterInstance = character;
        m_tutorialCamera = FindFirstObjectByType<Camera>();

        HumanoidSkeleton skeleton = character.GetComponentInChildren<HumanoidSkeleton>(true);
        if (skeleton == null)return;

        if (m_armController == null)m_armController = gameObject.AddComponent<ArmController>();
        m_armController.playerArm = skeleton;
        m_armController.enabled = false;

        if (m_bodyController == null)m_bodyController = gameObject.AddComponent<BodyController>();
        m_bodyController.playerBody = skeleton;
        m_bodyController.enabled = false;
    }

    private void LoadPoseData()
    {
        ExcelPoseJudgeLoader loader = new ExcelPoseJudgeLoader();
        loader.LoadCsv();
        m_poseDatas = loader.GetCSVDatas();
    }

    private void BeginAttempt()
    {
        if (m_currentPoseIndex >= EPoseCount)
        {
            GameSession.Load(GameSession.GameplayScene);
            return;
        }

        ShowCurrentPoseFrames();

        m_attemptElapsed = 0.0f;
        m_poseHoldElapsed = 0.0f;
        b_m_resolvingAttempt = false;
        b_m_attemptRunning = true;
        SetStatus($"ポーズ {m_currentPoseIndex + 1} / {EPoseCount}\n動く枠が重なるタイミングでポーズを合わせよう！");
    }

    /// <summary>現在のポーズ枠を、カメラ初期化状態に関係なく表示します。</summary>
    private void ShowCurrentPoseFrames()
    {
        if (m_currentPoseIndex < 0 || m_currentPoseIndex >= m_posePrefixes.Length)return;

        HideAllPoseFrames();
        string prefix = m_posePrefixes[m_currentPoseIndex];
        m_approachingFrame = FindPoseFrame(prefix + "_ApproachingFrame");
        m_waitingFrame = FindPoseFrame(prefix + "_WaitingFrame");
        m_successFrame = FindPoseFrame(prefix + "_SuccessFrame");
        m_failureFrame = FindPoseFrame(prefix + "_FailureFrame");

        AlignCurrentPoseFramesToCharacter();

        SetActive(m_approachingFrame, true);
        SetActive(m_waitingFrame, true);
        m_waitingScale = m_waitingFrame != null
            ? m_waitingFrame.transform.localScale
            : Vector3.one;
        if (m_approachingFrame != null)
        {
            m_approachingFrame.transform.localScale =
                m_waitingScale * m_approachingStartScale;
        }
    }

    /// <summary>キャラクターの画面上の中心と外形へ、現在のポーズ枠を揃えます。</summary>
    private void AlignCurrentPoseFramesToCharacter()
    {
        if (m_poseCanvasInstance == null || m_characterInstance == null)return;

        Camera targetCamera = m_tutorialCamera != null
            ? m_tutorialCamera
            : FindFirstObjectByType<Camera>();
        RectTransform canvasRect = m_poseCanvasInstance.transform as RectTransform;
        if (targetCamera == null || canvasRect == null)return;

        Renderer[] renderers = m_characterInstance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)return;

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; ++i)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;
        Vector2 screenMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 screenMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        bool b_hasVisiblePoint = false;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 screenPoint = targetCamera.WorldToScreenPoint(corner);
                    if (screenPoint.z <= 0.0f)continue;

                    b_hasVisiblePoint = true;
                    screenMin = Vector2.Min(screenMin, screenPoint);
                    screenMax = Vector2.Max(screenMax, screenPoint);
                }
            }
        }

        if (!b_hasVisiblePoint)return;

        Vector2 screenCenter = (screenMin + screenMax) * 0.5f + m_frameScreenOffset;
        float frameSize = Mathf.Max(screenMax.x - screenMin.x, screenMax.y - screenMin.y)
            * m_framePadding;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenCenter, null, out Vector2 localCenter))return;

        FitFrame(m_approachingFrame, canvasRect, localCenter, frameSize);
        FitFrame(m_waitingFrame, canvasRect, localCenter, frameSize);
        FitFrame(m_successFrame, canvasRect, localCenter, frameSize);
        FitFrame(m_failureFrame, canvasRect, localCenter, frameSize);
    }

    private static void FitFrame(
        GameObject _frame,
        RectTransform _canvasRect,
        Vector2 _localCenter,
        float _size)
    {
        if (_frame == null)return;

        RectTransform frameRect = _frame.transform as RectTransform;
        if (frameRect == null)return;

        frameRect.SetParent(_canvasRect, false);
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.anchoredPosition = _localCenter;
        frameRect.sizeDelta = Vector2.one * _size;
        frameRect.localRotation = Quaternion.identity;
        frameRect.localScale = Vector3.one;
    }

    private IEnumerator ResolveAttempt(bool _success)
    {
        if (b_m_resolvingAttempt)yield break;

        b_m_resolvingAttempt = true;
        b_m_attemptRunning = false;
        SetActive(m_approachingFrame, false);
        SetActive(m_waitingFrame, false);
        SetActive(_success ? m_successFrame : m_failureFrame, true);

        if (_success)
        {
            SetStatus("成功！ 次のポーズへ進みます");
            StartCoroutine(PlaySuccessPresentation());
            ++m_currentPoseIndex;
        }
        else
        {
            SetStatus($"失敗… ポーズ {m_currentPoseIndex + 1}をもう一度！");
        }

        yield return new WaitForSeconds(m_resultDisplaySeconds);

        if (m_currentPoseIndex >= EPoseCount)
        {
            SetStatus("チュートリアルクリア！");
            yield return new WaitForSeconds(0.5f);
            GameSession.Load(GameSession.GameplayScene);
            yield break;
        }

        BeginAttempt();
    }

    private bool IsCurrentPoseMatched()
    {
        if (m_poseDatas == null
            || m_currentPoseIndex < 0
            || m_currentPoseIndex >= m_poseDatas.Count
            || AngleDataManager.Instance == null
            || AngleDataManager.Instance.angleData == null)return false;

        float[] angles = AngleDataManager.Instance.angleData.angle;
        if (angles == null || angles.Length < 4)return false;

        CSVPoseData pose = m_poseDatas[m_currentPoseIndex];
        return IsInRange(angles[0], pose.LeftelbowRotation)
            && IsInRange(angles[1], pose.LeftShoulderRotation)
            && IsInRange(angles[2], pose.RightelbowRotation)
            && IsInRange(angles[3], pose.RightShoulderRotation);
    }

    /// <summary>キャラクター制御に必要な身体座標と角度が届いているか確認します。</summary>
    private bool HasTrackingData()
    {
        if (m_armController == null
            || m_bodyController == null
            || PositionDataManager.Instance == null
            || PositionDataManager.Instance.positionData == null
            || AngleDataManager.Instance == null
            || AngleDataManager.Instance.angleData == null)return false;

        Vector3[] body = PositionDataManager.Instance.positionData.Body;
        if (body == null || body.Length < 37)return false;

        Vector3 leftShoulder = body[(int)MediapipeBodyPart.left_shoulder];
        Vector3 rightShoulder = body[(int)MediapipeBodyPart.right_shoulder];
        Vector3 leftWrist = body[(int)MediapipeBodyPart.left_wrist];
        Vector3 rightWrist = body[(int)MediapipeBodyPart.right_wrist];
        return (leftShoulder - rightShoulder).sqrMagnitude > Mathf.Epsilon
            && (leftWrist - leftShoulder).sqrMagnitude > Mathf.Epsilon
            && (rightWrist - rightShoulder).sqrMagnitude > Mathf.Epsilon;
    }

    private static bool IsInRange(float _angle, Vector3 _expected)
    {
        return !float.IsNaN(_angle)
            && !float.IsInfinity(_angle)
            && _angle >= _expected.x - _expected.y
            && _angle <= _expected.x + _expected.y;
    }

    private void HideAllPoseFrames()
    {
        for (int poseIndex = 0; poseIndex < m_posePrefixes.Length; ++poseIndex)
        {
            string prefix = m_posePrefixes[poseIndex];
            SetActive(FindPoseFrame(prefix + "_ApproachingFrame"), false);
            SetActive(FindPoseFrame(prefix + "_WaitingFrame"), false);
            SetActive(FindPoseFrame(prefix + "_SuccessFrame"), false);
            SetActive(FindPoseFrame(prefix + "_FailureFrame"), false);
        }
    }

    private GameObject FindPoseFrame(string _name)
    {
        if (m_poseCanvasInstance == null)return null;

        Transform[] transforms = m_poseCanvasInstance.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; ++i)
        {
            if (transforms[i].name == _name)return transforms[i].gameObject;
        }
        return null;
    }

    private void CreateStatusText()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)return;

        GameObject statusObject = new GameObject(
            "Tutorial Progress",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        statusObject.transform.SetParent(canvas.transform, false);
        RectTransform rect = statusObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1.0f);
        rect.anchorMax = new Vector2(0.5f, 1.0f);
        rect.pivot = new Vector2(0.5f, 1.0f);
        rect.anchoredPosition = new Vector2(0.0f, -100.0f);
        rect.sizeDelta = new Vector2(1100.0f, 150.0f);

        m_statusText = statusObject.GetComponent<Text>();
        m_statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        m_statusText.fontSize = 32;
        m_statusText.alignment = TextAnchor.MiddleCenter;
        m_statusText.color = Color.white;
        m_statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
        m_statusText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    /// <summary>成功時だけ最前面に表示するFlash、放射光、文字を生成します。</summary>
    private void CreateSuccessPresentation()
    {
        GameObject canvasObject = new GameObject(
            "Tutorial Success Presentation",
            typeof(RectTransform),
            typeof(Canvas));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        GameObject flashObject = new GameObject(
            "Success Flash",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        flashObject.transform.SetParent(canvasObject.transform, false);
        RectTransform flashRect = flashObject.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        m_successFlash = flashObject.GetComponent<Image>();
        m_successFlash.raycastTarget = false;
        m_successFlash.color = new Color(1.0f, 0.78f, 0.08f, 0.0f);

        const int rayCount = 20;
        for (int rayIndex = 0; rayIndex < rayCount; ++rayIndex)
        {
            GameObject rayObject = new GameObject(
                $"Success Ray {rayIndex + 1}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            rayObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rayRect = rayObject.GetComponent<RectTransform>();
            rayRect.anchorMin = new Vector2(0.5f, 0.5f);
            rayRect.anchorMax = new Vector2(0.5f, 0.5f);
            rayRect.pivot = new Vector2(0.0f, 0.5f);
            rayRect.anchoredPosition = Vector2.zero;
            rayRect.sizeDelta = new Vector2(520.0f, 10.0f);
            rayRect.localRotation = Quaternion.Euler(
                0.0f,
                0.0f,
                rayIndex * (360.0f / rayCount));
            Image ray = rayObject.GetComponent<Image>();
            ray.raycastTarget = false;
            ray.color = new Color(1.0f, 0.72f, 0.05f, 0.0f);
            m_successRays.Add(ray);
        }

        GameObject textObject = new GameObject(
            "Success Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(1000.0f, 220.0f);
        m_successText = textObject.GetComponent<Text>();
        m_successText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        m_successText.fontSize = 96;
        m_successText.fontStyle = FontStyle.Bold;
        m_successText.alignment = TextAnchor.MiddleCenter;
        m_successText.raycastTarget = false;
        m_successText.text = "PERFECT!";
        m_successText.color = new Color(1.0f, 0.9f, 0.2f, 0.0f);
        m_successText.rectTransform.localScale = Vector3.zero;
    }

    /// <summary>金色Flashと放射光を広げ、PERFECT文字を勢いよく表示します。</summary>
    private IEnumerator PlaySuccessPresentation()
    {
        if (m_successFlash == null || m_successText == null)yield break;

        const float animationSeconds = 0.9f;
        float elapsed = 0.0f;
        while (elapsed < animationSeconds)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / animationSeconds);
            float appear = Mathf.Clamp01(progress / 0.2f);
            float fade = 1.0f - Mathf.Clamp01((progress - 0.55f) / 0.45f);
            float alpha = appear * fade;

            m_successFlash.color = new Color(
                1.0f,
                0.78f,
                0.08f,
                0.48f * alpha);
            for (int rayIndex = 0; rayIndex < m_successRays.Count; ++rayIndex)
            {
                Image ray = m_successRays[rayIndex];
                ray.color = new Color(1.0f, 0.72f, 0.05f, 0.78f * alpha);
                ray.rectTransform.localScale = new Vector3(
                    Mathf.Lerp(0.15f, 1.35f, progress),
                    Mathf.Lerp(2.5f, 0.2f, progress),
                    1.0f);
            }

            float punchScale = progress < 0.28f
                ? Mathf.Lerp(0.1f, 1.35f, progress / 0.28f)
                : Mathf.Lerp(1.35f, 1.0f, (progress - 0.28f) / 0.72f);
            m_successText.rectTransform.localScale = Vector3.one * punchScale;
            m_successText.rectTransform.localRotation = Quaternion.Euler(
                0.0f,
                0.0f,
                Mathf.Sin(progress * Mathf.PI * 4.0f) * 4.0f * fade);
            m_successText.color = new Color(1.0f, 0.9f, 0.2f, alpha);
            yield return null;
        }

        m_successFlash.color = new Color(1.0f, 0.78f, 0.08f, 0.0f);
        for (int rayIndex = 0; rayIndex < m_successRays.Count; ++rayIndex)
        {
            m_successRays[rayIndex].color = new Color(1.0f, 0.72f, 0.05f, 0.0f);
        }
        m_successText.color = new Color(1.0f, 0.9f, 0.2f, 0.0f);
        m_successText.rectTransform.localScale = Vector3.zero;
    }

    private void SetStatus(string _message)
    {
        if (m_statusText != null)m_statusText.text = _message;
    }

    private static void SetNamedObjectActive(string _name, bool _active)
    {
        GameObject target = GameObject.Find(_name);
        if (target != null)target.SetActive(_active);
    }

    private static void SetActive(GameObject _target, bool _active)
    {
        if (_target != null)_target.SetActive(_active);
    }
}
