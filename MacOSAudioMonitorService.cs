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
                // For macOS, use process detection as it's more reliable than volume checking
                CheckAudioProcesses();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"macOS audio monitoring error: {ex.Message}");
                
                // Fallback: assume no audio activity
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
                            Console.WriteLine($"macOS: Audio process detected - {processName}");
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
                    Console.WriteLine($"macOS: Audio state changed to {isActive}");
                    
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

        public void Dispose()
        {
            StopMonitoring();
            _cancellationTokenSource?.Dispose();
        }
    }
}
