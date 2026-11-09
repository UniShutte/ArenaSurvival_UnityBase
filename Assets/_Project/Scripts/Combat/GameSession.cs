using System;
using System.Collections;
using System.Collections.Generic;
using ArenaSurvival.Enemies;
using ArenaSurvival.HealthSystem;
using ArenaSurvival.Player;
using UnityEngine;

namespace ArenaSurvival.Combat
{
    public enum RoundState { Preparing, Playing, Won, Lost }

    /// <summary>Owns one scene's score and result, not the UI or saved settings.</summary>
    public sealed class GameSession : MonoBehaviour
    {
        private readonly HashSet<EnemyAgent> remaining = new();
        private PlayerMovement movement;
        private PlayerLook look;
        private WeaponEffects effects;

        public RoundState State { get; private set; } = RoundState.Preparing;
        public int Score { get; private set; }
        public int RemainingEnemies => remaining.Count;
        public Health PlayerHealth { get; private set; }
        public Weapon PlayerWeapon { get; private set; }
        public event Action Changed;

        private void Awake()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        /// <summary>Waits for the existing PlayerSpawner and Weapon.Start before binding.</summary>
        private IEnumerator Start()
        {
            yield return null;
            movement = FindFirstObjectByType<PlayerMovement>();
            if (movement == null || !movement.TryGetComponent(out Health health) ||
                !movement.TryGetComponent(out PlayerLook playerLook) ||
                !movement.TryGetComponent(out Weapon weapon))
            {
                Debug.LogError("GameSession: Player needs Movement, Look, Weapon and Health.", this);
                enabled = false;
                yield break;
            }
            PlayerHealth = health;
            PlayerWeapon = weapon;
            look = playerLook;
            effects = movement.GetComponent<WeaponEffects>();

            foreach (EnemyAgent enemy in FindObjectsByType<EnemyAgent>(FindObjectsSortMode.None))
                if (enemy.GetComponent<Health>().CurrentHealth > 0 && remaining.Add(enemy))
                    enemy.Defeated += OnEnemyDefeated;

            if (remaining.Count == 0)
            {
                Debug.LogError("GameSession: place at least one active, living PF_Enemy.", this);
                enabled = false;
                yield break;
            }
            PlayerHealth.Died += OnPlayerDied;
            State = RoundState.Playing;
            Changed?.Invoke();
            if (PlayerHealth.CurrentHealth == 0) Finish(RoundState.Lost);
        }

        private void OnEnemyDefeated(EnemyAgent enemy)
        {
            if (State != RoundState.Playing || !remaining.Remove(enemy)) return;
            enemy.Defeated -= OnEnemyDefeated;
            Score += Mathf.Max(0, enemy.Reward);
            if (remaining.Count == 0) Finish(RoundState.Won);
            else Changed?.Invoke();
        }

        private void OnPlayerDied() { Finish(RoundState.Lost); }

        /// <summary>Commits the result once and freezes gameplay, but lets the last sound finish.</summary>
        private void Finish(RoundState result)
        {
            if (State != RoundState.Playing) return;
            State = result;
            PlayerWeapon.enabled = false;
            movement.enabled = false;
            look.enabled = false;
            if (effects != null) effects.enabled = false;
            Time.timeScale = 0f;
            AudioListener.pause = false;
            Changed?.Invoke();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (PlayerHealth != null) PlayerHealth.Died -= OnPlayerDied;
            foreach (EnemyAgent enemy in remaining)
                if (enemy != null) enemy.Defeated -= OnEnemyDefeated;
            remaining.Clear();
        }
    }
}