using Cinemachine;
using UnityEngine;

namespace ArenaSurvival.Lab4
{
    public sealed class CameraFollowTarget : MonoBehaviour
    {
        [SerializeField] private Transform cameraTarget;
        private CinemachineVirtualCamera virtualCamera;

        private void Start()
        {
            // The player prefab cannot hold a reference to a scene camera.
            virtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
            if (cameraTarget == null || virtualCamera == null)
            {
                Debug.LogError("Assign Camera Target and create one active Cinemachine Virtual Camera.", this);
                enabled = false;
                return;
            }
            virtualCamera.Follow = cameraTarget;
            virtualCamera.LookAt = null;
        }

        private void OnDestroy()
        {
            if (virtualCamera != null && virtualCamera.Follow == cameraTarget)
                virtualCamera.Follow = null;
        }
    }
}
