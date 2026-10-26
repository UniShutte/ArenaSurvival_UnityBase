using UnityEngine;
using UnityEngine.Pool;
using System;                      // Action и событие HitConfirmed
using ArenaSurvival.HealthSystem;  // Health

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

        [Header("Impact Audio")]
        [SerializeField] private AudioSource impactAudioPrefab; // prefab PF_ImpactAudio
        [SerializeField] private AudioClip enemyHitClip;        // звук попадания по цели
        [SerializeField] private AudioClip stoneHitClip;        // звук рикошета по камню

        private Rigidbody body;
        private SphereCollider sphere;
        private int collisionMask;
        private IObjectPool<Projectile> ownerPool;

        private float remainingLifetime;
        private int damage;
        private bool isReleased;

        /// <summary> Подтверждение реального уменьшения здоровья. </summary>
        public static event Action<Health> HitConfirmed;

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
                ResolveHit(hit.collider, hit.point); 
        }

        private void OnTriggerEnter(Collider other)
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

        /// <summary>
        /// Единственный путь обработки попадания.
        /// </summary>
        private void ResolveHit(Collider other, Vector3 point)
        {
            // 1. Повторный вызов после возврата в пул и триггеры нас не интересуют.
            if (isReleased || other == null || other.isTrigger)
            {
                return;
            }

            // 2. Health имеет приоритет над StoneSurface: каменная сфера-цель
            //    должна звучать как попадание, а не как промах о декорацию.
            Health health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                // 3. Урон наносим только живой цели.
                if (health.CurrentHealth > 0)
                {
                    int before = health.CurrentHealth;
                    health.ApplyDamage(damage);
                    // 4. Звук и подтверждение — только если HP действительно уменьшилось.
                    if (health.CurrentHealth < before)
                    {
                        PlayImpact(enemyHitClip, point);
                        HitConfirmed?.Invoke(health);
                    }
                }

                // Попадание по уже мёртвой цели нового подтверждения не даёт.
                Release();
                return;
            }

            // 5. Цели нет, но есть маркер камня: играем рикошет.
            if (other.GetComponentInParent<StoneSurface>() != null)
            {
                PlayImpact(stoneHitClip, point);
                Release();
                return;
            }

            // 6. Поддержка другого IDamageable. Health обработан выше,
            //    поэтому урон не удваивается.
            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.ApplyDamage(damage);
            }

            // 7. Возврат в пул. Защита isReleased обязательна: SphereCast
            //    и trigger могут сообщить об одном столкновении.
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


        /// <summary>
        /// Временный источник звука в точке попадания: AudioSource самого снаряда
        /// обрывается, когда пул выключает объект.
        /// </summary>
        private void PlayImpact(AudioClip clip, Vector3 point)
        {
            if (clip == null || impactAudioPrefab == null) return; // нечего играть
                                                                   // Отдельный объект живёт независимо от снаряда и его пула.
            AudioSource source = Instantiate(impactAudioPrefab, point, Quaternion.identity);
            source.clip = clip;
            source.Play();
            // Удаляем ровно после окончания клипа, чтобы звук не оборвался.
            Destroy(source.gameObject, clip.length + 0.1f);
        }
    }


}
