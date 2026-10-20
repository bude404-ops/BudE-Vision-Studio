using UnityEngine;

namespace BudeStudio
{
    /// <summary>
    /// Simple 3D player controller for starter maps.
    /// WASD/arrows to move, Space to jump.
    /// </summary>
    [AddComponentMenu("Bude Studio/Player Controller 3D")]
    public class PlayerController3D : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6f;
        public float jumpForce = 6f;

        [Header("Ground Check")]
        public LayerMask groundMask = ~0;
        public float groundCheckDistance = 0.35f;

        private Rigidbody _body;
        private bool _grounded;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.freezeRotation = true;
        }

        private void Update()
        {
            _grounded = Physics.Raycast(transform.position, Vector3.down,
                GetComponent<Collider>().bounds.extents.y + groundCheckDistance, groundMask);

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = new Vector3(h, 0f, v).normalized;

            if (_grounded && Input.GetKeyDown(KeyCode.Space))
                _body.linearVelocity = new Vector3(_body.linearVelocity.x, jumpForce, _body.linearVelocity.z);

            Vector3 vel = _body.linearVelocity;
            Vector3 target = dir * moveSpeed;
            _body.linearVelocity = new Vector3(target.x, vel.y, target.z);

            if (dir.sqrMagnitude > 0.01f)
                transform.forward = Vector3.Slerp(transform.forward, dir, 0.25f);
        }
    }
}
