using System;
using System.IO;
using System.Text.Json;

namespace PetViewerLinux
{
    public class Config
    {
        public EdgeCalibration EdgeCalibration { get; set; } = new EdgeCalibration();
        public bool IsDanceEnabled { get; set; } = true;
    }
    
    public class ConfigManager
    {
        private static readonly string ConfigFileName = "config.json";
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            ".linuxcutepet", 
            ConfigFileName
        );
        
        public static Config LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<Config>(json);
                    return config ?? new Config();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
            }
            
            return new Config();
        }
        
        public static void SaveConfig(Config config)
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigFilePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }
        
        public static bool ConfigExists()
        {
            return File.Exists(ConfigFilePath);
        }
        
        public static bool IsCalibrationNeeded()
        {
            if (!ConfigExists())
                return true;
                
            var config = LoadConfig();
            return !config.EdgeCalibration.IsCalibrated;
        }
    }
}