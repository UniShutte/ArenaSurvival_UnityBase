using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary> Возвращает Player в начальную позицию, если персонаж упал ниже заданной высоты. </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerFallRespawner : MonoBehaviour
    {
        [Header("Respawn Settings")]
        [SerializeField]
        private float fallThresholdY = -10f;

        [SerializeField, Min(0f)]
        private float safeHeightOffset = 0.2f;

        private CharacterController characterController;

        private Vector3 respawnPosition;
        private Quaternion respawnRotation;

        private void Awake()
        {
            characterController =
                GetComponent<CharacterController>();
        }

        private void Start()
        {
            // Player создаётся в PlayerSpawnPoint,
            // поэтому стартовый Transform используется
            // как точка возврата.
            respawnPosition = transform.position;
            respawnRotation = transform.rotation;
        }

        private void Update()
        {
            if (transform.position.y < fallThresholdY)
            {
                Respawn();
            }
        }

        [ContextMenu("Respawn")]
        public void Respawn()
        {
            // Перед телепортацией временно отключаем
            // CharacterController.
            characterController.enabled = false;

            transform.SetPositionAndRotation(
                respawnPosition +
                Vector3.up * safeHeightOffset,
                respawnRotation);

            characterController.enabled = true;
        }
    }
}