using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace PetViewerLinux
{
    public class LinuxAudioMonitorService : IAudioMonitorService
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

            // Monitor audio activity every 100ms
            _monitoringTimer = new Timer(CheckAudioActivity, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
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
                // Use pactl to check if there are any active audio streams
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "pactl",
                        Arguments = "list sink-inputs",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Check if there are any active audio streams
                var isActive = HasActiveAudioStreams(output);

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
                // Silently handle errors - audio monitoring is not critical
                System.Diagnostics.Debug.WriteLine($"Audio monitoring error: {ex.Message}");
                
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

        private bool HasActiveAudioStreams(string output)
        {
            try
            {
                // Check if there are any active sink inputs (applications playing audio)
                return !string.IsNullOrWhiteSpace(output) && output.Contains("Sink Input #");
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
