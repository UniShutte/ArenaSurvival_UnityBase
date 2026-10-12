using System;
using ArenaSurvival.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Состояние одного оружия.
    /// Обычный сериализуемый класс: магазин и таймер принадлежат экземпляру игрока,
    /// поэтому помечены [NonSerialized] и не сохраняются в префаб.
    /// </summary>
    [Serializable]
    public sealed class WeaponSlot
    {
        public WeaponData data;        // характеристики из ассета
        public ProjectilePool pool;    // свой пул снарядов для этого оружия
        public GameObject view;        // визуальная ветка, которую включаем и выключаем
        public Transform muzzle;       // точка вылета снаряда
        [NonSerialized] public int ammo;            // текущий магазин
        [NonSerialized] public float nextShotTime;  // время следующего разрешённого выстрела
    }

    /// <summary>
    /// Три слота оружия: выбор клавишами 1, 2, 3, перезарядка по R.
    /// Weapon не создаёт снаряды: он просит их у пула и сообщает о выстреле событиями.
    /// </summary>
    [DefaultExecutionOrder(200)]   // выстрел должен выполняться после Cinemachine Brain (порядок 100)
    public sealed class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponSlot[] slots;       // ровно три элемента
        [SerializeField] private PlayerLook playerLook;    // нужен, чтобы знать, захвачен ли курсор
        [SerializeField] private Camera aimCamera;         // камера, из центра которой считаем прицел
        [SerializeField, Min(1)] private float maximumAimDistance = 500;  // предел луча прицела

        private int current;            // индекс выбранного слота
        private int aimMask;            // слои, которые учитывает луч прицела
        private float reloadEnd;        // время окончания перезарядки
        private bool selectionApplied;  // отличает первое подключение от повторного выбора

        public bool IsReloading { get; private set; }   // читают представление и HUD
        public bool IsAiming { get; private set; }      // читают представление и камера прицеливания
        public int CurrentIndex => current;
        public int Ammo => slots[current].ammo;
        public WeaponData CurrentData => slots[current].data;

        public event Action Changed;            // изменился магазин, выбор или перезарядка
        public event Action<int> Fired;         // выстрел: индекс слота
        public event Action<int> ReloadStarted; // начало перезарядки: индекс слота

        private void Start()
        {
            // Ссылки ищем сами, если они не назначены в Inspector.
            if (playerLook == null) playerLook = GetComponent<PlayerLook>();
            if (aimCamera == null) aimCamera = Camera.main;

            // Проверяем конфигурацию до начала работы: лучше ошибка в Console,
            // чем молча неработающая стрельба.
            if (slots == null || slots.Length != 3 || playerLook == null || aimCamera == null)
            { Debug.LogError("Assign three weapon slots, look and camera.", this); enabled = false; return; }

            foreach (WeaponSlot slot in slots)
            {
                if (slot == null || slot.data == null || slot.pool == null || slot.view == null || slot.muzzle == null
                    || slot.data.roundsPerMinute <= 0 || slot.data.magazineSize < 1 || slot.data.pellets < 1)
                { Debug.LogError("Invalid weapon slot.", this); enabled = false; return; }

                slot.ammo = slot.data.magazineSize;   // каждый запуск начинается с полного магазина
            }

            // Луч прицела не должен попадать в собственное оружие, в летящий снаряд и в интерфейс.
            aimMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player", "Projectile", "UI");

            Select(0);   // стартовое оружие — пистолет
        }

        private void Update()
        {
            // При паузе (timeScale = 0) и при свободном курсоре ввод оружия не читается.
            if (Time.timeScale == 0 || !playerLook.IsCursorLocked) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // Цифры выбирают слот. else if — за кадр обрабатывается ровно одно нажатие.
                if (keyboard.digit1Key.wasPressedThisFrame) Select(0);
                else if (keyboard.digit2Key.wasPressedThisFrame) Select(1);
                else if (keyboard.digit3Key.wasPressedThisFrame) Select(2);
            }

            // Перезарядка завершается по времени: это единственный источник истины
            // по патронам. Анимация перезарядки патроны не начисляет.
            if (IsReloading && Time.time >= reloadEnd)
            {
                slots[current].ammo = CurrentData.magazineSize;
                IsReloading = false;
                Changed?.Invoke();
            }

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) BeginReload();

            // Прицеливание — это удержание правой кнопки мыши, но не во время перезарядки.
            IsAiming = !IsReloading && Mouse.current != null && Mouse.current.rightButton.isPressed;
        }

        // Cinemachine Brain обновляет камеру с порядком 100, поэтому выстрел
        // выполняется в LateUpdate: прицел считается по уже применённому положению.
        private void LateUpdate()
        {
            if (Time.timeScale == 0 || !playerLook.IsCursorLocked || IsReloading) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            // Автоматический режим стреляет при удержании, одиночный — по новому нажатию.
            bool requested = CurrentData.automatic ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;

            // Пустой магазин не создаёт снаряд и не вызывает событие Fired.
            if (requested && Ammo > 0 && Time.time >= slots[current].nextShotTime) Fire();
        }

        private void Select(int index)
        {
            // Повторное нажатие клавиши текущего оружия не должно отменять перезарядку
            // и не должно сбрасывать таймер выстрела.
            if (selectionApplied && index == current) return;
            selectionApplied = true;

            IsReloading = false;   // смена оружия отменяет перезарядку
            IsAiming = false;
            current = index;

            // Включаем только выбранную модель. Пулы при этом остаются активными,
            // иначе уже летящие снаряды исчезли бы вместе с родителем.
            for (int i = 0; i < slots.Length; i++) slots[i].view.SetActive(i == current);

            Changed?.Invoke();
        }

        private void BeginReload()
        {
            // Полный магазин не перезаряжается: иначе R запускал бы звук и анимацию впустую.
            if (IsReloading || Ammo == CurrentData.magazineSize) return;

            IsReloading = true;
            IsAiming = false;
            reloadEnd = Time.time + CurrentData.reloadSeconds;
            ReloadStarted?.Invoke(current);
            Changed?.Invoke();
        }

        private void Fire()
        {
            WeaponSlot slot = slots[current];
            WeaponData data = slot.data;

            // Луч из центра экрана определяет, КУДА целится игрок.
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            Vector3 point = Physics.Raycast(ray, out RaycastHit hit, maximumAimDistance,
                aimMask, QueryTriggerInteraction.Ignore) ? hit.point : ray.GetPoint(maximumAimDistance);

            // Снаряд летит от дула к найденной точке, а не параллельно взгляду.
            Vector3 direction = point - slot.muzzle.position;

            // Не стреляем назад и не пробиваем укрытие, закрывающее ствол.
            if (Vector3.Dot(direction, ray.direction) <= 0 || Physics.Linecast(aimCamera.transform.position,
                slot.muzzle.position, aimMask, QueryTriggerInteraction.Ignore)) return;

            Quaternion basis = Quaternion.LookRotation(direction);

            // Патрон расходуется один раз на выстрел, ДО цикла дробин.
            slot.ammo--;
            slot.nextShotTime = Time.time + 60f / data.roundsPerMinute;  // 60f, а не 60!

            for (int i = 0; i < data.pellets; i++)
            {
                // Новое случайное смещение на каждую дробину: одно смещение перед циклом
                // отправило бы все дробинки по одной траектории.
                Vector2 spread = UnityEngine.Random.insideUnitCircle * Mathf.Tan(data.spreadDegrees * Mathf.Deg2Rad);
                Vector3 flight = (basis * new Vector3(spread.x, spread.y, 1)).normalized;

                slot.pool.Spawn(slot.muzzle.position, Quaternion.LookRotation(flight), flight * data.projectileSpeed, data.damage);
            }

            // События сообщают о факте. Подписчики (анимация, звук, HUD) появятся позже.
            Fired?.Invoke(current);
            Changed?.Invoke();
        }

        private void OnDisable()
        {
            // Выключенное оружие не должно оставаться «перезаряжающимся».
            IsReloading = false;
            IsAiming = false;
        }
    }
}