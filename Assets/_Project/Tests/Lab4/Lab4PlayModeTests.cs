using System.Collections;
using System.Linq;
using System.Reflection;
using ArenaSurvival.Combat;
using ArenaSurvival.HealthSystem;
using ArenaSurvival.Player;
using Cinemachine;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ArenaSurvival.Lab4.Tests
{
    public sealed class Lab4PlayModeTests
    {
        private Health[] targets;
        private ArenaSession session;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Arena_02");
            yield return null;
            // Isolate the component tests from a student's Inspector-wired session and HUD.
            foreach (ArenaHud hud in Object.FindObjectsByType<ArenaHud>(FindObjectsSortMode.None))
                hud.enabled = false;
            foreach (ArenaSession existing in Object.FindObjectsByType<ArenaSession>(FindObjectsSortMode.None))
                existing.enabled = false;
            targets = Object.FindObjectsByType<Health>(FindObjectsSortMode.None);
            var root = new GameObject("SessionUnderTest");
            root.SetActive(false);
            session = root.AddComponent<ArenaSession>();
            SetField(session, "targets", targets);
            root.SetActive(true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompletionCountsEachTargetOnceAndStopsPlayerInput()
        {
            Assert.That(session.State, Is.EqualTo(ArenaState.Playing));
            Assert.That(session.TargetCount, Is.EqualTo(4));
            int hits = 0;
            session.TargetHit += () => hits++;
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].ApplyDamage(targets[i].MaximumHealth);
                targets[i].ApplyDamage(25);
                Assert.That(session.DestroyedCount, Is.EqualTo(i + 1));
            }
            Assert.That(hits, Is.EqualTo(4));
            Assert.That(session.State, Is.EqualTo(ArenaState.Completed));
            PlayerLook look = Object.FindFirstObjectByType<PlayerLook>();
            Assert.That(look.enabled, Is.False);
            Assert.That(look.IsCursorLocked, Is.False);
            Assert.That(look.GetComponent<Weapon>().enabled, Is.False);
            Assert.That(look.GetComponent<PlayerMovement>().enabled, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledSessionCancelsRestartAndCanBeEnabledAgain()
        {
            foreach (Health target in targets)
                target.ApplyDamage(target.MaximumHealth);
            int sceneHandle = SceneManager.GetActiveScene().handle;
            int changes = 0;
            session.Changed += () => changes++;
            session.Restart();
            session.Restart();
            Assert.That(session.State, Is.EqualTo(ArenaState.Restarting));
            Assert.That(changes, Is.EqualTo(1), "Repeated clicks must not start another restart.");
            session.enabled = false;
            yield return new WaitForSecondsRealtime(3.2f);
            Assert.That((int)SceneManager.GetActiveScene().handle, Is.EqualTo(sceneHandle));
            session.enabled = true;
            Assert.That(session.State, Is.EqualTo(ArenaState.Completed));
            Assert.That(session.DestroyedCount, Is.EqualTo(4));
            Assert.That(changes, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator RestartReloadsArenaWithHealthyTargets()
        {
            foreach (Health target in targets)
                target.ApplyDamage(target.MaximumHealth);
            int oldHandle = SceneManager.GetActiveScene().handle;
            SetField(session, "restartDelaySeconds", 0.1f);
            session.Restart();
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((int)SceneManager.GetActiveScene().handle == oldHandle && Time.realtimeSinceStartup < deadline)
                yield return null;
            yield return null;
            Assert.That((int)SceneManager.GetActiveScene().handle, Is.Not.EqualTo(oldHandle));
            Assert.That(session == null, Is.True);
            Health[] restored = Object.FindObjectsByType<Health>(FindObjectsSortMode.None);
            Assert.That(restored.Length, Is.EqualTo(4));
            Assert.That(restored.All(t => t.CurrentHealth == t.MaximumHealth), Is.True);
            Assert.That(Object.FindFirstObjectByType<PlayerLook>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator HealthBarRefreshesAndUnsubscribes()
        {
            Health target = targets.Single(t => t.MaximumHealth == 50);
            var row = new GameObject("HealthRowUnderTest", typeof(RectTransform));
            row.SetActive(false);
            Slider slider = new GameObject("Slider", typeof(RectTransform)).AddComponent<Slider>();
            slider.transform.SetParent(row.transform);
            TMP_Text label = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(row.transform);
            TargetHealthBar bar = row.AddComponent<TargetHealthBar>();
            SetField(bar, "target", target);
            SetField(bar, "healthSlider", slider);
            SetField(bar, "valueText", label);
            row.SetActive(true);
            yield return null;
            Assert.That(slider.value, Is.EqualTo(50));
            target.ApplyDamage(25);
            Assert.That(label.text, Is.EqualTo("25 / 50"));
            Assert.That(slider.value, Is.EqualTo(25));
            bar.enabled = false;
            target.ApplyDamage(25);
            Assert.That(slider.value, Is.EqualTo(25), "Disabled view must not receive events.");
            bar.enabled = true;
            Assert.That(slider.value, Is.Zero);
            Assert.That(label.text, Is.EqualTo("0 / 50"));
        }

        [UnityTest]
        public IEnumerator CameraBindsRuntimeTargetAndUsesItsPose()
        {
            foreach (CinemachineVirtualCamera old in Object.FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None))
                old.gameObject.SetActive(false);
            var cameraObject = new GameObject("VirtualCameraUnderTest");
            var camera = cameraObject.AddComponent<CinemachineVirtualCamera>();
            camera.AddCinemachineComponent<CinemachineHardLockToTarget>();
            camera.AddCinemachineComponent<CinemachineSameAsFollowTarget>();
            var root = new GameObject("PlayerCameraTargetUnderTest");
            root.SetActive(false);
            root.transform.SetPositionAndRotation(new Vector3(3, 100, 5), Quaternion.Euler(20, 35, 0));
            var binder = root.AddComponent<CameraFollowTarget>();
            SetField(binder, "cameraTarget", root.transform);
            root.SetActive(true);
            yield return null;
            camera.InternalUpdateCameraState(Vector3.up, -1f);
            Assert.That(camera.Follow, Is.SameAs(root.transform));
            Assert.That(Vector3.Distance(camera.State.FinalPosition, root.transform.position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(camera.State.FinalOrientation, root.transform.rotation), Is.LessThan(0.01f));
            Object.Destroy(root);
            yield return null;
            Assert.That(camera.Follow == null, Is.True);
        }

        private static void SetField(object instance, string name, object value)
        {
            FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(instance, value);
        }
    }
}
