using System.Collections;
using System.Linq;
using ArenaSurvival.Combat;
using ArenaSurvival.HealthSystem;
using ArenaSurvival.Lab4;
using ArenaSurvival.Player;
using ArenaSurvival.Spawning;
using Cinemachine;
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
        private Mouse mouse;
        private InputSettings originalInputSettings;
        private InputSettings testInputSettings;
        private HideFlags originalInputHideFlags;
        private float originalCaptureDeltaTime;

        [UnitySetUp]
        public IEnumerator LoadArena()
        {
            originalInputSettings = InputSystem.settings;
            originalInputHideFlags = originalInputSettings.hideFlags;
            // Input System destroys HideAndDontSave settings when replacing them.
            originalInputSettings.hideFlags = HideFlags.DontUnloadUnusedAsset;
            testInputSettings = Object.Instantiate(originalInputSettings);
            InputSystem.settings = testInputSettings;
            // Batch-mode tests have no focused Game View; do not change the project's input asset.
            testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testInputSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            originalCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Arena_01");
            yield return null;
            // These tests isolate the original gameplay; Lab4 scene tests cover round completion.
            Object.FindFirstObjectByType<ArenaSession>().enabled = false;
            player = Object.FindFirstObjectByType<PlayerMovement>();
            pool = Object.FindFirstObjectByType<ProjectilePool>();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
        }

        [TearDown]
        public void RemoveDevices()
        {
            if (keyboard != null)
                InputSystem.RemoveDevice(keyboard);
            if (mouse != null)
                InputSystem.RemoveDevice(mouse);
            InputSystem.settings = originalInputSettings;
            originalInputSettings.hideFlags = originalInputHideFlags;
            Object.Destroy(testInputSettings);
            Time.captureDeltaTime = originalCaptureDeltaTime;
        }

        [UnityTest]
        public IEnumerator ArenaHasPlayerTerrainTargetsAndCollisionLayers()
        {
            Assert.That(player, Is.Not.Null);
            Assert.That(pool, Is.Not.Null);
            Assert.That(Vector3.Dot(player.transform.up, Vector3.up), Is.GreaterThan(0.999f));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(player.GetComponentInChildren<Camera>(), Is.Null);
            Assert.That(Camera.main.GetComponent<CinemachineBrain>(), Is.Not.Null);
            Transform cameraTarget = player.transform.Find("CameraTarget");
            Assert.That(cameraTarget.localPosition.y, Is.EqualTo(0.7f));
            Assert.That(Object.FindFirstObjectByType<CinemachineVirtualCamera>().Follow, Is.SameAs(cameraTarget));
            Assert.That(Terrain.activeTerrain.terrainData.terrainLayers.Length, Is.GreaterThanOrEqualTo(3));

            Health[] targets = Object.FindObjectsByType<Health>(FindObjectsSortMode.None);
            Assert.That(targets.Select(target => target.MaximumHealth), Is.EquivalentTo(new[] { 50, 100, 150, 200 }));
            foreach (Health target in targets)
                Assert.That(target.GetComponent<Collider>(), Is.Not.Null, target.name);

            int projectileLayer = LayerMask.NameToLayer("Projectile");
            Assert.That(Physics.GetIgnoreLayerCollision(projectileLayer, LayerMask.NameToLayer("Player")), Is.True);
            Assert.That(Physics.GetIgnoreLayerCollision(projectileLayer, LayerMask.NameToLayer("Damageable")), Is.False);
            Assert.That(Physics.GetIgnoreLayerCollision(projectileLayer, LayerMask.NameToLayer("Environment")), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DynamicRocksFallTogetherAndCanBothBePushed()
        {
            string[] names = { "PF_LittleRock", "PF_LittleRock_03_GreenMoss" };
            Rigidbody[] rocks = names.Select(name => GameObject.Find(name).GetComponent<Rigidbody>()).ToArray();
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.transform.position = new Vector3(0f, 99.5f, 0f);
            platform.transform.localScale = new Vector3(30f, 1f, 30f);
            try
            {
                for (int i = 0; i < rocks.Length; i++)
                {
                    Rigidbody rock = rocks[i];
                    Assert.That(rock.gameObject.isStatic, Is.False, rock.name);
                    Assert.That(rock.GetComponent<Renderer>().isPartOfStaticBatch, Is.False, rock.name);
                    Assert.That(rock.isKinematic, Is.False);
                    Assert.That(rock.useGravity, Is.True);
                    rock.position = new Vector3(i == 0 ? -5f : 5f, 104f, 0f);
                    rock.rotation = Quaternion.identity;
                    rock.linearVelocity = Vector3.zero;
                    rock.angularVelocity = Vector3.zero;
                }
                yield return new WaitForSeconds(0.6f);
                foreach (Rigidbody rock in rocks)
                {
                    Assert.That(rock.position.y, Is.LessThan(103f), rock.name + " must fall.");
                    Vector3 renderOrigin = rock.GetComponent<Renderer>().localToWorldMatrix.MultiplyPoint3x4(Vector3.zero);
                    Assert.That(Vector3.Distance(renderOrigin, rock.transform.position), Is.LessThan(0.01f),
                        rock.name + " renderer must follow its physical body.");
                }
                yield return new WaitForSeconds(1.5f);

                CharacterController controller = player.GetComponent<CharacterController>();
                foreach (Rigidbody rock in rocks)
                {
                    Bounds bounds = rock.GetComponent<Collider>().bounds;
                    controller.enabled = false;
                    player.transform.SetPositionAndRotation(
                        new Vector3(bounds.center.x, 100.95f, bounds.min.z - 1.5f), Quaternion.identity);
                    player.ResetVelocity();
                    controller.enabled = true;
                    yield return new WaitForSeconds(0.1f);
                    Vector3 before = rock.position;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                    yield return new WaitForSeconds(1.2f);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    Vector3 displacement = Vector3.ProjectOnPlane(rock.position - before, Vector3.up);
                    Assert.That(displacement.magnitude, Is.GreaterThan(0.05f), rock.name + " must respond to Player.");
                    yield return null;
                }
            }
            finally
            {
                Object.Destroy(platform);
            }
        }

        [UnityTest]
        public IEnumerator PlayerCanWalkTerrainRouteThroughNestedEntrance()
        {
            GameObject entrance = GameObject.Find("PF_ArenaEntrance");
            Assert.That(entrance, Is.Not.Null);
            Assert.That(entrance.transform.childCount, Is.EqualTo(3));
            Assert.That(entrance.GetComponentsInChildren<BoxCollider>().Length, Is.EqualTo(3));
            Vector3[] waypoints =
            {
                new Vector3(-29f, 0f, -48f),
                new Vector3(-22f, 0f, -35f),
                new Vector3(-16f, 0f, -24f),
                new Vector3(-12f, 0f, -16f),
                new Vector3(-12f, 0f, -9f),
                new Vector3(-5f, 0f, -8f)
            };
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            foreach (Vector3 waypoint in waypoints)
            {
                float deadline = Time.time + 12f;
                Vector3 offset;
                do
                {
                    offset = waypoint - player.transform.position;
                    offset.y = 0;
                    if (offset.sqrMagnitude < 1f)
                        break;
                    player.transform.rotation = Quaternion.LookRotation(offset);
                    yield return null;
                } while (Time.time < deadline);
                Assert.That(offset.magnitude, Is.LessThan(1f), "Route blocked near " + waypoint);
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Assert.That(player.transform.position.y, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator TiltedSpawnPointStillCreatesUprightPlayer()
        {
            PlayerSpawner spawner = Object.FindFirstObjectByType<PlayerSpawner>();
            GameObject.Find("PlayerSpawnPoint").transform.rotation = Quaternion.Euler(23f, 55f, 11f);
            player.gameObject.SetActive(false);
            Object.Instantiate(spawner);
            yield return null;
            yield return null;

            PlayerMovement spawned = Object.FindFirstObjectByType<PlayerMovement>();
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned, Is.Not.SameAs(player));
            Assert.That(Vector3.Dot(spawned.transform.up, Vector3.up), Is.GreaterThan(0.999f));
            Assert.That(Mathf.DeltaAngle(spawned.transform.eulerAngles.y, 55f), Is.EqualTo(0f).Within(0.1f));
            Assert.That(Object.FindFirstObjectByType<CinemachineVirtualCamera>().Follow,
                Is.SameAs(spawned.transform.Find("CameraTarget")));
        }

        [UnityTest]
        public IEnumerator MouseFiresWeaponAndSpaceJumps()
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.transform.position = new Vector3(0f, 100f, 0f);
            platform.transform.localScale = new Vector3(10f, 1f, 10f);
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.SetPositionAndRotation(new Vector3(0f, 101.4f, 0f), Quaternion.identity);
            player.ResetVelocity();
            controller.enabled = true;
            Health target = Object.FindObjectsByType<Health>(FindObjectsSortMode.None)
                .Single(item => item.MaximumHealth == 50);
            target.transform.position = new Vector3(0f, 102f, 3f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.2f);

            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
            yield return new WaitForSeconds(0.3f);
            Assert.That(target.CurrentHealth, Is.EqualTo(25), "Mouse -> Weapon -> Pool -> Projectile must deal damage.");
            Assert.That(pool.TotalCreated, Is.EqualTo(1));
            InputSystem.QueueStateEvent(mouse, new MouseState());

            Assert.That(controller.isGrounded, Is.True);
            float groundedY = player.transform.position.y;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return new WaitForSeconds(0.15f);
            Assert.That(player.transform.position.y, Is.GreaterThan(groundedY + 0.5f));
            Object.Destroy(platform);
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
