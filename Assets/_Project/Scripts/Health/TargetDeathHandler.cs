using UnityEngine;

namespace ArenaSurvival.HealthSystem
{
    [RequireComponent(typeof(Health))]
    public sealed class TargetDeathHandler : MonoBehaviour
    {
        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            health.Died -= HandleDied;
        }

        private void HandleDied()
        {
            gameObject.SetActive(false);
        }
    }
}
