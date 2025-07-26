using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PetViewerLinux
{
    public class AudioMonitor : IAudioMonitor, IDisposable
    {
        public event Action<float>? VolumeChanged;
        public bool IsMonitoring { get; private set; }

        private Timer? _monitoringTimer;
        private readonly int _pollIntervalMs = 100; // Poll every 100ms
        private float _lastVolume = 0f;

        public void StartMonitoring()
        {
            if (IsMonitoring) return;

            IsMonitoring = true;
            _monitoringTimer = new Timer(MonitorVolume, null, 0, _pollIntervalMs);
        }

        public void StopMonitoring()
        {
            if (!IsMonitoring) return;

            IsMonitoring = false;
            _monitoringTimer?.Dispose();
            _monitoringTimer = null;
        }

        private void MonitorVolume(object? state)
        {
            try
            {
                float currentVolume = GetSystemVolumeLevel();
                
                // Only trigger event if volume changed significantly
                if (Math.Abs(currentVolume - _lastVolume) > 0.01f)
                {
                    _lastVolume = currentVolume;
                    VolumeChanged?.Invoke(currentVolume);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue monitoring
                Console.WriteLine($"Audio monitoring error: {ex.Message}");
            }
        }

        private float GetSystemVolumeLevel()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsVolumeLevel();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxVolumeLevel();
            }
            else
            {
                // Fallback for other platforms (macOS, etc.)
                return 0f;
            }
        }

        private float GetWindowsVolumeLevel()
        {
            try
            {
                // For Windows, we could use NAudio.CoreAudioApi to get actual volume levels
                // For now, simulate basic functionality by checking if common audio processes are running
                var processInfo = new ProcessStartInfo
                {
                    FileName = "tasklist",
                    Arguments = "/fi \"imagename eq audiodg.exe\" /fo csv",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // If audiodg.exe is running, there might be audio activity
                    if (output.Contains("audiodg.exe"))
                    {
                        // Return a varying level to simulate audio activity
                        return (float)(Math.Sin(DateTime.Now.Millisecond / 1000.0) * 0.3 + 0.4);
                    }
                }
                return 0f;
            }
            catch
            {
                return 0f;
            }
        }

        private float GetLinuxVolumeLevel()
        {
            try
            {
                // First try to get actual audio levels from PulseAudio
                var startInfo = new ProcessStartInfo
                {
                    FileName = "pactl",
                    Arguments = "list sink-inputs",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Parse the output to check for active audio streams and their volumes
                    if (output.Contains("Sink Input"))
                    {
                        // Look for volume information in the output
                        var lines = output.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.Trim().StartsWith("Volume:"))
                            {
                                // Parse volume percentage - look for patterns like "75%" 
                                var volumeMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)%");
                                if (volumeMatch.Success && int.TryParse(volumeMatch.Groups[1].Value, out int volumePercent))
                                {
                                    return volumePercent / 100.0f; // Convert to 0-1 range
                                }
                            }
                        }
                        
                        // If we have sink inputs but couldn't parse volume, assume moderate activity
                        return 0.4f;
                    }
                }
                
                // No active sink inputs found
                return 0f;
            }
            catch
            {
                // Fallback: try to detect if there's any audio system activity using a different approach
                try
                {
                    // Check if any audio processes are running
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "pgrep",
                        Arguments = "-f \"(pulseaudio|alsa|pipewire)\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(processInfo);
                    if (process != null)
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        // If audio system processes are running, return a low level to indicate basic functionality
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            // Return a varying level to simulate some audio activity for testing
                            return (float)(Math.Sin(DateTime.Now.Millisecond / 1000.0) * 0.2 + 0.3);
                        }
                    }
                }
                catch
                {
                    // Fall through to return 0
                }
                return 0f;
            }
        }

        public void Dispose()
        {
            StopMonitoring();
            _monitoringTimer?.Dispose();
        }
    }
}