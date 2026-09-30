using ArenaSurvival.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Запрашивает снаряды из ProjectilePool.
    ///
    /// Weapon не создаёт снаряды через Instantiate
    /// и не уничтожает их через Destroy.
    /// </summary>
    public sealed class Weapon :
        MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private ProjectilePool projectilePool;

        [SerializeField]
        private Transform firePoint;

        [SerializeField]
        private PlayerLook playerLook;

        [Header("Weapon Settings")]
        [SerializeField, Min(0f)]
        private float projectileSpeed = 25f;

        [SerializeField, Min(1)]
        private int damage = 25;

        [SerializeField, Min(0.01f)]
        private float fireInterval = 0.2f;

        private float nextAllowedFireTime;

        private void Awake()
        {
            if (playerLook == null)
            {
                playerLook =
                    GetComponent<PlayerLook>();
            }

            ValidateReferences();
        }

        private void Update()
        {
            if (!CanFire())
            {
                return;
            }

            Mouse mouse =
                Mouse.current;

            if (mouse == null ||
                !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            Fire();
        }

        private bool CanFire()
        {
            if (projectilePool == null ||
                firePoint == null ||
                playerLook == null)
            {
                return false;
            }

            if (!playerLook.IsCursorLocked)
            {
                return false;
            }

            return Time.time >=
                   nextAllowedFireTime;
        }

        private void Fire()
        {
            nextAllowedFireTime =
                Time.time + fireInterval;

            Vector3 velocity =
                firePoint.forward *
                projectileSpeed;

            projectilePool.Spawn(
                firePoint.position,
                firePoint.rotation,
                velocity,
                damage);
        }

        public void SetProjectilePool(ProjectilePool pool)
        {
            projectilePool = pool;
        }

        private void ValidateReferences()
        {
            if (projectilePool == null)
            {
                Debug.LogWarning(
                    "Weapon: Projectile Pool не назначен.",
                    this);
            }

            if (firePoint == null)
            {
                Debug.LogWarning(
                    "Weapon: Fire Point не назначен.",
                    this);
            }

            if (playerLook == null)
            {
                Debug.LogWarning(
                    "Weapon: Player Look не найден.",
                    this);
            }
        }

    }
}