using UnityEngine;

namespace ArenaSurvival.Lab6
{
    /// <summary>A scene-owned UI source that remains audible while the listener is paused.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class UiClickAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip click;
        private AudioSource source;
        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.ignoreListenerPause = true;
            if (click == null)
            {
                Debug.LogError("UiClickAudio: assign Click.", this);
                enabled = false;
            }
        }

        public void PlayClick()
        {
            if (isActiveAndEnabled) source.PlayOneShot(click);
        }
    }
}
