using System.Collections;
using ArenaSurvival.Combat;
using ArenaSurvival.Enemies;
using ArenaSurvival.HealthSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.Lab6
{
    /// <summary>Reads combat state; never changes health, ammunition or the score.</summary>
    [DefaultExecutionOrder(400)]
    public sealed class CombatHud : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private TMP_Text weaponName;
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Slider playerHealth;
        [SerializeField] private TMP_Text playerHealthText;
        [SerializeField] private Image crosshair;
        [SerializeField] private GameObject hitMarker;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button restartButton;
        private Weapon weapon;
        private Health health;
        private Camera viewCamera;
        private int aimMask;
        private bool subscribed;
        private bool menuVisible;
        private float markerUntil;

        private void Awake()
        {
            if (session == null || weaponName == null || ammoText == null || scoreText == null ||
                playerHealth == null || playerHealthText == null || crosshair == null ||
                hitMarker == null || resultPanel == null || resultText == null || restartButton == null)
            {
                Debug.LogError("CombatHud: assign all UI references and GameSession.", this);
                enabled = false;
                return;
            }
            aimMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player", "Projectile", "UI");
            hitMarker.SetActive(false);
            resultPanel.SetActive(false);
        }

        private void OnEnable() { StartCoroutine(Bind()); }

        /// <summary>Also runs after re-enabling the HUD; Start alone would not run again.</summary>
        private IEnumerator Bind()
        {
            while (session.State == RoundState.Preparing) yield return null;
            weapon = session.PlayerWeapon;
            health = session.PlayerHealth;
            viewCamera = Camera.main;
            if (weapon == null || health == null || viewCamera == null)
            {
                Debug.LogError("CombatHud: session or Main Camera is not ready.", this);
                enabled = false;
                yield break;
            }
            weapon.Changed += RefreshWeapon;
            health.HealthChanged += RefreshHealth;
            session.Changed += RefreshSession;
            Projectile.HitConfirmed += OnHit;
            subscribed = true;
            RefreshWeapon();
            RefreshHealth(health.CurrentHealth, health.MaximumHealth);
            RefreshSession();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (subscribed)
            {
                if (weapon != null) weapon.Changed -= RefreshWeapon;
                if (health != null) health.HealthChanged -= RefreshHealth;
                if (session != null) session.Changed -= RefreshSession;
                Projectile.HitConfirmed -= OnHit;
                subscribed = false;
            }
            markerUntil = 0f;
            if (hitMarker != null) hitMarker.SetActive(false);
        }

        private void RefreshWeapon()
        {
            weaponName.text = weapon.CurrentData.displayName;
            ammoText.text = weapon.IsReloading ? "Reloading..." :
                $"{weapon.Ammo} / {weapon.CurrentData.magazineSize}";
        }

        private void RefreshHealth(int current, int maximum)
        {
            playerHealth.maxValue = maximum;
            playerHealth.value = current;
            playerHealthText.text = $"{current} / {maximum}";
        }

        private void RefreshSession()
        {
            scoreText.text = $"Score: {session.Score}   Enemies: {session.RemainingEnemies}";
            bool finished = session.State == RoundState.Won || session.State == RoundState.Lost;
            resultPanel.SetActive(finished);
            if (finished)
            {
                resultText.text = (session.State == RoundState.Won ? "Victory" : "Defeat") +
                    $"\nScore: {session.Score}";
                restartButton.Select();
            }
        }

        private void OnHit(Health damaged)
        {
            if (damaged != null && damaged.GetComponent<EnemyAgent>() != null)
                markerUntil = Time.unscaledTime + 0.1f;
        }

        public void SetMenuVisible(bool visible)
        {
            menuVisible = visible;
            markerUntil = 0f;
        }

        private void LateUpdate()
        {
            if (!subscribed) return;
            bool aiming = !menuVisible && session.State == RoundState.Playing && Time.timeScale > 0f;
            crosshair.gameObject.SetActive(aiming);
            hitMarker.SetActive(!menuVisible && Time.unscaledTime < markerUntil);
            if (!aiming) return;
            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            bool liveEnemy = false;
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, aimMask, QueryTriggerInteraction.Ignore))
            {
                EnemyAgent enemy = hit.collider.GetComponentInParent<EnemyAgent>();
                liveEnemy = enemy != null && enemy.GetComponent<Health>().CurrentHealth > 0;
            }
            crosshair.color = liveEnemy ? Color.red : Color.white;
        }
    }
}