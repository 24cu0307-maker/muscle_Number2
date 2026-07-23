/*━━━━━━━━━
@file        CameraInputUtility.cs
@brief       Legacy Input ManagerとNew Input Systemのキー入力差を吸収する。
@author      24CU0139 ラヤマジ プラシャント
@date        作成日 2026/07/21
最終更新日   2026/07/21
@remarks     InspectorのKeyCode設定を維持したまま、現在のInput方式で判定する。
━━━━━━━━━*/
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

//現在有効なInput方式に合わせてキー入力を判定
internal static class CameraInputUtility
{
    private const string E_ALPHA_PREFIX = "Alpha"; //数字キーを表すKeyCodeの接頭辞

    //キーが押された瞬間を返す
    public static bool IsKeyDown(KeyCode _keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current; //現在接続されているキーボード

        if (keyboard == null) { return false; }

        //現在のキーボード配列から表示名で検索し、A～Zなどの全列挙を不要にする
        var keyControl = keyboard.FindKeyOnCurrentKeyboardLayout(GetDisplayName(_keyCode));
        return keyControl != null && keyControl.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(_keyCode);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    //KeyCode名をInput Systemのキー表示名へ変換
    private static string GetDisplayName(KeyCode _keyCode)
    {
        string keyName = _keyCode.ToString(); //Inspectorで選択されたKeyCode名

        //Alpha0～Alpha9は、キーボード上の表示名0～9へ変換
        if (keyName.StartsWith(E_ALPHA_PREFIX)) { return keyName.Substring(E_ALPHA_PREFIX.Length); }

        //KeyCodeと表示名が異なるキーだけ変換し、それ以外は同じ名前で検索
        switch (_keyCode)
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
