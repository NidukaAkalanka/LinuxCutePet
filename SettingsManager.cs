using System;
using System.Configuration;
using System.IO;

namespace PetViewerLinux
{
    public static class SettingsManager
    {
        private const string SETTINGS_FILE = "PetSettings.config";
        private static readonly string _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LinuxCutePet",
            SETTINGS_FILE);

        public static float TriggerVolumeThreshold
        {
            get => GetFloatSetting("TriggerVolumeThreshold", 0.3f); // Default 30%
            set => SetSetting("TriggerVolumeThreshold", value.ToString());
        }

        private static float GetFloatSetting(string key, float defaultValue)
        {
            try
            {
                string? value = GetSetting(key);
                if (value != null && float.TryParse(value, out float result))
                {
                    return Math.Clamp(result, 0f, 1f); // Ensure value is between 0 and 1
                }
            }
            catch
            {
                // Fall through to default
            }
            return defaultValue;
        }

        private static string? GetSetting(string key)
        {
            try
            {
                if (!File.Exists(_settingsPath))
                    return null;

                var lines = File.ReadAllLines(_settingsPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2 && parts[0].Trim() == key)
                    {
                        return parts[1].Trim();
                    }
                }
            }
            catch
            {
                // Ignore errors and return null
            }
            return null;
        }

        private static void SetSetting(string key, string value)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Read existing settings
                var settings = new System.Collections.Generic.Dictionary<string, string>();
                if (File.Exists(_settingsPath))
                {
                    var lines = File.ReadAllLines(_settingsPath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            settings[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }

                // Update setting
                settings[key] = value;

                // Write back to file
                var output = new System.Collections.Generic.List<string>();
                foreach (var kvp in settings)
                {
                    output.Add($"{kvp.Key}={kvp.Value}");
                }
                File.WriteAllLines(_settingsPath, output);
            }
            catch
            {
                // Ignore errors - settings will use defaults
            }
        }
    }
}