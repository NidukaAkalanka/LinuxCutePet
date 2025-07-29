using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PetViewerLinux
{
    public class AppConfig
    {
        [JsonPropertyName("cachedFirstRun")]
        public bool CachedFirstRun { get; set; } = false;
        
        [JsonPropertyName("lastCacheDate")]
        public DateTime? LastCacheDate { get; set; }
        
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";
        
        [JsonPropertyName("isDanceEnabled")]
        public bool IsDanceEnabled { get; set; } = true;
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
                Console.WriteLine($"Attempting to save config to: {ConfigFilePath}");
                
                // Try a simple string write first
                var jsonContent = JsonSerializer.Serialize(config, JsonOptions);
                Console.WriteLine($"JSON serialized: {jsonContent}");
                
                File.WriteAllText(ConfigFilePath, jsonContent);
                Console.WriteLine("File write completed successfully");
                
                // Verify the file was written
                if (File.Exists(ConfigFilePath))
                {
                    Console.WriteLine("✅ Config file exists after write");
                    var content = File.ReadAllText(ConfigFilePath);
                    Console.WriteLine($"File content: {content}");
                }
                else
                {
                    Console.WriteLine("❌ Config file does not exist after write");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save config: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        public static void MarkCachingComplete()
        {
            var config = LoadConfig();
            config.CachedFirstRun = true;
            config.LastCacheDate = DateTime.Now;
            SaveConfig(config);
        }
        
        public static bool IsCachingRequired()
        {
            var config = LoadConfig();
            return !config.CachedFirstRun;
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
