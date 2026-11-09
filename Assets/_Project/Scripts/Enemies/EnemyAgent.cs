using System;
using ArenaSurvival.HealthSystem;
using ArenaSurvival.Player;
using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>Moves one rolling enemy and publishes its death, not the score.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider), typeof(Health))]
    public sealed class EnemyAgent : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float aggroRadius = 8f;
        [SerializeField, Min(0.1f)] private float wanderRadius = 6f;
        [SerializeField, Min(0f)] private float acceleration = 8f;
        [SerializeField, Min(0f)] private float maximumSpeed = 3f;
        [SerializeField, Min(1)] private int contactDamage = 10;
        [SerializeField, Min(0.1f)] private float contactInterval = 1f;
        [SerializeField, Min(0f)] private float knockbackSpeed = 2f;
        [SerializeField, Min(0)] private int reward = 100;

        private Rigidbody body;
        private SphereCollider shell;
        private Health health;
        private Transform player;
        private Vector3 home;
        private Vector3 destination;
        private float chooseAt;
        private float attackAt;
        private float repelledUntil;
        private bool dead;

        public bool IsChasing { get; private set; }
        public int Reward => reward;
        public event Action<EnemyAgent> Defeated;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            shell = GetComponent<SphereCollider>();
            health = GetComponent<Health>();
            home = body.position;
        }

        private void OnEnable() { health.Died += Die; }
        private void OnDisable() { health.Died -= Die; }
        private void Start() { ChooseDestination(); }

        /// <summary>Applies horizontal acceleration in the physics loop.</summary>
        private void FixedUpdate()
        {
            if (dead || Time.timeScale == 0f) return;
            if (player == null)
            {
                // shortcut: one player per scene; use explicit targets for multiplayer.
                PlayerMovement found = FindFirstObjectByType<PlayerMovement>();
                if (found == null) return;
                player = found.transform;
            }

            UpdateAggro(Flat(player.position - body.position).magnitude);
            if (Time.time < repelledUntil) return;
            if (!IsChasing && (Time.time >= chooseAt ||
                Flat(destination - body.position).sqrMagnitude < 0.36f))
                ChooseDestination();

            Vector3 target = IsChasing ? player.position : destination;
            Vector3 direction = Flat(target - body.position).normalized;
            Vector3 horizontal = Flat(body.linearVelocity);
            Vector3 desired = Vector3.ClampMagnitude(
                horizontal + direction * acceleration * Time.fixedDeltaTime, maximumSpeed);
            body.AddForce(desired - horizontal, ForceMode.VelocityChange);
        }

        /// <summary>Keeps pursuit between the two distance thresholds.</summary>
        private void UpdateAggro(float distance)
        {
            if (!IsChasing && distance <= aggroRadius) IsChasing = true;
            else if (IsChasing && distance > aggroRadius * 2f)
            {
                IsChasing = false;
                ChooseDestination();
            }
        }

        private void ChooseDestination()
        {
            Vector2 random = UnityEngine.Random.insideUnitCircle * wanderRadius;
            destination = home + new Vector3(random.x, 0f, random.y);
            chooseAt = Time.time + UnityEngine.Random.Range(2f, 4f);
        }

        /// <summary>The child sensor reports proximity; the shell must also touch the player.</summary>
        private void OnTriggerStay(Collider other)
        {
            if (!isActiveAndEnabled || dead || Time.timeScale == 0f || Time.time < attackAt) return;
            PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
            if (movement == null || !movement.TryGetComponent(out Health playerHealth) ||
                playerHealth.CurrentHealth <= 0) return;
            // The prefab has uniform scale 1 and its shell is centred at the origin.
            if (Vector3.Distance(body.position, other.ClosestPoint(body.position)) > shell.radius + 0.05f)
                return;

            attackAt = Time.time + contactInterval;
            playerHealth.ApplyDamage(contactDamage);
            Vector3 away = Flat(body.position - movement.transform.position);
            if (away.sqrMagnitude < 0.001f) away = -Flat(movement.transform.forward);
            body.linearVelocity = Vector3.up * body.linearVelocity.y;
            body.AddForce(away.normalized * knockbackSpeed, ForceMode.VelocityChange);
            repelledUntil = Time.time + 0.45f;
        }

        private void Die()
        {
            if (dead) return;
            dead = true;
            Defeated?.Invoke(this);
            // shortcut: enemies are recreated by scene reload; add a reset before pooling them.
            gameObject.SetActive(false);
        }

        private static Vector3 Flat(Vector3 value) => Vector3.ProjectOnPlane(value, Vector3.up);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aggroRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, aggroRadius * 2f);
        }
    }
}