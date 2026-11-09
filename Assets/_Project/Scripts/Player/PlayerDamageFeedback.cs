using ArenaSurvival.HealthSystem;
using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>Plays feedback only when health decreases.</summary>
    [RequireComponent(typeof(Health))]
    public sealed class PlayerDamageFeedback : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip hurtClip;
        private Health health;
        private int previousHealth;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (source == null || hurtClip == null)
            {
                Debug.LogError("PlayerDamageFeedback: assign Source and Hurt Clip.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            previousHealth = health.CurrentHealth;
            health.HealthChanged += OnHealthChanged;
        }

        private void OnDisable() { health.HealthChanged -= OnHealthChanged; }

        private void OnHealthChanged(int current, int maximum)
        {
            if (current < previousHealth) source.PlayOneShot(hurtClip);
            previousHealth = current;
        }
    }
}