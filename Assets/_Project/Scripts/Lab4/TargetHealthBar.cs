using ArenaSurvival.HealthSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.Lab4
{
    public sealed class TargetHealthBar : MonoBehaviour
    {
        [SerializeField] private Health target;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TMP_Text valueText;

        private void Awake()
        {
            if (target == null || healthSlider == null || valueText == null)
            {
                Debug.LogError("Assign Target, Health Slider and Value Text.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            target.HealthChanged += Render;
            Render(target.CurrentHealth, target.MaximumHealth);
        }

        private void Start() => Render(target.CurrentHealth, target.MaximumHealth);

        private void OnDisable()
        {
            if (target != null)
                target.HealthChanged -= Render;
        }

        private void Render(int current, int maximum)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maximum;
            healthSlider.SetValueWithoutNotify(current);
            valueText.text = $"{current} / {maximum}";
        }
    }
}
