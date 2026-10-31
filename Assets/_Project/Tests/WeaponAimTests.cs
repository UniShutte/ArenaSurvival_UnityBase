using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArenaSurvival.Combat;
using ArenaSurvival.HealthSystem;
using ArenaSurvival.Player;
using Cinemachine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ArenaSurvival.Tests
{
    public sealed class WeaponAimTests
    {
        private Weapon weapon;
        private ProjectilePool pool;
        private Camera camera;
        private Transform muzzle;
        private readonly List<GameObject> objects = new List<GameObject>();
        private float previousCaptureDelta;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync("Arena_02");
            yield return null;
            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
            player.enabled = false;
            player.GetComponent<PlayerLook>().enabled = false;
            weapon = player.GetComponent<Weapon>();
            weapon.enabled = false;
            player.GetComponent<CharacterController>().enabled = false;
            player.transform.SetPositionAndRotation(new Vector3(0, 100, 0), Quaternion.identity);
            WeaponSlot[] slots = (WeaponSlot[])typeof(Weapon)
                .GetField("slots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weapon);
            WeaponSlot slot = slots[weapon.CurrentIndex];
            muzzle = slot.muzzle;
            pool = slot.pool;
            Assert.That(muzzle, Is.Not.Null);
            Assert.That(pool, Is.Not.Null);
            camera = Camera.main;
            camera.GetComponent<CinemachineBrain>().ManualUpdate();
            yield return null;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in objects) Object.Destroy(item);
            objects.Clear();
            Time.captureDeltaTime = previousCaptureDelta;
        }

        [UnityTest]
        public IEnumerator OffsetMuzzleHitsCrosshairAtDifferentDistances()
        {
            Vector3 offset = muzzle.localPosition;
            foreach (float distance in new[] { 2f, 6f, 30f, 60f })
            {
                GameObject target = Cube(camera.transform.position + camera.transform.forward * distance, Vector3.one * 0.4f);
                Health health = target.AddComponent<Health>();
                Physics.SyncTransforms();
                Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
                Assert.That(target.GetComponent<Collider>().Raycast(ray, out RaycastHit hit, 100), Is.True);
                Fire();
                Projectile shot = Object.FindFirstObjectByType<Projectile>();
                Assert.That(shot.transform.position, Is.EqualTo(muzzle.position));
                Assert.That(Vector3.Angle(shot.GetComponent<Rigidbody>().linearVelocity,
                    hit.point - muzzle.position), Is.LessThan(0.05f));
                float deadline = Time.time + 3;
                while (health.CurrentHealth == 100 && Time.time < deadline) yield return new WaitForFixedUpdate();
                Assert.That(health.CurrentHealth, Is.EqualTo(health.MaximumHealth - weapon.CurrentData.damage),
                    "Crosshair hit at distance " + distance);
                Assert.That(pool.ActiveCount, Is.Zero);
                Assert.That(muzzle.localPosition, Is.EqualTo(offset), "Do not recenter the right-hand muzzle.");
                target.SetActive(false);
            }
        }

        [UnityTest]
        public IEnumerator AimIgnoresTriggersPlayerAndProjectileLayers()
        {
            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            GameObject target = Cube(ray.GetPoint(12), Vector3.one);
            Health health = target.AddComponent<Health>();
            GameObject trigger = Cube(ray.GetPoint(2), Vector3.one);
            trigger.GetComponent<Collider>().isTrigger = true;
            GameObject player = Cube(ray.GetPoint(4), Vector3.one);
            player.layer = LayerMask.NameToLayer("Player");
            GameObject projectile = Cube(ray.GetPoint(6), Vector3.one);
            projectile.layer = LayerMask.NameToLayer("Projectile");
            projectile.GetComponent<Collider>().isTrigger = true;
            Physics.SyncTransforms();
            Assert.That(target.GetComponent<Collider>().Raycast(ray, out RaycastHit hit, 100), Is.True);
            Fire();
            Projectile shot = Object.FindFirstObjectByType<Projectile>();
            Assert.That(Vector3.Angle(shot.GetComponent<Rigidbody>().linearVelocity,
                hit.point - muzzle.position), Is.LessThan(0.05f));
            yield return new WaitForSeconds(0.7f);
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaximumHealth - weapon.CurrentData.damage));
            Assert.That(pool.ActiveCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator EmptySkyUsesFiniteFallbackPoint()
        {
            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            Fire();
            Projectile shot = Object.FindFirstObjectByType<Projectile>();
            Vector3 velocity = shot.GetComponent<Rigidbody>().linearVelocity;
            Assert.That(Vector3.Angle(velocity, ray.GetPoint(500) - muzzle.position), Is.LessThan(0.05f));
            Assert.That(velocity.magnitude, Is.EqualTo(weapon.CurrentData.projectileSpeed).Within(0.001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator NearbyCoverCannotBeBypassedByOffsetMuzzle()
        {
            GameObject cover = Cube(Vector3.Lerp(camera.transform.position, muzzle.position, 0.7f), Vector3.one * 0.2f);
            Physics.SyncTransforms();
            Fire();
            Assert.That(pool.ActiveCount, Is.Zero, "Muzzle behind cover must not create a projectile beyond it.");
            cover.SetActive(false);
            Cube(camera.transform.position + camera.transform.forward * 0.3f, Vector3.one * 0.1f);
            Physics.SyncTransforms();
            Fire();
            Assert.That(pool.ActiveCount, Is.Zero, "A point behind the muzzle must not cause a backwards shot.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoverBetweenMuzzleAndTargetStopsProjectile()
        {
            Vector3 targetPoint = camera.transform.position + camera.transform.forward * 8;
            GameObject target = Cube(targetPoint, Vector3.one * 0.4f);
            Health health = target.AddComponent<Health>();
            Cube(Vector3.Lerp(muzzle.position, targetPoint, 0.3f), Vector3.one * 0.15f);
            Physics.SyncTransforms();
            Fire();
            yield return new WaitForSeconds(0.6f);
            Assert.That(health.CurrentHealth, Is.EqualTo(100), "Cover outside the camera ray still blocks the muzzle path.");
            Assert.That(pool.ActiveCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator TriggerContactUsesParentHealthAndConfirmsOnlyOnce()
        {
            var target = new GameObject("ParentTarget");
            objects.Add(target);
            Health health = target.AddComponent<Health>();
            target.AddComponent<StoneSurface>();
            GameObject child = Cube(camera.transform.position + Vector3.up * 10, Vector3.one);
            child.transform.SetParent(target.transform, true);
            Collider collider = child.GetComponent<Collider>();
            Projectile shot = pool.Spawn(child.transform.position, Quaternion.identity, Vector3.zero,
                weapon.CurrentData.damage);
            AudioClip clip = AudioClip.Create("HitRegression", 4410, 1, 44100, false);
            typeof(Projectile).GetField("enemyHitClip", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(shot, clip);
            int confirmations = 0;
            System.Action<Health> onHit = hit => { if (hit == health) confirmations++; };
            Projectile.HitConfirmed += onHit;
            try
            {
                // Exercise the trigger callback, then simulate the duplicate swept contact.
                shot.SendMessage("OnTriggerEnter", collider, SendMessageOptions.RequireReceiver);
                typeof(Projectile).GetMethod("ResolveHit", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(shot, new object[] { collider, child.transform.position });
                Assert.That(health.CurrentHealth,
                    Is.EqualTo(health.MaximumHealth - weapon.CurrentData.damage));
                Assert.That(confirmations, Is.EqualTo(1));
                Assert.That(pool.ActiveCount, Is.Zero);
                Assert.That(shot.gameObject.activeSelf, Is.False);
                Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None)
                    .Count(source => source.clip == clip), Is.EqualTo(1),
                    "A trigger hit must play the health impact once, not the stone impact.");
            }
            finally
            {
                Projectile.HitConfirmed -= onHit;
                foreach (AudioSource source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None)
                    .Where(source => source.clip == clip)) Object.Destroy(source.gameObject);
                Object.Destroy(clip);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator AimedFireUsesMatchingNonLoopingClipForEveryWeapon()
        {
            WeaponEffects effects = weapon.GetComponent<WeaponEffects>();
            var views = (WeaponEffectSlot[])typeof(WeaponEffects)
                .GetField("effects", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(effects);
            for (int index = 0; index < views.Length; index++)
            {
                typeof(Weapon).GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(weapon, new object[] { index });
                typeof(Weapon).GetProperty("IsAiming").SetValue(weapon, true);
                Animator animator = views[index].animator;
                AnimationClip aim = animator.runtimeAnimatorController.animationClips
                    .Single(clip => clip.name.EndsWith("_Aim"));
                AnimationClip fireAim = animator.runtimeAnimatorController.animationClips
                    .Single(clip => clip.name.EndsWith("_FireAim"));
                Assert.That(fireAim.isLooping, Is.False, fireAim.name);
                aim.SampleAnimation(animator.gameObject, 0);
                Vector3 aimPosition = animator.transform.localPosition;
                fireAim.SampleAnimation(animator.gameObject, 0);
                Assert.That(Vector3.Distance(animator.transform.localPosition, aimPosition), Is.LessThan(0.0001f));
                fireAim.SampleAnimation(animator.gameObject, fireAim.length);
                Assert.That(Vector3.Distance(animator.transform.localPosition, aimPosition), Is.LessThan(0.0001f));
                animator.Play("Aim", 0, 0);
                animator.Update(0);
                typeof(WeaponEffects).GetMethod("OnFired", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(effects, new object[] { index });
                animator.Update(0.01f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("FireAim"), Is.True,
                    "Aimed shot must enter FireAim: " + fireAim.name);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EveryWeaponUsesItsAssignedTargetAndStoneSounds()
        {
            var slots = (WeaponSlot[])typeof(Weapon)
                .GetField("slots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weapon);
            string[] targetClips = { "target_impact_pistol", "target_impact_rifle", "target_impact_shotgun" };
            string[] stoneClips = { "Ricochet_Pistol", "Ricochet_Rifle", "Ricochet_buck" };
            for (int index = 0; index < slots.Length; index++)
            {
                foreach (bool stone in new[] { false, true })
                {
                    GameObject target = Cube(camera.transform.position + Vector3.up * 10, Vector3.one);
                    if (stone) target.AddComponent<StoneSurface>();
                    else target.AddComponent<Health>();
                    Projectile shot = slots[index].pool.Spawn(target.transform.position,
                        Quaternion.identity, Vector3.zero, slots[index].data.damage);
                    var clip = (AudioClip)typeof(Projectile)
                        .GetField(stone ? "stoneHitClip" : "enemyHitClip", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(shot);
                    Assert.That(clip, Is.Not.Null);
                    Assert.That(clip.name, Is.EqualTo(stone ? stoneClips[index] : targetClips[index]));
                    Assert.That(clip.channels, Is.EqualTo(1), "Spatial impact clips must be imported as mono.");
                    Assert.That(clip.loadState, Is.EqualTo(AudioDataLoadState.Loaded));
                    shot.SendMessage("OnTriggerEnter", target.GetComponent<Collider>(), SendMessageOptions.RequireReceiver);
                    AudioSource[] sounds = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None)
                        .Where(source => source.clip == clip).ToArray();
                    Assert.That(sounds.Length, Is.EqualTo(1));
                    Assert.That(sounds[0].transform.position, Is.EqualTo(target.transform.position));
                    Assert.That(sounds[0].spatialBlend, Is.EqualTo(1));
                    Assert.That(sounds[0].pitch, Is.EqualTo(1));
                    Assert.That(sounds[0].loop, Is.False);
                    Assert.That(sounds[0].outputAudioMixerGroup.name, Is.EqualTo("Impacts"));
                    Assert.That(slots[index].pool.ActiveCount, Is.Zero);
                    Assert.That(shot.gameObject.activeSelf, Is.False);
                    Object.Destroy(sounds[0].gameObject);
                    target.SetActive(false);
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator AllWeaponsSpawnAtTheAnimatedBarrelInHipAimAndRecoilPoses()
        {
            var slots = (WeaponSlot[])typeof(Weapon)
                .GetField("slots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weapon);
            WeaponEffects effects = weapon.GetComponent<WeaponEffects>();
            effects.enabled = false;
            for (int index = 0; index < slots.Length; index++)
            {
                typeof(Weapon).GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(weapon, new object[] { index });
                WeaponSlot slot = slots[index];
                Animator animator = slot.view.GetComponentInChildren<Animator>();
                animator.enabled = false;
                Transform model = animator.transform.Find("Model");
                AnimationClip idle = animator.runtimeAnimatorController.animationClips
                    .Single(clip => clip.name.EndsWith("_Idle"));
                idle.SampleAnimation(animator.gameObject, 0);
                Vector3 barrelOffset = model.InverseTransformPoint(slot.muzzle.position);
                foreach (string suffix in new[] { "_Idle", "_Aim", "_Fire", "_FireAim" })
                {
                    AnimationClip clip = animator.runtimeAnimatorController.animationClips
                        .Single(item => item.name.EndsWith(suffix));
                    foreach (float time in new[] { 0f, clip.length * 0.2f, clip.length })
                    {
                        clip.SampleAnimation(animator.gameObject, time);
                        Vector3 expected = model.TransformPoint(barrelOffset);
                        Assert.That(Vector3.Distance(slot.muzzle.position, expected), Is.LessThan(0.001f),
                            clip.name + " must move the muzzle with the visible barrel.");
                        Physics.SyncTransforms();
                        Fire();
                        Projectile[] shots = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
                        Assert.That(shots.Length, Is.EqualTo(slot.data.pellets), clip.name);
                        foreach (Projectile shot in shots)
                        {
                            Assert.That(Vector3.Distance(shot.transform.position, expected), Is.LessThan(0.001f));
                            typeof(Projectile).GetMethod("Release", BindingFlags.Instance | BindingFlags.NonPublic)
                                .Invoke(shot, null);
                        }
                        Assert.That(slot.pool.ActiveCount, Is.Zero);
                    }
                }
            }
            yield return null;
        }

        private GameObject Cube(Vector3 position, Vector3 scale)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.layer = LayerMask.NameToLayer("Environment");
            objects.Add(cube);
            return cube;
        }

        private void Fire() => typeof(Weapon).GetMethod("Fire", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(weapon, null);
    }
}
