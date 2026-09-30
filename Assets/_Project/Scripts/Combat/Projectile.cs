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
    [RequireComponent(typeof(Collider))]
    public sealed class Projectile :
        MonoBehaviour
    {
        [Header("Lifetime")]
        [SerializeField, Min(0.1f)]
        private float maximumLifetime = 3f;

        private Rigidbody body;
        private IObjectPool<Projectile> ownerPool;

        private float remainingLifetime;
        private int damage;
        private bool isReleased;

        private void Awake()
        {
            body =
                GetComponent<Rigidbody>();
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

        private void OnTriggerEnter(
            Collider other)
        {
            if (isReleased)
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