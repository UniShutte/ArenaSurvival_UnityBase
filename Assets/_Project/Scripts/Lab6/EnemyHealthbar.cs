using ArenaSurvival.HealthSystem;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.Lab6
{
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Slider slider;
        private Camera viewCamera;
        private void Awake()
        {
            if (health == null || slider == null)
            { Debug.LogError("Assign Health and Slider.", this); enabled = false; }
        }
        private void OnEnable()
        {
            if (health == null || slider == null) return;
            health.HealthChanged += Refresh;
            Refresh(health.CurrentHealth, health.MaximumHealth);
        }
        private void Start() { viewCamera = Camera.main; Refresh(health.CurrentHealth, health.MaximumHealth); }
        private void OnDisable() { if (health != null) health.HealthChanged -= Refresh; }
        private void Refresh(int current, int maximum)
        { slider.maxValue = maximum; slider.value = current; }
        private void LateUpdate()
        {
            if (viewCamera == null) return;
            transform.position = health.transform.position + Vector3.up * 0.85f;
            transform.rotation = viewCamera.transform.rotation;
        }
    }
}