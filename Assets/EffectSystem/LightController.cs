/*━━━━━━━━━
@file LightController.cs
@brief ライト点灯・回転・移動・色変更を制御
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks EffectSystemからIlluminationを呼んで使用
━━━━━━━━━*/

using UnityEngine;
using UnityEngine.Serialization;

public enum LightMoveMode
{
    OneWay,         //開始値から目標値まで一度だけ移動する
    ReturnOnce,     //開始値から目標値まで移動し、その後開始値へ戻る
    PingPongLoop    //点灯中、開始値と目標値の間を指定回数だけ往復する
}

public class LightController : MonoBehaviour
{
    private const float DefaultMotionTime = 1.0f;       //移動や回転にかける初期時間
    private const float MinimumMotionTime = 0.01f;      //0除算を防ぐための最短時間
    private const float DefaultStopTime = 1.0f;         //ライトを点灯しておく初期時間
    private const float MinimumStopTime = 0.0f;         //点灯時間の下限
    private const int DefaultPingPongCount = 1;         //往復回数の初期値
    private const int MinimumPingPongCount = 1;         //往復回数の下限
    private const int DefaultBlinkCount = 3;            //明滅回数の初期値
    private const int MinimumBlinkCount = 1;            //明滅回数の下限
    private const float DefaultBlinkTime = 1.0f;        //明滅全体にかける初期時間
    private const float MinimumBlinkTime = 0.01f;       //明滅間隔の0除算を防ぐための最短時間
    private const float RoundTripMultiplier = 2.0f;     //片道進行率を往復進行率に変換するための値
    private const float HalfProgress = 0.5f;            //往路と復路の境目
    private const float ProgressMin = 0.0f;             //補間開始値
    private const float ProgressMax = 1.0f;             //補間終了値
    private const int EmptyColorCount = 0;              //色配列が空かを判定するための値
    private const int FirstColorIndex = 0;              //色配列の先頭番号
    private const int BlinkStepMultiplier = 2;          //1回の明滅をOFFとONの2段階として扱うための値

    [FormerlySerializedAs("l_mlight")]
    [SerializeField] private Light m_light;                         //点灯・消灯・色変更を行うライト
    [FormerlySerializedAs("f_mStopTime")]
    [SerializeField, Min(MinimumStopTime)] private float m_stopTime = DefaultStopTime; //ライトを点灯しておく秒数

    [Header("Rotation")]
    [FormerlySerializedAs("m_angleRange")]
    [SerializeField] private Vector3 m_rotationRange = Vector3.zero; //開始時の角度に加算する回転角度
    [FormerlySerializedAs("m_moveTime")]
    [SerializeField, Min(MinimumMotionTime)] private float m_rotationTime = DefaultMotionTime; //開始角度から目標角度まで回転する秒数
    [FormerlySerializedAs("m_moveMode")]
    [SerializeField] private LightMoveMode m_rotationMode = LightMoveMode.OneWay; //回転の動き方
    [SerializeField, Min(MinimumPingPongCount)] private int m_rotationPingPongCount = DefaultPingPongCount; //回転の往復回数
    [SerializeField] private bool m_bUseLocalRotation = true;        //ONならlocalRotation、OFFならrotationを動かす
    [SerializeField] private bool m_bReturnStartRotationOnStop = false; //消灯時に開始角度へ戻すか

    [Header("Position")]
    [SerializeField] private Vector3 m_positionRange = Vector3.zero; //開始時の位置に加算する移動量
    [SerializeField, Min(MinimumMotionTime)] private float m_positionTime = DefaultMotionTime; //開始位置から目標位置まで移動する秒数
    [SerializeField] private LightMoveMode m_positionMode = LightMoveMode.OneWay; //移動の動き方
    [SerializeField, Min(MinimumPingPongCount)] private int m_positionPingPongCount = DefaultPingPongCount; //移動の往復回数
    [SerializeField] private bool m_bUseLocalPosition = true;        //ONならlocalPosition、OFFならpositionを動かす
    [SerializeField] private bool m_bReturnStartPositionOnStop = false; //消灯時に開始位置へ戻すか

    [Header("Color")]
    [SerializeField] private Color[] m_colors;                       //点灯時間を等分して順番に切り替えるライト色
    [SerializeField] private bool m_bReturnStartColorOnStop = false; //消灯時に開始色へ戻すか

    [Header("Blink")]
    [SerializeField] private bool m_bUseBlink = false;                //ONなら点灯開始時にライトを明滅させる
    [SerializeField, Min(MinimumBlinkCount)] private int m_blinkCount = DefaultBlinkCount; //明滅する回数
    [SerializeField, Min(MinimumBlinkTime)] private float m_blinkTime = DefaultBlinkTime; //明滅全体にかける秒数

    private float m_stopTimer;               //点灯してからの経過時間
    private float m_rotationTimer;           //回転に使う経過時間
    private float m_positionTimer;           //移動に使う経過時間
    private float m_blinkTimer;              //明滅に使う経過時間
    private bool m_bIlluminating;            //現在ライト演出中か
    private bool m_bBlinking;                //現在明滅中か
    private Quaternion m_startRotation;      //Illumination開始時の角度
    private Quaternion m_targetRotation;     //開始角度にm_rotationRangeを足した目標角度
    private Vector3 m_startPosition;         //Illumination開始時の位置
    private Vector3 m_targetPosition;        //開始位置にm_positionRangeを足した目標位置
    private Color m_startColor;              //Illumination開始時のライト色

    private void Awake()
    {
        //InspectorでLightが未設定の場合、同じGameObject上のLightを自動取得する。
        if (m_light == null)
        {
            m_light = GetComponent<Light>();
        }

        SetLightEnabled(false);
    }

    private void Update()
    {
        if (!m_bIlluminating) { return; }

        m_stopTimer += Time.deltaTime;
        m_rotationTimer += Time.deltaTime;
        m_positionTimer += Time.deltaTime;
        m_blinkTimer += Time.deltaTime;

        if (m_stopTimer >= m_stopTime)
        {
            StopIlluminate();
            return;
        }

        UpdateRotation();
        UpdatePosition();
        UpdateColor();
        UpdateBlink();
    }

    public void Illumination()
    {
        //EffectSystemから呼ばれる入口。
        //呼ばれた瞬間の角度・位置・色を開始値として保存し、設定された範囲分だけ動かす。
        m_startRotation = GetCurrentRotation();
        m_targetRotation = m_startRotation * Quaternion.Euler(m_rotationRange);
        m_startPosition = GetCurrentPosition();
        m_targetPosition = m_startPosition + m_positionRange;
        m_startColor = m_light == null ? Color.white : m_light.color;
        m_stopTimer = ProgressMin;
        m_rotationTimer = ProgressMin;
        m_positionTimer = ProgressMin;
        m_blinkTimer = ProgressMin;
        m_bIlluminating = true;
        m_bBlinking = m_bUseBlink;

        SetLightEnabled(true);
        SetCurrentRotation(m_startRotation);
        SetCurrentPosition(m_startPosition);
        ApplyColor(FirstColorIndex);
    }

    private void StopIlluminate()
    {
        //点灯時間が終わったらライトを消す。
        //必要なら角度・位置・色をIllumination開始時へ戻す。
        SetLightEnabled(false);
        m_bIlluminating = false;
        m_stopTimer = ProgressMin;
        m_rotationTimer = ProgressMin;
        m_positionTimer = ProgressMin;
        m_blinkTimer = ProgressMin;
        m_bBlinking = false;

        if (m_bReturnStartRotationOnStop)
        {
            SetCurrentRotation(m_startRotation);
        }

        if (m_bReturnStartPositionOnStop)
        {
            SetCurrentPosition(m_startPosition);
        }

        if (m_bReturnStartColorOnStop)
        {
            SetLightColor(m_startColor);
        }
    }

    private void UpdateRotation()
    {
        //回転設定に応じて現在角度を更新する。
        float progress = GetMotionProgress(
            m_rotationTimer,
            m_rotationTime,
            m_rotationMode,
            m_rotationPingPongCount); //0から1の角度補間率

        SetCurrentRotation(Quaternion.Slerp(m_startRotation, m_targetRotation, progress));
    }

    private void UpdatePosition()
    {
        //移動設定に応じて現在位置を更新する。
        float progress = GetMotionProgress(
            m_positionTimer,
            m_positionTime,
            m_positionMode,
            m_positionPingPongCount); //0から1の位置補間率

        SetCurrentPosition(Vector3.Lerp(m_startPosition, m_targetPosition, progress));
    }

    private void UpdateColor()
    {
        //ライト色配列が設定されていない場合は何もしない。
        if (m_colors == null || m_colors.Length == EmptyColorCount) { return; }

        float colorChangeInterval = m_stopTime / m_colors.Length; //1色あたりの表示時間

        if (colorChangeInterval <= ProgressMin)
        {
            ApplyColor(FirstColorIndex);
            return;
        }

        int colorIndex = Mathf.FloorToInt(m_stopTimer / colorChangeInterval); //現在表示する色番号
        colorIndex = Mathf.Clamp(colorIndex, FirstColorIndex, m_colors.Length - 1);
        ApplyColor(colorIndex);
    }

    private void UpdateBlink()
    {
        //明滅設定がOFF、または明滅が終了している場合は通常点灯に戻す。
        if (!m_bUseBlink || !m_bBlinking)
        {
            SetLightEnabled(true);
            return;
        }

        int blinkStepCount = Mathf.Max(MinimumBlinkCount, m_blinkCount) * BlinkStepMultiplier; //OFF/ONを含めた明滅段階数
        float blinkTime = Mathf.Max(MinimumBlinkTime, m_blinkTime);                            //安全な明滅時間
        float blinkInterval = blinkTime / blinkStepCount;                                      //1段階あたりの時間

        if (m_blinkTimer >= blinkTime)
        {
            //明滅が終わったら通常点灯へ戻す。
            m_bBlinking = false;
            SetLightEnabled(true);
            return;
        }

        int blinkStep = Mathf.FloorToInt(m_blinkTimer / blinkInterval); //現在の明滅段階
        bool lightEnabled = blinkStep % BlinkStepMultiplier != 0;       //偶数段階はOFF、奇数段階はON
        SetLightEnabled(lightEnabled);
    }

    private float GetMotionProgress(
        float _timer,
        float _motiontime,
        LightMoveMode _motionmode,
        int _pingpongcount)
    {
        //動き方に応じて、開始値から目標値までの補間率を作る。
        float motionTime = Mathf.Max(MinimumMotionTime, _motiontime); //安全な移動時間
        int pingPongCount = Mathf.Max(MinimumPingPongCount, _pingpongcount); //安全な往復回数

        if (_motionmode == LightMoveMode.OneWay)
        {
            return Mathf.Clamp01(_timer / motionTime);
        }

        if (_motionmode == LightMoveMode.ReturnOnce)
        {
            return GetReturnProgress(_timer, motionTime);
        }

        return GetLimitedPingPongProgress(_timer, motionTime, pingPongCount);
    }

    private float GetReturnProgress(
        float _timer,
        float _motiontime)
    {
        //開始値→目標値→開始値へ戻る補間率を作る。
        float roundTripProgress = Mathf.Clamp01(_timer / (_motiontime * RoundTripMultiplier)); //往復全体の進行率
        return roundTripProgress <= HalfProgress
            ? roundTripProgress * RoundTripMultiplier
            : (ProgressMax - roundTripProgress) * RoundTripMultiplier;
    }

    private float GetLimitedPingPongProgress(
        float _timer,
        float _motiontime,
        int _pingpongcount)
    {
        //指定回数だけ往復し、終了後は開始値で止める。
        float totalTime = _motiontime * RoundTripMultiplier * _pingpongcount; //指定往復回数に必要な合計時間

        if (_timer >= totalTime)
        {
            return ProgressMin;
        }

        return Mathf.PingPong(_timer / _motiontime, ProgressMax);
    }

    private Quaternion GetCurrentRotation()
    {
        //設定に応じてローカル角度またはワールド角度を取得する。
        return m_bUseLocalRotation ? transform.localRotation : transform.rotation;
    }

    private void SetCurrentRotation(Quaternion _rotation)
    {
        //設定に応じてローカル角度またはワールド角度を更新する。
        if (m_bUseLocalRotation)
        {
            transform.localRotation = _rotation;
        }
        else
        {
            transform.rotation = _rotation;
        }
    }

    private Vector3 GetCurrentPosition()
    {
        //設定に応じてローカル位置またはワールド位置を取得する。
        return m_bUseLocalPosition ? transform.localPosition : transform.position;
    }

    private void SetCurrentPosition(Vector3 _position)
    {
        //設定に応じてローカル位置またはワールド位置を更新する。
        if (m_bUseLocalPosition)
        {
            transform.localPosition = _position;
        }
        else
        {
            transform.position = _position;
        }
    }

    private void ApplyColor(int _colorindex)
    {
        //指定された色番号の色をLightへ反映する。
        if (m_colors == null || m_colors.Length == EmptyColorCount) { return; }

        int colorIndex = Mathf.Clamp(_colorindex, FirstColorIndex, m_colors.Length - 1); //安全な色番号
        SetLightColor(m_colors[colorIndex]);
    }

    private void SetLightColor(Color _color)
    {
        //Lightが設定されている場合だけ色を変更する。
        if (m_light == null) { return; }

        m_light.color = _color;
    }

    private void SetLightEnabled(bool _benabled)
    {
        //Lightが設定されている場合だけ点灯状態を変更する。
        if (m_light == null) { return; }

        m_light.enabled = _benabled;
    }
}
