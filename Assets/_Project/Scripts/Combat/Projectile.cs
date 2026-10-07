using UnityEngine;
using UnityEngine.Pool;

namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Повторно используемый физический снаряд.
    ///
    /// Projectile не уничтожает себя через Destroy.
    /// После столкновения или окончания времени жизни
    /// он возвращается в Object Pool.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class Projectile :
        MonoBehaviour
    {
        [Header("Lifetime")]
        [SerializeField, Min(0.1f)]
        private float maximumLifetime = 3f;

        private Rigidbody body;
        private SphereCollider sphere;
        private int collisionMask;
        private IObjectPool<Projectile> ownerPool;

        private float remainingLifetime;
        private int damage;
        private bool isReleased;

        private void Awake()
        {
            body =
                GetComponent<Rigidbody>();
            sphere = GetComponent<SphereCollider>();
            for (int layer = 0; layer < 32; layer++)
                if (!Physics.GetIgnoreLayerCollision(gameObject.layer, layer))
                    collisionMask |= 1 << layer;
        }

        /// <summary>
        /// Вызывается один раз после создания
        /// экземпляра пулом.
        /// </summary>
        public void Initialize(
            IObjectPool<Projectile> pool)
        {
            ownerPool = pool;
        }

        /// <summary>
        /// Подготавливает объект к очередному выстрелу.
        /// </summary>
        public void Activate(
            Vector3 position,
            Quaternion rotation,
            Vector3 velocity,
            int damageAmount)
        {
            isReleased = false;
            damage = damageAmount;
            remainingLifetime =
                maximumLifetime;

            transform.SetPositionAndRotation(
                position,
                rotation);

            body.linearVelocity =
                Vector3.zero;

            body.angularVelocity =
                Vector3.zero;

            gameObject.SetActive(true);

            body.linearVelocity =
                velocity;
        }

        private void Update()
        {
            remainingLifetime -=
                Time.deltaTime;

            if (remainingLifetime <= 0f)
            {
                Release();
            }
        }

        private void FixedUpdate()
        {
            if (isReleased) return;
            Vector3 step = body.linearVelocity * Time.fixedDeltaTime;
            if (step.sqrMagnitude == 0f) return;
            Vector3 scale = transform.lossyScale;
            float radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            Vector3 center = body.position + body.rotation * Vector3.Scale(sphere.center, scale);
            // Trigger contacts alone can miss thin obstacles between physics steps.
            if (Physics.SphereCast(center, radius, step.normalized,
                    out RaycastHit hit, step.magnitude, collisionMask, QueryTriggerInteraction.Ignore))
                OnTriggerEnter(hit.collider);
        }

        private void OnTriggerEnter(
            Collider other)
        {
            if (isReleased || other.isTrigger)
            {
                return;
            }

            if (other.TryGetComponent(
                    out IDamageable damageable))
            {
                damageable.ApplyDamage(
                    damage);
            }

            // Снаряд возвращается в пул после
            // столкновения с целью или окружением.
            Release();
        }

        private void Release()
        {
            if (isReleased)
            {
                return;
            }

            isReleased = true;

            body.linearVelocity =
                Vector3.zero;

            body.angularVelocity =
                Vector3.zero;

            ownerPool?.Release(this);
        }
    }
}
