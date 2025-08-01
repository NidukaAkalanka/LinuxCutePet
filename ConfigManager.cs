using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PetViewerLinux
{
    public class AppConfig
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";
        
        [JsonPropertyName("isDanceEnabled")]
        public bool IsDanceEnabled { get; set; } = true;
        
        [JsonPropertyName("windowWidth")]
        public double WindowWidth { get; set; } = 300.0;
        
        [JsonPropertyName("windowHeight")]
        public double WindowHeight { get; set; } = 300.0;
        
        [JsonPropertyName("assetsDirectory")]
        public string AssetsDirectory { get; set; } = "Assets";
    }
    
    public static class ConfigManager
    {
        private static readonly string ConfigFileName = "config.json";
        private static string ConfigFilePath => Path.Combine(GetExecutableDirectory(), ConfigFileName);
        
        // Configure JSON options to be trim-safe
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        public static AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var jsonContent = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<AppConfig>(jsonContent, JsonOptions);
                    return config ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
            }
            
            // Return default config if file doesn't exist or loading failed
            return new AppConfig();
        }
        
        public static void SaveConfig(AppConfig config)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(ConfigFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
            }
        }
        
        public static void UpdateDanceEnabled(bool isEnabled)
        {
            var config = LoadConfig();
            config.IsDanceEnabled = isEnabled;
            SaveConfig(config);
        }
        
        public static bool GetDanceEnabled()
        {
            var config = LoadConfig();
            return config.IsDanceEnabled;
        }
        
        public static void UpdateWindowSize(double width, double height)
        {
            var config = LoadConfig();
            config.WindowWidth = width;
            config.WindowHeight = height;
            SaveConfig(config);
        }
        
        public static (double Width, double Height) GetWindowSize()
        {
            var config = LoadConfig();
            return (config.WindowWidth, config.WindowHeight);
        }
        
        public static void UpdateAssetsDirectory(string assetsDirectory)
        {
            var config = LoadConfig();
            config.AssetsDirectory = assetsDirectory;
            SaveConfig(config);
        }
        
        public static string GetAssetsDirectory()
        {
            var config = LoadConfig();
            return config.AssetsDirectory;
        }
        
        public static List<string> GetAvailableMods()
        {
            var modNames = new List<string>();
            var baseDirectory = GetExecutableDirectory();
            
            try
            {
                if (Directory.Exists(baseDirectory))
                {
                    var directories = Directory.GetDirectories(baseDirectory, "Assets-*");
                    foreach (var dir in directories)
                    {
                        var dirName = Path.GetFileName(dir);
                        if (dirName.StartsWith("Assets-"))
                        {
                            var modName = dirName.Substring("Assets-".Length);
                            if (!string.IsNullOrWhiteSpace(modName))
                            {
                                modNames.Add(modName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to scan for mods: {ex.Message}");
            }
            
            return modNames;
        }
        
        public static string GetAssetsPath()
        {
            var assetsDirectory = GetAssetsDirectory();
            return Path.Combine(GetExecutableDirectory(), assetsDirectory);
        }
        
        private static string GetExecutableDirectory()
        {
            try
            {
                // For single-file deployments, use AppContext.BaseDirectory (recommended by IL3000 warning)
                var baseDirectory = AppContext.BaseDirectory;
                if (!string.IsNullOrEmpty(baseDirectory))
                {
                    return baseDirectory;
                }
                
                // Fallback to process executable path
                var processPath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(processPath))
                {
                    return Path.GetDirectoryName(processPath) ?? Environment.CurrentDirectory;
                }
                
                // Final fallback to assembly location (for development builds only)
                var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    return Path.GetDirectoryName(assemblyLocation) ?? Environment.CurrentDirectory;
                }
            }
            catch
            {
                // Fallback to current directory
            }
            
            return Environment.CurrentDirectory;
        }
        
        public static bool ConfigExists()
        {
            return File.Exists(ConfigFilePath);
        }
        
        public static string GetConfigPath()
        {
            return ConfigFilePath;
        }
        
        public static void DebugPrintPaths()
        {
            Console.WriteLine($"Executable Directory: {GetExecutableDirectory()}");
            Console.WriteLine($"Config File Path: {ConfigFilePath}");
            Console.WriteLine($"Current Directory: {Environment.CurrentDirectory}");
            Console.WriteLine($"Process Path: {Environment.ProcessPath}");
            Console.WriteLine($"Assembly Location: {System.Reflection.Assembly.GetExecutingAssembly().Location}");
        }
    }
}
