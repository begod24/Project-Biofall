using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Biofall.Core;

namespace Biofall.UI
{
    public sealed class PauseSettings : MonoBehaviour
    {

        private ISettingsService _settingsService;
        private ISettingsService SettingsService =>
            _settingsService ??= ServiceLocator.Get<ISettingsService>();
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider shakeSlider;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown fullscreenDropdown;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button backButton;

        public event Action Closed;

        private List<Resolution> _resolutions;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(SettingsService.MasterVolume);
                masterVolumeSlider.onValueChanged.AddListener(SettingsService.SetMasterVolume);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(SettingsService.MusicVolume);
                musicVolumeSlider.onValueChanged.AddListener(SettingsService.SetMusicVolume);
            }
            if (shakeSlider != null)
            {
                shakeSlider.SetValueWithoutNotify(SettingsService.CameraShakeIntensity);
                shakeSlider.onValueChanged.AddListener(SettingsService.SetCameraShakeIntensity);
            }

            SetupDisplayDropdowns();
            if (applyButton != null) applyButton.onClick.AddListener(ApplyDisplay);
            if (backButton != null) backButton.onClick.AddListener(Close);

            gameObject.SetActive(false);
        }

        public void Open()
        {
            if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(SettingsService.MasterVolume);
            if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(SettingsService.MusicVolume);
            if (shakeSlider != null) shakeSlider.SetValueWithoutNotify(SettingsService.CameraShakeIntensity);
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void SetupDisplayDropdowns()
        {
            if (resolutionDropdown != null)
            {
                _resolutions = new List<Resolution>();
                var options = new List<string>();
                var seen = new HashSet<string>();
                foreach (var r in Screen.resolutions)
                {
                    string key = r.width + " x " + r.height;
                    if (!seen.Add(key)) continue;
                    _resolutions.Add(r);
                    options.Add(key);
                }
                int current = 0;
                for (int i = 0; i < _resolutions.Count; i++)
                    if (_resolutions[i].width == Screen.width && _resolutions[i].height == Screen.height) current = i;

                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(options);
                resolutionDropdown.SetValueWithoutNotify(current);
                resolutionDropdown.RefreshShownValue();
            }

            if (fullscreenDropdown != null)
            {
                fullscreenDropdown.ClearOptions();
                fullscreenDropdown.AddOptions(new List<string> { "Fullscreen", "Windowed", "Borderless" });
                fullscreenDropdown.SetValueWithoutNotify(ModeToIndex(Screen.fullScreenMode));
                fullscreenDropdown.RefreshShownValue();
            }
        }

        private void ApplyDisplay()
        {
            if (_resolutions == null || _resolutions.Count == 0 ||
                resolutionDropdown == null || fullscreenDropdown == null) return;

            Resolution r = _resolutions[Mathf.Clamp(resolutionDropdown.value, 0, _resolutions.Count - 1)];
            SettingsService.ApplyDisplay(r.width, r.height, IndexToMode(fullscreenDropdown.value));
        }

        private static int ModeToIndex(FullScreenMode mode) => mode switch
        {
            FullScreenMode.ExclusiveFullScreen => 0,
            FullScreenMode.Windowed => 1,
            _ => 2,
        };

        private static FullScreenMode IndexToMode(int index) => index switch
        {
            0 => FullScreenMode.ExclusiveFullScreen,
            1 => FullScreenMode.Windowed,
            _ => FullScreenMode.FullScreenWindow,
        };
    }
}
