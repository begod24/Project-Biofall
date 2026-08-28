using System;
using UnityEngine;

namespace Biofall.Core
{
    // Same keys and same behaviour as the old static GameSettings, so saved preferences carry
    // over. What changed is that the store and the screen are injected instead of reached for.
    public sealed class GameSettingsService : ISettingsService
    {
        private const string VolumeKey = "bf_master_volume";
        private const string MusicKey = "bf_music_volume";
        private const string ShakeKey = "bf_camera_shake";
        private const string ShakeIntensityKey = "bf_shake_intensity";
        private const string ResWKey = "bf_res_w";
        private const string ResHKey = "bf_res_h";
        private const string FsModeKey = "bf_fs_mode";

        private readonly ISettingsStore _store;
        private readonly IDisplayDevice _display;

        private float _volume;
        private float _music;
        private float _shake;

        public event Action MusicVolumeChanged;

        public float MasterVolume => _volume;
        public float MusicVolume => _music;
        public float CameraShakeIntensity => _shake;
        public bool CameraShakeEnabled => _shake > 0f;

        // The constructor applies the stored resolution, which is why the bootstrap builds this
        // before anything that reads a volume in its own Awake.
        public GameSettingsService(ISettingsStore store, IDisplayDevice display)
        {
            _store = store;
            _display = display;

            _volume = Mathf.Clamp01(_store.GetFloat(VolumeKey, 1f));
            _music = Mathf.Clamp01(_store.GetFloat(MusicKey, 1f));

            float defaultShake = _store.GetInt(ShakeKey, 1) == 1 ? 1f : 0f;
            _shake = Mathf.Clamp01(_store.GetFloat(ShakeIntensityKey, defaultShake));

            _display.SetListenerVolume(_volume);
            ApplySavedDisplay();
        }

        public void SetMasterVolume(float value)
        {
            _volume = Mathf.Clamp01(value);
            _display.SetListenerVolume(_volume);
            _store.SetFloat(VolumeKey, _volume);
            _store.Save();
        }

        public void SetMusicVolume(float value)
        {
            _music = Mathf.Clamp01(value);
            _store.SetFloat(MusicKey, _music);
            _store.Save();
            MusicVolumeChanged?.Invoke();
        }

        public void SetCameraShakeIntensity(float value)
        {
            _shake = Mathf.Clamp01(value);
            _store.SetFloat(ShakeIntensityKey, _shake);
            _store.SetInt(ShakeKey, _shake > 0f ? 1 : 0);
            _store.Save();
        }

        public void ApplyDisplay(int width, int height, FullScreenMode mode)
        {
            width = Mathf.Max(640, width);
            height = Mathf.Max(480, height);

            _display.SetResolution(width, height, mode);
            _store.SetInt(ResWKey, width);
            _store.SetInt(ResHKey, height);
            _store.SetInt(FsModeKey, (int)mode);
            _store.Save();
        }

        private void ApplySavedDisplay()
        {
            if (!_store.HasKey(ResWKey)) return;

            int w = _store.GetInt(ResWKey, _display.Width);
            int h = _store.GetInt(ResHKey, _display.Height);
            var mode = (FullScreenMode)_store.GetInt(FsModeKey, (int)_display.FullScreenMode);

            _display.SetResolution(w, h, mode);
        }
    }
}
