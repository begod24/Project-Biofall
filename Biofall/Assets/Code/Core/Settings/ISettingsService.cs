using System;
using UnityEngine;

namespace Biofall.Core
{
    // One owner for what the player chose. Screens read it and keep no copy of their own.
    public interface ISettingsService
    {
        float MasterVolume { get; }
        float MusicVolume { get; }
        float CameraShakeIntensity { get; }
        bool CameraShakeEnabled { get; }

        event Action MusicVolumeChanged;

        void SetMasterVolume(float value);
        void SetMusicVolume(float value);
        void SetCameraShakeIntensity(float value);
        void ApplyDisplay(int width, int height, FullScreenMode mode);
    }
}
