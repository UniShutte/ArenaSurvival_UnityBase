using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// Перемещает Player через CharacterController.
    ///
    /// Компонент отвечает за ходьбу, ускорение,
    /// прыжок и гравитацию, но не вращает камеру.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)]
        private float walkSpeed = 5f;

        [SerializeField, Min(0f)]
        private float sprintSpeed = 8f;

        [SerializeField, Min(0f)]
        private float acceleration = 20f;

        [Header("Vertical Movement")]
        [SerializeField, Min(0f)]
        private float jumpHeight = 1.5f;

        [SerializeField]
        private float gravity = -20f;

        [SerializeField]
        private float groundedVerticalVelocity = -2f;

        [SerializeField, Min(0f)] private float groundProbeDistance = 0.12f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        private CharacterController characterController;
        private PlayerInputReader inputReader;

        private float currentHorizontalSpeed;
        private float verticalVelocity;
        private Vector3 horizontalVelocity;
        private float lastGroundedTime = float.NegativeInfinity;
        private float lastJumpPressedTime = float.NegativeInfinity;
        private float groundedStepOffset;
        private int groundMask;

        // Контакт снизу из предыдущего Move: не зависит от угла склона
        // и не теряется, пока контроллер физически стоит на поверхности.
        private bool touchedGroundLastMove;

        /// <summary>
        /// Текущая горизонтальная скорость нужна,
        /// например, для расчёта силы толкания.
        /// </summary>
        public float CurrentHorizontalSpeed =>
            currentHorizontalSpeed;

        private void Awake()
        {
            characterController =
                GetComponent<CharacterController>();

            inputReader =
                GetComponent<PlayerInputReader>();
            groundedStepOffset = characterController.stepOffset;
            groundMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player", "Projectile", "UI", "Damageable");
        }

        private void Update()
        {
            if (inputReader.WasJumpPressed()) lastJumpPressedTime = Time.time;

            // Контакт берётся у самого контроллера. Геометрический зонд
            // не является признаком контакта: на склоне он может не достать
            // поверхность, и тогда стоящий персонаж считался бы летящим.
            bool contact = verticalVelocity <= 0f
                && (characterController.isGrounded || touchedGroundLastMove);

            // Землёй для прыжка считается только поверхность не круче slopeLimit.
            bool walkable = contact
                && ProbeGround(out RaycastHit groundHit)
                && Vector3.Angle(groundHit.normal, Vector3.up) <= characterController.slopeLimit;

            if (contact)
            {
                // Прижимаем к поверхности. Без этого гравитация накапливается
                // каждый кадр, и контроллер разгоняется вниз по склону:
                // визуально это выглядит как неуправляемое скольжение.
                verticalVelocity = groundedVerticalVelocity;

                // Управление доступно при любом контакте, а не только на пологом
                // склоне: на слишком крутой поверхности игрок может уйти в сторону,
                // вместо того чтобы «залипнуть». В полёте контакта нет, поэтому
                // правило «в воздухе направление не меняется» сохраняется.
                Vector2 movementInput = inputReader.ReadMovement();
                UpdateHorizontalSpeed(movementInput);
                // Keep this world-space velocity unchanged until landing, even when looking around.
                horizontalVelocity = (transform.right * movementInput.x + transform.forward * movementInput.y)
                    * currentHorizontalSpeed;
            }

            if (walkable) lastGroundedTime = Time.time;

            if (Time.time - lastGroundedTime <= coyoteTime && Time.time - lastJumpPressedTime <= jumpBufferTime)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lastGroundedTime = lastJumpPressedTime = float.NegativeInfinity;
                // В кадре прыжка stepOffset обнуляется, как при обычном отрыве.
                contact = false;
            }

            characterController.stepOffset = contact ? groundedStepOffset : 0f;
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 totalVelocity =
                horizontalVelocity +
                Vector3.up * verticalVelocity;

            // Move ожидает перемещение за текущий кадр,
            // поэтому скорость умножается на Time.deltaTime.
            CollisionFlags flags = characterController.Move(
                totalVelocity * Time.deltaTime);
            touchedGroundLastMove = (flags & CollisionFlags.Below) != 0;
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
        }

        public void ResetVelocity()
        {
            currentHorizontalSpeed = 0f;
            verticalVelocity = 0f;
            horizontalVelocity = Vector3.zero;
            lastGroundedTime = lastJumpPressedTime = float.NegativeInfinity;
            touchedGroundLastMove = false;
        }

        private void UpdateHorizontalSpeed(
            Vector2 movementInput)
        {
            bool hasMovementInput =
                movementInput.sqrMagnitude > 0.001f;

            float targetSpeed = 0f;

            if (hasMovementInput)
            {
                targetSpeed = inputReader.IsSprintHeld()
                    ? sprintSpeed
                    : walkSpeed;
            }

            // MoveTowards создаёт плавный разгон и остановку.
            currentHorizontalSpeed = Mathf.MoveTowards(
                currentHorizontalSpeed,
                targetSpeed,
                acceleration * Time.deltaTime);
        }

        /// <summary>
        /// Ищет поверхность под капсулой и возвращает её нормаль.
        ///
        /// Радиус зонда меньше радиуса капсулы: широкий SphereCast
        /// цепляется за боковые грани тонких плит и за края.
        /// </summary>
        private bool ProbeGround(out RaycastHit hit)
        {
            float radius = Mathf.Min(characterController.radius * 0.9f, 0.25f);
            Vector3 foot = transform.TransformPoint(characterController.center)
                - Vector3.up * (characterController.height * 0.5f);
            Vector3 origin = foot + Vector3.up * (radius + 0.05f);
            // На склоне поверхность проходит выше оси капсулы: вертикальный путь
            // до неё растёт как r / cos(угол). С постоянным запасом 0.12 м пол
            // находился только до 36°, хотя slopeLimit равен 45°.
            float slopeReach = characterController.radius
                * (1f / Mathf.Cos(characterController.slopeLimit * Mathf.Deg2Rad) - 1f);
            float distance = 0.05f + groundProbeDistance + Mathf.Max(0f, slopeReach);
            return Physics.SphereCast(origin, radius, Vector3.down, out hit, distance, groundMask, QueryTriggerInteraction.Ignore);
        }
    }
}
