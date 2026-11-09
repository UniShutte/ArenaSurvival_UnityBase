using ArenaSurvival.HealthSystem;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.Lab6
{
    /// <summary>Places a non-interactive world-space bar above a rolling enemy.</summary>
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Slider slider;
        private Camera viewCamera;

        private void Awake()
        {
            if (health == null || slider == null)
            {
                Debug.LogError("EnemyHealthBar: assign Health and Slider.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            viewCamera = Camera.main;
            health.HealthChanged += Refresh;
            Refresh(health.CurrentHealth, health.MaximumHealth);
        }

        private void OnDisable()
        {
            if (health != null) health.HealthChanged -= Refresh;
        }

        private void Refresh(int current, int maximum)
        {
            slider.maxValue = maximum;
            slider.value = current;
        }

        private void LateUpdate()
        {
            if (viewCamera == null) return;
            transform.position = health.transform.position + Vector3.up * 0.85f;
            transform.rotation = viewCamera.transform.rotation;
        }
    }
}