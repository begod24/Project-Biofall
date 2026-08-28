using UnityEngine;

namespace Biofall.Core
{
    // The screen and the audio listener, behind an interface for the same reason.
    public interface IDisplayDevice
    {
        int Width { get; }
        int Height { get; }
        FullScreenMode FullScreenMode { get; }

        void SetResolution(int width, int height, FullScreenMode mode);
        void SetListenerVolume(float volume);
    }
}
