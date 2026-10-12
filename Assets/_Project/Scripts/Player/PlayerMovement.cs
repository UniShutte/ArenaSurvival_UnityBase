using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// Перемещает Player через CharacterController.
    /// Компонент отвечает за ходьбу, ускорение, прыжок и гравитацию,
    /// но не вращает камеру: обзор живёт в PlayerLook.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]   // без контроллера компонент бессмысленен
    [RequireComponent(typeof(PlayerInputReader))]     // ввод читает отдельный компонент
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 5f;      // обычная скорость, м/с
        [SerializeField, Min(0f)] private float sprintSpeed = 8f;    // скорость с Shift
        [SerializeField, Min(0f)] private float acceleration = 20f;  // как быстро набирается скорость

        [Header("Vertical Movement")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.5f;   // высота прыжка в метрах
        [SerializeField] private float gravity = -20f;               // ускорение свободного падения
        [SerializeField] private float groundedVerticalVelocity = -2f; // лёгкое прижатие к полу на земле

        [SerializeField, Min(0f)] private float groundProbeDistance = 0.12f; // запас зонда пола
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;          // допуск прыжка после схода с края
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;     // буфер нажатия Space

        [Header("Crouch")]
        // Ссылка на узел CameraTarget внутри префаба Player. Именно на него,
        // а не на Main Camera — почему, объясняется ниже в этом шаге.
        [SerializeField] private Transform cameraTarget;

        [SerializeField, Min(0.5f)] private float crouchHeight = 1.1f;  // высота капсулы в приседе
        [SerializeField, Min(0f)] private float crouchSpeed = 2.5f;      // скорость передвижения в приседе

        private float standingHeight;            // исходная высота капсулы, обычно 1.8
        private Vector3 standingCenter;          // исходный центр капсулы
        private Vector3 standingCameraPosition;  // исходная локальная позиция CameraTarget
        private bool crouchReady;                // удалось ли запомнить исходные значения

        /// <summary>
        /// Истина, пока персонаж приседает. Нужна другим компонентам,
        /// например PlayerFootsteps из части 5_2.
        /// </summary>
        public bool IsCrouching { get; private set; }

        private CharacterController characterController;  // ссылка на контроллер того же объекта
        private PlayerInputReader inputReader;            // ссылка на чтение ввода

        private float currentHorizontalSpeed;   // текущая скорость по земле, нужна для толкания
        private float verticalVelocity;         // вертикальная скорость: вверх плюс, вниз минус
        private Vector3 horizontalVelocity;     // скорость полёта в МИРОВЫХ координатах
        private float lastGroundedTime = float.NegativeInfinity;   // когда последний раз стояли на земле
        private float lastJumpPressedTime = float.NegativeInfinity; // когда последний раз нажали Space
        private float groundedStepOffset;       // исходный stepOffset из Inspector
        private int groundMask;                 // маска слоёв, по которым ищем пол
        private bool touchedGroundLastMove;     // был ли контакт снизу в прошлом Move

        /// <summary>
        /// Текущая горизонтальная скорость нужна,
        /// например, для расчёта силы толкания.
        /// </summary>
        public float CurrentHorizontalSpeed =>
            currentHorizontalSpeed;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            inputReader = GetComponent<PlayerInputReader>();

            groundedStepOffset = characterController.stepOffset;

            groundMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player", "Projectile", "UI", "Damageable");

            // Запоминаем габариты стоящей капсулы: к ним будем возвращаться при вставании.
            standingHeight = characterController.height;
            standingCenter = characterController.center;

            if (cameraTarget == null)
            {
                // Ходьба и прыжок продолжат работать, отключится только приседание.
                Debug.LogError("PlayerMovement: назначьте Camera Target, приседание отключено.", this);
                return;
            }

            standingCameraPosition = cameraTarget.localPosition;
            crouchReady = true;
        }

        private void Update()
        {
            // Приседание выполняется ДО проверки пола: ProbeGround использует
            // текущую высоту капсулы, значит размеры должны быть уже обновлены.
            UpdateCrouch();

            if (inputReader.WasJumpPressed()) lastJumpPressedTime = Time.time;

            // contact — это ФАКТ контакта, а не «ровная ли поверхность».
            // characterController.isGrounded сообщает, касался ли контроллер земли
            // во время последнего Move. touchedGroundLastMove — то же самое,
            // но взятое из флагов предыдущего Move (CollisionFlags.Below).
            // Оба признака не зависят от угла склона, поэтому не теряются на уклонах.
            bool contact = verticalVelocity <= 0f
                && (characterController.isGrounded || touchedGroundLastMove);

            // walkable — годится ли поверхность для прыжка: только не круче slopeLimit.
            // Нормаль поверхности даёт отдельный зонд ProbeGround (шаг 9).
            bool walkable = contact
                && ProbeGround(out RaycastHit groundHit)
                && Vector3.Angle(groundHit.normal, Vector3.up) <= characterController.slopeLimit;

            if (contact)
            {
                // Ввод читается только при контакте с поверхностью.
                Vector2 movementInput = inputReader.ReadMovement();
                UpdateHorizontalSpeed(movementInput);

                // Мировая скорость собирается ОДИН раз из направлений тела и ввода.
                // transform.right и transform.forward берутся в момент нажатия, поэтому
                // дальнейший поворот мыши уже не влияет на этот вектор.
                horizontalVelocity = (transform.right * movementInput.x + transform.forward * movementInput.y)
                    * currentHorizontalSpeed;
            }

            // Обновляем «время последней земли» только на поверхности, годной для прыжка.
            // На слишком крутом склоне прыжок запрещён, но управление остаётся.
            if (walkable) lastGroundedTime = Time.time;

            // Прыжок разрешён, только если СВЕЖИ ОБА времени.
            // && означает «и то, и другое»: нажатие не старше буфера И земля не старше coyote.
            // Прыжок из приседа в первой версии запрещён.
            if (!IsCrouching 
                && Time.time - lastGroundedTime <= coyoteTime 
                && Time.time - lastJumpPressedTime <= jumpBufferTime)
            {
                // Классическая формула скорости для нужной высоты прыжка.
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

                // Оба времени сбрасываются, иначе одно нажатие даст несколько прыжков.
                lastGroundedTime = lastJumpPressedTime = float.NegativeInfinity;

                // В кадре прыжка персонаж уже не считается стоящим: stepOffset обнулится.
                contact = false;
            }

            // stepOffset не может быть больше высоты капсулы минус небольшой запас.
            // При высоте 1.1 исходные 0.3 допустимы, но после дальнейших правок
            // высоты это правило защитит от неверных значений.
            characterController.stepOffset = contact
                ? Mathf.Min(groundedStepOffset, characterController.height - 0.05f)
                : 0f;

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

            // После телепортации персонаж снова стоит в полный рост.
            IsCrouching = false;
            ApplyCapsuleHeight(standingHeight);

            if (crouchReady) cameraTarget.localPosition = standingCameraPosition;
        }


        private void UpdateHorizontalSpeed(
            Vector2 movementInput)
        {
            bool hasMovementInput =
                movementInput.sqrMagnitude > 0.001f;

            float targetSpeed = 0f;

            if (hasMovementInput)
            {
                // В приседе sprint запрещён.
                targetSpeed = IsCrouching
                    ? crouchSpeed
                    : inputReader.IsSprintHeld()
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
        /// Нормаль нужна, чтобы отличить пологий склон от стены.
        /// </summary>
        private bool ProbeGround(out RaycastHit hit)
        {
            // Радиус зонда меньше радиуса капсулы. Широкий SphereCast у края тонкой
            // плиты цепляется за боковую грань, её нормаль почти горизонтальна,
            // и поверхность ошибочно перестаёт считаться землёй.
            float radius = Mathf.Min(characterController.radius * 0.9f, 0.25f);

            // Нижняя точка капсулы: центр объекта минус половина высоты.
            Vector3 foot = transform.TransformPoint(characterController.center)
                - Vector3.up * (characterController.height * 0.5f);

            // Центр зонда поднимаем так, чтобы его низ был на 5 см выше ступней.
            Vector3 origin = foot + Vector3.up * (radius + 0.05f);

            // На склоне поверхность проходит ВЫШЕ оси капсулы, поэтому вертикальный
            // путь до неё равен r / cos(угол), а не r. Дополнительный запас считаем
            // по предельному уклону slopeLimit.
            float slopeReach = characterController.radius
                * (1f / Mathf.Cos(characterController.slopeLimit * Mathf.Deg2Rad) - 1f);
            float distance = 0.05f + groundProbeDistance + Mathf.Max(0f, slopeReach);

            return Physics.SphereCast(origin, radius, Vector3.down, out hit, distance, groundMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Проверяет, помещается ли стоящая капсула в текущем месте.
        /// </summary>
        private bool CanStand()
        {
            // Радиус и низ капсулы уменьшаем: иначе проверка коснётся пола
            // и примет его за потолок, и встать будет нельзя даже на открытом месте.
            float radius = characterController.radius * 0.9f;
            Vector3 foot = transform.TransformPoint(standingCenter)
                - Vector3.up * (standingHeight * 0.5f);
            Vector3 bottom = foot + Vector3.up * (radius + 0.05f);
            Vector3 top = foot + Vector3.up * (standingHeight - radius);

            // Маска groundMask не содержит слой Player, поэтому капсула не найдёт саму себя.
            return !Physics.CheckCapsule(bottom, top, radius, groundMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Меняет высоту капсулы так, чтобы ступни остались на месте.
        /// </summary>
        private void ApplyCapsuleHeight(float height)
        {
            // Ступни находятся на фиксированном расстоянии от корня объекта.
            // Центр капсулы должен быть выше ступней ровно на половину высоты,
            // иначе при уменьшении высоты капсула «провалится» в пол или поднимет игрока.
            float footY = standingCenter.y - standingHeight * 0.5f;
            characterController.height = height;
            characterController.center = new Vector3(standingCenter.x, footY + height * 0.5f, standingCenter.z);
        }

        private void UpdateCrouch()
        {
            if (!crouchReady) return;   // ссылка на камеру не назначена, приседание выключено

            // Нажатый Ctrl включает приседание всегда.
            // Отпущенный Ctrl только запрашивает вставание, и только если есть место.
            if (inputReader.IsCrouchHeld()) IsCrouching = true;
            else if (IsCrouching && CanStand()) IsCrouching = false;

            // Высоту капсулы меняем сразу: иначе зонд пола и CheckCapsule
            // будут работать со старыми размерами.
            float height = IsCrouching ? crouchHeight : standingHeight;
            ApplyCapsuleHeight(height);

            // Камеру опускаем плавно, на разность высот. MoveTowards идёт
            // со скоростью 4 м/с, поэтому переход занимает доли секунды.
            Vector3 cameraPosition = standingCameraPosition - Vector3.up * (standingHeight - height);
            cameraTarget.localPosition = Vector3.MoveTowards(cameraTarget.localPosition, cameraPosition, 4f * Time.deltaTime);
        }

    }
}
