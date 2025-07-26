using System;

namespace PetViewerLinux
{
    public interface IAudioMonitorService
    {
        event EventHandler<AudioActivityChangedEventArgs> AudioActivityChanged;
        void StartMonitoring();
        void StopMonitoring();
        bool IsMonitoring { get; }
    }

    public class AudioActivityChangedEventArgs : EventArgs
    {
        public bool IsActive { get; set; } // True if any sound is playing
        public DateTime Timestamp { get; set; }
    }
}
