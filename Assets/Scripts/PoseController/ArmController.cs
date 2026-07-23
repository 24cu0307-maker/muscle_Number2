using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>
///腕のコントロール
///</summary>

public class ArmController : MonoBehaviour
{
    //データを保存領域
    //[SerializeField] private PositionDataManager dataManager;

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

    public void Awake()
    {
        //カメラ
        cameraJudge = new CameraRelativeMovement();

        

        //方向計算クラス
        _DirectionVectorCalculator = new DirectionVectorCalculator();


    }



    void Update()
    {
        if (!PositionDataManager.Instance) return;

        //座標を取得
        _Body = PositionDataManager.Instance.positionData.Body;




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


    }
}
