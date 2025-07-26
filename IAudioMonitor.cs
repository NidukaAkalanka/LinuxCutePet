using System;

namespace PetViewerLinux
{
    public interface IAudioMonitor
    {
        event Action<float> VolumeChanged;
        bool IsMonitoring { get; }
        void StartMonitoring();
        void StopMonitoring();
        void Dispose();
    }
}