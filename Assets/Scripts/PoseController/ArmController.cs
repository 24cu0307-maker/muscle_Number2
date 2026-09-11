using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;



// ============================================================
// 補正値（調整用）
// ============================================================
[Serializable]
public class Correction
{
    [Header("左肩補正値")]
    [Range(-360, 360)]
    [SerializeField]
    public int[] Correction1 = new int[3];

    [Header("左肩補正値")]
    [Range(-360, 360)]
    [SerializeField]
    public int[] Correction2 = new int[3];

    [Header("右肩補正値")]
    [Range(-360, 360)]
    [SerializeField]
    public int[] Correction3 = new int[3];

    [Header("右肘補正値")]
    [Range(-360, 360)]
    [SerializeField]
    public int[] Correction4 = new int[3];
}



///<summary>
///腕のコントロール
///</summary>
public class ArmController : MonoBehaviour
{
    //データを保存領域
    //[SerializeField] private PositionDataManager dataManager;


    //腕が水平　０

    //腕が↑↑　１

    //補正値
    [Header("補正値")]
    [SerializeField]
    private List<Correction> CorrectionValue =
        new List<Correction>();


    [Header("左肩補正値")]
    [Range(-360, 360)]
    [SerializeField]
    private int[] Correction1 = new int[3];

    [Header("左肩補正値")]
    [Range(-360, 360)]
    [SerializeField]
    private int[] Correction2 = new int[3];

    [Header("右肩補正値")]
    [Range(-360, 360)]
    [SerializeField]
    private int[] Correction3 = new int[3];

    [Header("右肘補正値")]
    [Range(-360, 360)]
    [SerializeField]
    private int[] Correction4 = new int[3];


    // ============================================================
    // Inspector確認用
    // ============================================================

    [Header("===== リアルタイム確認 =====")]

    [Header("左腕")]
    [SerializeField]
    private float debugLeftUpperAngle;

    [SerializeField]
    private Vector3 debugLeftShoulderCorrection;

    [SerializeField]
    private Vector3 debugLeftElbowCorrection;


    [Header("右腕")]
    [SerializeField]
    private float debugRightUpperAngle;

    [SerializeField]
    private Vector3 debugRightShoulderCorrection;

    [SerializeField]
    private Vector3 debugRightElbowCorrection;


    // 補間状態確認
    [Header("補間状態")]

    [SerializeField]
    private float debugLeftLerp;

    [SerializeField]
    private float debugRightLerp;

    // ============================================================
    // 角度設定
    // ============================================================

    [Header("角度設定")]

    // ↓↓ の角度
    [SerializeField]
    private float downAngle = -90f;


    // ←→ の角度
    [SerializeField]
    private float horizontalAngle = 0f;


    // ↑↑ の角度
    [SerializeField]
    private float upAngle = 90f;

    //カメラ判定
    [SerializeField] public CameraRelativeMovement cameraJudge;

    //カメラ取得
    [SerializeField] public GameObject _camera;

    //人体のオブジェクトの情報
    [SerializeField] public HumanoidSkeleton playerArm;

    //座標を格納する箱
    [SerializeField] private Vector3[] _Body = new Vector3[37];

    //方向を格納
    [SerializeField] private Vector3 _RightShoulderRotationDir;
    [SerializeField] private Vector3 _RightElbowRotationDir;
    [SerializeField] private Vector3 _LeftShoulderRotationDir;
    [SerializeField] private Vector3 _LeftElbowRotationDir;
    [SerializeField] private Vector3 _NeckDir;

    [SerializeField] private Vector3 _LeftShoulderRotationDirMirrored;
    [SerializeField] private Vector3 _LeftElbowRotationDirMirrored;
    [SerializeField] private Vector3 _RightShoulderRotationDirMirrored;
    [SerializeField] private Vector3 _RightElbowRotationDirMirrored;
    [SerializeField] private Vector3 _NeckDirMirrored;

    //方向計算するクラス
    DirectionVectorCalculator _DirectionVectorCalculator;

    [SerializeField] private TMP_Text scoreText;

    public float debugfloat;

    public float Getfloat() { return debugfloat; }

    public void Awake()
    {
        //カメラ
        cameraJudge = new CameraRelativeMovement();

        _camera = GameObject.Find("Camera");

        //方向計算クラス
        _DirectionVectorCalculator = new DirectionVectorCalculator();


    }

    /*
    void Update()
    {
        // ----------------------------------------------------
        // PositionDataManager確認
        // ----------------------------------------------------

        if (!PositionDataManager.Instance)
            return;


        // ----------------------------------------------------
        // CorrectionValue確認
        //
        // 0 = ←→
        // 1 = ↑↑
        // 2 = ↓↓
        // ----------------------------------------------------

        if (CorrectionValue == null ||
            CorrectionValue.Count < 3)
        {
            Debug.LogWarning(
                "CorrectionValueは3個必要です。" +
                "\n[0] = ←→" +
                "\n[1] = ↑↑" +
                "\n[2] = ↓↓"
            );

            return;
        }


        // ----------------------------------------------------
        // Body取得
        // ----------------------------------------------------

        _Body =
            PositionDataManager.Instance.positionData.Body;


        // ----------------------------------------------------
        // カメラ取得
        // ----------------------------------------------------

        if (_camera == null)
        {
            _camera = GameObject.Find("Camera");
        }


        // ====================================================
        // 方向ベクトル計算
        // ====================================================

        // 右肩 → 右肘
        _RightShoulderRotationDir =
            _DirectionVectorCalculator.Vector(
                _Body[14],
                _Body[12]
            );


        // 右肘 → 右手首
        _RightElbowRotationDir =
            _DirectionVectorCalculator.Vector(
                _Body[16],
                _Body[14]
            );


        // 左肩 → 左肘
        _LeftShoulderRotationDir =
            _DirectionVectorCalculator.Vector(
                _Body[13],
                _Body[11]
            );


        // 左肘 → 左手首
        _LeftElbowRotationDir =
            _DirectionVectorCalculator.Vector(
                _Body[15],
                _Body[13]
            );


        // 首
        _NeckDir =
            _DirectionVectorCalculator.Vector(
                _Body[0],
                _Body[36]
            );


        // ====================================================
        // X軸反転
        // ====================================================

        _LeftShoulderRotationDirMirrored =
            _DirectionVectorCalculator.MirroredVector_X(
                _Body[13],
                _Body[11]
            );


        _LeftElbowRotationDirMirrored =
            _DirectionVectorCalculator.MirroredVector_X(
                _Body[15],
                _Body[13]
            );


        _RightShoulderRotationDirMirrored =
            _DirectionVectorCalculator.MirroredVector_X(
                _Body[14],
                _Body[12]
            );


        _RightElbowRotationDirMirrored =
            _DirectionVectorCalculator.MirroredVector_X(
                _Body[16],
                _Body[14]
            );


        _NeckDirMirrored =
            _DirectionVectorCalculator.MirroredVector_X(
                _Body[0],
                _Body[36]
            );


        // ====================================================
        // 補正前の回転
        // ====================================================

        Quaternion baseUpperWorldL =
            Quaternion.LookRotation(
                _RightShoulderRotationDirMirrored.normalized
            );


        Quaternion baseUpperWorldR =
            Quaternion.LookRotation(
                _LeftShoulderRotationDirMirrored.normalized
            );


        // ====================================================
        // 肩 → 肘の角度
        //
        // -180 ～ 180
        // ====================================================
        /*
        float leftUpperAngle =
            NormalizeAngle(
                baseUpperWorldL.eulerAngles.x
            );


        float rightUpperAngle =
            NormalizeAngle(
                baseUpperWorldR.eulerAngles.x
            );
        /////////
        float leftUpperAngle =
    GetVerticalAngle(_RightShoulderRotationDirMirrored);

        float rightUpperAngle =
            GetVerticalAngle(_LeftShoulderRotationDirMirrored);

        // ====================================================
        // 左腕の補正値
        // ====================================================

        Vector3 leftShoulderCorrection =
            GetCorrection(
                leftUpperAngle,
                CorrectionValue[2].Correction1, // ↓↓
                CorrectionValue[0].Correction1, // ←→
                CorrectionValue[1].Correction1  // ↑↑
            );



        Vector3 leftElbowCorrection =
            GetCorrection(
                leftUpperAngle,
                CorrectionValue[2].Correction2, // ↓↓
                CorrectionValue[0].Correction2, // ←→
                CorrectionValue[1].Correction2  // ↑↑
            );


        // ====================================================
        // 右腕の補正値
        // ====================================================

        Vector3 rightShoulderCorrection =
            GetCorrection(
                rightUpperAngle,
                CorrectionValue[2].Correction3, // ↓↓
                CorrectionValue[0].Correction3, // ←→
                CorrectionValue[1].Correction3  // ↑↑
            );


        Vector3 rightElbowCorrection =
            GetCorrection(
                rightUpperAngle,
                CorrectionValue[2].Correction4, // ↓↓
                CorrectionValue[0].Correction4, // ←→
                CorrectionValue[1].Correction4  // ↑↑
            );



        // ====================================================
        // 位置確認用
        // ====================================================
        debugLeftUpperAngle = leftUpperAngle;
        debugLeftShoulderCorrection = leftShoulderCorrection;
        debugLeftElbowCorrection = leftElbowCorrection;

        debugRightUpperAngle = rightUpperAngle;
        debugRightShoulderCorrection = rightShoulderCorrection;
        debugRightElbowCorrection = rightElbowCorrection;

        // ====================================================
        // 左腕
        // ====================================================

        // ----------------------------------------------------
        // 肩 → 肘
        // ----------------------------------------------------

        Quaternion upperWorldL =
            Quaternion.LookRotation(
                _RightShoulderRotationDirMirrored.normalized
            )
            *
            Quaternion.Euler(
                leftShoulderCorrection
            );


        // ----------------------------------------------------
        // 肘 → 手首
        // ----------------------------------------------------

        Quaternion lowerWorldL =
            Quaternion.LookRotation(
                _RightElbowRotationDirMirrored.normalized
            )
            *
            Quaternion.Euler(
                leftElbowCorrection
            );


        // ====================================================
        // 右腕
        // ====================================================

        // ----------------------------------------------------
        // 肩 → 肘
        // ----------------------------------------------------

        Quaternion upperWorldR =
            Quaternion.LookRotation(
                _LeftShoulderRotationDirMirrored.normalized
            )
            *
            Quaternion.Euler(
                rightShoulderCorrection
            );


        // ----------------------------------------------------
        // 肘 → 手首
        // ----------------------------------------------------

        Quaternion lowerWorldR =
            Quaternion.LookRotation(
                _LeftElbowRotationDirMirrored.normalized
            )
            *
            Quaternion.Euler(
                rightElbowCorrection
            );


        // ====================================================
        // 上腕に適用
        // ====================================================

        playerArm.playerLeftArm[0].rotation =
            upperWorldL;


        playerArm.playerRightArm[0].rotation =
            upperWorldR;


        // ====================================================
        // 前腕に適用
        // ====================================================

        playerArm.playerLeftArm[1].localRotation =
            Quaternion.Inverse(
                playerArm.playerLeftArm[0].rotation
            )
            *
            lowerWorldL;


        playerArm.playerRightArm[1].localRotation =
            Quaternion.Inverse(
                playerArm.playerRightArm[0].rotation
            )
            *
            lowerWorldR;


        // ====================================================
        // デバッグ
        // ====================================================

        Debug.Log(
            $"左腕 肩→肘 : {leftUpperAngle:F1}°"
        );

        Debug.Log(
            $"右腕 肩→肘 : {rightUpperAngle:F1}°"
        );




    }



    // ============================================================
    // 角度から補正値を取得
    //
    // [2] ↓↓
    //       ↓
    // [0] ←→
    //       ↓
    // [1] ↑↑
    //
    // の間を滑らかに補間
    // ============================================================

    private Vector3 GetCorrection(
        float angle,
        int[] downCorrection,
        int[] horizontalCorrection,
        int[] upCorrection)
    {
        angle = NormalizeAngle(angle);


        // ========================================================
        // ↓↓ → ←→
        //
        // -90° → 0°
        // ========================================================

        if (angle <= horizontalAngle)
        {
            float t =
                Mathf.InverseLerp(
                    downAngle,
                    horizontalAngle,
                    angle
                );


            return new Vector3(

                Mathf.Lerp(
                    downCorrection[0],
                    horizontalCorrection[0],
                    t
                ),

                Mathf.Lerp(
                    downCorrection[1],
                    horizontalCorrection[1],
                    t
                ),

                Mathf.Lerp(
                    downCorrection[2],
                    horizontalCorrection[2],
                    t
                )
            );
        }


        // ========================================================
        // ←→ → ↑↑
        //
        // 0° → +90°
        // ========================================================

        float t2 =
            Mathf.InverseLerp(
                horizontalAngle,
                upAngle,
                angle
            );


        return new Vector3(

            Mathf.Lerp(
                horizontalCorrection[0],
                upCorrection[0],
                t2
            ),

            Mathf.Lerp(
                horizontalCorrection[1],
                upCorrection[1],
                t2
            ),

            Mathf.Lerp(
                horizontalCorrection[2],
                upCorrection[2],
                t2
            )
        );
    }


    // ============================================================
    // 角度を -180～180° に変換
    // ============================================================

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;


        if (angle > 180f)
        {
            angle -= 360f;
        }


        if (angle < -180f)
        {
            angle += 360f;
        }


        return angle;
    }








    private float GetVerticalAngle(Vector3 direction)
    {
        direction.Normalize();

        float horizontalLength =
            new Vector2(direction.x, direction.z).magnitude;

        return Mathf.Atan2(
            direction.y,
            horizontalLength
        ) * Mathf.Rad2Deg;
    }
    */


    void Update()
    {




        if (!PositionDataManager.Instance) return;

        //座標を取得
        _Body = PositionDataManager.Instance.positionData.Body;

        if (_camera == null)
        {
            _camera = GameObject.Find("Camera");

        }

        //方向ベクトルを計算　※引数（a、b）→ a - b
        _RightShoulderRotationDir = _DirectionVectorCalculator.Vector(_Body[14], _Body[12]);
        _RightElbowRotationDir = _DirectionVectorCalculator.Vector(_Body[16], _Body[14]);
        _LeftShoulderRotationDir = _DirectionVectorCalculator.Vector(_Body[13], _Body[11]);
        _LeftElbowRotationDir = _DirectionVectorCalculator.Vector(_Body[15], _Body[13]);
        _NeckDir = _DirectionVectorCalculator.Vector(_Body[0], _Body[36]);
        //X軸反転方向ベクトルを計算　※引数（a、b）→ a - b
        _LeftShoulderRotationDirMirrored = _DirectionVectorCalculator.MirroredVector_X(_Body[13], _Body[11]);
        _LeftElbowRotationDirMirrored = _DirectionVectorCalculator.MirroredVector_X(_Body[15], _Body[13]);
        _RightShoulderRotationDirMirrored = _DirectionVectorCalculator.MirroredVector_X(_Body[14], _Body[12]);
        _RightElbowRotationDirMirrored = _DirectionVectorCalculator.MirroredVector_X(_Body[16], _Body[14]);
        _NeckDirMirrored = _DirectionVectorCalculator.MirroredVector_X(_Body[0], _Body[36]);


        // =========================
        // 左腕
        // =========================
        // 肩 → 肘
        Quaternion upperWorldL =
            Quaternion.LookRotation(
                _RightShoulderRotationDirMirrored.normalized
            )
            * Quaternion.Euler(
                Correction1[0],
                Correction1[1],
                Correction1[2]
            );

        // 肘 → 手首
        Quaternion lowerWorldL =
            Quaternion.LookRotation(
                _RightElbowRotationDirMirrored.normalized
            )
            * Quaternion.Euler(
                Correction2[0],
                Correction2[1],
                Correction2[2]
            );




        // =========================
        // 右腕
        // =========================
        Quaternion upperWorldR =
            Quaternion.LookRotation(
                _LeftShoulderRotationDirMirrored.normalized
            )
            * Quaternion.Euler(
                Correction3[0],
                Correction3[1],
                Correction3[2]
            );

        Quaternion lowerWorldR =
            Quaternion.LookRotation(
                _LeftElbowRotationDirMirrored.normalized
            )
            * Quaternion.Euler(
                Correction4[0],
                Correction4[1],
                Correction4[2]
            );



        // 左腕の角度に変換
        Vector3 leftUpperAngle = upperWorldL.eulerAngles;
        Vector3 leftLowerAngle = lowerWorldL.eulerAngles;

        // 右腕の角度に変換
        Vector3 rightUpperAngle = upperWorldR.eulerAngles;
        Vector3 rightLowerAngle = lowerWorldR.eulerAngles;


        leftUpperAngle.y = ClampAroundAngle(leftUpperAngle.y, 178f, 40f);
        leftUpperAngle.y = ClampAroundAngle(leftUpperAngle.y, 178f, 40f);
        leftUpperAngle.y = ClampAroundAngle(leftUpperAngle.y, 178f, 40f);
        leftUpperAngle.y = ClampAroundAngle(leftUpperAngle.y, 178f, 40f);
        /*
        // X
        leftUpperAngle.x = NormalizeAngle(leftUpperAngle.x);
        leftLowerAngle.x = NormalizeAngle(leftLowerAngle.x);

        rightUpperAngle.x = NormalizeAngle(rightUpperAngle.x);
        rightLowerAngle.x = NormalizeAngle(rightLowerAngle.x);

        // Y
        leftUpperAngle.y = NormalizeAngle(leftUpperAngle.y);
        leftLowerAngle.y = NormalizeAngle(leftLowerAngle.y);

        rightUpperAngle.y = NormalizeAngle(rightUpperAngle.y);
        rightLowerAngle.y = NormalizeAngle(rightLowerAngle.y);

        // Z
        leftUpperAngle.z = NormalizeAngle(leftUpperAngle.z);
        leftLowerAngle.z = NormalizeAngle(leftLowerAngle.z);

        rightUpperAngle.z = NormalizeAngle(rightUpperAngle.z);
        rightLowerAngle.z = NormalizeAngle(rightLowerAngle.z);
        */



        // =========================
        // 上腕
        // =========================
        playerArm.playerLeftArm[0].rotation = upperWorldL;
        playerArm.playerRightArm[0].rotation = upperWorldR;



        // =========================
        // 前腕
        // =========================
        // 上腕の回転を基準にして
        // 「肘 → 手首」のワールド回転をローカル回転へ変換
        playerArm.playerLeftArm[1].localRotation =
            Quaternion.Inverse(
                playerArm.playerLeftArm[0].rotation
            ) * lowerWorldL;

        playerArm.playerRightArm[1].localRotation =
            Quaternion.Inverse(
                playerArm.playerRightArm[0].rotation
            ) * lowerWorldR;
    }


    /*
    playerArm.playerLeftArm[0].rotation = lowerWorldL;
    playerArm.playerRightArm[0].rotation = lowerWorldR;
    */

    /*
    //カメラの向きに応じて方向を変更
    if (!(cameraJudge.CameraDirection(_camera)))
    {


        //各ボーンにベクトル方向を与える　※基本補正値Quaternion.Euler(90, 90, 90)
        playerArm.playerRightArm[0].rotation = Quaternion.LookRotation(_LeftShoulderRotationDirMirrored.normalized) * Quaternion.Euler(Correction1[0], Correction1[1], Correction1[2]);
        playerArm.playerRightArm[1].rotation = Quaternion.LookRotation(_LeftElbowRotationDirMirrored.normalized) * Quaternion.Euler(Correction2[0], Correction2[1], Correction2[2]);
        playerArm.playerLeftArm[0].rotation = Quaternion.LookRotation(_RightShoulderRotationDirMirrored.normalized) * Quaternion.Euler(Correction3[0], Correction3[1], Correction3[2]);
        playerArm.playerLeftArm[1].rotation = Quaternion.LookRotation(_RightElbowRotationDirMirrored.normalized) * Quaternion.Euler(Correction4[0], Correction4[1], Correction4[2]);
        //_playerBody[6].rotation = Quaternion.LookRotation(_NeckDirMirrored.normalized) * Quaternion.Euler(45, 0, 0);

    }
    else

    {
        //各ボーンにベクトル方向を与える　※基本補正値Quaternion.Euler(90, 90, 90)
        playerArm.playerRightArm[0].rotation = Quaternion.LookRotation(_RightShoulderRotationDir.normalized) * Quaternion.Euler(Correction1[0], Correction1[1], Correction1[2]);
        playerArm.playerRightArm[1].rotation = Quaternion.LookRotation(_RightElbowRotationDir.normalized) * Quaternion.Euler(Correction2[0], Correction2[1], Correction2[2]);
        playerArm.playerLeftArm[0].rotation = Quaternion.LookRotation(_LeftShoulderRotationDir.normalized) * Quaternion.Euler(Correction3[0], Correction3[1], Correction3[2]);
        playerArm.playerLeftArm[1].rotation = Quaternion.LookRotation(_LeftElbowRotationDir.normalized) * Quaternion.Euler(Correction4[0], Correction4[1], Correction4[2]);
        //_playerBody[6].rotation = Quaternion.LookRotation(_NeckDir.normalized) * Quaternion.Euler(45, 0, 0);
    }
    */




    /*
    //カメラの向きに応じて方向を変更
    if (!(cameraJudge.CameraDirection(_camera)))
    {


        //各ボーンにベクトル方向を与える　※基本補正値Quaternion.Euler(90, 90, 90)
        playerArm.playerRightArm[0].rotation = Quaternion.LookRotation(_LeftShoulderRotationDirMirrored.normalized) * Quaternion.Euler(90, 90, 90);
        playerArm.playerRightArm[1].rotation = Quaternion.LookRotation(_LeftElbowRotationDirMirrored.normalized) * Quaternion.Euler(90, 90, 90);
        playerArm.playerLeftArm[0].rotation = Quaternion.LookRotation(_RightShoulderRotationDirMirrored.normalized) * Quaternion.Euler(90, 90, 90);
        playerArm.playerLeftArm[1].rotation = Quaternion.LookRotation(_RightElbowRotationDirMirrored.normalized) * Quaternion.Euler(90, 0, 0);
        //_playerBody[6].rotation = Quaternion.LookRotation(_NeckDirMirrored.normalized) * Quaternion.Euler(45, 0, 0);

    }
    else

    {
        //各ボーンにベクトル方向を与える　※基本補正値Quaternion.Euler(90, 90, 90)
        playerArm.playerRightArm[0].rotation = Quaternion.LookRotation(_RightShoulderRotationDir.normalized) * Quaternion.Euler(90, 90, 90);
        playerArm.playerRightArm[1].rotation = Quaternion.LookRotation(_RightElbowRotationDir.normalized) * Quaternion.Euler(90, 90, 90);
        playerArm.playerLeftArm[0].rotation = Quaternion.LookRotation(_LeftShoulderRotationDir.normalized) * Quaternion.Euler(90, 90, 90);
        playerArm.playerLeftArm[1].rotation = Quaternion.LookRotation(_LeftElbowRotationDir.normalized) * Quaternion.Euler(90, 0, 0);
        //_playerBody[6].rotation = Quaternion.LookRotation(_NeckDir.normalized) * Quaternion.Euler(45, 0, 0);
    }
    */

    private float ClampAroundAngle(float angle, float center, float maxDifference)
    {
        float difference = Mathf.DeltaAngle(center, angle);

        difference = Mathf.Clamp(
            difference,
            -maxDifference,
            maxDifference
        );

        return center + difference;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

}
