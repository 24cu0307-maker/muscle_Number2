using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>
///体のコントロール
///</summary>

public class BodyController : MonoBehaviour
{
    //データを保存領域
    //[SerializeField] private PositionDataManager dataManager;

    private HumanoidSkeleton skeleton;

    private Quaternion[] initialSplineRotation = new Quaternion[4];

    //座標を格納する箱
    [SerializeField] private Vector3[] _Body = new Vector3[37];


    //人体のオブジェクトの情報
    [SerializeField] public HumanoidSkeleton playerBody;



    [SerializeField] public CameraRelativeMovement cameraJudge;
    [SerializeField] public GameObject _camera;

    [SerializeField] private Vector3[] _BodyDir = new Vector3[5];

    [SerializeField] private Vector3[] _BodyDirMirrored = new Vector3[5];


    //方向計算するクラス
    DirectionVectorCalculator _DirectionVectorCalculator;

    // Start is called before the first frame update
    public void Awake()
    {
        //カメラ
        cameraJudge = new CameraRelativeMovement();

        _camera = GameObject.Find("Camera");


        //方向計算クラス
        _DirectionVectorCalculator = new DirectionVectorCalculator();

        skeleton = GameObject.Find("HumanoidSkeleton").GetComponent<HumanoidSkeleton>();

        initialSplineRotation = skeleton.GetInitialSplineRotation();
    }

    // Update is called once per frame
    void Update()
    {
        if (!PositionDataManager.Instance) return;

        //座標を取得
        _Body = PositionDataManager.Instance.positionData.Body;

        if (_camera == null)
        {
            _camera = GameObject.Find("Camera");

        }



        //方向
        _BodyDir[0] = _DirectionVectorCalculator.Vector(_Body[(int)MediapipeBodyPart.middle_spine], _Body[(int)MediapipeBodyPart.lower_spine]);
        _BodyDir[1] = _DirectionVectorCalculator.Vector(_Body[(int)MediapipeBodyPart.upper_spine], _Body[(int)MediapipeBodyPart.middle_spine]);
        _BodyDir[2] = _DirectionVectorCalculator.Vector(_Body[(int)MediapipeBodyPart.neck], _Body[(int)MediapipeBodyPart.upper_spine]);
        _BodyDir[3] = _DirectionVectorCalculator.Vector(_Body[0], _Body[(int)MediapipeBodyPart.neck]);

        _BodyDir[4] = _DirectionVectorCalculator.Vector(_Body[(int)MediapipeBodyPart.right_shoulder], _Body[(int)MediapipeBodyPart.left_shoulder]);



        //方向
        _BodyDirMirrored[0] = _DirectionVectorCalculator.MirroredVector_X(_Body[(int)MediapipeBodyPart.middle_spine], _Body[(int)MediapipeBodyPart.lower_spine]);
        _BodyDirMirrored[1] = _DirectionVectorCalculator.MirroredVector_X(_Body[(int)MediapipeBodyPart.upper_spine], _Body[(int)MediapipeBodyPart.middle_spine]);
        _BodyDirMirrored[2] = _DirectionVectorCalculator.MirroredVector_X(_Body[(int)MediapipeBodyPart.neck], _Body[(int)MediapipeBodyPart.upper_spine]);
        _BodyDirMirrored[3] = _DirectionVectorCalculator.MirroredVector_X(_Body[0], _Body[(int)MediapipeBodyPart.neck]);

        _BodyDirMirrored[4] = _DirectionVectorCalculator.MirroredVector_X(_Body[(int)MediapipeBodyPart.right_shoulder], _Body[(int)MediapipeBodyPart.left_shoulder]);




        Quaternion[] corrections =
{
    Quaternion.Euler(70, 0, 0),
    Quaternion.Euler(70, 0, 0),
    Quaternion.Euler(70, 0, 0),
    Quaternion.Euler(45, 0, 0)
};

        
        for (int i = 0; i < 4; i++)
        {
            Quaternion targetRotation =
      Quaternion.LookRotation(_BodyDirMirrored[i])
      * corrections[i];


            Vector3 angle = playerBody.playerSpline[i].eulerAngles;

            Debug.Log($"Spline[{i}] Angle = {angle}");


            playerBody.playerSpline[i].rotation = targetRotation;
        }
        

      


        /*
        //各ボーンにベクトル方向を与える　※基本補正値Quaternion.Euler(70, 0, 0)
        playerBody.playerSpline[0].rotation = Quaternion.LookRotation(_BodyDirMirrored[0]) * Quaternion.Euler(70, 0, 0);
        playerBody.playerSpline[1].rotation = Quaternion.LookRotation(_BodyDirMirrored[1]) * Quaternion.Euler(70, 0, 0);
        playerBody.playerSpline[2].rotation = Quaternion.LookRotation(_BodyDirMirrored[2]) * Quaternion.Euler(70, 0, 0);
        playerBody.playerSpline[3].rotation = Quaternion.LookRotation(_BodyDirMirrored[3]) * Quaternion.Euler(45, 0, 0);

        playerBody.playerSpline[1].rotation = Quaternion.LookRotation(_BodyDirMirrored[4]) * Quaternion.Euler(0, 90, 0);
        playerBody.playerSpline[2].rotation = Quaternion.LookRotation(_BodyDirMirrored[4]) * Quaternion.Euler(0, 90, 0);
        */

        /*
        //カメラの向きに応じて方向を変更
        if (!(cameraJudge.CameraDirection(_camera)))
        {
            //各ボーンにベクトル方向を与える　※基本補正値Quaternion.Euler(70, 0, 0)
            playerBody.playerSpline[0].rotation = Quaternion.LookRotation(_BodyDirMirrored[0]) * Quaternion.Euler(70, 0, 0);
            playerBody.playerSpline[1].rotation = Quaternion.LookRotation(_BodyDirMirrored[1]) * Quaternion.Euler(70, 0, 0);
            playerBody.playerSpline[2].rotation = Quaternion.LookRotation(_BodyDirMirrored[2]) * Quaternion.Euler(70, 0, 0);
            playerBody.playerSpline[3].rotation = Quaternion.LookRotation(_BodyDirMirrored[3]) * Quaternion.Euler(45, 0, 0);

            playerBody.playerSpline[1].rotation = Quaternion.LookRotation(_BodyDirMirrored[4]) * Quaternion.Euler(0, 90, 0);
            playerBody.playerSpline[2].rotation = Quaternion.LookRotation(_BodyDirMirrored[4]) * Quaternion.Euler(0, 90, 0);

        }
        else

        {
            //各ボーンにベクトル方向を与える　※基本補正値Quaternion.Euler(70, 0, 0)
            playerBody.playerSpline[0].rotation = Quaternion.LookRotation(_BodyDir[0]) * Quaternion.Euler(70, 0, 0);
            playerBody.playerSpline[1].rotation = Quaternion.LookRotation(_BodyDir[1]) * Quaternion.Euler(70, 0, 0);
            playerBody.playerSpline[2].rotation = Quaternion.LookRotation(_BodyDir[2]) * Quaternion.Euler(70, 0, 0);
            playerBody.playerSpline[3].rotation = Quaternion.LookRotation(_BodyDir[3]) * Quaternion.Euler(45, 0, 0);

            playerBody.playerSpline[1].rotation = Quaternion.LookRotation(_BodyDir[4]) * Quaternion.Euler(0, 90, 0);
            playerBody.playerSpline[2].rotation = Quaternion.LookRotation(_BodyDir[4]) * Quaternion.Euler(0, 90, 0);

        }
        */
    }
}
