using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArenaSurvival.Lab6
{
    /// <summary>Starts one validated scene transition and restores global time and audio.</summary>
    public sealed class SceneNavigator : MonoBehaviour
    {
        [SerializeField] private string mainMenuPath = "Assets/_Project/Scenes/MainMenu.unity";
        [SerializeField] private Button[] loadingButtons;
        [SerializeField] private TMP_Text status;
        public bool IsLoading { get; private set; }
        public string Error { get; private set; }

        public void RestartLevel() { LoadLevel(SceneManager.GetActiveScene().path); }
        public void OpenMainMenu() { LoadLevel(mainMenuPath); }
        public void QuitApplication() { Application.Quit(); }

        /// <summary>Returns false without changing pause state when configuration is invalid.</summary>
        public bool LoadLevel(string path)
        {
            if (!isActiveAndEnabled || IsLoading) return false;
            if (string.IsNullOrWhiteSpace(path) || SceneUtility.GetBuildIndexByScenePath(path) < 0)
                return Fail("Add the scene to the active Build Profile Scene List: " + path);
            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
                if (operation == null) return Fail("Unity did not start the scene load.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return Fail("Scene load failed: " + path);
            }
            IsLoading = true;
            Error = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (status != null) status.text = "Loading...";
            if (loadingButtons != null)
                foreach (Button button in loadingButtons)
                    if (button != null) button.interactable = false;
            return true;
        }

        private bool Fail(string message)
        {
            Error = message;
            if (status != null) status.text = message;
            Debug.LogError(message, this);
            return false;
        }
    }
}