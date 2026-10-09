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
            bool grounded = verticalVelocity <= 0f && ProbeGround();
            if (grounded)
            {
                lastGroundedTime = Time.time;
                verticalVelocity = groundedVerticalVelocity;
                Vector2 movementInput = inputReader.ReadMovement();
                UpdateHorizontalSpeed(movementInput);
                // Keep this world-space velocity unchanged until landing, even when looking around.
                horizontalVelocity = (transform.right * movementInput.x + transform.forward * movementInput.y)
                    * currentHorizontalSpeed;
            }
            if (Time.time - lastGroundedTime <= coyoteTime && Time.time - lastJumpPressedTime <= jumpBufferTime)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lastGroundedTime = lastJumpPressedTime = float.NegativeInfinity;
                grounded = false;
            }
            characterController.stepOffset = grounded ? groundedStepOffset : 0f;
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 totalVelocity =
                horizontalVelocity +
                Vector3.up * verticalVelocity;

            // Move ожидает перемещение за текущий кадр,
            // поэтому скорость умножается на Time.deltaTime.
            CollisionFlags flags = characterController.Move(
                totalVelocity * Time.deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
        }

        public void ResetVelocity()
        {
            currentHorizontalSpeed = 0f;
            verticalVelocity = 0f;
            horizontalVelocity = Vector3.zero;
            lastGroundedTime = lastJumpPressedTime = float.NegativeInfinity;
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

        private bool ProbeGround()
        {
            float radius = characterController.radius * 0.9f;
            Vector3 foot = transform.TransformPoint(characterController.center)
                - Vector3.up * (characterController.height * 0.5f);
            return Physics.SphereCast(foot + Vector3.up * (radius + 0.05f), radius, Vector3.down,
                out RaycastHit hit, groundProbeDistance + 0.05f, groundMask, QueryTriggerInteraction.Ignore)
                && Vector3.Angle(hit.normal, Vector3.up) <= characterController.slopeLimit;
        }
    }
}

