/*━━━━━━━━━
@file CanvasScalerBridge.cs
@brief CanvasScaler追加処理の分離
@author 24CU0000 Name
@date 2026/07/09
最終更新日 2026/07/09
@remarks 自動生成UIの解像度対応に使用
━━━━━━━━━*/

using UnityEngine;
using UnityEngine.UI;

namespace CrossProjectRanking
{
    /// <summary>
    /// 自動生成CanvasへCanvasScalerを追加するための小さな補助クラスです。
    /// RankingDisplaySystem本体の役割をランキング表示に集中させるため分離しています。
    /// </summary>
    public static class CanvasScalerBridge
    {
        private const float ReferenceResolutionWidth = 1920.0f;  //基準解像度の横幅
        private const float ReferenceResolutionHeight = 1080.0f; //基準解像度の縦幅
        private const float MatchWidthOrHeight = 0.5f;           //横幅と縦幅のどちらにも極端に寄せないための比率

        public static void AddScaler(GameObject _canvasobject)
        {
            //Canvasに解像度対応用のCanvasScalerを付ける。
            //既存UIを使う場合はこの自動生成Canvasを使わないため、影響は確認用UIだけに限定される。
            CanvasScaler scaler = _canvasobject.AddComponent<CanvasScaler>(); //自動生成Canvas用のスケーラー
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceResolutionWidth, ReferenceResolutionHeight);
            scaler.matchWidthOrHeight = MatchWidthOrHeight;
        }
    }
}
