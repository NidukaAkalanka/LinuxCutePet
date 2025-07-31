using System;
using System.Runtime.InteropServices;

namespace PetViewerLinux
{
    public static class AudioMonitorFactory
    {
        /// <summary>
        /// Creates the appropriate audio monitor service for the current platform
        /// </summary>
        /// <returns>Platform-specific audio monitor service</returns>
        public static IAudioMonitorService CreateAudioMonitor()
        {
            if (OperatingSystem.IsLinux())
            {
                return new LinuxAudioMonitorService();
            }
            else if (OperatingSystem.IsWindows())
            {
                return new WindowsAudioMonitorService();
            }
            // else if (OperatingSystem.IsMacOS())
            // {
            //     return new MacOSAudioMonitorService();
            // }
            else
            {
                // Fallback for any other platforms
                return new DummyAudioMonitorService();
            }
        }

        /// <summary>
        /// Creates a specific audio monitor service for testing or manual platform selection
        /// </summary>
        /// <param name="platform">The target platform</param>
        /// <returns>Platform-specific audio monitor service</returns>
        public static IAudioMonitorService CreateAudioMonitor(AudioPlatform platform)
        {
            return platform switch
            {
                AudioPlatform.Linux => new LinuxAudioMonitorService(),
                AudioPlatform.Windows => new WindowsAudioMonitorService(),
                //AudioPlatform.MacOS => new MacOSAudioMonitorService(),
                AudioPlatform.Dummy => new DummyAudioMonitorService(),
                _ => throw new ArgumentException($"Unsupported platform: {platform}")
            };
        }

        /// <summary>
        /// Gets the current platform
        /// </summary>
        /// <returns>Current platform enum</returns>
        public static AudioPlatform GetCurrentPlatform()
        {
            if (OperatingSystem.IsLinux())
                return AudioPlatform.Linux;
            else if (OperatingSystem.IsWindows())
                return AudioPlatform.Windows;
            // else if (OperatingSystem.IsMacOS())
            //     return AudioPlatform.MacOS;
            else
                return AudioPlatform.Dummy;
        }
    }

    public enum AudioPlatform
    {
        Linux,
        Windows,
        //MacOS,
        Dummy
    }
}
