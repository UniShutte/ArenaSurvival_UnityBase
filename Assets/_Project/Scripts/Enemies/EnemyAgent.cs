using System;
using ArenaSurvival.HealthSystem;
using ArenaSurvival.Player;
using UnityEngine;

namespace ArenaSurvival.Enemies
{
    [RequireComponent(typeof(Rigidbody), typeof(Health))]
    public sealed class EnemyAgent : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float aggroRadius = 8;
        [SerializeField, Min(0.1f)] private float wanderRadius = 6;
        [SerializeField, Min(0)] private float acceleration = 8;
        [SerializeField, Min(0)] private float maximumSpeed = 3;
        [SerializeField, Min(1)] private int contactDamage = 10;
        [SerializeField, Min(0.1f)] private float contactInterval = 1;
        [SerializeField, Min(0)] private float knockbackSpeed = 2;
        [SerializeField, Min(0)] private int reward = 100;
        private Rigidbody body;
        private Health health;
        private Transform player;
        private Vector3 home;
        private Vector3 destination;
        private float chooseAt;
        private float attackAt;
        private float repelledUntil;
        private bool chasing;
        private bool dead;
        public bool IsChasing => chasing;
        public static event Action<int> Killed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            health = GetComponent<Health>();
            home = transform.position;
        }
        private void OnEnable() { health.Died += Die; }
        private void OnDisable() { health.Died -= Die; }
        private void Start() { ChooseDestination(); }

        private void FixedUpdate()
        {
            if (dead) return;
            if (player == null)
            {
                PlayerMovement found = FindFirstObjectByType<PlayerMovement>();
                if (found == null) return;
                player = found.transform;
            }
            float distance = Vector3.Distance(body.position, player.position);
            if (!chasing && distance <= aggroRadius) chasing = true;
            else if (chasing && distance > aggroRadius * 2)
            { chasing = false; ChooseDestination(); }
            if (Time.time < repelledUntil) return;
            if (!chasing && (Time.time >= chooseAt || Flat(destination - body.position).magnitude < 0.6f))
                ChooseDestination();
            Vector3 direction = Flat((chasing ? player.position : destination) - body.position).normalized;
            body.AddForce(direction * acceleration, ForceMode.Acceleration);
            Vector3 horizontal = Vector3.ClampMagnitude(Flat(body.linearVelocity), maximumSpeed);
            body.linearVelocity = horizontal + Vector3.up * body.linearVelocity.y;
        }

        private void ChooseDestination()
        {
            Vector2 random = UnityEngine.Random.insideUnitCircle * wanderRadius;
            destination = home + new Vector3(random.x, 0, random.y);
            chooseAt = Time.time + UnityEngine.Random.Range(2f, 4f);
        }

        private void OnTriggerStay(Collider other)
        {
            if (dead || Time.time < attackAt || !other.TryGetComponent(out PlayerMovement movement)) return;
            if (!other.TryGetComponent(out Health playerHealth) || playerHealth.CurrentHealth <= 0) return;
            // The larger sensor detects proximity; actual damage requires contact with the body.
            if (Vector3.Distance(body.position, other.ClosestPoint(body.position)) > 0.55f) return;
            attackAt = Time.time + contactInterval;
            playerHealth.ApplyDamage(contactDamage);
            Vector3 away = Flat(body.position - movement.transform.position);
            if (away.sqrMagnitude < 0.001f) away = -movement.transform.forward;
            body.linearVelocity = Vector3.up * body.linearVelocity.y;
            body.AddForce(away.normalized * knockbackSpeed, ForceMode.VelocityChange);
            repelledUntil = Time.time + 0.45f;
        }

        private void Die()
        {
            if (dead) return;
            dead = true;
            Killed?.Invoke(reward);
            gameObject.SetActive(false);
        }
        private static Vector3 Flat(Vector3 value) => Vector3.ProjectOnPlane(value, Vector3.up);
    }
}