using System.Collections;
using System.Linq;
using ArenaSurvival.Combat;
using ArenaSurvival.HealthSystem;
using ArenaSurvival.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ArenaSurvival.Tests
{
    public sealed class Lab3PlayModeTests
    {
        private PlayerMovement player;
        private ProjectilePool pool;
        private Keyboard keyboard;

        [UnitySetUp]
        public IEnumerator LoadArena()
        {
            yield return SceneManager.LoadSceneAsync("Arena_01");
            yield return null;
            player = Object.FindFirstObjectByType<PlayerMovement>();
            pool = Object.FindFirstObjectByType<ProjectilePool>();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public void RemoveKeyboard()
        {
            if (keyboard != null)
                InputSystem.RemoveDevice(keyboard);
        }

        [Test]
        public void ArenaHasPlayerTerrainTargetsAndCollisionLayers()
        {
            Assert.That(player, Is.Not.Null);
            Assert.That(pool, Is.Not.Null);
            Assert.That(Vector3.Dot(player.transform.up, Vector3.up), Is.GreaterThan(0.999f));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(player.GetComponentInChildren<Camera>().transform.localPosition.y, Is.EqualTo(0.7f));
            Assert.That(Terrain.activeTerrain.terrainData.terrainLayers.Length, Is.GreaterThanOrEqualTo(3));

            Health[] targets = Object.FindObjectsByType<Health>(FindObjectsSortMode.None);
            Assert.That(targets.Select(target => target.MaximumHealth), Is.EquivalentTo(new[] { 50, 100, 150, 200 }));
            foreach (Health target in targets)
                Assert.That(target.GetComponent<Collider>(), Is.Not.Null, target.name);

            int projectileLayer = LayerMask.NameToLayer("Projectile");
            Assert.That(Physics.GetIgnoreLayerCollision(projectileLayer, LayerMask.NameToLayer("Player")), Is.True);
            Assert.That(Physics.GetIgnoreLayerCollision(projectileLayer, LayerMask.NameToLayer("Damageable")), Is.False);
            Assert.That(Physics.GetIgnoreLayerCollision(projectileLayer, LayerMask.NameToLayer("Environment")), Is.False);
        }

        [UnityTest]
        public IEnumerator RealProjectilesKillAllTargetsAndAreReused()
        {
            Health[] targets = Object.FindObjectsByType<Health>(FindObjectsSortMode.None);
            Projectile firstProjectile = null;
            foreach (Health target in targets)
            {
                // Move the real scene target away from unrelated environment colliders.
                target.transform.position = new Vector3(0f, 100f, 0f);
                Physics.SyncTransforms();
                int deathCount = 0;
                target.Died += () => deathCount++;
                int hits = target.MaximumHealth / 25;
                for (int hit = 1; hit <= hits; hit++)
                {
                    Projectile projectile = pool.Spawn(target.transform.position - Vector3.forward * 2f,
                        Quaternion.identity, Vector3.forward * 25f, 25);
                    if (firstProjectile == null)
                        firstProjectile = projectile;
                    Assert.That(projectile, Is.SameAs(firstProjectile));

                    float deadline = Time.time + 1f;
                    while (projectile.gameObject.activeSelf && Time.time < deadline)
                        yield return new WaitForFixedUpdate();

                    Assert.That(projectile.gameObject.activeSelf, Is.False, "Projectile did not return after impact.");
                    Assert.That(target.CurrentHealth, Is.EqualTo(target.MaximumHealth - hit * 25));
                    Assert.That(target.gameObject.activeSelf, Is.EqualTo(hit < hits));
                    Assert.That(pool.ActiveCount, Is.Zero);
                }

                target.ApplyDamage(25);
                Assert.That(deathCount, Is.EqualTo(1), "Died must be raised once.");
            }
            Assert.That(pool.TotalCreated, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MissExpiresAndPooledStateResets()
        {
            Projectile projectile = pool.Spawn(new Vector3(0f, 100f, 0f), Quaternion.identity,
                Vector3.forward * 25f, 25);
            yield return new WaitForSeconds(3.2f);
            Assert.That(projectile.gameObject.activeSelf, Is.False);
            Assert.That(pool.ActiveCount, Is.Zero);

            Health target = Object.FindObjectsByType<Health>(FindObjectsSortMode.None)
                .Single(item => item.MaximumHealth == 50);
            target.transform.position = new Vector3(0f, 100f, 0f);
            Physics.SyncTransforms();
            Projectile reused = pool.Spawn(target.transform.position - Vector3.forward * 2f,
                Quaternion.identity, Vector3.forward * 25f, 50);
            Assert.That(reused, Is.SameAs(projectile));
            yield return new WaitForSeconds(0.3f);
            Assert.That(target.CurrentHealth, Is.Zero, "Damage and lifetime must reset on reuse.");
            Assert.That(target.gameObject.activeSelf, Is.False);
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.TotalCreated, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DeathHandlerUnsubscribesWhenDisabled()
        {
            Health target = Object.FindFirstObjectByType<Health>();
            TargetDeathHandler handler = target.GetComponent<TargetDeathHandler>();
            for (int i = 0; i < 3; i++)
            {
                handler.enabled = false;
                handler.enabled = true;
            }
            handler.enabled = false;
            target.ApplyDamage(target.MaximumHealth);
            Assert.That(target.gameObject.activeSelf, Is.True, "Disabled handler must not remain subscribed.");
            target.ResetHealth();
            handler.enabled = true;
            target.ApplyDamage(target.MaximumHealth);
            Assert.That(target.gameObject.activeSelf, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MovementUsesInputAndFallRespawnClearsVelocity()
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = new Vector3(0f, 100f, 0f);
            controller.enabled = true;
            Vector3 start = player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(0.5f);
            Assert.That(Vector3.Dot(player.transform.position - start, player.transform.forward), Is.GreaterThan(1f));
            Assert.That(player.CurrentHorizontalSpeed, Is.EqualTo(5f).Within(0.1f));

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            yield return new WaitForSeconds(0.3f);
            Assert.That(player.CurrentHorizontalSpeed, Is.EqualTo(8f).Within(0.1f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;

            controller.enabled = false;
            player.transform.position = new Vector3(0f, -20f, 0f);
            controller.enabled = true;
            yield return null;
            yield return null;

            Vector3 spawn = GameObject.Find("PlayerSpawnPoint").transform.position;
            Assert.That(Vector3.Distance(player.transform.position, spawn), Is.LessThan(1f));
            Assert.That(player.CurrentHorizontalSpeed, Is.Zero);
            Assert.That(controller.enabled, Is.True);
            Assert.That(Vector3.Dot(player.transform.up, Vector3.up), Is.GreaterThan(0.999f));
        }
    }
}
