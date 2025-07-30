using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace PetViewerLinux
{
    public class WindowsAudioMonitorService : IAudioMonitorService
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

            // Monitor audio activity every 200ms (slightly slower than Linux for Windows compatibility)
            _monitoringTimer = new Timer(CheckAudioActivity, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(200));
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
                // Use Windows PowerShell to check audio sessions more reliably
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-Command \"Get-WmiObject -Class Win32_SoundDevice | Where-Object {$_.Status -eq 'OK'} | Measure-Object | Select-Object -ExpandProperty Count\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // If PowerShell command fails, fallback to process detection
                if (string.IsNullOrEmpty(output) || !string.IsNullOrEmpty(error))
                {
                    CheckAudioProcesses();
                    return;
                }

                // For Windows, we primarily rely on process detection since direct audio monitoring requires complex APIs
                CheckAudioProcesses();
            }
            catch (Exception ex)
            {
                // Fallback to process-based detection
                System.Diagnostics.Debug.WriteLine($"Windows audio monitoring error: {ex.Message}");
                CheckAudioProcesses();
            }
        }

        private void CheckAudioProcesses()
        {
            try
            {
                // Check for common audio/media processes
                var audioProcesses = new[]
                {
                    "chrome", "firefox", "msedge", "spotify", "vlc", "wmplayer", 
                    "winamp", "foobar2000", "musicbee", "aimp", "potplayer",
                    "mpc-hc", "mpc-be", "discord", "teams", "zoom", "skype"
                };

                bool isActive = false;
                
                foreach (var processName in audioProcesses)
                {
                    var processes = Process.GetProcessesByName(processName);
                    if (processes.Length > 0)
                    {
                        Console.WriteLine($"Windows: Audio process detected - {processName}");
                        isActive = true;
                        break;
                    }
                }

                // Only notify if state changed
                if (isActive != _lastActivityState)
                {
                    _lastActivityState = isActive;
                    Console.WriteLine($"Windows: Audio state changed to {isActive}");
                    
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
                System.Diagnostics.Debug.WriteLine($"Windows process detection error: {ex.Message}");
                
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

        private bool HasAudioActivity(string output)
        {
            try
            {
                // Try to parse volume level - if volume > 0, there might be audio
                if (float.TryParse(output.Trim(), out float volume))
                {
                    return volume > 0.01f; // Consider anything above 1% as activity
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            StopMonitoring();
            _cancellationTokenSource?.Dispose();
        }
    }
}
