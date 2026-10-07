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
    [DefaultExecutionOrder(200)]
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

        [SerializeField]
        private Camera aimCamera;

        [SerializeField, Min(1f)]
        private float maximumAimDistance = 500f;

        [Header("Weapon Settings")]
        [SerializeField, Min(0f)]
        private float projectileSpeed = 25f;

        [SerializeField, Min(1)]
        private int damage = 25;

        [SerializeField, Min(0.01f)]
        private float fireInterval = 0.2f;

        private float nextAllowedFireTime;
        private int aimMask;

        private void Awake()
        {
            aimMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player", "Projectile", "UI");
            if (playerLook == null)
            {
                playerLook =
                    GetComponent<PlayerLook>();
            }
        }

        private void Start()
        {
            if (aimCamera == null)
                aimCamera = Camera.main;
            // PlayerSpawner assigns the scene pool after Instantiate and before Start.
            ValidateReferences();
        }

        // Cinemachine Brain runs at order 100: aim after it has applied this frame's look.
        private void LateUpdate()
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
                playerLook == null || aimCamera == null)
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
            Ray aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            Vector3 aimPoint = Physics.Raycast(aimRay, out RaycastHit hit, maximumAimDistance,
                aimMask, QueryTriggerInteraction.Ignore) ? hit.point : aimRay.GetPoint(maximumAimDistance);
            Vector3 direction = aimPoint - firePoint.position;

            // Do not fire backwards at a surface behind the muzzle, or spawn through nearby cover.
            if (Vector3.Dot(direction, aimRay.direction) <= 0f ||
                Physics.Linecast(aimCamera.transform.position, firePoint.position,
                    aimMask, QueryTriggerInteraction.Ignore))
                return;

            nextAllowedFireTime =
                Time.time + fireInterval;

            Vector3 velocity =
                direction.normalized *
                projectileSpeed;

            projectilePool.Spawn(
                firePoint.position,
                Quaternion.LookRotation(direction),
                velocity,
                damage);
        }

        public void SetProjectilePool(ProjectilePool pool)
        {
            projectilePool = pool;
        }

        private void ValidateReferences()
        {
            if (aimCamera == null)
                Debug.LogWarning("Weapon: assign Aim Camera or tag the scene camera MainCamera.", this);

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
