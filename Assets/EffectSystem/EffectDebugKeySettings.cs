/*━━━━━━━━━*
*@file EffectDebugKeySettings.cs*
*@brief Gameplayで使用するDebugキーとDebug操作を一括管理する*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks InspectorからすべてのDebugキーを変更可能*
*━━━━━━━━━*/

using GameFlowTemplate;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

/// <summary>
/// EffectSystem関連のDebug表示に使用するキー設定を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class EffectDebugKeySettings : MonoBehaviour
{
    private const string EAlphaPrefix = "Alpha"; //数字KeyCodeの接頭辞
    private bool b_m_resultRequested; //F10によるResult遷移の多重実行防止
    private bool b_m_restartRequested; //Restartの多重実行防止

    public static bool ForceAllSuccess { get; private set; } //全成功Debug状態

    [SerializeField] private KeyCode m_voltageToggleKey =
        KeyCode.F8; //Voltage Debug Panel表示切替Key
    [SerializeField] private KeyCode m_exitDebugKey =
        KeyCode.F10; //Debug再生終了Key
    [SerializeField] private KeyCode m_restartKey =
        KeyCode.F5; //現在のGameplayを最初から再読込するKey
    [SerializeField] private KeyCode m_forceSuccessToggleKey =
        KeyCode.F6; //全判定成功の切替Key
    [SerializeField] private KeyCode m_cameraRetargetKey =
        KeyCode.F9; //Camera注視対象を再設定するKey

    public KeyCode VoltageToggleKey
    {
        get
        {
            return m_voltageToggleKey;
        }
    }

    public KeyCode ExitDebugKey
    {
        get
        {
            return m_exitDebugKey;
        }
    }

    public KeyCode RestartKey => m_restartKey;
    public KeyCode ForceSuccessToggleKey => m_forceSuccessToggleKey;
    public KeyCode CameraRetargetKey => m_cameraRetargetKey;

    /// <summary>
    /// Debug表示Componentの有効状態に関係なく、Result遷移キーを監視します。
    /// </summary>
    private void Update()
    {
        if (!b_m_restartRequested && IsKeyDown(m_restartKey))
        {
            RestartCurrentScene();
            return;
        }

        if (IsKeyDown(m_forceSuccessToggleKey))
        {
            ForceAllSuccess = !ForceAllSuccess;
            Debug.Log(
                $"[DebugKey] 全成功判定: {(ForceAllSuccess ? "ON" : "OFF")}",
                this);
        }

        if (IsKeyDown(m_cameraRetargetKey))
        {
            RetargetCamera();
        }

        if (!b_m_resultRequested && IsKeyDown(m_exitDebugKey))
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
    public static bool IsKeyDown(KeyCode _keycode)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current; //現在接続中のKeyboard
        if (keyboard == null)return false;

        KeyControl keyControl =
            keyboard.FindKeyOnCurrentKeyboardLayout(
                GetDisplayName(_keycode)); //指定Keyに対応するControl
        return keyControl != null && keyControl.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(_keycode);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    /// <summary>
    /// KeyCodeをInput Systemの表示名へ変換します。
    /// </summary>
    private static string GetDisplayName(KeyCode _keycode)
    {
        string keyName = _keycode.ToString(); //KeyCode名
        if (keyName.StartsWith(EAlphaPrefix))
        {
            return keyName.Substring(EAlphaPrefix.Length);
        }

        switch (_keycode)
        {
            case KeyCode.Return: return "Enter";
            case KeyCode.LeftArrow: return "Left Arrow";
            case KeyCode.RightArrow: return "Right Arrow";
            case KeyCode.UpArrow: return "Up Arrow";
            case KeyCode.DownArrow: return "Down Arrow";
            case KeyCode.LeftShift: return "Left Shift";
            case KeyCode.RightShift: return "Right Shift";
            case KeyCode.LeftControl: return "Left Ctrl";
            case KeyCode.RightControl: return "Right Ctrl";
            default: return keyName;
        }
    }
#endif
}
