using System.Collections;
using System.Collections.Generic;
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
            muzzle = player.transform.Find("CameraTarget/FirePoint");
            pool = Object.FindFirstObjectByType<ProjectilePool>();
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
                Assert.That(health.CurrentHealth, Is.EqualTo(75), "Crosshair hit at distance " + distance);
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
            Assert.That(health.CurrentHealth, Is.EqualTo(75));
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
            Assert.That(velocity.magnitude, Is.EqualTo(25).Within(0.001f));
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
