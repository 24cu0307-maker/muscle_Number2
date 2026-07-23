/*━━━━━━━━━
@file        SCameraState.cs
@brief       カメラの角度、距離、FOVなどをまとめた実行時の状態。
@author      24CU0139 ラヤマジ プラシャント
@date        作成日 2026/07/21
最終更新日   2026/07/23
@remarks     Shot間の最短補間とShot内の円軌道補間を管理する。
━━━━━━━━━*/
using UnityEngine;

//カメラの角度、距離、FOVなどをまとめた構造体
internal readonly struct SCameraState
{
    public readonly float m_yaw;         //横角度
    public readonly float m_pitch;       //縦角度
    public readonly float m_radius;      //円軌道半径
    public readonly float m_height;      //高さ
    public readonly float m_side;        //横ずらし
    public readonly float m_fieldOfView; //FOV
    public readonly float m_roll;        //カメラの前後軸を中心にしたロール角
    public readonly ECameraPathType m_pathType; //移動経路
    public readonly Vector3 m_position;  //注視点を基準にした指定位置
    public readonly float m_orbitAngleOffset; //2点間円軌道の追加回転角

    //カメラ状態を初期化
    public SCameraState(float _yaw, float _pitch, float _radius, float _height, float _side, float _fieldOfView, float _roll,
        ECameraPathType _pathType, Vector3 _position, float _orbitAngleOffset)
    {
        m_yaw = _yaw;
        m_pitch = _pitch;
        m_radius = _radius;
        m_height = _height;
        m_side = _side;
        m_fieldOfView = _fieldOfView;
        m_roll = _roll;
        m_pathType = _pathType;
        m_position = _position;
        m_orbitAngleOffset = _orbitAngleOffset;
    }

    //Front Referenceを基準にしたカメラ位置を返す
    public Vector3 GetLocalPosition()
    {
        if (m_pathType == ECameraPathType.OrbitPointToPoint)
        {
            return GetOrbitPointPosition(m_position, m_radius); //Pointの方向・高さとRadiusから開始／終了位置を作る
        }

        if (m_pathType == ECameraPathType.PointToPoint) return m_position;

        Vector3 orbitDirection = Quaternion.Euler(-m_pitch, m_yaw, 0.0f) * Vector3.forward; //円軌道上の方向
        return orbitDirection * m_radius + Vector3.up * m_height + Vector3.right * m_side;
    }

    //Shot内で、選択した経路を保って2つのカメラ状態を補間
    public static SCameraState LerpOrbit(SCameraState _from, SCameraState _to, float _rate)
    {
        Vector3 position = _to.m_pathType == ECameraPathType.OrbitPointToPoint
            ? LerpOrbitPoints(_from, _to, _rate)
            : Vector3.LerpUnclamped(_from.m_position, _to.m_position, _rate); //直接指定したPoint AとPoint B
        return new SCameraState(Mathf.LerpUnclamped(_from.m_yaw, _to.m_yaw, _rate), Mathf.LerpUnclamped(_from.m_pitch, _to.m_pitch, _rate),
            Mathf.LerpUnclamped(_from.m_radius, _to.m_radius, _rate), Mathf.LerpUnclamped(_from.m_height, _to.m_height, _rate), Mathf.LerpUnclamped(_from.m_side, _to.m_side, _rate),
            Mathf.LerpUnclamped(_from.m_fieldOfView, _to.m_fieldOfView, _rate), Mathf.LerpUnclamped(_from.m_roll, _to.m_roll, _rate),
            _to.m_pathType, position, _to.m_orbitAngleOffset);
    }

    //Point AとPoint Bを必ず通るように、水平角度と半径を補間
    private static Vector3 LerpOrbitPoints(SCameraState _from, SCameraState _to, float _rate)
    {
        float fromAngle = Mathf.Atan2(_from.m_position.x, _from.m_position.z) * Mathf.Rad2Deg; //Point Aの水平角度
        float toAngle = Mathf.Atan2(_to.m_position.x, _to.m_position.z) * Mathf.Rad2Deg;       //Point Bの水平角度
        float angle = fromAngle + (Mathf.DeltaAngle(fromAngle, toAngle) + _to.m_orbitAngleOffset) * _rate; //補間中の角度
        float distance = Mathf.LerpUnclamped(_from.m_radius, _to.m_radius, _rate); //Targetからカメラまでの距離
        float radian = angle * Mathf.Deg2Rad; //三角関数で使用する角度
        Vector3 point = new Vector3(Mathf.Sin(radian), Mathf.LerpUnclamped(_from.m_position.y, _to.m_position.y, _rate), Mathf.Cos(radian)); //補間中の方向と高さ

        return GetOrbitPointPosition(point, distance);
    }

    //PointのX/Zを方向、Yを高さとして、Targetから指定距離のカメラ位置を返す
    private static Vector3 GetOrbitPointPosition(Vector3 _point, float _distance)
    {
        float height = Mathf.Clamp(_point.y, -_distance, _distance); //距離を超える高さは真上／真下までに制限
        float horizontalRadius = Mathf.Sqrt(Mathf.Max(0.0f, _distance * _distance - height * height)); //高さを除いた水平半径
        Vector2 horizontalDirection = new Vector2(_point.x, _point.z); //Targetから見た水平方向

        //方向が未設定なら正面を使用し、ゼロ除算を防ぐ
        if (horizontalDirection.sqrMagnitude <= Mathf.Epsilon) horizontalDirection = Vector2.up;
        horizontalDirection.Normalize();

        return new Vector3(horizontalDirection.x * horizontalRadius, height, horizontalDirection.y * horizontalRadius);
    }

}
