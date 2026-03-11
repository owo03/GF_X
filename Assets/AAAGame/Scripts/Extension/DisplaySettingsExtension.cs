using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

public static class DisplaySettingsExtension
{
    private const int DefaultWidth = 1280;
    private const int DefaultHeight = 720;

    public static List<Vector2Int> GetSupportedResolutions()
    {
        var unique = new List<Vector2Int>();
        var seen = new HashSet<string>();
        Resolution[] resolutions = Screen.resolutions;
        if (resolutions == null || resolutions.Length == 0)
        {
            unique.Add(new Vector2Int(Screen.width, Screen.height));
            return unique;
        }

        for (int i = resolutions.Length - 1; i >= 0; i--)
        {
            var value = new Vector2Int(resolutions[i].width, resolutions[i].height);
            string key = value.x + "x" + value.y;
            if (seen.Add(key))
            {
                unique.Add(value);
            }
        }

        unique.Sort((left, right) =>
        {
            int heightCompare = left.y.CompareTo(right.y);
            if (heightCompare != 0)
            {
                return heightCompare;
            }
            return left.x.CompareTo(right.x);
        });
        return unique;
    }

    public static void ApplySavedDisplaySettings(this SettingComponent com)
    {
        bool isFullScreen = com.GetBool(ConstBuiltin.Setting.FullScreen, true);
        int width = com.GetInt(ConstBuiltin.Setting.ResolutionWidth, DefaultWidth);
        int height = com.GetInt(ConstBuiltin.Setting.ResolutionHeight, DefaultHeight);
        ApplyDisplaySettings(com, width, height, isFullScreen, false);
    }

    public static void ApplyDisplaySettings(this SettingComponent com, int width, int height, bool isFullScreen, bool saveSettings = true)
    {
        width = Mathf.Max(800, width);
        height = Mathf.Max(600, height);
        Screen.SetResolution(width, height, isFullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);

        if (saveSettings)
        {
            com.SetInt(ConstBuiltin.Setting.ResolutionWidth, width);
            com.SetInt(ConstBuiltin.Setting.ResolutionHeight, height);
            com.SetBool(ConstBuiltin.Setting.FullScreen, isFullScreen);
            com.Save();
        }

        if (GFBuiltin.Instance != null)
        {
            GFBuiltin.Instance.UpdateCanvasScaler();
        }
    }
}
