/*━━━━━━━━━
@file        CameraShotPreset.cs
@brief       1カット分のカメラ位置、角度、時間を保存する設定アセット
@author      24CU0139 ラヤマジ プラシャント
@date 作成日 2026/07/12
最終更新日   2026/07/23
@remarks     このアセットの数値だけを調整します。
━━━━━━━━━*/
using UnityEngine;
using UnityEngine.Serialization;

//1カット分のカメラ設定
[CreateAssetMenu( fileName = "Shot_New", menuName = "Camera/ShotPreset")]
public sealed class CameraShotPreset : ScriptableObject
{
    private const int E_MEMO_MIN_LINE = 2;       //Memoの最小行数
    private const int E_MEMO_MAX_LINE = 4;       //Memoの最大行数
    private const string E_DEFAULT_MEMO = "何を見せるカメラかを書きます。";  //Memoの初期値

    private const float E_MIN_TRANSITION_DURATION = 0.0f;        //切り替え時間の最小値
    private const float E_DEFAULT_TRANSITION_DURATION = 0.25f;   //切り替え時間の初期値
    private const float E_MIN_MOVE_DURATION = 0.01f;             //移動時間の最小値
    private const float E_DEFAULT_MOVE_DURATION = 1.0f;          //移動時間の初期値
    private const float E_MIN_MOVE_SPEED = 0.01f;                //移動速度倍率の最小値
    private const float E_DEFAULT_MOVE_SPEED = 1.0f;             //移動速度倍率の初期値
    private const float E_MIN_HOLD_DURATION = 0.0f;              //停止時間の最小値
    private const float E_DEFAULT_START_HOLD_DURATION = 0.0f;    //開始点停止時間の初期値
    private const float E_DEFAULT_HOLD_DURATION = 0.4f;          //停止時間の初期値

    private const float E_MIN_YAW = -180.0f;                     //横角度の最小値
    private const float E_MAX_YAW = 180.0f;                      //横角度の最大値
    private const float E_DEFAULT_START_YAW = -30.0f;            //開始横角度の初期値
    private const float E_DEFAULT_END_YAW = 30.0f;               //終了横角度の初期値

    private const float E_MIN_PITCH = -45.0f;                    //縦角度の最小値
    private const float E_MAX_PITCH = 45.0f;                     //縦角度の最大値
    private const float E_DEFAULT_START_PITCH = 0.0f;            //開始縦角度の初期値
    private const float E_DEFAULT_END_PITCH = -3.0f;             //終了縦角度の初期値

    private const float E_MIN_RADIUS = 1.5f;                     //円軌道半径の最小値
    private const float E_MAX_RADIUS = 12.0f;                    //円軌道半径の最大値
    private const float E_DEFAULT_START_RADIUS = 4.2f;           //開始半径の初期値
    private const float E_DEFAULT_END_RADIUS = 3.5f;             //終了半径の初期値

    private const float E_MIN_OFFSET = -3.0f;                     //高さと横ずらしの最小値
    private const float E_MAX_OFFSET = 3.0f;                      //高さと横ずらしの最大値
    private const float E_DEFAULT_START_HEIGHT = 0.0f;            //開始高さの初期値
    private const float E_DEFAULT_END_HEIGHT = -0.2f;             //終了高さの初期値
    private const float E_DEFAULT_SIDE = 0.0f;                    //横ずらしの初期値

    private const float E_MIN_FIELD_OF_VIEW = 15.0f;              //FOVの最小値
    private const float E_MAX_FIELD_OF_VIEW = 90.0f;              //FOVの最大値
    private const float E_DEFAULT_START_FIELD_OF_VIEW = 45.0f;    //開始FOVの初期値
    private const float E_DEFAULT_END_FIELD_OF_VIEW = 36.0f;      //終了FOVの初期値

    private const float E_MIN_ROLL = -720.0f;                     //ロール角の最小値
    private const float E_MAX_ROLL = 720.0f;                      //ロール角の最大値
    private const float E_DEFAULT_ROLL = 0.0f;                    //ロール角の初期値

    private static readonly Vector3 E_DEFAULT_START_POSITION = new Vector3(0.0f, 0.0f, 4.2f); //開始点の初期値
    private static readonly Vector3 E_DEFAULT_END_POSITION = new Vector3(0.0f, 0.0f, 3.5f);   //終了点の初期値

    [Header("このカットの説明")]
    [TextArea(E_MEMO_MIN_LINE, E_MEMO_MAX_LINE)]
    [SerializeField]
    [FormerlySerializedAs("memo")]
    private string m_memo = E_DEFAULT_MEMO;  //このShotの説明文

    [Header("注目する場所")]
    [Tooltip("全身はWaist、筋肉はChest、表情はFaceが目安")]
    [SerializeField]
    [FormerlySerializedAs("targetPoint")]
    private ECameraTargetPoint m_targetPoint = ECameraTargetPoint.Chest; //カメラが見る場所

    [Header("時間")]
    [Tooltip("前のカットから開始位置へ移る時間")]
    [Min(E_MIN_TRANSITION_DURATION)]
    [SerializeField]
    [FormerlySerializedAs("transitionDuration")]
    private float m_transitionDuration = E_DEFAULT_TRANSITION_DURATION; //カット切り替え時間

    [Tooltip("開始位置から終了位置へ動く時間")]
    [Min(E_MIN_MOVE_DURATION)]
    [SerializeField]
    [FormerlySerializedAs("moveDuration")]
    private float m_moveDuration = E_DEFAULT_MOVE_DURATION;  //Shot内の移動時間

    [Tooltip("1.0が標準速度。値を大きくすると速く、小さくすると遅く移動します。")]
    [Min(E_MIN_MOVE_SPEED)]
    [SerializeField]
    private float m_moveSpeed = E_DEFAULT_MOVE_SPEED; //Shot内の移動速度倍率

    [Tooltip("Point Aを映してから移動を始めるまでの時間")]
    [Min(E_MIN_HOLD_DURATION)]
    [SerializeField]
    private float m_startHoldDuration = E_DEFAULT_START_HOLD_DURATION; //開始点での停止時間

    [Tooltip("移動が終わったあと、その画角を見せる時間")]
    [Min(E_MIN_HOLD_DURATION)]
    [SerializeField]
    [FormerlySerializedAs("holdDuration")]
    private float m_holdDuration = E_DEFAULT_HOLD_DURATION;  //移動後の停止時間

    [Tooltip("カメラ移動の加速と減速を決めるカーブ")]
    [SerializeField]
    [FormerlySerializedAs("moveCurve")]
    private AnimationCurve m_moveCurve = AnimationCurve.EaseInOut(E_MIN_HOLD_DURATION, E_MIN_HOLD_DURATION, E_DEFAULT_MOVE_DURATION, E_DEFAULT_MOVE_DURATION);  //移動カーブ

    [Header("移動経路")]
    [Tooltip("OrbitByAngleRadiusは角度と半径、PointToPointは直線、OrbitPointToPointは2点を通る円弧移動です。")]
    [SerializeField]
    private ECameraPathType m_pathType = ECameraPathType.OrbitByAngleRadius; //Shot内の移動経路

    [Tooltip("Point A。OrbitPointToPointではX/Zが開始方向、Yが開始高さです。距離はStart Radiusで指定します。")]
    [SerializeField]
    [FormerlySerializedAs("m_startPosition")]
    private Vector3 m_pointA = E_DEFAULT_START_POSITION; //注視点を基準にしたPoint A

    [Tooltip("Point B。OrbitPointToPointではX/Zが終了方向、Yが終了高さです。距離はEnd Radiusで指定します。")]
    [SerializeField]
    [FormerlySerializedAs("m_endPosition")]
    private Vector3 m_pointB = E_DEFAULT_END_POSITION; //注視点を基準にしたPoint B

    [Tooltip("OrbitPointToPointの追加回転角。0は最短円弧、360は一周追加、-360は逆方向へ一周追加します。")]
    [SerializeField]
    private float m_orbitAngleOffset; //Point AからPoint Bへ移動するときの追加回転角

    [Header("開始設定")]
    [Tooltip("0度がFront Referenceを置いた側、+90度が右側、-90度が左側、180度が背面")]
    [Range(E_MIN_YAW, E_MAX_YAW)]
    [SerializeField]
    [FormerlySerializedAs("startYaw")]
    private float m_startYaw = E_DEFAULT_START_YAW;  //開始時の横角度

    [Tooltip("プラスで上側、マイナスで下側から映します。")]
    [Range(E_MIN_PITCH, E_MAX_PITCH)]
    [SerializeField]
    [FormerlySerializedAs("startPitch")]
    private float m_startPitch = E_DEFAULT_START_PITCH;  //開始時の縦角度

    [Tooltip("開始時のTargetとカメラの距離。OrbitPointToPointでも使用します。")]
    [Range(E_MIN_RADIUS, E_MAX_RADIUS)]
    [SerializeField]
    [FormerlySerializedAs("startDistance")]
    [FormerlySerializedAs("m_startDistance")]
    private float m_startRadius = E_DEFAULT_START_RADIUS; //開始時の円軌道半径

    [Tooltip("カメラを上下へずらします。")]
    [Range(E_MIN_OFFSET, E_MAX_OFFSET)]
    [SerializeField]
    [FormerlySerializedAs("startHeight")]
    private float m_startHeight = E_DEFAULT_START_HEIGHT;  //開始時の高さ

    [Tooltip("カメラを左右へずらします。")]
    [Range(E_MIN_OFFSET, E_MAX_OFFSET)]
    [SerializeField]
    [FormerlySerializedAs("startSide")]
    private float m_startSide = E_DEFAULT_SIDE;  //開始時の横ずらし

    [Tooltip("小さいほど望遠で大きく映ります。")]
    [Range(E_MIN_FIELD_OF_VIEW, E_MAX_FIELD_OF_VIEW)]
    [SerializeField]
    [FormerlySerializedAs("startFieldOfView")]
    private float m_startFieldOfView = E_DEFAULT_START_FIELD_OF_VIEW;  //開始時のFOV

    [Tooltip("カメラの前後軸を中心に画面を傾ける開始ロール角。0度が水平、360度が1周です。")]
    [Range(E_MIN_ROLL, E_MAX_ROLL)]
    [SerializeField]
    [FormerlySerializedAs("m_startDutch")]
    [FormerlySerializedAs("startDutch")]
    private float m_startRoll = E_DEFAULT_ROLL;  //開始時のカメラロール角

    [Header("終了設定")]
    [Range(E_MIN_YAW, E_MAX_YAW)]
    [SerializeField]
    [FormerlySerializedAs("endYaw")]
    private float m_endYaw = E_DEFAULT_END_YAW;  //終了時の横角度

    [Range(E_MIN_PITCH, E_MAX_PITCH)]
    [SerializeField]
    [FormerlySerializedAs("endPitch")]
    private float m_endPitch = E_DEFAULT_END_PITCH;  //終了時の縦角度

    [Range(E_MIN_RADIUS, E_MAX_RADIUS)]
    [SerializeField]
    [FormerlySerializedAs("endDistance")]
    [FormerlySerializedAs("m_endDistance")]
    [Tooltip("終了時のTargetとカメラの距離。OrbitPointToPointでも使用します。")]
    private float m_endRadius = E_DEFAULT_END_RADIUS; //終了時の円軌道半径

    [Range(E_MIN_OFFSET, E_MAX_OFFSET)]
    [SerializeField]
    [FormerlySerializedAs("endHeight")]
    private float m_endHeight = E_DEFAULT_END_HEIGHT; //終了時の高さ

    [Range(E_MIN_OFFSET, E_MAX_OFFSET)]
    [SerializeField]
    [FormerlySerializedAs("endSide")]
    private float m_endSide = E_DEFAULT_SIDE; //終了時の横ずらし

    [Range(E_MIN_FIELD_OF_VIEW, E_MAX_FIELD_OF_VIEW)]
    [SerializeField]
    [FormerlySerializedAs("endFieldOfView")]
    private float m_endFieldOfView = E_DEFAULT_END_FIELD_OF_VIEW; //終了時のFOV

    [Tooltip("カメラの前後軸を中心に画面を傾ける終了ロール角。360度で正方向へ1周、-360度で逆方向へ1周します。")]
    [Range(E_MIN_ROLL, E_MAX_ROLL)]
    [SerializeField]
    [FormerlySerializedAs("m_endDutch")]
    [FormerlySerializedAs("endDutch")]
    private float m_endRoll = E_DEFAULT_ROLL; //終了時のカメラロール角

    //プロパティ
    public string Memo => m_memo;
    public ECameraTargetPoint TargetPoint => m_targetPoint;
    public float TransitionDuration => m_transitionDuration;
    public float MoveDuration => m_moveDuration;
    public float MoveSpeed => Mathf.Max(m_moveSpeed, E_MIN_MOVE_SPEED);
    public float StartHoldDuration => m_startHoldDuration;
    public float HoldDuration => m_holdDuration;
    public AnimationCurve MoveCurve => m_moveCurve;
    public ECameraPathType PathType => m_pathType;
    public Vector3 PointA => m_pointA;
    public Vector3 PointB => m_pointB;
    public float OrbitAngleOffset => m_orbitAngleOffset;

    //開始位置のプロパティ
    public float StartYaw => m_startYaw;
    public float StartPitch => m_startPitch;
    public float StartRadius => m_startRadius;
    public float StartHeight => m_startHeight;
    public float StartSide => m_startSide;
    public float StartFieldOfView => m_startFieldOfView;
    public float StartRoll => m_startRoll;

    //終了位置のプロパティ
    public float EndYaw => m_endYaw;
    public float EndPitch => m_endPitch;
    public float EndRadius => m_endRadius;
    public float EndHeight => m_endHeight;
    public float EndSide => m_endSide;
    public float EndFieldOfView => m_endFieldOfView;
    public float EndRoll => m_endRoll;
}
