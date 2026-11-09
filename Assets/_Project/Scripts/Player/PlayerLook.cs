using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>Rotates the player and CameraTarget; PauseMenu owns Escape.</summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerLook : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [Header("Look Settings")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.08f;
        [SerializeField, Range(0f, 89f)] private float verticalLookLimit = 85f;
        private PlayerInputReader inputReader;
        private float cameraPitch;
        private bool isCursorLocked;
        public bool IsCursorLocked => isCursorLocked;

        private void Awake()
        {
            inputReader = GetComponent<PlayerInputReader>();
            if (cameraTransform == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                if (childCamera != null) cameraTransform = childCamera.transform;
            }
            if (cameraTransform == null)
            {
                Debug.LogError("PlayerLook: assign CameraTarget.", this);
                enabled = false;
                return;
            }
            LockCursor();
        }

        private void Update()
        {
            UpdateCursorState();
            if (!isCursorLocked) return;
            Vector2 lookDelta = inputReader.ReadLookDelta();
            transform.Rotate(Vector3.up * (lookDelta.x * mouseSensitivity), Space.Self);
            cameraPitch = Mathf.Clamp(cameraPitch - lookDelta.y * mouseSensitivity,
                -verticalLookLimit, verticalLookLimit);
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void UpdateCursorState()
        {
            if (!isCursorLocked && inputReader.WasPrimaryButtonPressed()) LockCursor();
        }

        /// <summary>Updates both Unity's cursor and the state read by Weapon.</summary>
        public void SetCursorLocked(bool locked)
        {
            if (locked) LockCursor();
            else UnlockCursor();
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isCursorLocked = true;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isCursorLocked = false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) UnlockCursor();
        }

        private void OnDisable() { UnlockCursor(); }
    }
}