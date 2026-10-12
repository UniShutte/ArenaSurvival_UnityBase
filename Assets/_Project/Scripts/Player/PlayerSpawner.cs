using UnityEngine;

namespace ArenaSurvival.Spawning
{
    /// <summary>
    /// Создаёт Player в заданной точке появления.
    ///
    /// Компонент не управляет Player после создания и ничего не знает о пулах
    /// снарядов: три пула находятся внутри prefab Player, поэтому передавать
    /// их из сцены больше не нужно.
    /// </summary>
    public sealed class PlayerSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject playerPrefab;   // ассет PF_FirstPersonPlayer
        [SerializeField] private Transform spawnPoint;      // объект сцены PlayerSpawnPoint

        private GameObject spawnedPlayer;   // ссылка на созданный экземпляр

        private void Start()
        {
            SpawnPlayer();
        }

        [ContextMenu("Spawn Player")]   // позволяет вызвать метод из контекстного меню компонента
        public void SpawnPlayer()
        {
            if (spawnedPlayer != null)
            {
                Debug.LogWarning("PlayerSpawner: Player уже создан.", this);
                return;
            }

            if (playerPrefab == null)
            {
                Debug.LogError("PlayerSpawner: Player Prefab не назначен.", this);
                return;
            }

            if (spawnPoint == null)
            {
                Debug.LogError("PlayerSpawner: Spawn Point не назначен.", this);
                return;
            }

            // Положение берём из точки появления, а поворот — только вокруг Y,
            // чтобы персонаж не появился наклонённым.
            spawnedPlayer = Instantiate(
                playerPrefab,
                spawnPoint.position,
                Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f));

            spawnedPlayer.name = playerPrefab.name;
        }
    }
}