/*━━━━━━━━━
@file       PoseCameraDirector.cs
@brief      CameraSequenceを読み込み、Cinemachine3系のポーズ用カメラを動かす。
@author     24CU0139 ラヤマジ プラシャント
@date       作成日 2026/07/12
最終更新日  2026/07/23
@remarks    Unity 6系 / Cinemachine3系用
━━━━━━━━━*/
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

//CameraSequenceを読み込み、ポーズ用カメラを動かす。
public sealed class PoseCameraDirector : MonoBehaviour
{
    private const float E_STOP_TIME_SCALE = 0.0f;               //ポーズ演出中のTimeScale
    private const float E_MIN_LOOK_DIRECTION_SQR = 0.0001f;     //カメラ向き計算の最小距離
    private const float E_FRONT_GIZMO_LENGTH = 1.2f;            //正面確認用Gizmoの長さ
    private const float E_WAIST_GIZMO_RADIUS = 0.08f;           //腰ターゲットのGizmo半径
    private const float E_CHEST_GIZMO_RADIUS = 0.10f;           //胸ターゲットのGizmo半径
    private const float E_FACE_GIZMO_RADIUS = 0.12f;            //顔ターゲットのGizmo半径
    private const float E_FRONT_REFERENCE_GIZMO_RADIUS = 0.10f; //正面目印のGizmo半径
    private const float E_FRONT_DIRECTION_GIZMO_RADIUS = 0.06f; //正面方向線の先端Gizmo半径
    private const float E_POINT_A_FADE_DURATION = 0.18f;        //Point Aへ入る前後の自動フェード時間
    private const float E_OUTPUT_POSITION_TOLERANCE = 0.0001f;  //Brain出力とPoint Aの位置許容誤差
    private const int E_OUTPUT_SETTLE_FRAME_COUNT = 3;          //Brain出力をPoint Aへ確定させる最大フレーム数
    private const int E_DEFAULT_GAMEPLAY_PRIORITY = 10;         //通常カメラの初期Priority
    private const int E_DEFAULT_ACTIVE_POSE_PRIORITY = 100;     //ポーズカメラ有効時の初期Priority
    private const int E_DEFAULT_INACTIVE_POSE_PRIORITY = 0;     //ポーズカメラ無効時の初期Priority

    [Header("Cinemachine 3")]
    [Tooltip("通常プレイ用のCinemachineCamera")]
    [SerializeField]
    [FormerlySerializedAs("gameplayCamera")]
    private CinemachineCamera m_gameplayCamera; //通常プレイ用カメラ

    [Tooltip("ポーズ演出用のCinemachineCamera")]
    [SerializeField]
    [FormerlySerializedAs("poseCamera")]
    private CinemachineCamera m_poseCamera; //ポーズ演出用カメラ

    [Tooltip("Main Cameraに付いているCinemachine Brain")]
    [SerializeField]
    [FormerlySerializedAs("cinemachineBrain")]
    private CinemachineBrain m_cinemachineBrain; //Cinemachineの切り替え担当

    [Header("モデルと正面")]
    [Tooltip("キャラクター全体の一番上のオブジェクト")]
    [SerializeField]
    [FormerlySerializedAs("characterRoot")]
    private Transform m_characterRoot; //キャラクターの親Transform

    [Tooltip("モデルの顔の前に置く空オブジェクト回転ではなく位置で正面を決める")]
    [SerializeField]
    [FormerlySerializedAs("frontReference")]
    private Transform m_frontReference; //モデル正面を示す空オブジェクト

    [Header("通常カメラの注視")]
    [Tooltip("ONならポーズ演出外でもGameplay CameraをChest Targetへ向け、プレイヤーを画面中央に保つ")]
    [SerializeField]
    private bool b_mKeepCharacterCentered = true; //通常カメラでもプレイヤーを中央に保つかどうか

    [Header("カメラターゲット")]
    [Tooltip("全身を見せるときの中心腰付近に置く")]
    [SerializeField]
    [FormerlySerializedAs("waistTarget")]
    private Transform m_waistTarget; //腰付近の注視点

    [Tooltip("筋肉を見せるときの中心胸付近に置く")]
    [SerializeField]
    [FormerlySerializedAs("chestTarget")]
    private Transform m_chestTarget; //胸付近の注視点

    [Tooltip("表情を見せるときの中心顔付近に置く")]
    [SerializeField]
    [FormerlySerializedAs("faceTarget")]
    private Transform m_faceTarget; //顔付近の注視点

    [Header("Priority")]
    [Tooltip("通常カメラの優先度")]
    [SerializeField]
    [FormerlySerializedAs("gameplayPriority")]
    private int m_gameplayPriority = E_DEFAULT_GAMEPLAY_PRIORITY; //通常カメラの優先度

    [Tooltip("ポーズカメラを有効にするときの優先度")]
    [SerializeField]
    [FormerlySerializedAs("activePosePriority")]
    private int m_activePosePriority = E_DEFAULT_ACTIVE_POSE_PRIORITY; //ポーズカメラ有効時の優先度

    [Tooltip("ポーズカメラを使わないときの優先度")]
    [SerializeField]
    [FormerlySerializedAs("inactivePosePriority")]
    private int m_inactivePosePriority = E_DEFAULT_INACTIVE_POSE_PRIORITY; //ポーズカメラ無効時の優先度

    [Header("テスト")]
    [Tooltip("ここへSequenceを入れるとPキーでテストできます。")]
    [SerializeField]
    [FormerlySerializedAs("testSequence")]
    private CameraSequence m_testSequence; //Pキーで再生するテスト用Sequence

    [Tooltip("複数の確認用Sequenceを登録します。Pキーでは上から順番にすべて再生します。")]
    [SerializeField]
    private List<CameraSequence> m_testSequenceList = new List<CameraSequence>(); //確認用Sequenceの一覧

    [Tooltip("PlayTestSequenceから個別再生するときの番号。0が一番上です。")]
    [Min(0)]
    [SerializeField]
    private int m_testSequenceIndex; //選択中の確認用Sequence番号

    [Tooltip("テスト再生キー")]
    [SerializeField]
    [FormerlySerializedAs("testPlayKey")]
    private KeyCode m_testPlayKey = KeyCode.P; //テスト再生キー

    [Tooltip("演出停止キー")]
    [SerializeField]
    [FormerlySerializedAs("stopKey")]
    private KeyCode m_stopKey = KeyCode.O; //演出停止キー

    private Coroutine m_playRoutine;        //再生中のCoroutine
    private Coroutine m_restoreBlendRoutine; //カメラ切り替え後にBlend設定を戻すCoroutine
    private bool b_mChangedTimeScale;       //TimeScaleを変更したかどうか
    private float m_savedTimeScale = 1.0f;  //変更前のTimeScale

    private SCameraState m_currentState; //現在のカメラ状態
    private Vector3 m_currentLookPoint;  //現在の注視点

    private bool b_mChangedBrainBlend;                      //BrainのBlendを変更したかどうか
    private CinemachineBlendDefinition m_savedBrainBlend;   //変更前のBlend設定
    private CameraFadeOverlay m_fadeOverlay;                 //Point Aへの導入を隠す自動フェード

    public bool IsPlaying => m_playRoutine != null;

    //Cinemachineの時間停止対応と初期カメラを設定
    private void Awake()
    {
        m_fadeOverlay = CameraFadeOverlay.Create(transform);

        if (m_cinemachineBrain != null)
        {
            m_cinemachineBrain.IgnoreTimeScale = true;
        }
        //初期状態は通常カメラを有効にする
        SetGameplayCameraActive();
    }

    //テスト用の再生キーと停止キーを確認
    private void Update()
    {
        //テスト用のキー入力で再生・停止を切り替え
        if (CameraInputUtility.IsKeyDown(m_testPlayKey))
        {
            if (IsPlaying) { StopSequence(); }
            else { PlayTestSequences(); }
        }

        if (CameraInputUtility.IsKeyDown(m_stopKey)) { StopSequence(); }
    }

    //通常カメラが有効な間もプレイヤーを画面中央に保つ
    private void LateUpdate()
    {
        if (!IsPlaying && b_mKeepCharacterCentered)
        {
            CenterGameplayCameraOnCharacter();
        }
    }

    //指定したカメラ演出を再生
    public void PlaySequence(CameraSequence _sequence)
    {

        if (!ValidateReferences() || !ValidateSequence(_sequence)) { return; }

        //再生中の演出があれば停止
        StopSequence();
        m_playRoutine = StartCoroutine(PlaySingleSequenceRoutine(_sequence));
    }

    //Test Sequence Listを上から順番にすべて再生
    public void PlayTestSequences()
    {
        //一覧が未設定の場合は、従来のTest Sequenceを再生する
        if (m_testSequenceList == null || m_testSequenceList.Count == 0)
        {
            PlaySequence(m_testSequence);
            return;
        }

        if (!ValidateReferences() || !ValidateSequenceList()) { return; }

        StopSequence();
        m_playRoutine = StartCoroutine(PlaySequenceListRoutine());
    }

    //Test Sequence Listの指定番号を再生
    public void PlayTestSequence(int _index)
    {
        m_testSequenceIndex = Mathf.Clamp(_index, 0, Mathf.Max(0, m_testSequenceList.Count - 1));
        PlaySequence(GetTestSequence());
    }

    //Inspectorで選択した確認用Sequenceを返す
    private CameraSequence GetTestSequence()
    {
        //一覧が未設定の場合は、従来のTest Sequenceを使用する
        if (m_testSequenceList == null || m_testSequenceList.Count == 0) return m_testSequence;

        m_testSequenceIndex = Mathf.Clamp(m_testSequenceIndex, 0, m_testSequenceList.Count - 1);
        return m_testSequenceList[m_testSequenceIndex];
    }

    //1つのSequenceを再生し、完了後に再生状態を解除
    private IEnumerator PlaySingleSequenceRoutine(CameraSequence _sequence)
    {
        yield return PlaySequenceRoutine(_sequence);
        m_playRoutine = null;
    }

    //登録されたSequenceを上から順番に連続再生
    private IEnumerator PlaySequenceListRoutine()
    {
        for (int i = 0; i < m_testSequenceList.Count; i++)
        {
            CameraSequence sequence = m_testSequenceList[i]; //今回再生するSequence
            if (sequence == null) { continue; }

            yield return PlaySequenceRoutine(sequence);
        }

        m_playRoutine = null;
    }

    //演出を停止して通常カメラへ戻す
    public void StopSequence()
    {
        //再生中のCoroutineがあれば停止
        if (m_playRoutine != null)
        {
            StopCoroutine(m_playRoutine);
            m_playRoutine = null;
        }

        //実行中のBlend復元待ちがあれば停止
        StopRestoreBlendRoutine();

        //Cinemachine BrainのBlend設定を元に戻す
        RestoreBrainBlend();
        RestoreTimeScale();
        m_fadeOverlay?.Hide();

        //停止時のBlend中にプレイヤーが画面外へ出ないよう、通常カメラへCutで戻す
        SwitchToGameplayCameraWithCut();
    }

    //CameraSequenceのShotを順番に再生
    private IEnumerator PlaySequenceRoutine(CameraSequence _sequence)
    {
        //TimeScaleを止める場合は、変更前のTimeScaleを保存してから0にする
        if (_sequence.PauseGameTime)
        {
            m_savedTimeScale = Time.timeScale;
            Time.timeScale = E_STOP_TIME_SCALE;
            b_mChangedTimeScale = true;
        }

        CameraShotPreset firstShot = FindFirstValidShot(_sequence); //最初に再生するShot
        Transform firstTarget = GetTarget(firstShot.TargetPoint);   //最初の注視点

        //最初のShotの開始状態を作り、現在の状態として保存
        SCameraState firstStartState = BuildStartState(firstShot);

        //フェード再生中にPoint Aへ瞬間移動し、Brainの実出力まで確定してから表示する
        yield return TransitionToShotStart(firstStartState, firstTarget, true);

        //Shotを順番に再生
        bool b_isFirstShot = true; //Sequence開始時に設定済みの最初のShotかどうか
        do
        {
            for (int i = 0; i < _sequence.Shots.Count; i++)
            {
                CameraShotPreset shot = _sequence.Shots[i]; //再生対象のShot

                if (shot == null) { continue; }

                Transform target = GetTarget(shot.TargetPoint);   //Shotの注視点
                SCameraState startState = BuildStartState(shot);  //Shot開始状態
                SCameraState endState = BuildEndState(shot);      //Shot終了状態

                //2つ目以降のShotは、導入移動中のカメラ回転を黒フェードで隠す
                if (!b_isFirstShot)
                {
                    yield return TransitionToShotStart(startState, target, false);
                }

                //開始点をCinemachineへ確定し、他の挙動を始める前に必ず1フレーム表示する
                yield return ConfirmShotStart(startState, target);

                b_isFirstShot = false;

                //Point Aを指定時間見せてから、Point Bへの移動を始める
                yield return HoldShot(startState, target, shot.StartHoldDuration);

                //速度倍率を移動時間へ反映して、Shot開始位置から終了位置へ移動する
                float moveDuration = shot.MoveDuration / shot.MoveSpeed; //速度倍率を反映した実移動時間
                yield return MoveInsideShot(startState, endState, target, moveDuration, shot.MoveCurve);

                //Shot終了位置で指定時間止める
                yield return HoldShot(endState, target, shot.HoldDuration);
            }
        }
        while (_sequence.Loop);

        //演出が終了したら、BlendとTimeScaleを元に戻す
        RestoreBrainBlend();
        RestoreTimeScale();

        //演出終了後に通常カメラへ戻すかどうかを確認
        if (_sequence.ReturnToGameplayCamera)
        {
            //異なるカメラ姿勢のBlend中にプレイヤーが画面外へ出ないよう、通常カメラへCutで戻す
            SwitchToGameplayCameraWithCut();
        }
    }

    //フェード処理の途中でPoint Aへ瞬間移動し、Main Cameraの実出力まで開始位置へ確定
    private IEnumerator TransitionToShotStart(SCameraState _startState, Transform _target, bool _activatePoseCamera)
    {
        //黒が完成した同じフェードCoroutine内で瞬間移動を実行する
        //Fadeの終了後に移動する順序へ戻らないよう、処理をActionとして渡す
        yield return m_fadeOverlay.FadeToBlack(E_POINT_A_FADE_DURATION,
            () => PrepareShotStart(_startState, _target, _activatePoseCamera));

        //Cinemachine BrainがMain CameraへPoint Aを出力したことを黒画面中に確認する
        yield return ConfirmShotStartOutput(_startState, _target);

        RestoreBrainBlend();

        //Point Aの実出力が確定した後だけフェードインを開始する
        yield return m_fadeOverlay.Fade(0.0f, E_POINT_A_FADE_DURATION);
    }

    //Point Aの全設定を一度に適用し、カメラ切り替えをCutへ固定
    private void PrepareShotStart(SCameraState _startState, Transform _target, bool _activatePoseCamera)
    {
        m_currentState = _startState;
        m_currentLookPoint = _target.position;
        ApplyState(m_currentState, _target, m_currentLookPoint);

        BeginCutBlend();
        if (_activatePoseCamera) SetPoseCameraActive();

        //Brainの次回更新を待つ間も、黒画面中の実カメラをPoint Aへ合わせる
        ForceOutputCameraToPoseCamera();
    }

    //Brainの更新方式に依存せず、Main CameraがPoint Aへ到達するまで黒画面を維持
    private IEnumerator ConfirmShotStartOutput(SCameraState _startState, Transform _target)
    {
        for (int i = 0; i < E_OUTPUT_SETTLE_FRAME_COUNT; i++)
        {
            ApplyState(_startState, _target, _target.position);
            ForceOutputCameraToPoseCamera();

            //前フレームのLateUpdateでBrainが出力を更新するまで待つ
            //WaitForEndOfFrameはバッチ検証で停止するため使用しない
            yield return null;

            Camera outputCamera = m_cinemachineBrain.GetComponent<Camera>(); //Game画面へ出力する実カメラ
            if (outputCamera == null ||
                (outputCamera.transform.position - m_poseCamera.transform.position).sqrMagnitude <= E_OUTPUT_POSITION_TOLERANCE)
            {
                yield break;
            }
        }

        //更新順の影響が残った場合も、フェードイン直前に最終位置を強制する
        ForceOutputCameraToPoseCamera();
    }

    //Main CameraをPose Cameraの現在位置へ即時同期
    private void ForceOutputCameraToPoseCamera()
    {
        Camera outputCamera = m_cinemachineBrain.GetComponent<Camera>(); //Brainが付いている実カメラ
        if (outputCamera == null) { return; }

        outputCamera.transform.SetPositionAndRotation(m_poseCamera.transform.position, m_poseCamera.transform.rotation);
    }

    //Shotの開始値をすべて確定し、移動・Orbit・FOV・Roll補間より先に表示
    private IEnumerator ConfirmShotStart(SCameraState _startState, Transform _target)
    {
        m_currentState = _startState;
        m_currentLookPoint = _target.position;
        ApplyState(m_currentState, _target, m_currentLookPoint);

        //Brainが確定した開始Stateを出力するまで、他の挙動を開始しない
        yield return null;
    }

    //Shotの開始位置から終了位置へ移動
    private IEnumerator MoveInsideShot(SCameraState _startState, SCameraState _endState, Transform _target, float _duration, AnimationCurve _moveCurve)
    {
        //移動時間が0以下なら、補間せずに終了位置へ直接切り替える
        if (_duration <= E_STOP_TIME_SCALE)
        {
            m_currentState = _endState;
            m_currentLookPoint = _target.position;
            ApplyState(m_currentState, _target, m_currentLookPoint);
            yield break;
        }

        float elapsed = 0.0f; //経過時間

        //経過時間が指定時間に達するまで、Shot開始位置から終了位置へ補間する
        while (elapsed < _duration)
        {
            float rate = Mathf.Clamp01(elapsed / _duration); // 0から1の進行率
            //Curveの先頭値に関係なく、移動率0では必ず開始Stateを使用する
            float curvedRate = rate <= E_STOP_TIME_SCALE ? E_STOP_TIME_SCALE : (_moveCurve != null ? _moveCurve.Evaluate(rate) : rate); //カーブ適用後の進行率

            //Yawを数値のまま補間し、注視点を中心に円弧を描いて移動する
            //-180度から180度の設定でも同一点扱いにせず、1周分の回転として扱う
            SCameraState state = SCameraState.LerpOrbit(_startState, _endState, curvedRate); //円軌道上のカメラ状態

            //補間後の状態をCinemachineに反映
            ApplyState(state, _target, _target.position);

            //現在の状態を保存
            m_currentState = state;
            m_currentLookPoint = _target.position;

            //経過時間を更新して次のフレームまで待つ
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        //補間が完了したら、Shot終了位置の状態をCinemachineに反映して、現在の状態を保存
        m_currentState = _endState;
        m_currentLookPoint = _target.position;
        ApplyState(m_currentState, _target, m_currentLookPoint);
    }

    //Shot終了位置で指定時間止める
    private IEnumerator HoldShot(SCameraState _state, Transform _target, float _duration)
    {
        float elapsed = 0.0f; //経過時間

        //経過時間が指定時間に達するまで、Shot終了位置で止める
        while (elapsed < _duration)
        {
            //現在の状態をCinemachineに反映
            m_currentLookPoint = _target.position;
            ApplyState(_state, _target, m_currentLookPoint);

            //経過時間を更新して次のフレームまで待つ
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        //補間が完了したら、Shot終了位置の状態をCinemachineに反映して、現在の状態を保存
        m_currentState = _state;
        m_currentLookPoint = _target.position;
    }

    //カメラ状態を実際のTransform、Lens、Targetに反映。
    private void ApplyState(SCameraState _state, Transform _target, Vector3 _lookPoint)
    {
        Quaternion frontRotation = GetFrontRotation(); //正面基準の回転

        Vector3 cameraPosition = _lookPoint + frontRotation * _state.GetLocalPosition(); //カメラのワールド座標

        Vector3 lookDirection = _lookPoint - cameraPosition; // カメラから注視点への方向

        if (lookDirection.sqrMagnitude < E_MIN_LOOK_DIRECTION_SQR) { return; }

        Quaternion cameraRotation = Quaternion.LookRotation(lookDirection, Vector3.up); //プレイヤーを中央に保つカメラ回転

        //Cinemachine3のTarget設定
        //位置を反映する前にTargetを設定し、初回フレームからプレイヤーを画面中央にする
        CameraTarget cameraTarget = m_poseCamera.Target;
        cameraTarget.TrackingTarget = _target;
        cameraTarget.LookAtTarget = _target;
        cameraTarget.CustomLookAtTarget = true;
        m_poseCamera.Target = cameraTarget;

        //Cinemachine 3のLens設定を先に更新する
        //CameraStateを作り直す前に設定し、Sequenceの初回フレームからStart Rollを反映する
        LensSettings lens = m_poseCamera.Lens;
        lens.FieldOfView = _state.m_fieldOfView;
        lens.Dutch = _state.m_roll;
        m_poseCamera.Lens = lens;

        //TransformとCinemachine内部のカメラ状態を同時に更新する
        //Standby中だった最初のフレームにも古い位置、向き、Lensを残さない
        m_poseCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        m_poseCamera.ForceCameraPosition(cameraPosition, cameraRotation);
        m_poseCamera.PreviousStateIsValid = false;
        m_poseCamera.UpdateCameraState(Vector3.up, -1.0f);
    }

    //Front Referenceの位置から、水平な正面回転
    private Quaternion GetFrontRotation()
    {
        Vector3 origin = GetFrontOrigin(); //正面方向を測る中心

        Vector3 flatForward = Vector3.ProjectOnPlane(m_frontReference.position - origin, Vector3.up); //水平方向だけを残した正面方向

        //正面方向がほとんどゼロベクトルなら、Front Referenceのforwardを使う
        if (flatForward.sqrMagnitude < E_MIN_LOOK_DIRECTION_SQR)
        {
            flatForward = Vector3.ProjectOnPlane(m_frontReference.forward, Vector3.up);
        }

        //それでもゼロベクトルなら、キャラクターのforwardを使う
        if (flatForward.sqrMagnitude < E_MIN_LOOK_DIRECTION_SQR)
        {
            flatForward = Vector3.ProjectOnPlane(m_characterRoot.forward, Vector3.up);
        }

        //それでもゼロベクトルなら、ワールドの前方向を使う
        if (flatForward.sqrMagnitude < E_MIN_LOOK_DIRECTION_SQR)
        {
            flatForward = Vector3.forward;
        }

        //正面方向を向く回転を作る
        return Quaternion.LookRotation(flatForward.normalized, Vector3.up);
    }

    //正面方向を測る中心を返す
    private Vector3 GetFrontOrigin()
    {
        if (m_chestTarget != null) return m_chestTarget.position;

        return m_characterRoot.position;
    }

    //Shot Presetで指定された注視点を返す
    private Transform GetTarget(ECameraTargetPoint _targetPoint)
    {
        switch (_targetPoint)
        {
            case ECameraTargetPoint.Waist:
                return m_waistTarget;

            case ECameraTargetPoint.Face:
                return m_faceTarget;

            case ECameraTargetPoint.Chest:
            default:
                return m_chestTarget;
        }
    }

    //Sequence内の最初の有効なShotを探す
    private CameraShotPreset FindFirstValidShot(CameraSequence _sequence)
    {
        //Sequence内のShotを順番に確認して、最初にnullでないShotを返す
        for (int i = 0; i < _sequence.Shots.Count; i++)
        {
            if (_sequence.Shots[i] != null) return _sequence.Shots[i];
        }

        return null;
    }

    /// Shotの開始状態を作る
    private SCameraState BuildStartState(CameraShotPreset _shot)
    {
        return new SCameraState(_shot.StartYaw, _shot.StartPitch, _shot.StartRadius, _shot.StartHeight, _shot.StartSide,
            _shot.StartFieldOfView, _shot.StartRoll, _shot.PathType, _shot.PointA, _shot.OrbitAngleOffset);
    }

    //Shotの終了状態を作る
    private SCameraState BuildEndState(CameraShotPreset _shot)
    {
        return new SCameraState(_shot.EndYaw, _shot.EndPitch, _shot.EndRadius, _shot.EndHeight, _shot.EndSide,
            _shot.EndFieldOfView, _shot.EndRoll, _shot.PathType, _shot.PointB, _shot.OrbitAngleOffset);
    }

    //通常カメラを有効
    private void SetGameplayCameraActive()
    {
        //Priorityを切り替える前に通常カメラをプレイヤーへ向ける
        CenterGameplayCameraOnCharacter();

        //通常カメラのPriorityを上げて、ポーズカメラのPriorityを下げる
        if (m_gameplayCamera != null)
        {
            m_gameplayCamera.Priority = m_gameplayPriority;
        }

        //ポーズカメラのPriorityを下げる
        if (m_poseCamera != null)
        {
            m_poseCamera.Priority = m_inactivePosePriority;
        }
    }

    //Gameplay Cameraの位置を変えず、Chest Targetが画面中央になる向きへ更新
    private void CenterGameplayCameraOnCharacter()
    {
        if (m_gameplayCamera == null || m_chestTarget == null) { return; }

        Vector3 cameraPosition = m_gameplayCamera.transform.position; //通常カメラの現在位置
        Vector3 lookDirection = m_chestTarget.position - cameraPosition; //胸の注視点へ向かう方向

        if (lookDirection.sqrMagnitude < E_MIN_LOOK_DIRECTION_SQR) { return; }

        Quaternion cameraRotation = Quaternion.LookRotation(lookDirection, Vector3.up); //プレイヤーを中央にする回転

        //Targetと内部状態を更新し、Brainへ正しい向きを即時反映する
        CameraTarget cameraTarget = m_gameplayCamera.Target;
        cameraTarget.TrackingTarget = m_chestTarget;
        cameraTarget.LookAtTarget = m_chestTarget;
        cameraTarget.CustomLookAtTarget = true;
        m_gameplayCamera.Target = cameraTarget;
        m_gameplayCamera.ForceCameraPosition(cameraPosition, cameraRotation);
    }

    //プレイヤーを画面中央に保ったまま、通常カメラへ即時切り替え
    private void SwitchToGameplayCameraWithCut()
    {
        //BrainのBlendを一時的にCutへ変更してから、通常カメラを有効にする
        BeginCutBlend();
        SetGameplayCameraActive();

        //BrainがCutを評価するまで1フレーム待ってから、元のBlend設定へ戻す
        m_restoreBlendRoutine = StartCoroutine(RestoreBrainBlendNextFrame());
    }

    //次のフレームでBrainのBlend設定を元に戻す
    private IEnumerator RestoreBrainBlendNextFrame()
    {
        yield return null;

        RestoreBrainBlend();
        m_restoreBlendRoutine = null;
    }

    //Blend設定の復元待ちCoroutineを停止
    private void StopRestoreBlendRoutine()
    {
        if (m_restoreBlendRoutine == null) { return; }

        StopCoroutine(m_restoreBlendRoutine);
        m_restoreBlendRoutine = null;
    }

    //ポーズカメラを有効
    private void SetPoseCameraActive()
    {
        m_gameplayCamera.Priority = m_gameplayPriority;
        m_poseCamera.Priority = m_activePosePriority;
    }

    //通常カメラから補間せず、Shot開始位置へ直接切り替えるBlendにする
    private void BeginCutBlend()
    {
        //Cinemachine Brainがnullなら何もしない
        if (m_cinemachineBrain == null || b_mChangedBrainBlend) { return; }

        //現在のBlend設定を保存して、Cutに変更
        m_savedBrainBlend = m_cinemachineBrain.DefaultBlend;
        m_cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, E_STOP_TIME_SCALE);

        //Blendを変更したことを記録
        b_mChangedBrainBlend = true;
    }

    //Cinemachine BrainのBlend設定を元に戻す
    private void RestoreBrainBlend()
    {
        if (!b_mChangedBrainBlend || m_cinemachineBrain == null){ return; }

        m_cinemachineBrain.DefaultBlend = m_savedBrainBlend;
        b_mChangedBrainBlend = false;
    }

    //TimeScaleを元に戻す。
    private void RestoreTimeScale()
    {
        if (!b_mChangedTimeScale) return;

        Time.timeScale = m_savedTimeScale;
        b_mChangedTimeScale = false;
    }

    //Directorに必要な参照が設定されているか確認
    private bool ValidateReferences()
    {
        if (m_gameplayCamera == null || m_poseCamera == null || m_cinemachineBrain == null || m_characterRoot == null ||
            m_frontReference == null || m_waistTarget == null || m_chestTarget == null || m_faceTarget == null)
        {
            Debug.LogError("PoseCameraDirectorの参照設定が不足しています。" + "Inspectorの空欄を確認してください。", this);

            return false;
        }

        return true;
    }

    //再生するSequenceが有効か確認
    private bool ValidateSequence(CameraSequence _sequence)
    {
        if (_sequence == null)
        {
            Debug.LogError("再生するCameraSequenceが設定されていません。", this);
            return false;
        }

        if (FindFirstValidShot(_sequence) == null)
        {
            Debug.LogError("CameraSequenceにShot Presetが入っていません。", _sequence);
            return false;
        }

        return true;
    }

    //Test Sequence Listに再生可能なSequenceが入っているか確認
    private bool ValidateSequenceList()
    {
        bool hasValidSequence = false; //再生可能なSequenceが見つかったかどうか

        for (int i = 0; i < m_testSequenceList.Count; i++)
        {
            CameraSequence sequence = m_testSequenceList[i]; //確認するSequence
            if (sequence == null) { continue; }
            if (!ValidateSequence(sequence)) { return false; }

            hasValidSequence = true;
        }

        if (hasValidSequence) return true;

        Debug.LogError("Test Sequence Listに再生可能なSequenceが入っていません。", this);
        return false;
    }

    //無効化時に、停止中のTimeScaleやBlend設定を戻す
    private void OnDisable()
    {
        if (m_playRoutine != null)
        {
            StopCoroutine(m_playRoutine);
            m_playRoutine = null;
        }

        StopRestoreBlendRoutine();

        RestoreBrainBlend();
        RestoreTimeScale();
    }

    //選択時に注視点と正面方向をScene上へ表示
    private void OnDrawGizmosSelected()
    {
        DrawTargetGizmo(m_waistTarget, E_WAIST_GIZMO_RADIUS);
        DrawTargetGizmo(m_chestTarget, E_CHEST_GIZMO_RADIUS);
        DrawTargetGizmo(m_faceTarget, E_FACE_GIZMO_RADIUS);
        DrawFrontReferenceGizmo();
    }

    //CameraFrontReferenceと正面方向のGizmoを表示
    private void DrawFrontReferenceGizmo()
    {
        if (m_frontReference == null || m_characterRoot == null) { return; }

        Vector3 origin = GetFrontOrigin(); //正面方向の基準点
        Vector3 flatForward = Vector3.ProjectOnPlane(m_frontReference.position - origin, Vector3.up); //正面方向

        //正面方向がほとんどゼロベクトルなら、Front Referenceのforwardを使う
        if (flatForward.sqrMagnitude < E_MIN_LOOK_DIRECTION_SQR)
        {
            flatForward = Vector3.ProjectOnPlane(m_frontReference.forward, Vector3.up);
        }

        //それでもゼロベクトルなら、キャラクターのforwardを使う
        if (flatForward.sqrMagnitude < E_MIN_LOOK_DIRECTION_SQR) { return; }

        Vector3 directionEnd = origin + flatForward.normalized * E_FRONT_GIZMO_LENGTH; //正面方向線の終点

        //Gizmoを描画
        Gizmos.DrawLine(origin, m_frontReference.position);
        Gizmos.DrawWireSphere(m_frontReference.position, E_FRONT_REFERENCE_GIZMO_RADIUS);
        Gizmos.DrawLine(origin, directionEnd);
        Gizmos.DrawWireSphere(directionEnd, E_FRONT_DIRECTION_GIZMO_RADIUS);
    }

    //カメラ注視点のGizmoを表示します。
    private static void DrawTargetGizmo(Transform _target, float _radius)
    {
        if (_target == null){ return; }
        Gizmos.DrawWireSphere(_target.position, _radius);
    }

}

//Canvas設定なしで使用できる全画面フェード
internal sealed class CameraFadeOverlay : MonoBehaviour
{
    private CanvasGroup m_canvasGroup; //フェードの透明度

    //Directorの子として黒い全画面Canvasを作る
    public static CameraFadeOverlay Create(Transform _parent)
    {
        GameObject canvasObject = new GameObject("Camera Fade Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup)); //フェードCanvas
        canvasObject.transform.SetParent(_parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        GameObject imageObject = new GameObject("Black", typeof(RectTransform), typeof(Image)); //黒い全画面画像
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        CameraFadeOverlay overlay = canvasObject.AddComponent<CameraFadeOverlay>();
        overlay.m_canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        overlay.Hide();
        return overlay;
    }

    //現在の透明度から指定透明度へフェード
    public IEnumerator Fade(float _alpha, float _duration)
    {
        float startAlpha = m_canvasGroup.alpha; //開始透明度
        float elapsed = 0.0f;                   //経過時間

        while (elapsed < _duration)
        {
            float rate = Mathf.SmoothStep(0.0f, 1.0f, elapsed / _duration); //自然な加減速
            m_canvasGroup.alpha = Mathf.Lerp(startAlpha, _alpha, rate);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        m_canvasGroup.alpha = _alpha;
    }

    //黒が完成した瞬間に指定処理を実行し、同じフェード処理内で1フレーム黒を維持
    public IEnumerator FadeToBlack(float _duration, Action _onBlack)
    {
        float startAlpha = m_canvasGroup.alpha; //開始透明度
        float elapsed = 0.0f;                   //経過時間

        //このCoroutine内で暗転と瞬間移動を完結し、呼び出し側へ戻ってから移動しない
        while (elapsed < _duration)
        {
            float rate = Mathf.SmoothStep(0.0f, 1.0f, elapsed / _duration); //自然な暗転
            m_canvasGroup.alpha = Mathf.Lerp(startAlpha, 1.0f, rate);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        m_canvasGroup.alpha = 1.0f;

        //Canvasを先に完全な黒として確定してから、カメラを瞬間移動する
        Canvas.ForceUpdateCanvases();
        _onBlack?.Invoke();

        //瞬間移動したフレームを完全な黒のまま維持する
        yield return null;
    }

    //フェードを解除
    public void Hide()
    {
        if (m_canvasGroup != null) m_canvasGroup.alpha = 0.0f;
    }
}
