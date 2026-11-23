using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArenaSurvival.Lab6
{
    /// <summary>Displays the catalogue and delegates loading to SceneNavigator.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private TMP_Dropdown levels;
        [SerializeField] private Button play;
        [SerializeField] private Button settingsButton;
        [SerializeField] private TMP_Text status;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private SettingsPanel settings;
        [SerializeField] private SceneNavigator navigator;

        private void Awake()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (levels == null 
                || play == null 
                || settingsButton == null 
                || status == null 
                || mainPanel == null 
                || settings == null 
                || navigator == null)
            {
                Debug.LogError("MainMenuController: assign all scene references.", this);
                enabled = false;
            }
        }

        private void OnEnable() { settings.Closed += OnSettingsClosed; }
        private void OnDisable() { if (settings != null) settings.Closed -= OnSettingsClosed; }

        private void Start()
        {
            levels.ClearOptions();
            if (catalog == null || catalog.levels == null || catalog.levels.Length == 0)
            {
                status.text = "No levels configured";
                levels.interactable = false;
                play.interactable = false;
                settingsButton.Select();
                return;
            }

            var names = new List<string>();

            foreach (LevelEntry entry in catalog.levels)
                names.Add(entry == null ? "Invalid level" : entry.title);

            levels.AddOptions(names);
            status.text = "";
            play.Select();
        }

        public void PlaySelected()
        {
            if (!isActiveAndEnabled || navigator.IsLoading || catalog == null || catalog.levels == null ||
                levels.value < 0 || levels.value >= catalog.levels.Length) return;

            LevelEntry entry = catalog.levels[levels.value];

            if (entry == null) { status.text = "Invalid level entry"; return; }

            if (navigator.LoadLevel(entry.scenePath)) levels.interactable = false;
            else status.text = navigator.Error;
        }

        public void OpenSettings()
        {
            if (!settings.IsReady || navigator.IsLoading) return;

            mainPanel.SetActive(false);
            settings.Open();
        }

        private void OnSettingsClosed()
        {
            mainPanel.SetActive(true);

            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            settingsButton.Select();
        }

        private void Update()
        {
            if (settings.IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                settings.Close();
        }
    }
}