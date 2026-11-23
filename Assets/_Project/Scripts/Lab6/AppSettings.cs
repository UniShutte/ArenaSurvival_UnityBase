using System;
using UnityEngine;
using UnityEngine.Audio;

namespace ArenaSurvival.Lab6
{
    /// <summary>Persists application settings without keeping scene UI references.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class AppSettings : MonoBehaviour
    {
        public static readonly string[] VolumeParameters =
        {
            "MasterVolume", "WeaponsVolume", "ImpactsVolume",
            "FootstepsVolume", "AmbienceVolume", "UIVolume"
        };
        private static readonly float[] Defaults = { 0.8f, 0.7f, 0.6f, 0.5f, 0.35f, 0.6f };
        [SerializeField] private AudioMixer mixer;
        private readonly float[] volumes = new float[6];
        public static AppSettings Instance { get; private set; }
        public bool IsReady { get; private set; }
        public string QualityName { get; private set; }
        public bool VSync { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (mixer == null)
            {
                Debug.LogError("AppSettings: assign AM_Game.", this);
                enabled = false;
            }
        }

        /// <summary>Mixer parameters are applied in Start, not Awake or OnEnable.</summary>
        private void Start()
        {
            string requested = PlayerPrefs.GetString("AS.Quality", "Medium");
            if (Array.IndexOf(QualitySettings.names, requested) < 0)
                requested = Array.IndexOf(QualitySettings.names, "Medium") >= 0 ? "Medium" :
                    QualitySettings.names[QualitySettings.GetQualityLevel()];
            VSync = PlayerPrefs.GetInt("AS.VSync", 1) != 0;
            SetQuality(requested);
            SetVSync(VSync);
            for (int i = 0; i < volumes.Length; i++)
                SetVolume(i, PlayerPrefs.GetFloat("AS." + VolumeParameters[i], Defaults[i]));
            RestoreDisplay();
            IsReady = true;
        }

        public bool SetQuality(string name)
        {
            int index = Array.IndexOf(QualitySettings.names, name);
            if (index < 0) return false;
            QualitySettings.SetQualityLevel(index, true);
            QualityName = name;
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            PlayerPrefs.SetString("AS.Quality", name);
            return true;
        }

        public void SetVSync(bool value)
        {
            VSync = value;
            QualitySettings.vSyncCount = value ? 1 : 0;
            PlayerPrefs.SetInt("AS.VSync", value ? 1 : 0);
        }

        public float GetVolume(int channel) => volumes[channel];

        public void SetVolume(int channel, float value)
        {
            if (channel < 0 || channel >= volumes.Length)
                throw new ArgumentOutOfRangeException(nameof(channel));
            if (float.IsNaN(value) || float.IsInfinity(value)) value = Defaults[channel];
            value = Mathf.Clamp01(value);
            if (!mixer.SetFloat(VolumeParameters[channel], VolumeToDecibels(value)))
            {
                Debug.LogError("Unknown mixer parameter: " + VolumeParameters[channel], this);
                return;
            }
            volumes[channel] = value;
            PlayerPrefs.SetFloat("AS." + VolumeParameters[channel], value);
        }

        public static float VolumeToDecibels(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return -80f;
            value = Mathf.Clamp01(value);
            return value <= 0.0001f ? -80f : 20f * Mathf.Log10(value);
        }

        /// <summary>Saves actual screen values only after the user confirms the trial.</summary>
        public void SaveDisplay()
        {
            PlayerPrefs.SetInt("AS.Width", Screen.width);
            PlayerPrefs.SetInt("AS.Height", Screen.height);
            PlayerPrefs.SetInt("AS.Windowed", Screen.fullScreenMode == FullScreenMode.Windowed ? 1 : 0);
            Save();
        }

        private void RestoreDisplay()
        {
            if (!PlayerPrefs.HasKey("AS.Width") || !PlayerPrefs.HasKey("AS.Height")) return;
            int width = PlayerPrefs.GetInt("AS.Width");
            int height = PlayerPrefs.GetInt("AS.Height");
            bool available = width == Screen.width && height == Screen.height;
            foreach (Resolution resolution in Screen.resolutions)
                if (resolution.width == width && resolution.height == height) available = true;
            if (!available) return;
            FullScreenMode mode = PlayerPrefs.GetInt("AS.Windowed", 1) == 1 ?
                FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
            Screen.SetResolution(width, height, mode);
        }

        public void Save() { PlayerPrefs.Save(); }
        private void OnApplicationQuit() { Save(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
