using UnityEngine;

namespace Biofall.Core
{
    public sealed class ScreenDisplayDevice : IDisplayDevice
    {
        public int Width => Screen.width;
        public int Height => Screen.height;
        public FullScreenMode FullScreenMode => Screen.fullScreenMode;

        public void SetResolution(int width, int height, FullScreenMode mode) =>
            Screen.SetResolution(width, height, mode);

        public void SetListenerVolume(float volume) => AudioListener.volume = volume;
    }
}
