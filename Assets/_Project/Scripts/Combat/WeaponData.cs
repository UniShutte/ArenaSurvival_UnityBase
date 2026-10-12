using UnityEngine;

namespace ArenaSurvival
{
    /// <summary>
    /// Характеристики оружия: магазин, темп, снаряд, урон.
    ///
    /// Это ScriptableObject — ассет с данными, а не компонент объекта.
    /// Текущий магазин хранится НЕ здесь, а в слоте Weapon: иначе два
    /// одинаковых пистолета делили бы один магазин, а значение сохранялось бы
    /// между запусками сцены.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena Survival/Weapon Data")]  // добавляет пункт в меню Create
    public sealed class WeaponData : ScriptableObject
    {
        public string displayName = "Pistol";          // имя для интерфейса и отладки

        [Min(1)] public int magazineSize = 18;         // сколько патронов в магазине
        [Min(1)] public float roundsPerMinute = 40;    // темп: выстрелов в минуту
        public bool automatic;                         // true — стрельба при удержании ЛКМ

        [Min(0.1f)] public float projectileSpeed = 45; // скорость снаряда, м/с
        [Min(1)] public int damage = 25;               // урон одного попадания
        [Min(1)] public int pellets = 1;               // сколько дробин за выстрел
        [Range(0, 20)] public float spreadDegrees;     // полуугол разброса в градусах
        [Min(0.1f)] public float reloadSeconds = 1.8f; // длительность перезарядки
    }
}
