/*━━━━━━━━━*
*@file AudienceAreaSpawner.cs*
*@brief 指定範囲へ観客を等間隔と誤差付きで生成する*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks MeshまたはPrefabをInspectorから設定*
*━━━━━━━━━*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 矩形範囲へ指定人数の観客をGrid配置します。
/// </summary>
public sealed class AudienceAreaSpawner : MonoBehaviour
{
    private const string EAudienceName = "Audience"; //生成Object名
    private const int EMinimumAudienceCount = 1; //最小人数
    private const float EMinimumIntervalSeconds = 0.1f; //最短Reaction間隔
    private const float EMinimumCullingInterval = 0.02f; //最短可視判定間隔
    private const float ERaycastMargin = 1.0f; //生成範囲上下のRay余白
    private const float EMinimumSuccessReactionRatio = 0.2f; //最低成功反応人数率
    private const float EMaximumSuccessReactionRatio = 1.0f; //最高成功反応人数率
    private const float EFailureReactionRatio = 0.45f; //失敗反応人数率
    private const float EMinimumSuccessStrength = 0.7f; //最低成功動作強度
    private const float EMaximumSuccessStrength = 1.8f; //最高成功動作強度
    private const float EFailureReactionStrength = 0.8f; //失敗動作強度


    [SerializeField,Range(0.1f,1.0f)] private float m_spawnAreaScale; 

    [SerializeField] private GameObject m_audiencePrefab; //任意の観客Prefab
    [SerializeField] private Mesh m_audienceMesh; //Prefab未使用時のMesh
    [SerializeField] private Material[] m_materials; //観客Material一覧
    [SerializeField] private GameObject m_generationVolumeObject; //生成範囲Cube
    [SerializeField] private Vector2 m_areaSize = new Vector2(20.0f, 12.0f); //配置範囲
    [SerializeField] private float m_areaHeight = 3.0f; //生成範囲の高さ
    [SerializeField] private bool b_m_showAreaVolumes = true; //半透明範囲表示
    [SerializeField] private GameObject[] m_exclusionObjects; //生成禁止Cube一覧
    [SerializeField] private LayerMask m_groundLayers = ~0; //床として判定するLayer
    [SerializeField] private float m_groundOffset; //足元と床の追加距離
    [SerializeField] private int m_audienceCount = 100; //生成人数
    [SerializeField] private Vector2 m_positionError = new Vector2(0.25f, 0.25f); //位置誤差
    [SerializeField] private Vector2 m_scaleRange = new Vector2(0.9f, 1.1f); //Scale差
    [SerializeField] private float m_yawError = 8.0f; //Y回転誤差
    [SerializeField] private Transform m_facingTarget; //観客が向くプレイヤー位置
    [SerializeField] private float m_modelYawOffset; //Prefab正面方向の補正
    [SerializeField] private bool b_m_spawnOnStart = true; //開始時自動生成
    [SerializeField] private bool b_m_autoReaction = true; //自動Reaction
    [SerializeField] private Vector2 m_reactionIntervalRange =
        new Vector2(1.0f, 2.5f); //Reaction間隔
    [SerializeField] private bool b_m_enableCameraCulling = true; //画面外の観客を無効化
    [SerializeField] private Camera m_targetCamera; //可視範囲を使用するCamera
    [SerializeField] private float m_cullingInterval = 0.15f; //可視判定間隔
    [SerializeField] private float m_cullingMargin = 0.5f; //画面端判定の余白
    [Header("Voltage Reactions")]
    [SerializeField] private VenueVoltageSystem m_voltageSystem; //成功失敗通知元
    [SerializeField] private bool b_m_useVoltageReactions = true; //判定連動を使用するか
    [SerializeField] private float m_minimumSuccessReactionRatio =
        EMinimumSuccessReactionRatio; //Voltage最低時の成功反応人数率
    [SerializeField] private float m_maximumSuccessReactionRatio =
        EMaximumSuccessReactionRatio; //Voltage最高時の成功反応人数率
    [SerializeField] private float m_failureReactionRatio =
        EFailureReactionRatio; //失敗時の反応人数率
    [SerializeField] private float m_minimumSuccessStrength =
        EMinimumSuccessStrength; //Voltage最低時の成功動作強度
    [SerializeField] private float m_maximumSuccessStrength =
        EMaximumSuccessStrength; //Voltage最高時の成功動作強度
    [SerializeField] private float m_failureReactionStrength =
        EFailureReactionStrength; //失敗時の動作強度

    [Header("Audience Voices")]
    [SerializeField] private AudioClip[] m_cheerVoiceClips;
    [SerializeField] private AudioClip[] m_disappointedVoiceClips;
    [SerializeField, Range(0.0f, 1.0f)] private float m_minimumVoiceVolume = 0.2f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_maximumVoiceVolume = 0.85f;
    [SerializeField, Min(1)] private int m_minimumSimultaneousVoices = 1;
    [SerializeField, Min(1)] private int m_maximumSimultaneousVoices = 5;
    [SerializeField, Range(0.0f, 1.0f)] private float m_preferenceVolumeBoost = 0.2f;
    [SerializeField] private Vector2 m_voicePitchRange = new Vector2(0.92f, 1.08f);
    [SerializeField, Range(0.0f, 0.5f)] private float m_voiceVolumeVariation = 0.08f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_voiceStereoPanRange = 0.3f;
    [SerializeField, Range(0.0f, 0.25f)] private float m_voiceStartOffsetSeconds = 0.04f;
    [SerializeField] private Vector2 m_voiceLowPassRange = new Vector2(16000.0f, 22000.0f);
    [SerializeField] private Vector2 m_voiceReverbMixRange = new Vector2(0.0f, 0.18f);

    private readonly List<AudienceReaction> m_audiences =
        new List<AudienceReaction>(); //生成済み観客一覧
    private readonly List<AudienceReaction> m_visibleAudiences =
        new List<AudienceReaction>(); //現在画面内の観客一覧
    private readonly List<AudioSource> m_voiceSources =
        new List<AudioSource>(); //歓声を重ねるため実行時に生成・再利用するAudioSource一覧
    private readonly List<AudioLowPassFilter> m_voiceLowPassFilters =
        new List<AudioLowPassFilter>(); //同じ声の聞こえ方へ個体差を付けるLowPass Filter一覧
    private readonly List<AudioReverbFilter> m_voiceReverbFilters =
        new List<AudioReverbFilter>(); //会場内の反響にランダム差を付けるReverb Filter一覧

    public IReadOnlyList<AudienceReaction> Audiences
    {
        get
        {
            return m_audiences;
        }
    }
    private readonly List<Bounds> m_audienceBounds =
        new List<Bounds>(); //観客ごとの基準Bounds
    private float m_nextReactionTime; //次回Reaction時刻
    private float m_nextCullingTime; //次回可視判定時刻
    private Coroutine m_sequentialCheerCoroutine; //Audience Choice成功中に歓声Clipを順番再生する処理

    /// <summary>
    /// 必要なら開始時に観客を生成します。
    /// </summary>
    private void Start()
    {
        if (b_m_spawnOnStart)
        {
            SpawnAudience();
        }
    }

    /// <summary>
    /// Component有効時にVoltage通知を購読します。
    /// </summary>
    private void OnEnable()
    {
        FindVoltageSystem();
        SubscribeVoltageEvents();
    }

    /// <summary>
    /// Component無効時にVoltage通知を解除します。
    /// </summary>
    private void OnDisable()
    {
        StopSequentialSuccessVoices();
        UnsubscribeVoltageEvents();
    }

    /// <summary>
    /// 一定間隔で観客へランダムなリアクションを実行します。
    /// </summary>
    private void Update()
    {
        if (b_m_useVoltageReactions && m_voltageSystem == null)
        {
            FindVoltageSystem();
            SubscribeVoltageEvents();
        }

        UpdateAudienceVisibility();
        if (!b_m_autoReaction || m_audiences.Count == 0)return;
        if (Time.time < m_nextReactionTime)return;

        PlayRandomReaction();
        float minimumInterval =
            Mathf.Max(EMinimumIntervalSeconds, m_reactionIntervalRange.x); //最短間隔
        float maximumInterval =
            Mathf.Max(minimumInterval, m_reactionIntervalRange.y); //最長間隔
        m_nextReactionTime =
            Time.time + Random.Range(minimumInterval, maximumInterval);
    }

    /// <summary>
    /// 現在の設定から観客を再生成します。
    /// </summary>
    [ContextMenu("Spawn Audience")]
    public void SpawnAudience()
    {
        ClearAudience();
        int audienceCount =
            Mathf.Max(EMinimumAudienceCount, m_audienceCount); //安全な人数
        Vector2 horizontalSize = GetHorizontalAreaSize(); //現在の横・奥行寸法
        int columns = Mathf.CeilToInt(Mathf.Sqrt(
            audienceCount
            * horizontalSize.x
            / Mathf.Max(0.01f, horizontalSize.y))); //横数
        int rows = Mathf.CeilToInt((float)audienceCount / columns); //縦数
        List<Vector3> candidatePositions =
            CreateCandidatePositions(
                columns,
                rows); //禁止範囲を除外した候補位置
        int spawnCount = Mathf.Min(
            audienceCount,
            candidatePositions.Count); //実際に生成可能な人数
        for (int i = 0; i < spawnCount; ++i)
        {
            int candidateIndex = Mathf.Min(
                candidatePositions.Count - 1,
                Mathf.FloorToInt(
                    (float)i * candidatePositions.Count / spawnCount)); //均等抽出位置
            CreateAudience(i, candidatePositions[candidateIndex]);
        }

        if (spawnCount < audienceCount)
        {
            Debug.LogWarning(
                $"生成可能な範囲が不足しているため、"
                + $"{audienceCount}人中{spawnCount}人を生成しました。",
                this);
        }
    }

    /// <summary>
    /// 禁止範囲を避けた等間隔の生成候補を作成します。
    /// </summary>
    private List<Vector3> CreateCandidatePositions(
        int _basecolumns,
        int _baserows)
    {
        List<Vector3> candidatePositions =
            new List<Vector3>(); //生成可能な候補位置
        for (int i = 0; i < _baserows; ++i)
        {
            for (int j = 0; j < _basecolumns; ++j)
            {
                float areaScale = Mathf.Clamp01(m_spawnAreaScale);
                float offset = (1.0f - areaScale) * 0.5f;

                float normalizedX = _basecolumns <= 1
                    ? 0.5f
                    : offset + (float)j / (_basecolumns - 1) * areaScale; //正規化横位置
                float normalizedZ = _baserows <= 1
                    ? 0.5f
                    : offset + (float)i / (_baserows - 1) * areaScale; //正規化奥行位置
                if (!TryGetGroundPosition(
                    normalizedX,
                    normalizedZ,
                    out Vector3 position))continue;
                if (IsInsideExclusionArea(position))continue;

                candidatePositions.Add(position);
            }
        }

        return candidatePositions;
    }

    /// <summary>
    /// 生成Cube内から下向きに床を検索してLocal配置位置を返します。
    /// </summary>
    private bool TryGetGroundPosition(
        float _normalizedx,
        float _normalizedz,
        out Vector3 _localposition)
    {
        if (m_generationVolumeObject == null)
        {
            _localposition = new Vector3(
                Mathf.Lerp(
                    -m_areaSize.x * 0.5f,
                    m_areaSize.x * 0.5f,
                    _normalizedx)
                + Random.Range(-m_positionError.x, m_positionError.x),
                0.0f,
                Mathf.Lerp(
                    -m_areaSize.y * 0.5f,
                    m_areaSize.y * 0.5f,
                    _normalizedz)
                + Random.Range(-m_positionError.y, m_positionError.y));
            return true;
        }

        Transform volumeTransform =
            m_generationVolumeObject.transform; //生成範囲Cube Transform
        Vector3 volumeLocalTop = new Vector3(
            Mathf.Lerp(-0.5f, 0.5f, _normalizedx),
            0.5f,
            Mathf.Lerp(-0.5f, 0.5f, _normalizedz)); //Cube上面の候補
        Vector3 worldPosition =
            volumeTransform.TransformPoint(volumeLocalTop); //Ray開始基準位置
        Vector3 horizontalError = volumeTransform.TransformVector(
            new Vector3(
                Random.Range(-m_positionError.x, m_positionError.x)
                / Mathf.Max(0.01f, Mathf.Abs(volumeTransform.lossyScale.x)),
                0.0f,
                Random.Range(-m_positionError.y, m_positionError.y)
                / Mathf.Max(0.01f, Mathf.Abs(volumeTransform.lossyScale.z))));
        worldPosition += horizontalError + Vector3.up * ERaycastMargin;
        float rayDistance =
            Mathf.Abs(volumeTransform.lossyScale.y)
            + ERaycastMargin * 2.0f; //Cube全高を通過する距離
        RaycastHit[] hits = Physics.RaycastAll(
            worldPosition,
            Vector3.down,
            rayDistance,
            m_groundLayers,
            QueryTriggerInteraction.Ignore); //範囲内の床候補
        bool b_foundGround = false; //有効な床を取得できたか
        RaycastHit hit = default; //採用する床
        float nearestDistance = float.MaxValue; //最短床距離
        for (int i = 0; i < hits.Length; ++i)
        {
            if (IsAreaHelperCollider(hits[i].collider))continue;
            if (hits[i].distance >= nearestDistance)continue;

            hit = hits[i];
            nearestDistance = hits[i].distance;
            b_foundGround = true;
        }

        if (!b_foundGround)
        {
            _localposition = Vector3.zero;
            return false;
        }

        Vector3 hitVolumeLocal =
            volumeTransform.InverseTransformPoint(hit.point); //Cube内の床位置
        if (Mathf.Abs(hitVolumeLocal.x) > 0.5f
            || Mathf.Abs(hitVolumeLocal.y) > 0.5f
            || Mathf.Abs(hitVolumeLocal.z) > 0.5f)
        {
            _localposition = Vector3.zero;
            return false;
        }

        _localposition = transform.InverseTransformPoint(hit.point);
        return true;
    }

    /// <summary>
    /// 生成範囲・禁止範囲表示用CubeのColliderか確認します。
    /// </summary>
    private bool IsAreaHelperCollider(Collider _collider)
    {
        if (_collider == null)return false;
        if (m_generationVolumeObject != null
            && _collider.transform.IsChildOf(
                m_generationVolumeObject.transform))return true;
        if (m_exclusionObjects == null)return false;

        for (int i = 0; i < m_exclusionObjects.Length; ++i)
        {
            GameObject exclusionObject = m_exclusionObjects[i]; //禁止範囲Object
            if (exclusionObject != null
                && _collider.transform.IsChildOf(
                    exclusionObject.transform))return true;
        }

        return false;
    }

    /// <summary>
    /// 現在使用している生成範囲の横幅と奥行きを返します。
    /// </summary>
    private Vector2 GetHorizontalAreaSize()
    {
        if (m_generationVolumeObject == null)return m_areaSize;

        Vector3 scale =
            m_generationVolumeObject.transform.lossyScale; //生成Cube World寸法
        return new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
    }

    /// <summary>
    /// 指定Local位置が生成禁止範囲内か確認します。
    /// </summary>
    private bool IsInsideExclusionArea(Vector3 _localposition)
    {
        if (m_exclusionObjects == null)return false;

        Vector3 worldPosition =
            transform.TransformPoint(_localposition); //候補のWorld位置
        for (int i = 0; i < m_exclusionObjects.Length; ++i)
        {
            GameObject exclusionObject = m_exclusionObjects[i]; //確認Cube
            if (exclusionObject == null)continue;

            Vector3 exclusionLocalPosition =
                exclusionObject.transform.InverseTransformPoint(
                    worldPosition); //Cube基準の候補位置
            bool b_insideX =
                Mathf.Abs(exclusionLocalPosition.x) <= 0.5f; //Cube横範囲内
            bool b_insideY =
                Mathf.Abs(exclusionLocalPosition.y) <= 0.5f; //Cube高さ範囲内
            bool b_insideZ =
                Mathf.Abs(exclusionLocalPosition.z) <= 0.5f; //Cube奥行範囲内
            if (b_insideX && b_insideY && b_insideZ)return true;
        }

        return false;
    }

    /// <summary>
    /// 観客一体を生成して外観差を設定します。
    /// </summary>
    private void CreateAudience(int _index, Vector3 _localposition)
    {
        GameObject audienceObject = m_audiencePrefab != null
            ? Instantiate(m_audiencePrefab, transform)
            : CreateMeshAudience(); //生成した観客
        audienceObject.name = $"{EAudienceName}_{_index:000}";
        audienceObject.transform.SetParent(transform, false);
        audienceObject.transform.localPosition = _localposition;
        audienceObject.transform.localRotation =
            Quaternion.Euler(0.0f, Random.Range(-m_yawError, m_yawError), 0.0f);
        float scale = Random.Range(m_scaleRange.x, m_scaleRange.y); //個体Scale
        audienceObject.transform.localScale *= scale;
        ApplyRandomMaterial(audienceObject);
        AlignAudienceFeet(audienceObject, _localposition);
        FaceAudienceTowardTarget(audienceObject);

        AudienceReaction reaction =
            audienceObject.GetComponent<AudienceReaction>(); //Reaction制御
        if (reaction == null)
        {
            reaction = audienceObject.AddComponent<AudienceReaction>();
        }
        reaction.CaptureCurrentTransform();

        m_audiences.Add(reaction);
        m_audienceBounds.Add(CreateAudienceBounds(audienceObject));
    }

    private void FaceAudienceTowardTarget(GameObject _audienceobject)
    {
        if (_audienceobject == null || m_facingTarget == null)return;

        Vector3 direction =
            m_facingTarget.position - _audienceobject.transform.position;
        direction.y = 0.0f;
        if (direction.sqrMagnitude <= 0.0001f)return;

        float yawVariation = Random.Range(-m_yawError, m_yawError);
        _audienceobject.transform.rotation = Quaternion.LookRotation(
            direction.normalized,
            Vector3.up) * Quaternion.Euler(
            0.0f,
            m_modelYawOffset + yawVariation,
            0.0f);
    }

    /// <summary>
    /// Renderer下端が取得した床位置へ合うよう観客を上方向へ補正します。
    /// </summary>
    private void AlignAudienceFeet(
        GameObject _audienceobject,
        Vector3 _groundlocalposition)
    {
        Renderer[] renderers =
            _audienceobject.GetComponentsInChildren<Renderer>(true); //観客Renderer群
        if (renderers.Length == 0)return;

        Bounds worldBounds = renderers[0].bounds; //観客全体Bounds
        for (int i = 1; i < renderers.Length; ++i)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        float groundWorldY =
            transform.TransformPoint(_groundlocalposition).y; //床World高さ
        float heightDifference =
            groundWorldY - worldBounds.min.y + m_groundOffset; //足元補正量
        _audienceobject.transform.position += Vector3.up * heightDifference;
    }

    /// <summary>
    /// 指定Meshから観客Objectを作成します。
    /// </summary>
    private GameObject CreateMeshAudience()
    {
        GameObject audienceObject = new GameObject(
            EAudienceName,
            typeof(MeshFilter),
            typeof(MeshRenderer)); //Mesh観客
        audienceObject.GetComponent<MeshFilter>().sharedMesh = m_audienceMesh;
        return audienceObject;
    }

    /// <summary>
    /// 登録Materialから一つを観客へ設定します。
    /// </summary>
    private void ApplyRandomMaterial(GameObject _audienceobject)
    {
        if (m_materials == null || m_materials.Length == 0)return;

        Renderer[] renderers =
            _audienceobject.GetComponentsInChildren<Renderer>(true); //外観Renderer
        Material material =
            m_materials[Random.Range(0, m_materials.Length)]; //選択Material
        for (int i = 0; i < renderers.Length; ++i)
        {
            renderers[i].sharedMaterial = material;
        }
    }

    /// <summary>
    /// 一部の観客へランダムなリアクションを実行します。
    /// </summary>
    public void PlayRandomReaction()
    {
        CollectVisibleAudiences();
        if (m_visibleAudiences.Count == 0)return;

        int reactionCount = Mathf.Max(
            EMinimumAudienceCount,
            Mathf.CeilToInt(
                m_visibleAudiences.Count
                * EMinimumSuccessReactionRatio)); //同時Reaction人数
        for (int i = 0; i < reactionCount; ++i)
        {
            AudienceReaction audience =
                m_visibleAudiences[
                    Random.Range(0, m_visibleAudiences.Count)]; //対象観客
            EAudienceReaction reaction =
                (EAudienceReaction)Random.Range(
                    0,
                    (int)EAudienceReaction.Disappointed); //通常動作種類
            audience.PlayReaction(reaction);
        }
    }

    /// <summary>
    /// 成功時にVoltage比例の人数と強度で観客を反応させます。
    /// </summary>
    public void PlaySuccessReaction(float _normalizedvoltage)
    {
        PlaySuccessReactionVisual(_normalizedvoltage);
        if (m_sequentialCheerCoroutine == null)
        {
            PlayAudienceVoices(
                m_cheerVoiceClips,
                Mathf.Clamp01(_normalizedvoltage),
                0.0f);
        }
    }

    /// <summary>
    /// 音声を追加せず、成功時の観客Animationだけをボルテージ比例で再生します。
    /// 連続歓声中に音声が多重発火しないよう、Event継続リアクションから使用します。
    /// </summary>
    public void PlaySuccessReactionVisual(float _normalizedvoltage)
    {
        float voltage = Mathf.Clamp01(_normalizedvoltage); //安全なVoltage
        float reactionRatio = Mathf.Lerp(
            Mathf.Clamp01(m_minimumSuccessReactionRatio),
            Mathf.Clamp01(m_maximumSuccessReactionRatio),
            voltage); //成功反応人数率
        float strength = Mathf.Lerp(
            Mathf.Max(0.0f, m_minimumSuccessStrength),
            Mathf.Max(0.0f, m_maximumSuccessStrength),
            voltage); //成功動作強度
        PlayAudienceReaction(
            true,
            reactionRatio,
            strength);
    }

    /// <summary>
    /// 失敗時に観客へ落胆リアクションを実行します。
    /// </summary>
    public void PlayFailureReaction()
    {
        PlayAudienceReaction(
            false,
            Mathf.Clamp01(m_failureReactionRatio),
            Mathf.Max(0.0f, m_failureReactionStrength));
        float voltage = 0.5f;
        if (m_voltageSystem != null)
        {
            voltage = m_voltageSystem.NormalizedVoltage;
        }
        PlayAudienceVoices(m_disappointedVoiceClips, voltage, 0.0f);
    }

    /// <summary>
    /// Audience Choiceの好みが高いほど少し大きな歓声を再生します。
    /// </summary>
    public void PlayPreferenceCheer(float _preference)
    {
        float voltage = 0.5f;
        if (m_voltageSystem != null)
        {
            voltage = m_voltageSystem.NormalizedVoltage;
        }
        PlayAudienceVoices(
            m_cheerVoiceClips,
            voltage,
            Mathf.Clamp01(_preference) * m_preferenceVolumeBoost);
    }

    /// <summary>
    /// 登録順に歓声Clipを繋ぎ、Audience Choiceの成功から終了まで途切れにくく再生します。
    /// 二つのAudioSourceを交互に使うことで、前のClip末尾と次の先頭をわずかに重ねます。
    /// </summary>
    public void StartSequentialSuccessVoices(float _preference)
    {
        StopSequentialSuccessVoices();
        if (m_cheerVoiceClips == null || m_cheerVoiceClips.Length == 0)return;

        float voltage = 0.5f;
        if (m_voltageSystem != null)
        {
            voltage = m_voltageSystem.NormalizedVoltage;
        }
        m_sequentialCheerCoroutine = StartCoroutine(
            PlaySequentialSuccessVoicesRoutine(
                Mathf.Clamp01(voltage),
                Mathf.Clamp01(_preference) * m_preferenceVolumeBoost));
    }

    /// <summary>
    /// 連続歓声Coroutineと、その処理が使用している全AudioSourceを即座に停止します。
    /// Event終了・中断のどちらから呼ばれても残響が次の通常状態へ残らないようにします。
    /// </summary>
    public void StopSequentialSuccessVoices()
    {
        if (m_sequentialCheerCoroutine != null)
        {
            StopCoroutine(m_sequentialCheerCoroutine);
            m_sequentialCheerCoroutine = null;
        }
        for (int i = 0; i < m_voiceSources.Count; ++i)
        {
            if (m_voiceSources[i] != null)
            {
                m_voiceSources[i].Stop();
            }
        }
    }

    /// <summary>
    /// DSP時計を基準にClipの長さとPitchから次の開始時刻を予約します。
    /// Frame Rateの揺れに左右されにくい予約再生とし、末尾を少し重ねて音切れを目立たなくします。
    /// </summary>
    private IEnumerator PlaySequentialSuccessVoicesRoutine(
        float _normalizedvoltage,
        float _volumeboost)
    {
        const double overlapSeconds = 0.04;
        EnsureVoiceSources(2);
        int clipIndex = 0; //登録順を維持し、末尾到達後は先頭へ戻るClip番号
        int sourceIndex = 0; //予約再生を交互に受け持つ二つのAudioSource番号
        double nextStartTime = AudioSettings.dspTime + 0.05; //次Clipを開始するDSP基準の絶対時刻
        while (true)
        {
            AudioClip clip = m_cheerVoiceClips[clipIndex];
            clipIndex = (clipIndex + 1) % m_cheerVoiceClips.Length;
            if (clip == null)
            {
                yield return null;
                continue;
            }

            AudioSource source = m_voiceSources[sourceIndex];
            int filterIndex = sourceIndex;
            sourceIndex = (sourceIndex + 1) % 2;
            ConfigureSequentialVoiceSource(
                source,
                filterIndex,
                clip,
                _normalizedvoltage,
                _volumeboost);
            source.PlayScheduled(nextStartTime);
            double playbackDuration = clip.length
                / Mathf.Max(0.01f, Mathf.Abs(source.pitch));
            nextStartTime += Mathf.Max(
                0.01f,
                (float)(playbackDuration - overlapSeconds));
            while (AudioSettings.dspTime < nextStartTime - 0.1)
            {
                yield return null;
            }
        }
    }

    /// <summary>
    /// 連続再生する一つの声へ、ボルテージ・好み・ランダム差から音量と音質を設定します。
    /// Clip順は固定しつつPitch、Pan、Filterだけを揺らし、同じ素材の反復感を弱めます。
    /// </summary>
    private void ConfigureSequentialVoiceSource(
        AudioSource _source,
        int _filterindex,
        AudioClip _clip,
        float _normalizedvoltage,
        float _volumeboost)
    {
        float minimumPitch = Mathf.Min(m_voicePitchRange.x, m_voicePitchRange.y);
        float maximumPitch = Mathf.Max(m_voicePitchRange.x, m_voicePitchRange.y);
        _source.clip = _clip;
        _source.time = 0.0f;
        _source.pitch = Random.Range(minimumPitch, maximumPitch);
        float baseVolume = Mathf.Lerp(
            m_minimumVoiceVolume,
            m_maximumVoiceVolume,
            _normalizedvoltage) + _volumeboost;
        _source.volume = Mathf.Clamp01(
            baseVolume + Random.Range(
                -m_voiceVolumeVariation,
                m_voiceVolumeVariation));
        _source.panStereo = Random.Range(
            -m_voiceStereoPanRange,
            m_voiceStereoPanRange);
        AudioLowPassFilter lowPass = m_voiceLowPassFilters[_filterindex];
        lowPass.cutoffFrequency = Random.Range(
            Mathf.Min(m_voiceLowPassRange.x, m_voiceLowPassRange.y),
            Mathf.Max(m_voiceLowPassRange.x, m_voiceLowPassRange.y));
    }

    private void PlayAudienceVoices(
        AudioClip[] _clips,
        float _normalizedvoltage,
        float _volumeboost)
    {
        if (_clips == null || _clips.Length == 0)return;

        float voltage = Mathf.Clamp01(_normalizedvoltage);
        int minimumCount = Mathf.Max(1, m_minimumSimultaneousVoices);
        int maximumCount = Mathf.Max(minimumCount, m_maximumSimultaneousVoices);
        int voiceCount = Mathf.RoundToInt(
            Mathf.Lerp(minimumCount, maximumCount, voltage));
        EnsureVoiceSources(voiceCount);

        float baseVolume = Mathf.Clamp01(
            Mathf.Lerp(m_minimumVoiceVolume, m_maximumVoiceVolume, voltage)
            + _volumeboost);
        float minimumPitch = Mathf.Min(m_voicePitchRange.x, m_voicePitchRange.y);
        float maximumPitch = Mathf.Max(m_voicePitchRange.x, m_voicePitchRange.y);
        for (int i = 0; i < voiceCount; ++i)
        {
            AudioClip clip = GetRandomVoiceClip(_clips);
            if (clip == null)continue;

            AudioSource source = m_voiceSources[i];
            source.pitch = Random.Range(minimumPitch, maximumPitch);
            source.volume = Mathf.Clamp01(
                baseVolume + Random.Range(
                    -m_voiceVolumeVariation,
                    m_voiceVolumeVariation));
            source.panStereo = Random.Range(
                -m_voiceStereoPanRange,
                m_voiceStereoPanRange);
            float reverbMix = Random.Range(
                Mathf.Min(m_voiceReverbMixRange.x, m_voiceReverbMixRange.y),
                Mathf.Max(m_voiceReverbMixRange.x, m_voiceReverbMixRange.y));
            source.reverbZoneMix = reverbMix;
            m_voiceReverbFilters[i].reverbLevel = Mathf.Lerp(
                -10000.0f,
                -3500.0f,
                Mathf.Clamp01(reverbMix));
            AudioLowPassFilter lowPass = m_voiceLowPassFilters[i];
            lowPass.cutoffFrequency = Random.Range(
                Mathf.Min(m_voiceLowPassRange.x, m_voiceLowPassRange.y),
                Mathf.Max(m_voiceLowPassRange.x, m_voiceLowPassRange.y));
            source.clip = clip;
            source.time = Random.Range(
                0.0f,
                Mathf.Min(
                    Mathf.Max(0.0f, m_voiceStartOffsetSeconds),
                    Mathf.Max(0.0f, clip.length - 0.01f)));
            source.Play();
        }
    }

    private void EnsureVoiceSources(int _requiredcount)
    {
        while (m_voiceSources.Count < _requiredcount)
        {
            GameObject voiceObject = new GameObject(
                $"AudienceVoice_{m_voiceSources.Count + 1:00}");
            voiceObject.transform.SetParent(transform, false);
            AudioSource source = voiceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0.0f;
            m_voiceSources.Add(source);
            AudioLowPassFilter lowPass =
                voiceObject.AddComponent<AudioLowPassFilter>();
            lowPass.lowpassResonanceQ = 1.0f;
            m_voiceLowPassFilters.Add(lowPass);
            AudioReverbFilter reverb =
                voiceObject.AddComponent<AudioReverbFilter>();
            reverb.reverbPreset = AudioReverbPreset.User;
            reverb.dryLevel = 0.0f;
            reverb.reverbLevel = -10000.0f;
            m_voiceReverbFilters.Add(reverb);
        }
    }

    private static AudioClip GetRandomVoiceClip(AudioClip[] _clips)
    {
        if (_clips == null || _clips.Length == 0)return null;

        for (int i = 0; i < _clips.Length; ++i)
        {
            AudioClip clip = _clips[Random.Range(0, _clips.Length)];
            if (clip != null)return clip;
        }

        return null;
    }

    /// <summary>
    /// 指定割合の画面内観客へ重複なしで判定リアクションを実行します。
    /// </summary>
    private void PlayAudienceReaction(
        bool _bsuccess,
        float _reactionratio,
        float _strength)
    {
        CollectVisibleAudiences();
        if (m_visibleAudiences.Count == 0)return;

        int reactionCount = Mathf.Clamp(
            Mathf.CeilToInt(m_visibleAudiences.Count * _reactionratio),
            EMinimumAudienceCount,
            m_visibleAudiences.Count); //今回反応する人数
        for (int i = 0; i < reactionCount; ++i)
        {
            int randomIndex = Random.Range(
                i,
                m_visibleAudiences.Count); //未選択範囲の抽選位置
            AudienceReaction selectedAudience =
                m_visibleAudiences[randomIndex]; //今回反応する観客
            m_visibleAudiences[randomIndex] = m_visibleAudiences[i];
            m_visibleAudiences[i] = selectedAudience;
            EAudienceReaction reaction = _bsuccess
                ? GetSuccessReaction()
                : EAudienceReaction.Disappointed; //判定別動作
            selectedAudience.PlayReaction(reaction, _strength);
        }

        m_nextReactionTime =
            Time.time
            + Mathf.Max(
                EMinimumIntervalSeconds,
                m_reactionIntervalRange.x);
    }

    /// <summary>
    /// 画面内で有効な観客を再利用Listへ集めます。
    /// </summary>
    private void CollectVisibleAudiences()
    {
        m_visibleAudiences.Clear();
        for (int i = 0; i < m_audiences.Count; ++i)
        {
            AudienceReaction audience = m_audiences[i]; //確認対象
            if (audience != null && audience.gameObject.activeInHierarchy)
            {
                m_visibleAudiences.Add(audience);
            }
        }
    }

    /// <summary>
    /// 成功向けの上方向リアクションをランダムに返します。
    /// </summary>
    private static EAudienceReaction GetSuccessReaction()
    {
        int reactionIndex = Random.Range(0, 3); //成功動作の抽選番号
        switch (reactionIndex)
        {
            case 0:
                return EAudienceReaction.Jump;
            case 1:
                return EAudienceReaction.Cheer;
            default:
                return EAudienceReaction.Bounce;
        }
    }

    /// <summary>
    /// VenueVoltageSystemを取得します。
    /// </summary>
    private void FindVoltageSystem()
    {
        if (m_voltageSystem != null)return;

        m_voltageSystem = FindFirstObjectByType<VenueVoltageSystem>();
    }

    /// <summary>
    /// Voltageの成功失敗通知を購読します。
    /// </summary>
    private void SubscribeVoltageEvents()
    {
        if (!b_m_useVoltageReactions || m_voltageSystem == null)return;

        m_voltageSystem.m_audienceSuccess -= PlaySuccessReaction;
        m_voltageSystem.m_audienceFailure -= PlayFailureReaction;
        m_voltageSystem.m_audienceSuccess += PlaySuccessReaction;
        m_voltageSystem.m_audienceFailure += PlayFailureReaction;
    }

    /// <summary>
    /// Voltageの成功失敗通知を解除します。
    /// </summary>
    private void UnsubscribeVoltageEvents()
    {
        if (m_voltageSystem == null)return;

        m_voltageSystem.m_audienceSuccess -= PlaySuccessReaction;
        m_voltageSystem.m_audienceFailure -= PlayFailureReaction;
    }

    /// <summary>
    /// 生成済み観客を削除します。
    /// </summary>
    [ContextMenu("Clear Audience")]
    public void ClearAudience()
    {
        m_audiences.Clear();
        m_audienceBounds.Clear();
        for (int i = transform.childCount - 1; i >= 0; --i)
        {
            GameObject child = transform.GetChild(i).gameObject; //削除対象
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    /// <summary>
    /// 指定Cameraの視錐台外にいる観客を無効化します。
    /// </summary>
    private void UpdateAudienceVisibility()
    {
        if (!b_m_enableCameraCulling)return;
        if (Time.unscaledTime < m_nextCullingTime)return;

        if (m_targetCamera == null)
        {
            m_targetCamera = Camera.main;
        }

        if (m_targetCamera == null)return;

        m_nextCullingTime =
            Time.unscaledTime
            + Mathf.Max(EMinimumCullingInterval, m_cullingInterval);
        Plane[] frustumPlanes =
            GeometryUtility.CalculateFrustumPlanes(m_targetCamera); //Camera視錐台
        int audienceCount =
            Mathf.Min(m_audiences.Count, m_audienceBounds.Count); //判定可能人数
        for (int i = 0; i < audienceCount; ++i)
        {
            AudienceReaction audience = m_audiences[i]; //判定対象
            if (audience == null)continue;

            Bounds localBounds = m_audienceBounds[i]; //生成時のLocal Bounds
            Vector3 worldCenter =
                audience.transform.TransformPoint(localBounds.center); //World中心
            Vector3 lossyScale = audience.transform.lossyScale; //現在World Scale
            Vector3 worldSize = Vector3.Scale(
                localBounds.size,
                new Vector3(
                    Mathf.Abs(lossyScale.x),
                    Mathf.Abs(lossyScale.y),
                    Mathf.Abs(lossyScale.z))); //World寸法
            worldSize += Vector3.one * Mathf.Max(0.0f, m_cullingMargin);
            Bounds worldBounds = new Bounds(
                worldCenter,
                worldSize); //余白付き判定Bounds
            bool b_isVisible =
                GeometryUtility.TestPlanesAABB(
                    frustumPlanes,
                    worldBounds); //Camera内判定
            if (audience.gameObject.activeSelf != b_isVisible)
            {
                audience.gameObject.SetActive(b_isVisible);
            }
        }
    }

    /// <summary>
    /// 観客Renderer群をまとめたLocal Boundsを生成します。
    /// </summary>
    private static Bounds CreateAudienceBounds(GameObject _audienceobject)
    {
        Renderer[] renderers =
            _audienceobject.GetComponentsInChildren<Renderer>(true); //観客Renderer群
        if (renderers.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }

        Bounds worldBounds = renderers[0].bounds; //統合前World Bounds
        for (int i = 1; i < renderers.Length; ++i)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 localCenter =
            _audienceobject.transform.InverseTransformPoint(
                worldBounds.center); //Local中心
        Vector3 lossyScale = _audienceobject.transform.lossyScale; //現在World Scale
        Vector3 localSize = new Vector3(
            SafeDivide(worldBounds.size.x, lossyScale.x),
            SafeDivide(worldBounds.size.y, lossyScale.y),
            SafeDivide(worldBounds.size.z, lossyScale.z)); //Local寸法
        return new Bounds(localCenter, localSize);
    }

    /// <summary>
    /// Scaleが0の場合を考慮して安全に除算します。
    /// </summary>
    private static float SafeDivide(float _value, float _divisor)
    {
        if (Mathf.Approximately(_divisor, 0.0f))return _value;

        return Mathf.Abs(_value / _divisor);
    }

    /// <summary>
    /// Scene上へ配置範囲を表示します。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (m_generationVolumeObject != null)
        {
            Gizmos.matrix =
                m_generationVolumeObject.transform.localToWorldMatrix;
            if (b_m_showAreaVolumes)
            {
                Gizmos.color = new Color(0.0f, 0.8f, 1.0f, 0.12f);
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }

            Gizmos.color = new Color(0.0f, 0.9f, 1.0f, 0.95f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
        else
        {
        Gizmos.matrix = transform.localToWorldMatrix;
        float areaHeight = Mathf.Max(0.01f, m_areaHeight); //安全な生成範囲高さ
        Vector3 areaCenter =
            new Vector3(0.0f, areaHeight * 0.5f, 0.0f); //底面を基準にした中心
        Vector3 areaVolumeSize =
            new Vector3(m_areaSize.x, areaHeight, m_areaSize.y); //生成範囲寸法
        if (b_m_showAreaVolumes)
        {
            Gizmos.color = new Color(0.0f, 0.8f, 1.0f, 0.12f);
            Gizmos.DrawCube(areaCenter, areaVolumeSize);
        }

        Gizmos.color = new Color(0.0f, 0.9f, 1.0f, 0.95f);
        Gizmos.DrawWireCube(areaCenter, areaVolumeSize);
        }

        if (m_exclusionObjects == null)return;

        for (int i = 0; i < m_exclusionObjects.Length; ++i)
        {
            GameObject exclusionObject = m_exclusionObjects[i]; //表示禁止Cube
            if (exclusionObject == null)continue;

            Gizmos.matrix = exclusionObject.transform.localToWorldMatrix;
            if (b_m_showAreaVolumes)
            {
                Gizmos.color = new Color(1.0f, 0.1f, 0.05f, 0.18f);
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }

            Gizmos.color = new Color(1.0f, 0.15f, 0.05f, 0.95f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}
