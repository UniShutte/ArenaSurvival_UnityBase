using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ArenaSurvival.Combat;
using ArenaSurvival.HealthSystem;
using ArenaSurvival.Player;
using Cinemachine;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ArenaSurvival.Lab4.Tests
{
    public sealed class Lab4SceneTests
    {
        private ArenaSession session;
        private ArenaHud hud;
        private Health[] targets;
        private Animator animator;
        private TMP_Text notice;
        private Button restart;
        private float previousCaptureDelta;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            previousCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync("Arena_01");
            yield return null;
            yield return null;
            session = Object.FindFirstObjectByType<ArenaSession>();
            hud = Object.FindFirstObjectByType<ArenaHud>();
            targets = Object.FindObjectsByType<Health>(FindObjectsSortMode.None)
                .OrderBy(t => t.MaximumHealth).ToArray();
            animator = Field<Animator>(hud, "feedbackAnimator");
            notice = Field<TMP_Text>(hud, "notificationText");
            restart = Field<Button>(hud, "restartButton");
        }

        [TearDown]
        public void RestoreClock() => Time.captureDeltaTime = previousCaptureDelta;

        [UnityTest]
        public IEnumerator SceneReferencesCameraAndAimAreReady()
        {
            Assert.That(targets.Select(t => t.MaximumHealth), Is.EqualTo(new[] { 50, 100, 150, 200 }));
            Assert.That(Field<Health[]>(session, "targets"), Is.EquivalentTo(targets));
            Assert.That(session.State, Is.EqualTo(ArenaState.Playing));
            Assert.That(Field<float>(session, "restartDelaySeconds"), Is.EqualTo(3));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
            Assert.That(brain.m_UpdateMethod, Is.EqualTo(CinemachineBrain.UpdateMethod.LateUpdate));
            Assert.That(brain.m_DefaultBlend.m_Style, Is.EqualTo(CinemachineBlendDefinition.Style.Cut));
            // Test coroutines resume before LateUpdate. Stop falling before comparing poses across frames.
            Object.FindFirstObjectByType<PlayerMovement>().enabled = false;
            yield return null;
            yield return null;
            Transform follow = Object.FindFirstObjectByType<CinemachineVirtualCamera>().Follow;
            Assert.That(Vector3.Distance(Camera.main.transform.position, follow.position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(Camera.main.transform.rotation, follow.rotation), Is.LessThan(0.01f));
            Assert.That(follow.Find("FirePoint").localPosition, Is.EqualTo(new Vector3(0, 0, 0.5f)));
            Assert.That(Field<TMP_Text>(hud, "progressText").text, Is.EqualTo("Targets: 0 / 4"));
            Assert.That(restart.gameObject.activeSelf, Is.False);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True);
            Assert.That(animator.GetComponent<Image>().color.a, Is.EqualTo(0.35f).Within(0.01f));
            TargetHealthBar[] bars = hud.GetComponentsInChildren<TargetHealthBar>();
            Assert.That(bars.Length, Is.EqualTo(4));
            Assert.That(bars.Select(b => Field<Health>(b, "target")), Is.EquivalentTo(targets));
            foreach (TargetHealthBar bar in bars)
            {
                Health target = Field<Health>(bar, "target");
                Assert.That(Field<Slider>(bar, "healthSlider").value, Is.EqualTo(target.MaximumHealth));
                Assert.That(Field<TMP_Text>(bar, "valueText").text,
                    Is.EqualTo($"{target.MaximumHealth} / {target.MaximumHealth}"));
            }
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            InputSystemUIInputModule module = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            Assert.That(module.point.action, Is.Not.Null);
            Assert.That(module.leftClick.action, Is.Not.Null);
            foreach (Transform root in SceneManager.GetActiveScene().GetRootGameObjects().Select(g => g.transform))
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    Assert.That(child.GetComponents<Component>().All(c => c != null), Is.True, child.name + " missing script");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RepeatedHitsRestartFeedbackAndKeepVictoryVisible()
        {
            Health target = targets.Last();
            target.ApplyDamage(25);
            yield return null;
            yield return null;
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Hit"), Is.True);
            Assert.That(notice.text, Is.EqualTo("Hit!"));
            yield return new WaitForSeconds(0.12f);
            float before = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            target.ApplyDamage(25);
            yield return null;
            yield return null;
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.LessThan(before));
            yield return new WaitForSeconds(0.4f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True);
            yield return new WaitForSecondsRealtime(0.4f);
            target.ApplyDamage(25);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(notice.text, Is.EqualTo("Hit!"), "The previous notice timer must not clear a newer hit.");
            foreach (Health item in targets)
                item.ApplyDamage(item.MaximumHealth);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Completed"), Is.True);
            Assert.That(animator.GetComponent<Image>().color, Is.EqualTo(Color.green));
            Assert.That(animator.transform.localScale, Is.EqualTo(new Vector3(1.3f, 1.3f, 1)));
            Assert.That(notice.text, Is.EqualTo("Arena cleared! Click Restart."));
        }

        [UnityTest]
        public IEnumerator ProjectilesCompleteRoundAndUiButtonRestartsActualScene()
        {
            ProjectilePool pool = Object.FindFirstObjectByType<ProjectilePool>();
            foreach (Health target in targets)
            {
                target.transform.position = new Vector3(0, 100, 0);
                Physics.SyncTransforms();
                while (target.CurrentHealth > 0)
                {
                    Projectile projectile = pool.Spawn(target.transform.position - Vector3.forward * 2,
                        Quaternion.identity, Vector3.forward * 25, 25);
                    float deadline = Time.time + 1;
                    while (projectile.gameObject.activeSelf && Time.time < deadline)
                        yield return new WaitForFixedUpdate();
                    Assert.That(projectile.gameObject.activeSelf, Is.False);
                }
            }
            Assert.That(session.State, Is.EqualTo(ArenaState.Completed));
            Assert.That(Field<TMP_Text>(hud, "progressText").text, Is.EqualTo("Targets: 4 / 4"));
            Assert.That(Object.FindFirstObjectByType<PlayerLook>().IsCursorLocked, Is.False);
            Assert.That(restart.gameObject.activeInHierarchy && restart.interactable, Is.True);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null,
                    restart.transform.TransformPoint(((RectTransform)restart.transform).rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(restart));
            int oldHandle = SceneManager.GetActiveScene().handle;
            ExecuteEvents.Execute(restart.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(restart.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(session.State, Is.EqualTo(ArenaState.Restarting));
            Assert.That(restart.interactable, Is.False);
            yield return new WaitForSecondsRealtime(1);
            Assert.That((int)SceneManager.GetActiveScene().handle, Is.EqualTo(oldHandle));
            float timeout = Time.realtimeSinceStartup + 12;
            while ((int)SceneManager.GetActiveScene().handle == oldHandle && Time.realtimeSinceStartup < timeout)
                yield return null;
            yield return null;
            yield return null;
            Assert.That((int)SceneManager.GetActiveScene().handle, Is.Not.EqualTo(oldHandle));
            Assert.That(Object.FindFirstObjectByType<ArenaSession>().State, Is.EqualTo(ArenaState.Playing));
            Assert.That(Object.FindObjectsByType<Health>(FindObjectsSortMode.None).All(t => t.CurrentHealth == t.MaximumHealth), Is.True);
            Assert.That(Object.FindFirstObjectByType<PlayerMovement>().enabled, Is.True);
            Assert.That(Object.FindFirstObjectByType<ProjectilePool>().ActiveCount, Is.Zero);
            Assert.That(Field<TMP_Text>(Object.FindFirstObjectByType<ArenaHud>(), "progressText").text, Is.EqualTo("Targets: 0 / 4"));
        }

        [UnityTest]
        public IEnumerator CancellingActualSceneRestartRestoresButtonOnReenable()
        {
            foreach (Health target in targets) target.ApplyDamage(target.MaximumHealth);
            int oldHandle = SceneManager.GetActiveScene().handle;
            restart.onClick.Invoke();
            session.enabled = false;
            yield return new WaitForSecondsRealtime(3.2f);
            Assert.That((int)SceneManager.GetActiveScene().handle, Is.EqualTo(oldHandle));
            session.enabled = true;
            Assert.That(session.State, Is.EqualTo(ArenaState.Completed));
            Assert.That(restart.interactable, Is.True);
            Assert.That(notice.text, Is.EqualTo("Arena cleared! Click Restart."));
        }

        [UnityTest]
        public IEnumerator HudFitsThreeResolutionsAndCanBeRendered()
        {
            Canvas canvas = hud.GetComponent<Canvas>();
            CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f));
            bool capture = System.Environment.GetCommandLineArgs().Contains("-lab4Screenshots");
            // Use a camera render target to test screen-space UI at fixed sizes, without changing the user's Game View.
            Camera camera = Camera.main;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            scaler.enabled = false;
            foreach (bool completed in new[] { false, true })
            {
                if (completed)
                    foreach (Health target in targets) target.ApplyDamage(target.MaximumHealth);
                foreach (Vector2Int size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(1024, 768) })
                {
                    var texture = new RenderTexture(size.x, size.y, 24);
                    camera.targetTexture = texture;
                    canvas.scaleFactor = Mathf.Sqrt(size.x / 1920f * size.y / 1080f);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    foreach (TMP_Text text in hud.GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate();
                        Assert.That(text.isTextOverflowing, Is.False, text.name + " text overflow at " + size);
                        var corners = new Vector3[4];
                        text.rectTransform.GetWorldCorners(corners);
                        foreach (Vector3 corner in corners)
                        {
                            Vector3 pixel = camera.WorldToScreenPoint(corner);
                            Assert.That(pixel.x, Is.InRange(-1f, size.x + 1f), text.name + " x at " + size);
                            Assert.That(pixel.y, Is.InRange(-1f, size.y + 1f), text.name + " y at " + size);
                        }
                    }
                    if (capture) Capture(camera, texture, (completed ? "completed-" : "playing-") + size.x + "x" + size.y);
                    camera.targetTexture = null;
                    Object.Destroy(texture);
                }
            }
        }

        private static void Capture(Camera camera, RenderTexture texture, string name)
        {
            Directory.CreateDirectory("Logs/Lab4VisualReview");
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            try
            {
                RenderPipeline.SubmitRenderRequest(camera,
                    new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                image.Apply();
                File.WriteAllBytes("Logs/Lab4VisualReview/" + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(image);
            }
        }

        private static T Field<T>(object instance, string name) =>
            (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
    }
}
