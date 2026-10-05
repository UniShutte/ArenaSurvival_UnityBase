using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.Lab4
{
    public sealed class ArenaHud : MonoBehaviour
    {
        [SerializeField] private ArenaSession session;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text notificationText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Animator feedbackAnimator;
        private Coroutine notification;
        private static readonly int Hit = Animator.StringToHash("Hit");
        private static readonly int Completed = Animator.StringToHash("Completed");

        private void Awake()
        {
            if (session == null || progressText == null || notificationText == null ||
                restartButton == null || feedbackAnimator == null)
            {
                Debug.LogError("Assign all ArenaHud references.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            session.Changed += Refresh;
            session.TargetHit += ShowHit;
            restartButton.onClick.AddListener(session.Restart);
            StopNotification();
            Refresh();
        }

        private void Start() => Refresh();

        private void OnDisable()
        {
            if (session != null)
            {
                session.Changed -= Refresh;
                session.TargetHit -= ShowHit;
                if (restartButton != null)
                    restartButton.onClick.RemoveListener(session.Restart);
            }
            StopNotification();
        }

        private void Refresh()
        {
            bool completed = session.State != ArenaState.Playing;
            progressText.text = $"Targets: {session.DestroyedCount} / {session.TargetCount}";
            restartButton.gameObject.SetActive(completed);
            restartButton.interactable = session.State == ArenaState.Completed;
            feedbackAnimator.SetBool(Completed, completed);
            if (completed)
            {
                StopNotification();
                feedbackAnimator.ResetTrigger(Hit);
                notificationText.text = session.State == ArenaState.Restarting
                    ? "Restarting..." : "Arena cleared! Click Restart.";
            }
        }

        private void ShowHit()
        {
            StopNotification();
            feedbackAnimator.SetTrigger(Hit);
            notification = StartCoroutine(HitNotice());
        }

        private IEnumerator HitNotice()
        {
            notificationText.text = "Hit!";
            yield return new WaitForSecondsRealtime(0.6f);
            notificationText.text = string.Empty;
            notification = null;
        }

        private void StopNotification()
        {
            if (notification != null)
                StopCoroutine(notification);
            notification = null;
            if (notificationText != null)
                notificationText.text = string.Empty;
        }
    }
}
