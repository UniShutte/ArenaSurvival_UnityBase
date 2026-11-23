using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArenaSurvival.Lab6
{
    /// <summary>Scene UI; keeps the display rollback timer outside hidden content.</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject content;
        [SerializeField] private CanvasGroup form;
        [SerializeField] private TMP_Dropdown quality;
        [SerializeField] private Toggle vSync;
        [SerializeField] private Slider[] volumeSliders;
        [SerializeField] private TMP_Text[] volumeLabels;
        [SerializeField] private TMP_Dropdown resolution;
        [SerializeField] private TMP_Dropdown windowMode;
        [SerializeField] private GameObject confirmation;
        [SerializeField] private TMP_Text countdown;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button revertButton;
        private readonly List<Vector2Int> sizes = new();
        private AppSettings settings;
        private bool testingDisplay;
        private float deadline;
        private int confirmAfterFrame;
        private int oldWidth;
        private int oldHeight;
        private FullScreenMode oldMode;
        public bool IsReady { get; private set; }
        public bool IsOpen => content != null && content.activeSelf;
        public event Action Closed;

        private void Awake()
        {
            if (content == null || form == null || quality == null || vSync == null ||
                resolution == null || windowMode == null || confirmation == null || countdown == null ||
                confirmButton == null || revertButton == null || volumeSliders == null ||
                volumeLabels == null || volumeSliders.Length != 6 || volumeLabels.Length != 6)
            {
                Debug.LogError("SettingsPanel: assign all references, six sliders and six labels.", this);
                enabled = false;
                return;
            }
            for (int i = 0; i < 6; i++)
                if (volumeSliders[i] == null || volumeLabels[i] == null)
                {
                    Debug.LogError("SettingsPanel: volume entry " + i + " is incomplete.", this);
                    enabled = false;
                    return;
                }
            content.SetActive(false);
            confirmation.SetActive(false);
        }

        private void OnEnable() { StartCoroutine(Initialize()); }

        private IEnumerator Initialize()
        {
            while (AppSettings.Instance == null || !AppSettings.Instance.IsReady) yield return null;
            settings = AppSettings.Instance;
            yield return null;
            quality.ClearOptions();
            quality.AddOptions(new List<string>(QualitySettings.names));
            windowMode.ClearOptions();
            windowMode.AddOptions(new List<string> { "Windowed", "FullScreenWindow" });
            BuildResolutionOptions();
            IsReady = true;
            Refresh();
        }

        public void Open()
        {
            if (!IsReady) return;
            BuildResolutionOptions();
            Refresh();
            content.SetActive(true);
            Select(quality);
        }

        /// <summary>Closing never silently accepts a trial display setting.</summary>
        public void Close()
        {
            if (!IsOpen) return;
            if (testingDisplay) RevertDisplay();
            settings.Save();
            content.SetActive(false);
            Closed?.Invoke();
        }

        private void Refresh()
        {
            quality.SetValueWithoutNotify(Array.IndexOf(QualitySettings.names, settings.QualityName));
            vSync.SetIsOnWithoutNotify(settings.VSync);
            for (int i = 0; i < 6; i++)
            {
                volumeSliders[i].SetValueWithoutNotify(settings.GetVolume(i));
                volumeLabels[i].text = Mathf.RoundToInt(settings.GetVolume(i) * 100f) + "%";
            }
            SetDisplayChoices(Screen.width, Screen.height, Screen.fullScreenMode);
        }

        private void BuildResolutionOptions()
        {
            sizes.Clear();
            foreach (Resolution item in Screen.resolutions)
            {
                var size = new Vector2Int(item.width, item.height);
                if (!sizes.Contains(size)) sizes.Add(size);
            }
            var current = new Vector2Int(Screen.width, Screen.height);
            if (!sizes.Contains(current)) sizes.Add(current);
            resolution.ClearOptions();
            var labels = new List<string>();
            foreach (Vector2Int size in sizes) labels.Add($"{size.x} x {size.y}");
            resolution.AddOptions(labels);
        }

        private void SetDisplayChoices(int width, int height, FullScreenMode mode)
        {
            int index = sizes.IndexOf(new Vector2Int(width, height));
            if (index >= 0) resolution.SetValueWithoutNotify(index);
            windowMode.SetValueWithoutNotify(mode == FullScreenMode.Windowed ? 0 : 1);
        }

        public void SetQuality(int index)
        {
            if (!IsReady || index < 0 || index >= QualitySettings.names.Length) return;
            settings.SetQuality(QualitySettings.names[index]);
        }

        public void SetVSync(bool value) { if (IsReady) settings.SetVSync(value); }
        public void SetMaster(float value) { SetVolume(0, value); }
        public void SetWeapons(float value) { SetVolume(1, value); }
        public void SetImpacts(float value) { SetVolume(2, value); }
        public void SetFootsteps(float value) { SetVolume(3, value); }
        public void SetAmbience(float value) { SetVolume(4, value); }
        public void SetUI(float value) { SetVolume(5, value); }

        private void SetVolume(int channel, float value)
        {
            if (!IsReady) return;
            settings.SetVolume(channel, value);
            volumeLabels[channel].text = Mathf.RoundToInt(settings.GetVolume(channel) * 100f) + "%";
        }

        /// <summary>Starts a reversible trial; preferences still hold the confirmed display.</summary>
        public void ApplyDisplay()
        {
            if (!IsReady || !IsOpen || testingDisplay || resolution.value < 0 ||
                resolution.value >= sizes.Count) return;
            oldWidth = Screen.width;
            oldHeight = Screen.height;
            oldMode = Screen.fullScreenMode;
            Vector2Int size = sizes[resolution.value];
            FullScreenMode mode = windowMode.value == 0 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
            Screen.SetResolution(size.x, size.y, mode);
            testingDisplay = true;
            deadline = Time.unscaledTime + 15f;
            confirmAfterFrame = Time.frameCount + 2;
            form.interactable = false;
            form.blocksRaycasts = false;
            confirmation.SetActive(true);
            confirmButton.interactable = false;
            Select(revertButton);
        }

        private void Update()
        {
            if (!testingDisplay) return;
            confirmButton.interactable = Time.frameCount >= confirmAfterFrame;
            countdown.text = "Keep display? " + Mathf.CeilToInt(Mathf.Max(0f, deadline - Time.unscaledTime));
            if (Time.unscaledTime >= deadline) RevertDisplay();
        }

        public void ConfirmDisplay()
        {
            if (!testingDisplay || Time.frameCount < confirmAfterFrame) return;
            EndDisplayTrial();
            settings.SaveDisplay();
            BuildResolutionOptions();
            SetDisplayChoices(Screen.width, Screen.height, Screen.fullScreenMode);
        }

        public void RevertDisplay()
        {
            if (!testingDisplay) return;
            Screen.SetResolution(oldWidth, oldHeight, oldMode);
            EndDisplayTrial();
            SetDisplayChoices(oldWidth, oldHeight, oldMode);
        }

        private void EndDisplayTrial()
        {
            testingDisplay = false;
            confirmation.SetActive(false);
            form.interactable = true;
            form.blocksRaycasts = true;
            Select(resolution);
        }

        private static void Select(Selectable control)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            control.Select();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (testingDisplay) RevertDisplay();
            IsReady = false;
        }
    }
}
