# Laboratory_3

## Завершение 3D-окружения и создание системы стрельбы на основе C#-событий и Object Pooling

## 1. Общая информация

**Дисциплина:** «Основы работы с Unity».
**Максимальная оценка:** 20 баллов.
**Формат выполнения:** индивидуально.

**Уточнение к выдаваемому проекту (3 октября 2026 года):** Unity 6000.3.24f1,
сцена `Assets/_Project/Scenes/Arena_01.unity`. Исправления управления, коллайдера
цели и возврата после падения уже входят в стартовую ветку преподавателя.
Не заменяйте проверенные скрипты проекта более ранними листингами из методички.
Листинги ниже объясняют устройство систем; актуальный исполняемый код находится
в `Assets/_Project/Scripts`. Cinemachine подключается в ЛР4, а не в ЛР3.
В ЛР3 ввод читается через `Keyboard.current` / `Mouse.current`.
Git-теги не требуются: результат фиксируется коммитами в отдельной ветке.

**Предыдущие работы:** `Laboratory_1`, часть `Laboratory_2`.

**Итоговый результат:** расширенная версия проекта `Arena Survival`, содержащая оформленное 3D-окружение на основе Terrain и ассетов из Asset Store, корректные коллайдеры, систему здоровья и урона, оружие, повторно используемые снаряды и диагностическое представление событий здоровья.

В ходе этой работы студенты завершают `Laboratory_2`, и одновременно подключают систему взаимодействия объектов через интерфейсы, C#-события и Object Pooling.

Основные C#-скрипты предоставляются в готовом виде. Самостоятельно писать всю систему с нуля не требуется.

Студент должен:

- разместить скрипты в правильных каталогах;
- дождаться успешной компиляции;
- добавить компоненты на соответствующие GameObject;
- назначить зависимости через Inspector;
- настроить параметры;
- проверить подписку и отписку от событий;
- проверить повторное использование снарядов;
- изучить ключевые участки предоставленного кода;
- найти и устранить ошибки конфигурации.

> Не обновляйте Unity, URP, Input System и импортированные пакеты без согласования с преподавателем.

---

# 2. Связь с предыдущими работами

В `Laboratory_1` были созданы:

- URP-проект;
- базовая сцена;
- Player от первого лица;
- `CharacterController`;
- система движения и обзора;
- `PlayerSpawner`;
- базовые физические препятствия;
- Git-репозиторий.

В первой части `Laboratory_2` были созданы:

- Terrain;
- Terrain Data;
- Terrain Layers;
- рельеф;
- визуальный маршрут;
- текстурированные зоны уровня.

Вместо ручного импорта отдельных моделей студенты загрузили готовые наборы из Asset Store.
Nеперь необходимо привести импортированные ассеты к структуре и требованиям проекта.

В `Laboratory_3` необходимо:

1. Завершить оформление окружения.
2. Проверить масштаб ассетов.
3. Настроить URP-материалы.
4. Создать собственные префабы-обёртки.
5. Добавить корректные коллайдеры.
6. Подключить возврат Player после падения.
7. Создать систему стрельбы.
8. Реализовать урон через `IDamageable`.
9. Организовать события `HealthChanged` и `Died`.
10. Повторно использовать снаряды через Object Pooling.

---

# 3. Что будет создано

К окончанию лабораторной работы сцена должна иметь примерно следующую структуру:

```text
Arena_01
├── Environment
│   ├── Terrain_Main
│   ├── Arena
│   ├── ModularStructures
│   ├── Props
│   └── Decorations
├── Gameplay
│   ├── PlayerSpawnPoint
│   ├── PlayerSpawner
│   ├── Combat
│   │   ├── ProjectilePool
│   │   └── ProjectileContainer
│   └── Targets
│       ├── Target_01
│       ├── Target_02
│       └── Target_03
└── Lighting
    ├── Directional Light
    └── Global Volume
```

Префаб Player должен быть расширен:

```text
PF_FirstPersonPlayer
├── CharacterController
├── PlayerInputReader
├── PlayerMovement
├── PlayerLook
├── ObstaclePusher
├── PlayerFallRespawner
├── Weapon
└── PlayerCamera
    ├── Camera
    ├── AudioListener
    └── FirePoint
```

Префаб цели:

```text
PF_DamageableTarget
├── Collider
├── Health
├── HealthDebugView
├── TargetDeathHandler
└── Visual
```

Префаб снаряда:

```text
PF_Projectile
├── Rigidbody
├── Collider
└── Projectile
```

---

# 4. Итоговый игровой сценарий

После запуска сцены должен работать следующий сценарий:

```text
Player создаётся в PlayerSpawnPoint
        ↓
Игрок перемещается по Terrain
        ↓
Проходит через оформленное окружение
        ↓
Входит на основную арену
        ↓
Наводит камеру на цель
        ↓
Нажимает левую кнопку мыши
        ↓
Weapon запрашивает снаряд у ProjectilePool
        ↓
Projectile получает скорость и летит вперёд
        ↓
Projectile находит IDamageable
        ↓
Health уменьшает здоровье
        ↓
Health публикует событие HealthChanged
        ↓
HealthDebugView выводит новое значение
        ↓
При нулевом здоровье публикуется Died
        ↓
Цель отключается
        ↓
Projectile возвращается в пул
```

При падении Player за пределы уровня он должен возвращаться к точке появления.

---

# 5. План выполнения

```text
Шаг 1. Проверить состояние проекта
        ↓
Шаг 2. Создать рабочую ветку в Fork
        ↓
Шаг 3. Привести Asset Store-ассеты в порядок
        ↓
Шаг 4. Проверить материалы и масштаб
        ↓
Шаг 5. Создать префабы окружения
        ↓
Шаг 6. Настроить коллайдеры
        ↓
Шаг 7. Завершить маршрут и игровые зоны
        ↓
Шаг 8. Исправить PlayerMovement
        ↓
Шаг 9. Добавить PlayerFallRespawner
        ↓
Шаг 10. Добавить интерфейс IDamageable
        ↓
Шаг 11. Добавить компонент Health
        ↓
Шаг 12. Добавить диагностическое представление здоровья
        ↓
Шаг 13. Создать префаб цели
        ↓
Шаг 14. Создать префаб снаряда
        ↓
Шаг 15. Создать ProjectilePool
        ↓
Шаг 16. Добавить Weapon на Player
        ↓
Шаг 17. Настроить физические слои
        ↓
Шаг 18. Проверить события и Object Pooling
        ↓
Шаг 19. Зафиксировать результат в Fork
        ↓
Шаг 20. Зафиксировать результат в ветке lab/03-combat и выполнить Push
        ↓
Шаг 21. Выполнить чистое клонирование
```

---

# 6. Шаг 1. Предварительная проверка проекта

Откройте `Arena_01` и запустите Play Mode.

Проверьте:

- Player создаётся через `PlayerSpawner`;
- работают `WASD`, прыжок и ускорение;
- камера вращается мышью;
- Player не проходит сквозь Terrain;
- Terrain содержит не менее трёх Terrain Layers;
- существует маршрут от PlayerSpawnPoint к арене;
- материалы не отображаются розовыми;
- Console не содержит ошибок компиляции;
- результат предыдущей работы отправлен в удалённый репозиторий.

Если базовое управление или Terrain не работают, сначала исправьте предыдущий этап.

---

# 7. Шаг 2. Создание рабочей ветки в Fork

Откройте репозиторий в Fork.

Переключитесь на основную ветку проекта:

```text
master
```

или:

```text
main
```

Название зависит от настроек вашего репозитория.

Выполните:

```text
Fetch
Pull
```

Перед созданием новой ветки список локальных изменений должен быть пустым.

Создайте ветку:

```text
lab/03-combat-and-pooling
```

Убедитесь, что Fork переключился на неё.

## Почему нужна отдельная ветка

Новая работа затрагивает:

- Player prefab;
- сцену;
- физические слои;
- окружение;
- C#-скрипты;
- новые префабы;
- настройки столкновений.

Если система будет настроена неправильно, отдельная ветка позволит вернуться к предыдущему стабильному состоянию.

---

# 8. Шаг 3. Работа с ассетами из Asset Store

## 8.1. Не переносите всё в `_Project`

Asset Store-пакеты могут содержать:

```text
Models
Materials
Textures
Shaders
Scripts
Demo
Documentation
Samples
```

Не следует механически переносить всё содержимое пакета в:

```text
Assets/_Project
```

Причины:

- могут потеряться внутренние ссылки пакета;
- обновление пакета станет сложнее;
- сторонние и собственные ассеты смешаются;
- станет непонятно, какие файлы созданы студентом.

Рекомендуемая структура:

```text
Assets/
├── _Project/
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Scripts/
│   └── Documentation/
└── ThirdParty/
    └── НазваниеПакета/
```

Если Asset Store импортировал пакет в собственный каталог, можно оставить его там.

Собственные префабы-обёртки создаются в:

```text
Assets/_Project/Prefabs/Environment/
```

---

## 8.2. Удаление демонстрационных файлов

Некоторые Asset Store-пакеты содержат:

```text
Demo Scenes
Example Scripts
Documentation
Showcase Prefabs
Samples
```

Не удаляйте их автоматически.

Сначала определите, используются ли они вашим уровнем.

Если демонстрационные сцены и примеры не нужны, их можно удалить через окно Project. Перед удалением создайте commit или убедитесь, что находитесь в рабочей ветке.

Плохой подход:

```text
Удалить половину пакета через файловый менеджер,
не проверив зависимости.
```

Удаление выполняйте через Unity Project Window.

---

## 8.3. Документирование источников

Откройте или создайте:

```text
Assets/_Project/Documentation/ASSET_SOURCES.md
```

Добавьте сведения о каждом наборе:

```markdown
# Источники ассетов

## Название пакета

- Источник: Unity Asset Store
- URL: ссылка на страницу ассета
- Автор: имя автора или студии
- Лицензия: Unity Asset Store EULA или другая указанная лицензия
- Дата получения: дата
- Использование: окружение сцены Arena_01
```

Не публикуйте платные ассеты в открытом репозитории, если лицензия этого не разрешает.

---

# 9. Шаг 4. Проверка материалов Asset Store

Разместите один объект из набора на тестовом участке сцены.

Проверьте:

- объект не отображается розовым;
- материал использует URP-совместимый Shader;
- текстуры назначены;
- Normal Map импортирована как `Normal map`;
- масштаб соответствует Player;
- объект не имеет случайных отключённых компонентов;
- Console не показывает Shader errors.

Если материал розовый, создайте новый материал в:

```text
Assets/_Project/Art/Materials/
```

Пример имени:

```text
M_Environment_Stone
```

Используйте:

```text
Universal Render Pipeline/Lit
```

Назначьте предоставленные текстуры.

## Плохой подход

```text
Для каждого экземпляра камня создать отдельный Material.
```

Это усложняет проект и увеличивает число материалов.

Предпочтительно:

```text
несколько объектов → один общий Material Asset
```

если визуальные параметры одинаковы.

---

# 10. Шаг 5. Проверка масштаба моделей

Разместите рядом с импортированной моделью стандартный Cube:

```text
Scale: (1, 1, 1)
```

Такой Cube имеет размер приблизительно один метр по каждой оси.

Оцените модель относительно Player:

```text
Player Height ≈ 1.8
Door Height ≈ 2–2.5
Crate Size ≈ 1
Wall Height ≈ 3–5
```

Если небольшая модель имеет размер здания, проверьте Import Settings или создайте собственный префаб-обёртку.

Плохой вариант:

```text
Каждый экземпляр модели имеет Scale (0.01, 0.01, 0.01).
```

Предпочтительно исправить масштаб один раз на уровне импорта или префаба.

---

# 11. Шаг 6. Создание префабов окружения

Выберите не менее четырёх подходящих объектов из Asset Store.

Создайте собственные префабы:

```text
PF_Environment_Rock
PF_Environment_Crate
PF_Environment_Pillar
PF_Environment_Wall
```

Сохраните их в:

```text
Assets/_Project/Prefabs/Environment/
```

Рекомендуемая структура префаба:

```text
PF_Environment_Rock
├── Collider
└── Visual
    └── импортированная модель
```

Корень префаба должен иметь:

```text
Position: (0, 0, 0)
Rotation: (0, 0, 0)
Scale:    (1, 1, 1)
```

Если Pivot импортированной модели неудобен, сместите дочерний `Visual`, а не корень префаба.

---

# 12. Шаг 7. Создание модульной композиции

Из нескольких префабов создайте хотя бы одну законченную композицию.

Например:

```text
PF_ArenaEntrance
├── PF_Environment_Pillar
├── PF_Environment_Pillar
├── PF_Environment_Wall
└── PF_Environment_Wall
```

или:

```text
PF_RuinsSection
├── PF_Environment_Rock
├── PF_Environment_Wall
└── Decorations
```

Сохраните композицию как префаб:

```text
PF_ArenaEntrance
```

Не требуется создавать сложную модульную систему из десятков элементов. Важно показать повторное использование и понятную структуру.

---

# 13. Шаг 8. Создание Prefab Variant

Выберите базовый префаб, например:

```text
PF_Environment_Crate
```

Создайте Variant:

```text
PF_Environment_Crate_Damaged
```

Вариант может отличаться:

- материалом;
- дочерней декоративной моделью;
- цветом;
- повреждённым Visual;
- дополнительным элементом.

Не создавайте Variant только из-за другого положения объекта в сцене.

Положение экземпляра является обычным scene override.

---

# 14. Шаг 9. Настройка коллайдеров

Крупные объекты, которые визуально блокируют путь, должны иметь Collider.

Используйте:

```text
Box Collider
Sphere Collider
Capsule Collider
```

Mesh Collider применяйте только тогда, когда примитивные коллайдеры недостаточно точно описывают важную форму.

Не добавляйте Collider на каждую мелкую декорацию.

Плохой игровой опыт:

```text
Player застревает в траве или маленьком камне,
который визуально не выглядит препятствием.
```

Для мелких декоративных объектов Collider обычно не нужен.

---

# 15. Шаг 10. Завершение игрового маршрута

Уровень должен содержать:

```text
PlayerSpawnPoint
        ↓
стартовая зона
        ↓
визуальный маршрут
        ↓
оформленная зона окружения
        ↓
основная арена
        ↓
зона с целями
```

Проверьте:

- Player может пройти весь маршрут;
- нет непроходимых ступеней;
- Terrain и искусственный пол соединены;
- крупные объекты имеют Collider;
- мелкая декорация не блокирует Player;
- цель стрельбы видна с игровой зоны;
- Player не появляется внутри объекта.

---

# 16. Контрольная точка 1

К этому моменту должны быть готовы:

- Terrain;
- не менее трёх Terrain Layers;
- оформленный маршрут;
- не менее четырёх префабов окружения;
- одна композиция из вложенных префабов;
- один Prefab Variant;
- корректные коллайдеры;
- заполненный `ASSET_SOURCES.md`;
- отсутствие ошибок Console.

В Fork создайте commit:

```text
Complete environment using Asset Store assets
```

Выполните Push рабочей ветки.

---

# 17. Шаг 11. Исправление PlayerMovement

В текущем `PlayerMovement` рассчитывается `totalVelocity`, но вызов `CharacterController.Move` отсутствует.

Кроме того, значение:

```text
Jump Height = 5
```

слишком велико для стандартного масштаба персонажа.

Откройте:

```text
Assets/_Project/Scripts/Player/PlayerMovement.cs
```

Замените поле:

```csharp
[SerializeField, Min(0f)]
private float jumpHeight = 5f;
```

на:

```csharp
[SerializeField, Min(0f)]
private float jumpHeight = 1.5f;
```

В конце метода `Update`, после расчёта `totalVelocity`, добавьте:

```csharp
// CharacterController.Move принимает перемещение за кадр.
// Поэтому скорость умножается на Time.deltaTime.
characterController.Move(
    totalVelocity * Time.deltaTime);
```

Итоговая часть метода должна выглядеть так:

```csharp
private void Update()
{
    Vector2 movementInput =
        inputReader.ReadMovement();

    UpdateHorizontalSpeed(movementInput);
    UpdateVerticalVelocity();

    Vector3 horizontalDirection =
        transform.right * movementInput.x +
        transform.forward * movementInput.y;

    Vector3 horizontalVelocity =
        horizontalDirection * currentHorizontalSpeed;

    Vector3 totalVelocity =
        horizontalVelocity +
        Vector3.up * verticalVelocity;

    // Move ожидает перемещение за текущий кадр.
    characterController.Move(
        totalVelocity * Time.deltaTime);
}
```

Запустите сцену и убедитесь, что движение и прыжок работают.

> Если в вашем локальном проекте вызов `Move` уже существует, повторно добавлять его не нужно.

---

# 18. Шаг 12. Добавление PlayerFallRespawner

Если компонент уже был создан, проверьте его работу.

Если компонента нет, создайте файл:

```text
Assets/_Project/Scripts/Player/PlayerFallRespawner.cs
```

Скопируйте код:

```csharp
using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// Возвращает Player в начальную позицию,
    /// если персонаж упал ниже заданной высоты.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerFallRespawner : MonoBehaviour
    {
        [Header("Respawn Settings")]
        [SerializeField]
        private float fallThresholdY = -10f;

        [SerializeField, Min(0f)]
        private float safeHeightOffset = 0.2f;

        private CharacterController characterController;

        private Vector3 respawnPosition;
        private Quaternion respawnRotation;

        private void Awake()
        {
            characterController =
                GetComponent<CharacterController>();
        }

        private void Start()
        {
            // Player создаётся в PlayerSpawnPoint,
            // поэтому стартовый Transform используется
            // как точка возврата.
            respawnPosition = transform.position;
            respawnRotation = transform.rotation;
        }

        private void Update()
        {
            if (transform.position.y < fallThresholdY)
            {
                Respawn();
            }
        }

        [ContextMenu("Respawn")]
        public void Respawn()
        {
            // Перед телепортацией временно отключаем
            // CharacterController.
            characterController.enabled = false;

            transform.SetPositionAndRotation(
                respawnPosition +
                Vector3.up * safeHeightOffset,
                respawnRotation);

            characterController.enabled = true;
        }
    }
}
```

Добавьте компонент на:

```text
PF_FirstPersonPlayer
```

Проверьте падение за пределы уровня.

---

# 19. Шаг 13. Подготовка структуры кода боевой системы

Создайте каталоги:

```text
Assets/_Project/Scripts/
├── Combat/
├── Health/
└── Views/
```

В каталоге `Combat` будут находиться:

```text
IDamageable.cs
Projectile.cs
ProjectilePool.cs
Weapon.cs
```

В каталоге `Health`:

```text
Health.cs
TargetDeathHandler.cs
```

В каталоге `Views`:

```text
HealthDebugView.cs
```

Все скрипты должны оставаться внутри области действия существующего:

```text
ArenaSurvival.Core.asmdef
```

Если `.asmdef` находится в родительском каталоге `Scripts`, дополнительные Assembly Definitions создавать не нужно.

---

# 20. Шаг 14. Интерфейс IDamageable

Создайте:

```text
Assets/_Project/Scripts/Combat/IDamageable.cs
```

Скопируйте код:

```csharp
namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Контракт объекта, который может получать урон.
    ///
    /// Projectile не обязан знать, попал ли он
    /// в противника, мишень или разрушаемый объект.
    /// Ему достаточно работать с IDamageable.
    /// </summary>
    public interface IDamageable
    {
        void ApplyDamage(int amount);
    }
}
```

## Зачем нужен интерфейс

Без интерфейса снаряд мог бы зависеть от конкретного класса:

```csharp
Target target =
    other.GetComponent<Target>();

target.Health -= damage;
```

Недостатки:

- снаряд знает конкретный тип цели;
- нельзя использовать тот же снаряд для другого объекта;
- внешний класс напрямую изменяет здоровье;
- добавление новой цели требует изменения Projectile.

Интерфейс позволяет написать:

```csharp
if (other.TryGetComponent(
        out IDamageable damageable))
{
    damageable.ApplyDamage(damage);
}
```

Projectile зависит от способности получать урон, а не от конкретного класса.

---

# 21. Шаг 15. Компонент Health

Создайте:

```text
Assets/_Project/Scripts/Health/Health.cs
```

Скопируйте код:

```csharp
using System;
using ArenaSurvival.Combat;
using UnityEngine;

namespace ArenaSurvival.HealthSystem
{
    /// <summary>
    /// Хранит и изменяет здоровье объекта.
    ///
    /// Компонент не знает о HUD, снарядах
    /// и визуальных эффектах.
    /// Он публикует события, когда состояние изменяется.
    /// </summary>
    public sealed class Health :
        MonoBehaviour,
        IDamageable
    {
        [Header("Health Settings")]
        [SerializeField, Min(1)]
        private int maximumHealth = 100;

        public int CurrentHealth { get; private set; }

        public int MaximumHealth =>
            maximumHealth;

        /// <summary>
        /// Передаёт текущее и максимальное здоровье.
        /// </summary>
        public event Action<int, int>
            HealthChanged;

        /// <summary>
        /// Публикуется один раз,
        /// когда здоровье достигает нуля.
        /// </summary>
        public event Action Died;

        private void Awake()
        {
            CurrentHealth =
                maximumHealth;
        }

        private void Start()
        {
            // Первое уведомление позволяет представлениям
            // получить начальное состояние.
            PublishHealthChanged();
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (CurrentHealth == 0)
            {
                return;
            }

            CurrentHealth = Mathf.Max(
                0,
                CurrentHealth - amount);

            PublishHealthChanged();

            if (CurrentHealth == 0)
            {
                Died?.Invoke();
            }
        }

        [ContextMenu("Reset Health")]
        public void ResetHealth()
        {
            CurrentHealth =
                maximumHealth;

            PublishHealthChanged();
        }

        private void PublishHealthChanged()
        {
            HealthChanged?.Invoke(
                CurrentHealth,
                MaximumHealth);
        }
    }
}
```

## Кто владеет событием

Только `Health` имеет право вызвать:

```csharp
HealthChanged?.Invoke(...)
Died?.Invoke()
```

Другие классы могут подписаться:

```csharp
health.Died += HandleDeath;
```

но не должны объявлять смерть вместо владельца состояния.

---

# 22. Шаг 16. Диагностическое представление HealthDebugView

Полноценный HUD будет создан в следующей лабораторной работе. Сейчас используется диагностическое представление, которое выводит изменения в Console.

Создайте:

```text
Assets/_Project/Scripts/Views/HealthDebugView.cs
```

Скопируйте код:

```csharp
using ArenaSurvival.HealthSystem;
using UnityEngine;

namespace ArenaSurvival.Views
{
    /// <summary>
    /// Диагностическое представление здоровья.
    ///
    /// Подписывается на события Health и выводит
    /// состояние в Console.
    /// Позже его можно заменить полноценным HUD.
    /// </summary>
    public sealed class HealthDebugView :
        MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Health health;

        private void Awake()
        {
            if (health == null)
            {
                health =
                    GetComponent<Health>();
            }

            if (health == null)
            {
                Debug.LogError(
                    "HealthDebugView: Health не назначен.",
                    this);

                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (health == null)
            {
                return;
            }

            health.HealthChanged +=
                HandleHealthChanged;

            health.Died +=
                HandleDied;
        }

        private void Start()
        {
            if (health != null)
            {
                HandleHealthChanged(
                    health.CurrentHealth,
                    health.MaximumHealth);
            }
        }

        private void OnDisable()
        {
            if (health == null)
            {
                return;
            }

            health.HealthChanged -=
                HandleHealthChanged;

            health.Died -=
                HandleDied;
        }

        private void HandleHealthChanged(
            int current,
            int maximum)
        {
            Debug.Log(
                $"{name}: Health {current}/{maximum}",
                this);
        }

        private void HandleDied()
        {
            Debug.Log(
                $"{name}: Died",
                this);
        }
    }
}
```

## Почему подписка находится в OnEnable

Объект может отключаться и включаться несколько раз.

Симметричная схема:

```text
OnEnable  → подписка
OnDisable → отписка
```

защищает компонент от накопления повторных обработчиков.

---

# 23. Шаг 17. Обработчик смерти TargetDeathHandler

Создайте:

```text
Assets/_Project/Scripts/Health/TargetDeathHandler.cs
```

Скопируйте код:

```csharp
using UnityEngine;

namespace ArenaSurvival.HealthSystem
{
    /// <summary>
    /// Реагирует на смерть цели.
    ///
    /// Health хранит состояние, а этот компонент
    /// определяет визуальную реакцию на смерть.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class TargetDeathHandler :
        MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Health health;

        [Header("Death Behaviour")]
        [SerializeField]
        private bool disableTargetOnDeath = true;

        private void Awake()
        {
            if (health == null)
            {
                health =
                    GetComponent<Health>();
            }
        }

        private void OnEnable()
        {
            health.Died +=
                HandleDied;
        }

        private void OnDisable()
        {
            health.Died -=
                HandleDied;
        }

        private void HandleDied()
        {
            if (disableTargetOnDeath)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
```

## Разделение ответственности

`Health` отвечает за данные здоровья.

`TargetDeathHandler` отвечает за реакцию на смерть.

`HealthDebugView` отвечает за отображение состояния.

Это лучше, чем один большой класс:

```csharp
public class Target : MonoBehaviour
{
    // Хранит здоровье.
    // Обновляет HUD.
    // Проигрывает звук.
    // Создаёт эффект.
    // Выдаёт очки.
    // Удаляет объект.
}
```

Такой класс изменяется по слишком большому количеству причин.

---

# 24. Шаг 18. Создание префаба цели

Создайте GameObject:

```text
DamageableTarget
```

Добавьте или создайте дочерний объект:

```text
DamageableTarget
└── Visual
```

На корневом объекте должны находиться:

```text
Collider
Health
HealthDebugView
TargetDeathHandler
```

На `Visual` назначьте:

- Cube;
- Capsule;
- импортированную модель;
- собственный материал.

Сохраните объект как:

```text
PF_DamageableTarget
```

в каталог:

```text
Assets/_Project/Prefabs/Gameplay/
```

Разместите на арене не менее трёх экземпляров:

```text
Target_01
Target_02
Target_03
```

Настройте разное количество здоровья через override, например:

```text
Target_01: 50
Target_02: 100
Target_03: 150
```

Не выполняйте Apply, если разные значения относятся только к отдельным экземплярам.

---

# 25. Шаг 19. Создание физического слоя Damageable

Откройте:

```text
Edit → Project Settings → Tags and Layers
```

Создайте Layer:

```text
Damageable
```

Назначьте этот Layer корневым объектам целей.

Если Unity спросит:

```text
Apply to children?
```

подтвердите, если все дочерние части цели должны принадлежать тому же слою.

---

# 26. Шаг 20. Подготовка Projectile

Создайте Sphere:

```text
GameObject → 3D Object → Sphere
```

Назовите:

```text
Projectile
```

Рекомендуемый масштаб:

```text
Scale: (0.15, 0.15, 0.15)
```

Добавьте или проверьте:

```text
Sphere Collider
Rigidbody
```

Для `Sphere Collider` включите:

```text
Is Trigger: true
```

Для Rigidbody:

```text
Use Gravity: false
Is Kinematic: false
Interpolation: Interpolate
Collision Detection: Continuous Dynamic
```

Создайте Layer:

```text
Projectile
```

и назначьте его снаряду.

---

# 27. Шаг 21. Код Projectile

Создайте:

```text
Assets/_Project/Scripts/Combat/Projectile.cs
```

Скопируйте код:

```csharp
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
```

## Какие состояния сбрасываются

Перед повторным использованием сбрасываются:

```text
isReleased
damage
remainingLifetime
Position
Rotation
linearVelocity
angularVelocity
active state
```

Если не сбросить скорость, повторно выданный снаряд может продолжить старое движение.

---

# 28. Шаг 22. Создание префаба снаряда

Добавьте компонент:

```text
Projectile
```

на объект `Projectile`.

Сохраните его как:

```text
PF_Projectile
```

в каталог:

```text
Assets/_Project/Prefabs/Gameplay/
```

Удалите временный экземпляр из сцены.

---

# 29. Шаг 23. Код ProjectilePool

Создайте:

```text
Assets/_Project/Scripts/Combat/ProjectilePool.cs
```

Скопируйте код:

```csharp
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
    public sealed class ProjectilePool :
        MonoBehaviour
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

        public int TotalCreated =>
            totalCreated;

        public int ActiveCount =>
            activeCount;

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

        private static void OnTakeFromPool(
            Projectile projectile)
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
```

---

# 30. Важная проверка ProjectilePool

В методе `CreateProjectile` объект получает ссылку на пул:

```csharp
projectile.Initialize(pool);
```

Ссылка `pool` уже должна быть назначена к моменту первого вызова `CreateProjectile`. В стандартной реализации `ObjectPool<T>` объекты создаются лениво при первом `Get`, поэтому это условие выполняется.

Если используемая версия Unity создаёт стартовые объекты иным образом, преподаватель может предоставить скорректированный вариант.

---

# 31. Шаг 24. Настройка ProjectilePool в сцене

Внутри `Gameplay` создайте:

```text
Combat
```

Внутри:

```text
Combat
├── ProjectilePool
└── ProjectileContainer
```

Добавьте компонент `ProjectilePool` на одноимённый объект.

Назначьте:

```text
Projectile Prefab    → PF_Projectile
Projectile Container → ProjectileContainer
Default Capacity     → 20
Maximum Size         → 100
Log Pool Operations  → true для проверки
```

После проверки логирование можно отключить, чтобы не перегружать Console.

---

# 32. Шаг 25. Доработка PlayerLook для оружия

Weapon должен стрелять только тогда, когда курсор захвачен игровым окном.

Откройте:

```text
PlayerLook.cs
```

Добавьте публичное read-only свойство:

```csharp
public bool IsCursorLocked =>
    isCursorLocked;
```

Разместите его рядом с закрытыми полями:

```csharp
private PlayerInputReader inputReader;
private float cameraPitch;
private bool isCursorLocked;

public bool IsCursorLocked =>
    isCursorLocked;
```

Это свойство позволяет Weapon проверить состояние курсора, не получая возможность изменить его напрямую.

---

# 33. Шаг 26. Код Weapon

Создайте:

```text
Assets/_Project/Scripts/Combat/Weapon.cs
```

Скопируйте код:

```csharp
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
```

---

# 34. Шаг 27. Добавление FirePoint

Откройте:

```text
PF_FirstPersonPlayer
```

в Prefab Mode.

Внутри `PlayerCamera` создайте пустой дочерний объект:

```text
FirePoint
```

Пример структуры:

```text
PF_FirstPersonPlayer
└── PlayerCamera
    └── FirePoint
```

Настройте локальную позицию:

```text
Local Position: (0, -0.1, 0.5)
Local Rotation: (0, 0, 0)
```

Синяя ось Z должна быть направлена вперёд.

Добавьте на корень Player компонент:

```text
Weapon
```

Назначьте:

```text
Fire Point  → FirePoint
Player Look → PlayerLook на Player
```

Поле `Projectile Pool` нельзя назначить напрямую из prefab asset на scene object, потому что `ProjectilePool` находится в сцене, а Player является prefab asset.

Для решения этой зависимости используется следующий компонент связывания.

---

# 35. Шаг 28. Связывание Weapon с ProjectilePool

Prefab Asset не должен хранить прямую ссылку на объект конкретной сцены.

Поэтому PlayerSpawner после создания Player назначит ему ProjectilePool.

Сначала добавьте в `Weapon` публичный метод:

```csharp
public void SetProjectilePool(
    ProjectilePool pool)
{
    projectilePool = pool;
}
```

Разместите его перед `ValidateReferences`.

Фрагмент:

```csharp
public void SetProjectilePool(
    ProjectilePool pool)
{
    projectilePool = pool;
}

private void ValidateReferences()
{
    // ...
}
```

---

# 36. Шаг 29. Обновление PlayerSpawner

Откройте `PlayerSpawner.cs`.

Добавьте:

```csharp
using ArenaSurvival.Combat;
```

В поля класса добавьте:

```csharp
[SerializeField]
private ProjectilePool projectilePool;
```

После создания Player получите компонент Weapon и передайте пул.

Обновлённый метод должен выглядеть так:

```csharp
public void SpawnPlayer()
{
    if (spawnedPlayer != null)
    {
        Debug.LogWarning(
            "PlayerSpawner: Player уже создан.",
            this);

        return;
    }

    if (playerPrefab == null)
    {
        Debug.LogError(
            "PlayerSpawner: Player Prefab не назначен.",
            this);

        return;
    }

    if (spawnPoint == null)
    {
        Debug.LogError(
            "PlayerSpawner: Spawn Point не назначен.",
            this);

        return;
    }

    spawnedPlayer = Instantiate(
        playerPrefab,
        spawnPoint.position,
        spawnPoint.rotation);

    spawnedPlayer.name =
        playerPrefab.name;

    if (spawnedPlayer.TryGetComponent(
            out Weapon weapon))
    {
        weapon.SetProjectilePool(
            projectilePool);
    }
}
```

В Inspector объекта `PlayerSpawner` назначьте:

```text
Projectile Pool → scene object ProjectilePool
```

## Почему связь выполняется после Instantiate

Prefab Asset хранится в Project.

`ProjectilePool` находится в конкретной сцене.

Asset не должен напрямую ссылаться на scene object. Поэтому ссылка передаётся созданному экземпляру после `Instantiate`.

---

# 37. Шаг 30. Настройка слоёв столкновений

Создайте слои:

```text
Player
Projectile
Damageable
Environment
```

Назначьте:

- Player prefab → `Player`;
- PF_Projectile → `Projectile`;
- цели → `Damageable`;
- стены, Terrain и крупное окружение → `Environment`.

Откройте:

```text
Edit → Project Settings → Physics
```

Настройте Layer Collision Matrix.

Рекомендуемая логика:

| Слой 1 | Слой 2 | Столкновение |
|---|---|---|
| Projectile | Player | выключено |
| Projectile | Damageable | включено |
| Projectile | Environment | включено |
| Player | Environment | включено |
| Player | Damageable | по требованиям сцены |

Это предотвращает столкновение снаряда с Player сразу после выстрела.

---

# 38. Шаг 31. Первый тест стрельбы

Сохраните сцену и prefab assets.

Запустите Play Mode.

Проверьте:

1. Player создаётся.
2. Курсор захватывается.
3. Левая кнопка мыши создаёт снаряд.
4. Снаряд летит из `FirePoint`.
5. Снаряд не сталкивается с Player.
6. Снаряд попадает в цель.
7. В Console уменьшается здоровье.
8. После нескольких попаданий выводится `Died`.
9. Цель отключается.
10. Снаряд возвращается в пул.
11. После промаха снаряд возвращается по истечении времени жизни.
12. Console не содержит исключений.

---

# 39. Шаг 32. Проверка Object Pooling

Включите:

```text
Log Pool Operations
```

у `ProjectilePool`.

Выполните серию из 20–30 выстрелов.

В Console будут отображаться Instance ID снарядов.

Ожидаемый результат:

- первые выстрелы создают новые объекты;
- после возврата повторно используются прежние Instance ID;
- `TotalCreated` перестаёт быстро расти;
- `ActiveCount` увеличивается при выстреле;
- `ActiveCount` уменьшается при возврате.

Пример:

```text
Get 15432, created=5, active=1
Release 15432, created=5, active=0
Get 15432, created=5, active=1
```

Повторный Instance ID подтверждает повторное использование объекта.

После проверки отключите подробное логирование.

---

# 40. Шаг 33. Проверка двойного Release

В `Projectile` используется флаг:

```csharp
private bool isReleased;
```

Он защищает от ситуации, когда в одном кадре:

- снаряд столкнулся с объектом;
- одновременно завершился таймер.

Без защиты один объект мог бы дважды вызвать:

```csharp
ownerPool.Release(this);
```

Это нарушило бы состояние пула.

Не удаляйте проверку:

```csharp
if (isReleased)
{
    return;
}
```

---

# 41. Шаг 34. Проверка подписки и отписки

Выберите одну цель.

Во время Play Mode отключите и повторно включите её несколько раз.

После включения вызовите:

```text
Reset Health
```

через Context Menu компонента Health или перезапустите сцену.

Выполните один выстрел.

В Console должно появиться одно сообщение `HealthChanged` от `HealthDebugView`, а не несколько одинаковых сообщений.

Если сообщение повторяется, проверьте:

```text
OnEnable  → +=
OnDisable → -=
```

---

# 42. Почему повторно созданная lambda не отписывается

Плохой пример:

```csharp
private void OnEnable()
{
    health.Died +=
        () => Debug.Log("Died");
}

private void OnDisable()
{
    health.Died -=
        () => Debug.Log("Died");
}
```

Две lambda выглядят одинаково, но являются разными экземплярами делегата.

Поэтому отписка не удаляет исходный обработчик.

Предпочтительный вариант:

```csharp
private void OnEnable()
{
    health.Died +=
        HandleDied;
}

private void OnDisable()
{
    health.Died -=
        HandleDied;
}

private void HandleDied()
{
    Debug.Log("Died");
}
```

---

# 43. Контрольная точка 2

К этому моменту должны работать:

- `IDamageable`;
- `Health`;
- `HealthChanged`;
- `Died`;
- `HealthDebugView`;
- безопасная подписка и отписка;
- `TargetDeathHandler`;
- `PF_DamageableTarget`;
- `PF_Projectile`;
- `ProjectilePool`;
- `Weapon`;
- передача ProjectilePool через PlayerSpawner;
- физические слои;
- повторное использование снарядов;
- сброс скорости, таймера и флагов;
- отсутствие ошибок Console.

Создайте commit:

```text
Add event based damage system and projectile pooling
```

Выполните Push.

---

# 44. Финальная проверка уровня

Пройдите полный сценарий:

```text
Запуск сцены
        ↓
Создание Player
        ↓
Прохождение по Terrain
        ↓
Переход через окружение Asset Store
        ↓
Вход на арену
        ↓
Стрельба по трём целям
        ↓
Изменение здоровья
        ↓
Отключение уничтоженных целей
        ↓
Повторное использование снарядов
        ↓
Падение за пределы уровня
        ↓
Возврат Player к старту
```

Проверьте Game View и Console.

---

# 45. Финальная фиксация в Fork

Сохраните:

- сцену;
- Terrain Data;
- Terrain Layers;
- префабы окружения;
- Player prefab;
- Projectile prefab;
- Target prefab;
- Physics Layers;
- все C#-скрипты;
- `ASSET_SOURCES.md`.

Перейдите в Fork.

Просмотрите изменения.

Рекомендуемая история:

```text
Complete environment using Asset Store assets
Fix player movement and add fall respawn
Add damageable targets and health events
Add projectile prefab and object pool
Add weapon and configure collision layers
Complete combat scenario and playtest
```

Не создавайте один общий commit с сообщением:

```text
Lab 3 done
```

Последовательная история упрощает диагностику и защиту.

---

# 46. Фиксация результата в ветке

Работайте в отдельной ветке, например `lab/03-combat`. После проверки создайте
итоговый commit и отправьте ветку в свой удалённый репозиторий. В отчёте укажите
имя ветки и hash итогового коммита. Git-теги в этом практикуме не используются.

Преподаватель готовит старт ЛР4 в ветке `fix/lab3-ready-for-lab4`, затем
самостоятельно сливает её в `master`. Студенты начинают ЛР4 от выданного состояния.

---

# 47. Проверка чистого клонирования

Выполните Push всех commits рабочей ветки.

Клонируйте репозиторий в новый каталог через Fork.

Пример:

```text
D:\UnityProjects\ArenaSurvival-Lab3-Clean
```

Откройте проект через Unity Hub.

Проверьте:

- Terrain сохраняет форму и текстуры;
- Asset Store-ассеты присутствуют;
- материалы не стали розовыми;
- префабы окружения сохранили ссылки;
- Input System восстановился;
- Player создаётся;
- движение работает;
- стрельба работает;
- пул создаётся;
- цели получают урон;
- события вызываются;
- снаряды возвращаются;
- возврат после падения работает;
- Console не содержит ошибок.

> Если Asset Store-пакет не хранится в репозитории из-за лицензионных ограничений, в README должна быть указана точная инструкция по его повторной установке. Это необходимо согласовать с преподавателем заранее.

---

# 48. Требования к итоговому результату

## 48.1. Окружение

Необходимо иметь:

- Terrain;
- не менее трёх Terrain Layers;
- визуально читаемый маршрут;
- не менее четырёх собственных префабов-обёрток;
- одну композицию из вложенных префабов;
- один Prefab Variant;
- корректные коллайдеры;
- сведения об источниках ассетов.

## 48.2. Система урона

Необходимо подключить:

```text
IDamageable
Health
HealthDebugView
TargetDeathHandler
```

Здоровье цели должно уменьшаться при попадании.

## 48.3. События

`Health` должен публиковать:

```text
HealthChanged
Died
```

Подписчики должны подключаться в `OnEnable` и отключаться в `OnDisable`.

## 48.4. Object Pooling

Снаряды должны:

- создаваться через `ProjectilePool`;
- возвращаться через `Release`;
- повторно использоваться;
- сбрасывать скорость;
- сбрасывать angular velocity;
- сбрасывать таймер;
- сбрасывать damage;
- сбрасывать флаг возврата;
- защищаться от двойного Release.

## 48.5. Git

Необходимо:

- создать несколько осмысленных commits;
- выполнить Push;
- указать рабочую ветку и hash итогового коммита;
- проверить чистое клонирование.

---

# 49. Индивидуальное усложнение

После выполнения обязательной части можно реализовать одно дополнительное улучшение:

1. Создать визуальную шкалу здоровья над целью.
2. Добавить звук выстрела.
3. Добавить эффект попадания через отдельный пул.
4. Добавить разные типы урона.
5. Создать цель с восстановлением здоровья.
6. Добавить несколько типов снарядов.
7. Добавить ограниченный боезапас.
8. Добавить разброс выстрела.
9. Добавить визуальную модель оружия.
10. Создать второй тип `IDamageable`, например разрушаемый ящик.

Дополнительное задание не заменяет обязательные требования.

---

# 50. Критерии оценивания

| Критерий | Баллы |
|---|---:|
| Окружение завершено, ассеты организованы и имеют корректные коллайдеры | 3 |
| Созданы собственные префабы, композиция и Prefab Variant | 2 |
| PlayerMovement исправлен, возврат после падения работает | 1 |
| `IDamageable` и `Health` подключены корректно | 2 |
| `HealthChanged` и `Died` работают корректно | 2 |
| Подписка и отписка организованы безопасно | 2 |
| Projectile наносит урон и корректно возвращается в пул | 2 |
| ProjectilePool повторно использует объекты | 3 |
| Состояние pooled-объектов сбрасывается, двойной Release предотвращён | 1 |
| Git-история, ветка, чистое клонирование, отчёт и защита | 2 |
| **Итого** | **20** |

---

# 51. Условия допуска к защите

Работа готова к защите, если:

- проект открывается в версии Unity курса;
- Console не содержит ошибок компиляции;
- отсутствуют `Missing Script`;
- Player создаётся;
- движение работает;
- Player может пройти маршрут;
- окружение не содержит розовых материалов;
- цели имеют Collider и Health;
- снаряд наносит урон;
- `HealthChanged` вызывается;
- `Died` вызывается один раз;
- цель отключается после смерти;
- снаряды возвращаются в пул;
- повторно используются те же экземпляры;
- отсутствует двойной Release;
- `Library` не находится в Git;
- выполнен Push;
- итоговый коммит отправлен в рабочую ветку;
- чистое клонирование успешно открывается.

---

# 52. Вопросы для защиты

1. Зачем нужен интерфейс `IDamageable`?
2. Почему Projectile не должен напрямую изменять поле здоровья?
3. Кто является владельцем событий `HealthChanged` и `Died`?
4. Чем делегат отличается от события?
5. Почему только Health должен вызывать `Died`?
6. Зачем подписываться в `OnEnable`?
7. Зачем отписываться в `OnDisable`?
8. Почему повторно созданная lambda не подходит для отписки?
9. Какую ответственность выполняет `HealthDebugView`?
10. Почему логика смерти вынесена в `TargetDeathHandler`?
11. Когда Object Pooling полезен?
12. Почему пулинг не следует применять ко всем объектам?
13. Какие состояния Projectile необходимо сбрасывать?
14. Что произойдёт при двойном `Release`?
15. Для чего используется `collectionCheck`?
16. Почему снаряд не уничтожается через `Destroy`?
17. Как проверить, что снаряды повторно используются?
18. Чем `GC Alloc` отличается от утечки памяти?
19. Почему большой пул может увеличить потребление памяти?
20. Для чего используется Layer Collision Matrix?
21. Почему Projectile не должен сталкиваться с Player?
22. Почему Player prefab не может хранить прямую ссылку на scene ProjectilePool?
23. Как PlayerSpawner передаёт пул созданному Player?
24. Чем Prefab Variant отличается от scene override?
25. Почему сторонние Asset Store-ассеты не следует смешивать с собственными?
26. Для чего нужен `ASSET_SOURCES.md`?
27. Зачем выполнять чистое клонирование?

---

# 53. Типичные проблемы и способы устранения

## Player не двигается

Проверьте наличие вызова:

```csharp
characterController.Move(
    totalVelocity * Time.deltaTime);
```

в `PlayerMovement.Update`.

---

## Player прыгает слишком высоко

Проверьте:

```text
Jump Height
Gravity
```

Для базового проекта рекомендуется:

```text
Jump Height: 1.5
Gravity: -20
```

---

## Weapon сообщает, что Projectile Pool не назначен

Проверьте:

- поле `Projectile Pool` на `PlayerSpawner`;
- вызов `weapon.SetProjectilePool(projectilePool)`;
- наличие Weapon на Player prefab;
- наличие ProjectilePool в сцене.

---

## Снаряд сразу сталкивается с Player

Настройте Layer Collision Matrix:

```text
Projectile ↔ Player = выключено
```

Убедитесь, что слои назначены корневым объектам.

---

## Снаряд не движется

Проверьте:

- Rigidbody;
- `Use Gravity = false`;
- `Is Kinematic = false`;
- значение Projectile Speed;
- направление синей оси FirePoint;
- наличие `body.linearVelocity = velocity`.

---

## Снаряд проходит сквозь цель

Проверьте:

- Collider на цели;
- Collider на снаряде и `Is Trigger = true`;
- Rigidbody на снаряде, `Use Gravity = false`, `Is Kinematic = false`;
- `Collision Detection = Continuous Dynamic`;
- взаимодействие слоёв `Projectile` и `Damageable` в Physics;
- `Health` и Collider находятся на одном корневом GameObject цели.

Последний пункт важен: `Projectile` вызывает `TryGetComponent<IDamageable>`
на объекте столкнувшегося Collider. Collider только на дочернем `Visual`
не позволит этому коду найти `Health` на родителе.

## Цель теряет здоровье, но не исчезает

На корне `PF_DamageableTarget` должен быть включён `TargetDeathHandler`.
При `Died` он отключает корень через `SetActive(false)`, а не уничтожает его
через `Destroy`. Это нормальный результат ЛР3. Для 50/100/150/200 HP и урона
25 требуется соответственно 2/4/6/8 попаданий.

## Проверка вложенного префаба

В выдаваемом проекте `PF_ArenaEntrance` состоит из трёх вложенных экземпляров
`PF_Wall`. Откройте его в Prefab Mode: `LeftPillar`, `RightPillar` и `Lintel`
должны сохранять связь с исходным префабом, а не быть распакованными копиями.
