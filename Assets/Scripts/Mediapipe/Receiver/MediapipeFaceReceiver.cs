using Mediapipe;
using Mediapipe.Unity.Sample.Holistic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MediapipeFaceReceiver : MonoBehaviour
{

 
    //オブザーバー
    private void OnEnable()
    {
        HolisticTrackingSolution.OnFaceUpdated += OnFace;
    }

    //オブザーバー
    private void OnDisable()
    {
        HolisticTrackingSolution.OnFaceUpdated -= OnFace;
    }

    //478点のすべての座標を取得
    private void OnFace(NormalizedLandmarkList result)
    {

        //データは入っているか
        if (result == null)
            return;
        if (result.Landmark == null)
            return;
        if (result.Landmark.Count == 0)
            return;

        //座標を格納する箱
        Vector3[] Face = new Vector3[478];

        for (int i = 0; i < result.Landmark.Count; ++i)
        {

            //座標を受け取り
            NormalizedLandmark point = result.Landmark[i];

            //丸め誤差
            if (new Vector3(point.X, point.Y * -1, point.Z).magnitude <= 0.081111111f) return;

            //座標を格納
            Face[i] = new Vector3(point.X, point.Y * -1, point.Z); 

        }

        //データを保存
        PositionDataManager.Instance.positionData.SetFacePosition(Face);
    }


}
