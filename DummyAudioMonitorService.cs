using System;

namespace PetViewerLinux
{
    public class DummyAudioMonitorService : IAudioMonitorService
    {
        public event EventHandler<AudioActivityChangedEventArgs>? AudioActivityChanged;
        
        public bool IsMonitoring => false;

        public void StartMonitoring()
        {
            // Do nothing - this is a dummy implementation for unsupported platforms
        }

        public void StopMonitoring()
        {
            // Do nothing
        }

        public void Dispose()
        {
            // Do nothing
        }
    }
}
