using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionData
{
    [SerializeField] private Vector3[] _BodyBetweenSide = new Vector3[6];


    ///<summary>
    ///体の座標を格納する箱
    ///</summary>
    public Vector3[] Body { get; private set; } = new Vector3[37];

    ///<summary>
    ///顔の座標を格納する箱
    ///</summary>
    public Vector3[] Face { get; private set; } = new Vector3[478];

    ///<summary>
    ///左手の座標を格納する箱
    ///</summary>
    public Vector3[] LeftHand { get; private set; } = new Vector3[21];

    ///<summary>
    ///右手の座標を格納する箱
    ///</summary>
    public Vector3[] RightHand { get; private set; } = new Vector3[21];

    

    ///<summary>
    ///体の座標を保存
    ///のちのち計算を分ける
    ///</summary>
    public void SetBodyPosition(Vector3[] body)
    {
        Body = body;

        //mediapipeから取得した座標に追加で計算し座標を保存
        _BodyBetweenSide[0] = ((Body[12] + Body[24]) / 4);
        _BodyBetweenSide[1] = ((Body[11] + Body[23]) / 4);

        _BodyBetweenSide[2] = ((Body[12] + Body[24]) / 4) * 2;
        _BodyBetweenSide[3] = ((Body[11] + Body[23]) / 4) * 2;

        _BodyBetweenSide[4] = ((Body[12] + Body[24]) / 4) * 3;
        _BodyBetweenSide[5] = ((Body[11] + Body[23]) / 4) * 3;


        //中央
        Body[33] = (_BodyBetweenSide[0] + _BodyBetweenSide[1]) / 2;
        Body[34] = (_BodyBetweenSide[2] + _BodyBetweenSide[3]) / 2;
        Body[35] = (_BodyBetweenSide[4] + _BodyBetweenSide[5]) / 2;
        Body[36] = ((Body[11] + Body[12]) / 2);


    }

    ///<summary>
    ///顔の座標を保存
    ///</summary>
    public void SetFacePosition(Vector3[] face)
    {
        Face = face;
    }

    ///<summary>
    ///左手の座標を保存
    ///</summary>
    public void SetLeftHandPosition(Vector3[] leftHand)
    {
        LeftHand = leftHand;
    }

    ///<summary>
    ///右手の座標を保存
    ///</summary>
    public void SetRightHandPosition(Vector3[] rightHand)
    {
        RightHand = rightHand;
    }
}
