using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArenaSurvival.HealthSystem;
using ArenaSurvival.Combat;
using ArenaSurvival.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaSurvival.Lab4
{
    public enum ArenaState { Playing, Completed, Restarting }

    public sealed class ArenaSession : MonoBehaviour
    {
        [SerializeField] private Health[] targets;
        [SerializeField, Min(0.1f)] private float restartDelaySeconds = 3f;
        private CancellationTokenSource lifetime;
        private bool started;

        public ArenaState State { get; private set; } = ArenaState.Playing;
        public int DestroyedCount { get; private set; }
        public int TargetCount => targets == null ? 0 : targets.Length;
        public event Action Changed;
        public event Action TargetHit;

        private void Awake()
        {
            var unique = new HashSet<Health>();
            if (targets != null && targets.Length > 0)
            {
                foreach (Health target in targets)
                {
                    if (target == null || !unique.Add(target))
                    {
                        Debug.LogError("Targets must contain unique, non-null scene Health components.", this);
                        enabled = false;
                        return;
                    }
                }
                return;
            }
            Debug.LogError("Assign at least one target to ArenaSession.", this);
            enabled = false;
        }

        private void OnEnable()
        {
            lifetime = new CancellationTokenSource();
            foreach (Health target in targets)
            {
                target.Died += Refresh;
                target.HealthChanged += OnHealthChanged;
            }
            if (started)
                Refresh();
        }

        private void Start()
        {
            started = true;
            Refresh();
        }

        private void OnDisable()
        {
            if (targets != null)
                foreach (Health target in targets)
                    if (target != null)
                    {
                        target.Died -= Refresh;
                        target.HealthChanged -= OnHealthChanged;
                    }
            lifetime?.Cancel();
            lifetime?.Dispose();
            lifetime = null;
            if (State == ArenaState.Restarting)
                State = ArenaState.Completed;
        }

        private void OnHealthChanged(int current, int maximum)
        {
            if (State == ArenaState.Playing && current < maximum)
                TargetHit?.Invoke();
        }

        private void Refresh()
        {
            DestroyedCount = 0;
            foreach (Health target in targets)
                if (target.CurrentHealth == 0)
                    DestroyedCount++;
            if (State != ArenaState.Restarting)
                State = DestroyedCount == TargetCount ? ArenaState.Completed : ArenaState.Playing;
            if (State == ArenaState.Completed)
            {
                // PlayerLook.OnDisable releases the cursor so UI clicks cannot recapture it.
                PlayerLook look = FindFirstObjectByType<PlayerLook>();
                if (look != null)
                {
                    look.enabled = false;
                    if (look.TryGetComponent(out Weapon weapon))
                        weapon.enabled = false;
                    if (look.TryGetComponent(out PlayerMovement movement))
                        movement.enabled = false;
                }
            }
            Changed?.Invoke();
        }

        // Unity Button.onClick requires void. Exceptions are handled at this event boundary.
        public async void Restart()
        {
            if (!isActiveAndEnabled || State != ArenaState.Completed)
                return;
            CancellationToken token = lifetime.Token;
            try
            {
                State = ArenaState.Restarting;
                Changed?.Invoke();
                await Task.Delay(TimeSpan.FromSeconds(restartDelaySeconds), token);
                token.ThrowIfCancellationRequested();
                SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().path);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Disabling the session or leaving Play Mode cancels the pending delay.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                if (this != null && isActiveAndEnabled)
                {
                    State = ArenaState.Completed;
                    Changed?.Invoke();
                }
            }
        }
    }
}
