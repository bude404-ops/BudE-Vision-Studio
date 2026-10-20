using UnityEngine;

namespace BudeStudio
{
    /// <summary>Smooth camera follow for starter maps (2D and 3D).</summary>
    [AddComponentMenu("Bude Studio/Camera Follow")]
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("Usually the Player object.")]
        public Transform target;

        [Header("2D")]
        public bool is2D = true;
        public float smoothTime2D = 0.15f;
        [Tooltip("Camera looks a little above the player in 2D.")]
        public float heightOffset2D = 1.0f;
        public float fixedZ = -10f;

        [Header("3D")]
        public Vector3 offset3D = new Vector3(0f, 6f, -8f);
        public float smoothTime3D = 0.12f;

        private Vector3 _velocity;

        private void LateUpdate()
        {
            if (target == null) return;

            if (is2D)
            {
                Vector3 goal = new Vector3(target.position.x, target.position.y + heightOffset2D, fixedZ);
                transform.position = Vector3.SmoothDamp(transform.position, goal, ref _velocity, smoothTime2D);
            }
            else
            {
                Vector3 goal = target.position + offset3D;
                transform.position = Vector3.SmoothDamp(transform.position, goal, ref _velocity, smoothTime3D);
                transform.LookAt(target.position + Vector3.up * 0.5f);
            }
        }
    }
}
