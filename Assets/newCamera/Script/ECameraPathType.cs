/*━━━━━━━━━
@file        ECameraPathType.cs
@brief       Shot内で使用するカメラ移動経路の種類
@author      24CU0139 ラヤマジ プラシャント
@date 作成日 2026/07/22
最終更新日   2026/07/22
@remarks     CameraShotPresetの移動方法を選択します。
━━━━━━━━━*/

//Shot内で使用するカメラ移動経路
public enum ECameraPathType
{
    OrbitByAngleRadius, //角度と半径を指定する従来の円軌道
    PointToPoint,      //指定したPoint AからPoint Bへの直線移動
    OrbitPointToPoint  //Point AとPoint Bを通る円弧または渦巻き移動
}
