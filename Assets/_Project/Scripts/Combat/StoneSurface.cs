using UnityEngine;

namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Явный маркер каменной поверхности.
    ///
    /// Компонент ничего не делает сам: снаряд ищет его через
    /// GetComponentInParent и по нему выбирает звук рикошета.
    /// Пустой класс выбран специально — важна сама метка на объекте.
    /// </summary>
    public sealed class StoneSurface : MonoBehaviour { }
}