using UnityEngine;
using UnityEngine.Pool;

namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Создаёт и повторно использует снаряды.
    ///
    /// Пул уменьшает количество частых вызовов
    /// Instantiate и Destroy.
    /// </summary>
    public sealed class ProjectilePool : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Projectile projectilePrefab;

        [SerializeField]
        private Transform projectileContainer;

        [Header("Pool Settings")]
        [SerializeField, Min(1)]
        private int defaultCapacity = 20;

        [SerializeField, Min(1)]
        private int maximumSize = 100;

        [SerializeField]
        private bool logPoolOperations;

        private IObjectPool<Projectile> pool;

        private int totalCreated;
        private int activeCount;

        public int TotalCreated => totalCreated;
        public int ActiveCount => activeCount;

        private void Awake()
        {
            if (projectilePrefab == null)
            {
                Debug.LogError(
                    "ProjectilePool: Projectile Prefab не назначен.",
                    this);

                enabled = false;
                return;
            }

            pool = new ObjectPool<Projectile>(
                createFunc: CreateProjectile,
                actionOnGet: OnTakeFromPool,
                actionOnRelease: OnReturnedToPool,
                actionOnDestroy: OnDestroyPooledProjectile,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maximumSize);
        }

        public Projectile Spawn(
            Vector3 position,
            Quaternion rotation,
            Vector3 velocity,
            int damage)
        {
            if (pool == null)
            {
                return null;
            }

            Projectile projectile =
                pool.Get();

            activeCount++;

            projectile.Activate(
                position,
                rotation,
                velocity,
                damage);

            if (logPoolOperations)
            {
                Debug.Log(
                    $"ProjectilePool: Get {projectile.GetInstanceID()}, " +
                    $"created={totalCreated}, active={activeCount}",
                    this);
            }

            return projectile;
        }

        private Projectile CreateProjectile()
        {
            Projectile projectile =
                Instantiate(
                    projectilePrefab,
                    projectileContainer);

            projectile.Initialize(
                pool);

            projectile.gameObject.SetActive(
                false);

            totalCreated++;

            return projectile;
        }

        private static void OnTakeFromPool(Projectile projectile)
        {
            // Активация выполняется после установки
            // позиции, скорости и урона в Spawn.
        }

        private void OnReturnedToPool(
            Projectile projectile)
        {
            activeCount = Mathf.Max(
                0,
                activeCount - 1);

            projectile.gameObject.SetActive(
                false);

            if (logPoolOperations)
            {
                Debug.Log(
                    $"ProjectilePool: Release {projectile.GetInstanceID()}, " +
                    $"created={totalCreated}, active={activeCount}",
                    this);
            }
        }

        private static void OnDestroyPooledProjectile(
            Projectile projectile)
        {
            Destroy(
                projectile.gameObject);
        }
    }
}