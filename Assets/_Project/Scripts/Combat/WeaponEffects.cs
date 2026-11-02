using System;          // нужен для атрибута [Serializable]
using UnityEngine;

namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Ссылки представления одного оружия.
    /// </summary>
    [Serializable]
    public sealed class WeaponEffectSlot
    {
        public Animator animator;     // Animator на AnimatedRoot этого оружия
        public ParticleSystem flash;  // система вспышки на MuzzleFlash
        public AudioSource audio;     // источник звука на этом же View
        public AudioClip shot;        // клип выстрела
        public AudioClip reload;      // клип перезарядки
    }

    /// <summary>
    /// Связывает события Weapon с анимацией, вспышкой и звуком.
    ///
    /// Компонент ничего не решает про патроны и урон:
    /// он только показывает то, что уже произошло.
    /// </summary>
    [DefaultExecutionOrder(300)] // после Weapon (200): параметры уже обновлены
    public sealed class WeaponEffects : MonoBehaviour
    {
        [SerializeField] private Weapon weapon;              // источник событий
        [SerializeField] private WeaponEffectSlot[] effects; // три элемента, порядок = Slots

        // Ссылки на дочерние системы получаем при включении компонента,
        // а не создаём новый массив при каждом выстреле.
        private ParticleSystem[][] cachedSmokeSystems;

        private void Awake()
        {
            // Проверяем ссылки до первого выстрела, чтобы ошибка была видна сразу.
            if (weapon == null || effects == null || effects.Length != 3)
            { Debug.LogError("Assign Weapon and three effect slots.", this); enabled = false; return; }
            // Самостоятельно: добавьте в сообщение номер неверного элемента
            // и проверьте защиту, временно убрав одну ссылку.
            foreach (WeaponEffectSlot item in effects)
                if (item == null || item.animator == null || item.flash == null || item.audio == null)
                { Debug.LogError("Incomplete weapon effect slot.", this); enabled = false; return; }
        }

        // Подписка и отписка обязаны быть парными: иначе после повторного
        // включения объект получит одно событие дважды.
        private void OnEnable()
        {
            if (weapon == null) 
                return;

            weapon.Fired += OnFired;
            weapon.ReloadStarted += OnReload;
            weapon.Changed += OnWeaponChanged;

            OnWeaponChanged();
        }

        private void OnDisable()
        {
            if (weapon != null)
            {
                weapon.Fired -= OnFired;
                weapon.ReloadStarted -= OnReload;
                weapon.Changed -= OnWeaponChanged;
            }

            if (effects == null) 
                return;

            foreach (WeaponEffectSlot view in effects)
                ClearParticles(view);
        }

        private void LateUpdate()
        {
            // Bool-параметры описывают состояние, поэтому обновляются каждый кадр.
            WeaponEffectSlot view = effects[weapon.CurrentIndex];
            view.animator.SetBool("Aiming", weapon.IsAiming);
            view.animator.SetBool("Reloading", weapon.IsReloading);
        }

        private void OnFired(int index)
        {
            WeaponEffectSlot view = effects[index];
            view.animator.SetBool("Aiming", weapon.IsAiming);
            view.animator.SetBool("Reloading", false);
            view.animator.SetTrigger("Fire");

            // Restart the flash without clearing its child smoke.
            view.flash.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            view.flash.Play(false);
            // shortcut: all child systems are smoke; use explicit references for other effects.
            foreach (ParticleSystem smoke in view.flash.GetComponentsInChildren<ParticleSystem>())
                if (smoke != view.flash) smoke.Emit(2);

            if (view.shot != null) view.audio.PlayOneShot(view.shot);
        }

        private void OnReload(int index)
        {
            WeaponEffectSlot view = effects[index];
            // Trigger сбрасывается: иначе отдача сработает после перезарядки.
            view.animator.ResetTrigger("Fire");
            view.animator.SetBool("Reloading", true);
            if (view.reload != null) view.audio.PlayOneShot(view.reload);
        }

        private void CacheSmokeSystems()
        {
            if (effects == null)
            {
                cachedSmokeSystems = null;
                return;
            }

            cachedSmokeSystems = new ParticleSystem[effects.Length][];

            for (int i = 0; i < effects.Length; i++)
            {
                WeaponEffectSlot view = effects[i];

                if (view == null || view.flash == null)
                {
                    continue;
                }

                // true включает неактивные View других оружий.
                // Саму вспышку исключаем при обработке OnFired.
                cachedSmokeSystems[i] =
                    view.flash.GetComponentsInChildren<ParticleSystem>(true);
            }
        }

        private void OnWeaponChanged()
        {
            if (effects == null) 
                return;

            for (int i = 0; i < effects.Length; i++)
                if (i != weapon.CurrentIndex) ClearParticles(effects[i]);
        }

        private static void ClearParticles(WeaponEffectSlot view)
        {
            if (view != null && view.flash != null)
                view.flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}