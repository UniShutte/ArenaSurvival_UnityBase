using System.Collections;
using ArenaSurvival.Combat;
using ArenaSurvival.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArenaSurvival.Lab6
{
    /// <summary>Owns Escape and suspends input without cancelling Weapon's reload.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private CombatHud hud;
        [SerializeField] private SettingsPanel settings;
        [SerializeField] private SceneNavigator navigator;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        private PlayerMovement movement;
        private PlayerLook look;
        private bool paused;
        private bool resuming;
        private bool movementWasEnabled;
        private bool lookWasEnabled;

        private void Awake()
        {
            if (session == null || hud == null || settings == null || navigator == null ||
                pausePanel == null || resumeButton == null)
            {
                Debug.LogError("PauseMenu: assign all scene references.", this);
                enabled = false;
                return;
            }
            pausePanel.SetActive(false);
        }

        private void OnEnable()
        {
            session.Changed += OnSessionChanged;
            settings.Closed += OnSettingsClosed;
        }

        private IEnumerator Start()
        {
            while (session.State == RoundState.Preparing) yield return null;
            movement = session.PlayerHealth.GetComponent<PlayerMovement>();
            look = session.PlayerHealth.GetComponent<PlayerLook>();
        }

        private void Update()
        {
            if (look == null || session.State != RoundState.Playing || navigator.IsLoading ||
                Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (settings.IsOpen) settings.Close();
            else if (paused) Resume();
            else Pause();
        }

        private void Pause()
        {
            if (paused || look == null || session.State != RoundState.Playing) return;
            paused = true;
            movementWasEnabled = movement.enabled;
            lookWasEnabled = look.enabled;
            movement.enabled = false;
            look.enabled = false;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            hud.SetMenuVisible(true);
            pausePanel.SetActive(true);
            resumeButton.Select();
        }

        public void Resume()
        {
            if (paused && !resuming && !settings.IsOpen && !navigator.IsLoading)
                StartCoroutine(ResumeAfterRelease());
        }

        /// <summary>Consumes mouse and keyboard UI submission before gameplay can read input.</summary>
        private IEnumerator ResumeAfterRelease()
        {
            resuming = true;
            resumeButton.interactable = false;
            while (SubmitHeld()) yield return null;
            yield return null;
            if (session.State != RoundState.Playing || navigator.IsLoading) yield break;
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            movement.enabled = movementWasEnabled;
            look.enabled = lookWasEnabled;
            if (lookWasEnabled) look.SetCursorLocked(true);
            hud.SetMenuVisible(false);
            paused = false;
            resuming = false;
            resumeButton.interactable = true;
        }

        private static bool SubmitHeld()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            return (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed)) ||
                (keyboard != null && (keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed ||
                    keyboard.spaceKey.isPressed));
        }

        public void OpenSettings()
        {
            if (!paused || resuming || !settings.IsReady || navigator.IsLoading) return;
            pausePanel.SetActive(false);
            settings.Open();
        }

        private void OnSettingsClosed()
        {
            if (!paused || session.State != RoundState.Playing) return;
            pausePanel.SetActive(true);
            resumeButton.Select();
        }

        private void OnSessionChanged()
        {
            if (session.State == RoundState.Playing || session.State == RoundState.Preparing) return;
            StopAllCoroutines();
            paused = false;
            resuming = false;
            if (settings.IsOpen) settings.Close();
            pausePanel.SetActive(false);
            hud.SetMenuVisible(false);
            AudioListener.pause = false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && navigator != null && !navigator.IsLoading) Pause();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (session != null) session.Changed -= OnSessionChanged;
            if (settings != null) settings.Closed -= OnSettingsClosed;
            if (paused)
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
        }
    }
}
