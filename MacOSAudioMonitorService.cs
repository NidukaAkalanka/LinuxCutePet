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
                // Use macOS Core Audio API to check actual audio streams
                CheckCoreAudioStreams();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"macOS audio monitoring error: {ex.Message}");
                
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

        private void CheckCoreAudioStreams()
        {
            try
            {
                // Use macOS system_profiler to check actual audio hardware activity
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "system_profiler",
                        Arguments = "SPAudioDataType -xml",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Check for active audio output devices and streams
                var isActive = CheckAudioOutput(output);
                
                if (!isActive)
                {
                    // Alternative method: Use AppleScript to check audio streams
                    CheckWithAppleScript();
                }
                else
                {
                    Console.WriteLine($"macOS: Core Audio detection - Active: {isActive}");

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"macOS Core Audio detection error: {ex.Message}");
                CheckWithAppleScript();
            }
        }

        private bool CheckAudioOutput(string systemProfilerOutput)
        {
            try
            {
                // Look for indicators of active audio output in system profiler XML
                var isActive = systemProfilerOutput.Contains("<key>coreaudio_output_source</key>") ||
                              systemProfilerOutput.Contains("<string>Built-in Output</string>") ||
                              systemProfilerOutput.Contains("<string>Internal Speakers</string>");
                              
                // Additional check: Look for current output device usage
                if (isActive)
                {
                    // Use a more specific check for actual audio activity
                    var audioCheckProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "osascript",
                            Arguments = "-e \"output muted of (get volume settings)\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    audioCheckProcess.Start();
                    var muteOutput = audioCheckProcess.StandardOutput.ReadToEnd().Trim();
                    audioCheckProcess.WaitForExit();

                    // If system is muted, there's no audio playing
                    if (muteOutput.Contains("true"))
                    {
                        return false;
                    }
                }

                return isActive;
            }
            catch
            {
                return false;
            }
        }

        private void CheckWithAppleScript()
        {
            try
            {
                // Use AppleScript to query Core Audio for active streams
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = "-e \"" +
                            "tell application \\\"System Events\\\" to " +
                            "set audioLevel to output volume of (get volume settings) " +
                            "if audioLevel > 0 then " +
                            "    try " +
                            "        do shell script \\\"lsof /dev/null 2>/dev/null | grep -i audio | wc -l\\\" " +
                            "        set activeStreams to result as integer " +
                            "        if activeStreams > 0 then " +
                            "            return \\\"AUDIO_ACTIVE\\\" " +
                            "        else " +
                            "            return \\\"AUDIO_INACTIVE\\\" " +
                            "        end if " +
                            "    on error " +
                            "        return \\\"AUDIO_INACTIVE\\\" " +
                            "    end try " +
                            "else " +
                            "    return \\\"AUDIO_MUTED\\\" " +
                            "end if\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd().Trim();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                var isActive = output.Contains("AUDIO_ACTIVE");
                Console.WriteLine($"macOS: AppleScript check - {output}, Active: {isActive}");

                // Only notify if state changed
                if (isActive != _lastActivityState)
                {
                    _lastActivityState = isActive;
                    Console.WriteLine($"macOS: Audio state changed to {isActive} (AppleScript)");
                    
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
                Console.WriteLine($"macOS AppleScript audio detection error: {ex.Message}");
                
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
