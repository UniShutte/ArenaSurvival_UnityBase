using System;
using ArenaSurvival.Combat;
using UnityEngine;

namespace ArenaSurvival.HealthSystem
{
    /// <summary>
    /// Хранит и изменяет здоровье объекта.
    ///
    /// Компонент не знает о HUD, снарядах
    /// и визуальных эффектах.
    /// Он публикует события, когда состояние изменяется.
    /// </summary>
    public sealed class Health :
        MonoBehaviour,
        IDamageable
    {
        [Header("Health Settings")]
        [SerializeField, Min(1)]
        private int maximumHealth = 100;

        public int CurrentHealth { get; private set; }

        public int MaximumHealth =>
            maximumHealth;

        /// <summary>
        /// Передаёт текущее и максимальное здоровье.
        /// </summary>
        public event Action<int, int>
            HealthChanged;

        /// <summary>
        /// Публикуется один раз,
        /// когда здоровье достигает нуля.
        /// </summary>
        public event Action Died;

        private void Awake()
        {
            CurrentHealth =
                maximumHealth;
        }

        private void Start()
        {
            // Первое уведомление позволяет представлениям
            // получить начальное состояние.
            PublishHealthChanged();
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (CurrentHealth == 0)
            {
                return;
            }

            CurrentHealth = Mathf.Max(
                0,
                CurrentHealth - amount);

            PublishHealthChanged();

            if (CurrentHealth == 0)
            {
                Died?.Invoke();
            }
        }

        [ContextMenu("Reset Health")]
        public void ResetHealth()
        {
            CurrentHealth =
                maximumHealth;

            PublishHealthChanged();
        }

        private void PublishHealthChanged()
        {
            HealthChanged?.Invoke(
                CurrentHealth,
                MaximumHealth);
        }
    }
}