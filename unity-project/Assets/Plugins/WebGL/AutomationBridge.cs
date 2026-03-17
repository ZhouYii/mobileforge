using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// C# bridge to the JavaScript automation layer.
/// Exposes game state (current screen, button positions) so Playwright can query it.
/// Only active in WebGL builds; stubs on other platforms.
/// </summary>
public static class AutomationBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void JS_NotifyScreenChanged(string name);
    [DllImport("__Internal")] private static extern void JS_RegisterButton(string id, float x, float y, float w, float h);
    [DllImport("__Internal")] private static extern void JS_ClearButtons();
#endif

    public static void NotifyScreenChanged(string screenName)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JS_NotifyScreenChanged(screenName);
#else
        Debug.Log($"[AutomationBridge] Screen changed: {screenName}");
#endif
    }

    public static void RegisterButton(string id, float x, float y, float w, float h)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JS_RegisterButton(id, x, y, w, h);
#endif
    }

    public static void ClearButtons()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JS_ClearButtons();
#endif
    }
}
