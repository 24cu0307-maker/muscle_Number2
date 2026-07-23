using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
//通常クラスで管理したい
//
public class AngleCalculator : MonoBehaviour
{
    ///<summary>
    ///データを保存クラス
    ///</summary>
    [SerializeField] private PositionDataManager dataManager;

    ///<summary>
    ///角度情報保存クラス
    ///</summary>
    [SerializeField] private AngleDataManager angleDataManager;

    ///<summary>
    ///座標を格納する箱
    ///</summary>
    [SerializeField] private Vector3[] _Body = new Vector3[37];

    private void Update()
    {
        //座標データの更新
        _Body = dataManager.positionData.Body;


        //角度を取得
        //左肘
        float leftElbowAngle = Angle_Vec2(_Body[(int)MediapipeBodyPart.left_shoulder], _Body[(int)MediapipeBodyPart.left_elbow], _Body[(int)MediapipeBodyPart.left_wrist]);
        //右肘
        float RightElbowAngle = Angle_Vec2(_Body[(int)MediapipeBodyPart.right_shoulder], _Body[(int)MediapipeBodyPart.right_elbow], _Body[(int)MediapipeBodyPart.right_wrist]);

        /*
        //左手首
        float leftWristAngle = Angle_Vec2(_Body[(int)MediapipeBodyPart.left_elbow], _Body[(int)MediapipeBodyPart.left_wrist], _LeftHand[(int)MediapipeHandPart.middle_finger_mcp]);
        //右手首
        float rightWristAngle = Angle_Vec2(_Body[(int)MediapipeBodyPart.right_elbow], _Body[(int)MediapipeBodyPart.right_wrist], _RightHand[(int)MediapipeHandPart.middle_finger_mcp]);
        */
        
        //左手首
        float leftWristAngle = Angle_Vec2(_Body[(int)MediapipeBodyPart.left_elbow], _Body[(int)MediapipeBodyPart.left_wrist], _Body[(int)MediapipeBodyPart.left_index]);
        //右手首
        float rightWristAngle = Angle_Vec2(_Body[(int)MediapipeBodyPart.right_elbow], _Body[(int)MediapipeBodyPart.right_wrist], _Body[(int)MediapipeBodyPart.right_index]);
        

        //左肩
        float leftShoulderAngle = Angle_Vec2(_Body[(int)MediapipeBodyPart.left_hip], _Body[(int)MediapipeBodyPart.left_shoulder], _Body[(int)MediapipeBodyPart.left_elbow]);
        //右肩
        float RightShoulderAngle = Angle_Vec2(_Body[(int)MediapipeBodyPart.right_hip], _Body[(int)MediapipeBodyPart.right_shoulder], _Body[(int)MediapipeBodyPart.right_elbow]);

        ///<summary>
        ///左腕
        ///</summary>
        angleDataManager.angleData.SetAngle(0, leftElbowAngle);
        ///<summary>
        ///左肩
        ///</summary>
        angleDataManager.angleData.SetAngle(1, leftShoulderAngle);

        ///<summary>
        ///右腕
        ///</summary>
        angleDataManager.angleData.SetAngle(2, RightElbowAngle);
        ///<summary>
        ///右肩
        ///</summary>
        angleDataManager.angleData.SetAngle(3, RightShoulderAngle);


    }

    ///<summary>
    ///内積をx,y,z座標で行う。
    ///</summary>
    public float Angle_Vec3(Vector3 pointA, Vector3 pointB, Vector3 pointC)
    {
        //Bを中心とするベクトル
        //BからAのベクトル
        //BからCベクトル
        Vector3 BA＿Vec = pointA - pointB;
        Vector3 BC＿Vec = pointC - pointB;

        //内積
        float dotProduct = (BA＿Vec.x * BC＿Vec.x) + (BA＿Vec.y * BC＿Vec.y) + (BA＿Vec.z * BC＿Vec.z);

        //BA　BC　それぞれのベクトルの長さ
        float BA_Magnitude = Mathf.Sqrt((BA＿Vec.x * BA＿Vec.x) + (BA＿Vec.y * BA＿Vec.y) + (BA＿Vec.z * BA＿Vec.z));
        float BC_Magnitude = Mathf.Sqrt((BC＿Vec.x * BC＿Vec.x) + (BC＿Vec.y * BC＿Vec.y) + (BC＿Vec.z * BC＿Vec.z));

        float magnitude = BA_Magnitude * BC_Magnitude;

        //cosθ
        float cosTheta = dotProduct / magnitude;

        //誤差対策


        //ラジアン → 度数
        float angle =
            Mathf.Acos(cosTheta) * Mathf.Rad2Deg;

        return angle;
    }

    ///<summary>
    ///内積をx,y座標で行う。
    ///</summary>
    public float Angle_Vec2(Vector3 pointA, Vector3 pointB, Vector3 pointC)
    {
        //Bを中心とするベクトル
        //BからAのベクトル
        //BからCベクトル
        Vector3 BA＿Vec = pointA - pointB;
        Vector3 BC＿Vec = pointC - pointB;

        //内積
        float dotProduct = (BA＿Vec.x * BC＿Vec.x) + (BA＿Vec.y * BC＿Vec.y);

        //BA　BC　それぞれのベクトルの長さ
        float BA_Magnitude = Mathf.Sqrt((BA＿Vec.x * BA＿Vec.x) + (BA＿Vec.y * BA＿Vec.y));
        float BC_Magnitude = Mathf.Sqrt((BC＿Vec.x * BC＿Vec.x) + (BC＿Vec.y * BC＿Vec.y));

        float magnitude = BA_Magnitude * BC_Magnitude;

        //cosθ
        float cosTheta = dotProduct / magnitude;

        //誤差対策


        //ラジアン → 度数
        float angle =
            Mathf.Acos(cosTheta) * Mathf.Rad2Deg;

        return angle;
    }
}
