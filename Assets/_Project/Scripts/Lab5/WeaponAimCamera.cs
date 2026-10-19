// Тип Weapon лежит в сборке ArenaSurvival.Core — она указана в ссылках asmdef.
using ArenaSurvival.Combat;
// CinemachineVirtualCamera из пакета Cinemachine 2.10.
using Cinemachine;
using UnityEngine;

namespace ArenaSurvival.Lab5
{
    /// <summary>
    /// Меняет угол обзора виртуальной камеры, пока удерживается правая кнопка мыши.
    ///
    /// Скрипт не вращает камеру: повороты остаются в PlayerLook.
    /// </summary>
    [DefaultExecutionOrder(300)] // Update выполняется после Weapon (порядок 200)
    public sealed class WeaponAimCamera : MonoBehaviour
    {
        [SerializeField] private float normalFov = 60; // обычный угол обзора
        [SerializeField] private float aimFov = 48;    // угол обзора в прицеливании
        private Weapon weapon;                        // источник состояния IsAiming
        private CinemachineVirtualCamera cameraRig;   // виртуальная камера сцены

        private void Start()
        {
            // Компоненты ищутся один раз: искать их каждый кадр незачем.
            weapon = GetComponent<Weapon>();
            cameraRig = FindFirstObjectByType<CinemachineVirtualCamera>();
            if (weapon == null || cameraRig == null)
            { Debug.LogError("Assign a player weapon and a scene virtual camera.", this); enabled = false; }
        }

        private void Update()
        {
            // Целевой FOV: 48 в прицеливании и 60 в покое.
            float target = weapon.enabled && weapon.IsAiming ? aimFov : normalFov;
            // MoveTowards даёт плавный переход вместо мгновенного скачка.
            cameraRig.m_Lens.FieldOfView = Mathf.MoveTowards(cameraRig.m_Lens.FieldOfView, target, 90 * Time.deltaTime);
        }
    }
}