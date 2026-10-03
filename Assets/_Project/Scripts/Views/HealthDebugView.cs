using ArenaSurvival.HealthSystem;
using UnityEngine;

namespace ArenaSurvival.Views
{
    /// <summary>
    /// Диагностическое представление здоровья.
    ///
    /// Подписывается на события Health и выводит
    /// состояние в Console.
    /// Позже его можно заменить полноценным HUD.
    /// </summary>
    public sealed class HealthDebugView :
        MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Health health;

        private void Awake()
        {
            if (health == null)
            {
                health =
                    GetComponent<Health>();
            }

            if (health == null)
            {
                Debug.LogError(
                    "HealthDebugView: Health не назначен.",
                    this);

                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (health == null)
            {
                return;
            }

            health.HealthChanged +=
                HandleHealthChanged;

            health.Died +=
                HandleDied;
        }

        private void OnDisable()
        {
            if (health == null)
            {
                return;
            }

            health.HealthChanged -=
                HandleHealthChanged;

            health.Died -=
                HandleDied;
        }

        private void HandleHealthChanged(
            int current,
            int maximum)
        {
            Debug.Log(
                $"{name}: Health {current}/{maximum}",
                this);
        }

        private void HandleDied()
        {
            Debug.Log(
                $"{name}: Died",
                this);
        }
    }
}
