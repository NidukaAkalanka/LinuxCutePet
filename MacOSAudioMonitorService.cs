using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Threading;

namespace PetViewerLinux
{
    public class MacOSAudioMonitorService : IAudioMonitorService
    {
        public event EventHandler<AudioActivityChangedEventArgs>? AudioActivityChanged;
        
        private bool _isMonitoring = false;
        private CancellationTokenSource? _cancellationTokenSource;
        private Timer? _monitoringTimer;
        private bool _lastActivityState = false;

        public bool IsMonitoring => _isMonitoring;

        public void StartMonitoring()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
            _cancellationTokenSource = new CancellationTokenSource();

            // Monitor audio activity every 150ms (balanced for macOS performance)
            _monitoringTimer = new Timer(CheckAudioActivity, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(150));
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            _isMonitoring = false;
            _cancellationTokenSource?.Cancel();
            _monitoringTimer?.Dispose();
            _monitoringTimer = null;
        }

        private void CheckAudioActivity(object? state)
        {
            if (!_isMonitoring) return;

            try
            {
                // Use AppleScript to check macOS audio output
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = "-e \"output volume of (get volume settings)\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // If AppleScript fails, fallback to process detection
                if (string.IsNullOrEmpty(output) || !string.IsNullOrEmpty(error))
                {
                    CheckAudioProcesses();
                    return;
                }

                // Check if audio is actively playing (simple volume check)
                var isActive = HasAudioActivity();

                // Only notify if state changed
                if (isActive != _lastActivityState)
                {
                    _lastActivityState = isActive;
                    
                    // Dispatch to UI thread
                    Dispatcher.UIThread.Post(() =>
                    {
                        AudioActivityChanged?.Invoke(this, new AudioActivityChangedEventArgs
                        {
                            IsActive = isActive,
                            Timestamp = DateTime.Now
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                // Fallback to process-based detection
                System.Diagnostics.Debug.WriteLine($"macOS audio monitoring error: {ex.Message}");
                CheckAudioProcesses();
            }
        }

        private void CheckAudioProcesses()
        {
            try
            {
                // Check for common macOS audio/media processes
                var audioProcesses = new[]
                {
                    "Music", "Spotify", "VLC", "QuickTime Player", "iTunes", 
                    "Chrome", "Firefox", "Safari", "Discord", "Zoom", "Teams",
                    "FaceTime", "Skype", "Plex", "IINA", "Elmedia Player",
                    "JustAudioPlayer", "Audirvana", "Swinsian", "Vox"
                };

                bool isActive = false;
                
                foreach (var processName in audioProcesses)
                {
                    // Use pgrep to find processes (case-insensitive)
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "pgrep",
                            Arguments = $"-i \"{processName}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    try
                    {
                        process.Start();
                        var output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            isActive = true;
                            break;
                        }
                    }
                    catch
                    {
                        // Continue checking other processes
                        continue;
                    }
                }

                // Only notify if state changed
                if (isActive != _lastActivityState)
                {
                    _lastActivityState = isActive;
                    
                    Dispatcher.UIThread.Post(() =>
                    {
                        AudioActivityChanged?.Invoke(this, new AudioActivityChangedEventArgs
                        {
                            IsActive = isActive,
                            Timestamp = DateTime.Now
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"macOS process detection error: {ex.Message}");
                
                // Final fallback: assume no audio activity
                if (_lastActivityState != false)
                {
                    _lastActivityState = false;
                    Dispatcher.UIThread.Post(() =>
                    {
                        AudioActivityChanged?.Invoke(this, new AudioActivityChangedEventArgs
                        {
                            IsActive = false,
                            Timestamp = DateTime.Now
                        });
                    });
                }
            }
        }

        private bool HasAudioActivity()
        {
            try
            {
                // Check if any audio streams are active using macOS system commands
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "pmset",
                        Arguments = "-g audio",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // If output contains "audio", there might be audio activity
                return !string.IsNullOrEmpty(output) && output.ToLower().Contains("audio");
            }
            catch
            {
                // Fallback: assume there might be audio activity if processes are running
                return true; // Let process detection handle it
            }
        }

        public void Dispose()
        {
            StopMonitoring();
            _cancellationTokenSource?.Dispose();
        }
    }
}
