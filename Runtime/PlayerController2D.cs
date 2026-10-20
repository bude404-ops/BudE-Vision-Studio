using UnityEngine;

namespace BudeStudio
{
    /// <summary>
    /// Simple, tunable 2D player controller for starter maps.
    /// Arrow keys / A-D to move, Space / W / Up to jump.
    /// </summary>
    [AddComponentMenu("Bude Studio/Player Controller 2D")]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6f;
        public float jumpForce = 12f;

        [Header("Ground Check")]
        public LayerMask groundMask = ~0;
        public float groundCheckDistance = 0.15f;

        private Rigidbody2D _body;
        private Collider2D _col;
        private bool _grounded;
        private bool _facingRight = true;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
        }

        private void FixedUpdate()
        {
            float x = Input.GetAxisRaw("Horizontal");
            _body.linearVelocity = new Vector2(x * moveSpeed, _body.linearVelocity.y);

            if (x > 0f && !_facingRight) Flip();
            else if (x < 0f && _facingRight) Flip();
        }

        private void Update()
        {
            _grounded = IsGrounded();
            if (_grounded && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
                _body.linearVelocity = new Vector2(_body.linearVelocity.x, jumpForce);
        }

        private bool IsGrounded()
        {
            Bounds b = _col.bounds;
            Vector2 left = new Vector2(b.min.x + 0.02f, b.min.y - 0.01f);
            Vector2 right = new Vector2(b.max.x - 0.02f, b.min.y - 0.01f);
            return Physics2D.Linecast(left, left + Vector2.down * groundCheckDistance, groundMask)
                || Physics2D.Linecast(right, right + Vector2.down * groundCheckDistance, groundMask);
        }

        private void Flip()
        {
            _facingRight = !_facingRight;
            Vector3 s = transform.localScale;
            s.x *= -1f;
            transform.localScale = s;
        }
    }
}
