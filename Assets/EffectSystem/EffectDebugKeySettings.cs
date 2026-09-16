/*━━━━━━━━━*
*@file EffectDebugKeySettings.cs*
*@brief Gameplayで使用するDebugキーとDebug操作を一括管理する*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/09/16*
*@remarks InspectorからすべてのDebugキーを変更可能*
*━━━━━━━━━*/

using GameFlowTemplate;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// EffectSystem関連のDebug表示に使用するキー設定を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class EffectDebugKeySettings : MonoBehaviour
{
    private bool b_m_resultRequested; //F10によるResult遷移の多重実行防止
    private bool b_m_restartRequested; //Restartの多重実行防止

    public static bool ForceAllSuccess { get; private set; } //全成功Debug状態

    [SerializeField] private Key m_voltageToggleInputKey =
        Key.F8; //Voltage Debug Panel表示切替Key
    [SerializeField] private Key m_exitDebugInputKey =
        Key.F10; //Debug再生終了Key
    [SerializeField] private Key m_restartInputKey =
        Key.F5; //現在のGameplayを最初から再読込するKey
    [SerializeField] private Key m_forceSuccessToggleInputKey =
        Key.F6; //全判定成功の切替Key
    [SerializeField] private Key m_cameraRetargetInputKey =
        Key.F9; //Camera注視対象を再設定するKey

    public Key VoltageToggleKey
    {
        get
        {
            return m_voltageToggleInputKey;
        }
    }

    public Key ExitDebugKey
    {
        get
        {
            return m_exitDebugInputKey;
        }
    }

    public Key RestartKey => m_restartInputKey;
    public Key ForceSuccessToggleKey => m_forceSuccessToggleInputKey;
    public Key CameraRetargetKey => m_cameraRetargetInputKey;

    /// <summary>
    /// Debug表示Componentの有効状態に関係なく、Result遷移キーを監視します。
    /// </summary>
    private void Update()
    {
        if (!b_m_restartRequested && IsKeyDown(m_restartInputKey))
        {
            RestartCurrentScene();
            return;
        }

        if (IsKeyDown(m_forceSuccessToggleInputKey))
        {
            ForceAllSuccess = !ForceAllSuccess;
            string forceSuccessState = "OFF";
            if (ForceAllSuccess)
            {
                forceSuccessState = "ON";
            }

            Debug.Log($"[DebugKey] 全成功判定: {forceSuccessState}", this);
        }

        if (IsKeyDown(m_cameraRetargetInputKey))
        {
            RetargetCamera();
        }

        if (!b_m_resultRequested && IsKeyDown(m_exitDebugInputKey))
        {
            MoveToResult();
        }
    }

    private void RestartCurrentScene()
    {
        b_m_restartRequested = true;
        ForceAllSuccess = false;
        UnityEngine.SceneManagement.Scene activeScene =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(activeScene.name);
    }

    private void RetargetCamera()
    {
        PoseCameraDirector cameraDirector = FindFirstObjectByType<PoseCameraDirector>();
        if (cameraDirector == null)
        {
            Debug.LogWarning(
                "[DebugKey] PoseCameraDirectorが見つからないため再ターゲットできません。",
                this);
            return;
        }

        cameraDirector.RetargetCurrentFocus();
        Debug.Log("[DebugKey] カメラのフォーカス対象を再設定しました。", this);
    }

    private void MoveToResult()
    {

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning(
                "[EffectDebugKeySettings] GameManagerが見つからないためResultへ遷移できません。",
                this);
            return;
        }

        b_m_resultRequested = true;
        gameManager.FinishGame();
    }

    /// <summary>
    /// シーン内の設定を取得し、存在しなければ指定Objectへ追加します。
    /// </summary>
    public static EffectDebugKeySettings GetOrCreate(GameObject _owner)
    {
        EffectDebugKeySettings settings =
            FindFirstObjectByType<EffectDebugKeySettings>(); //現在の共通Key設定
        if (settings != null)return settings;
        if (_owner == null)return null;

        return _owner.AddComponent<EffectDebugKeySettings>();
    }

    /// <summary>
    /// 現在のInput方式に合わせて指定キーの押下を判定します。
    /// </summary>
    public static bool IsKeyDown(Key _inputSystemKey)
    {
        Keyboard keyboard = Keyboard.current; //現在接続中のKeyboard
        if (keyboard == null)return false;

        KeyControl keyControl = keyboard[_inputSystemKey];
        return keyControl != null && keyControl.wasPressedThisFrame;
    }
}
