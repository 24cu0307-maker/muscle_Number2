/*━━━━━━━━━*
*@file EffectDebugKeySettings.cs*
*@brief EffectSystem関連のDebug表示キーを一括管理する*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks Inspectorから各Debug表示キーを変更可能*
*━━━━━━━━━*/

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

    [SerializeField] private KeyCode m_liveEffectToggleKey =
        KeyCode.F7; //LiveEffect確認Panel表示切替Key
    [SerializeField] private KeyCode m_voltageToggleKey =
        KeyCode.F8; //Voltage Debug Panel表示切替Key
    [SerializeField] private KeyCode m_exitDebugKey =
        KeyCode.Escape; //Debug再生終了Key

    public KeyCode LiveEffectToggleKey
    {
        get
        {
            return m_liveEffectToggleKey;
        }
    }

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
