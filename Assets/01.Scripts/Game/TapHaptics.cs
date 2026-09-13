using UnityEngine;

namespace _01.Scripts.Game
{
    /// <summary>Short Android system haptics, respecting both game and device settings.</summary>
    public static class TapHaptics
    {
        public const string EnabledKey = "VioletTap.Feedback.HapticsEnabled";
        public static bool IsEnabled => PlayerPrefs.GetInt(EnabledKey, 1) == 1;

        public static void SetEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(EnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void Play(TapTargetType type, bool fever = false)
        {
            if (!IsEnabled || !Application.isFocused) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                        using var current = unity.GetStatic<AndroidJavaObject>("currentActivity");
                        using var window = current.Call<AndroidJavaObject>("getWindow");
                        using var view = window.Call<AndroidJavaObject>("getDecorView");
                        using var version = new AndroidJavaClass("android.os.Build$VERSION");
                        using var constants = new AndroidJavaClass("android.view.HapticFeedbackConstants");
                        bool modern = version.GetStatic<int>("SDK_INT") >= 30;
                        string pattern = type == TapTargetType.Bomb ? (modern ? "REJECT" : "LONG_PRESS")
                            : type == TapTargetType.TimeBonus ? (modern ? "CONFIRM" : "CONTEXT_CLICK")
                            : fever ? "CONTEXT_CLICK"
                            : type == TapTargetType.Quick ? "KEYBOARD_TAP" : "CLOCK_TICK";
                        // No IGNORE_GLOBAL_SETTING flag: device touch-feedback preferences still apply.
                        view.Call<bool>("performHapticFeedback", constants.GetStatic<int>(pattern));
                    }
                    catch (System.Exception) { /* Devices without haptic support keep playing normally. */ }
                }));
            }
            catch (System.Exception) { /* A missing Android activity must never interrupt a tap. */ }
#endif
        }
    }
}
