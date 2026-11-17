using UnityEngine;
using UnityEngine.InputSystem;
using GungeonFPS.Core;
using GungeonFPS.Enemies;

namespace GungeonFPS.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement (High-Speed Run & Gun)")]
        [SerializeField] private float moveSpeed = 11f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float jumpForce = 7.5f;

        [Header("Mouse Look")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float mouseSensitivity = 0.14f;
        [SerializeField] private float maxLookAngle = 85f;

        [Header("Invincible Dash")]
        [SerializeField] private float dashSpeed = 28f;
        [SerializeField] private float dashDuration = 0.18f;
        [SerializeField] private float dashCooldown = 0.75f;

        [Header("Blank (Clear Enemy Bullets)")]
        [SerializeField] private int startingBlanks = 2;
        [SerializeField] private float blankRadius = 25f;
        [SerializeField] private float blankKnockback = 14f;

        public bool IsDashInvincible => _dashTimer > 0f;
        public float DashCooldownProgress => dashCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(_dashCooldownTimer / dashCooldown);
        public int BlanksCount { get; private set; }
        public float CurrentHorizontalSpeed { get; private set; }

        private CharacterController _cc;
        private float _pitch;
        private float _verticalVelocity;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private Vector3 _dashDir;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            BlanksCount = startingBlanks;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (Keyboard.current == null || Mouse.current == null) return;

            HandleCursorToggle();
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                HandleLook();
                HandleDashAndBlank();
            }

            HandleMovement();
        }

        private void HandleCursorToggle()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }
            else if (Cursor.lockState != CursorLockMode.Locked && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleLook()
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(Vector3.up * delta.x);

            _pitch = Mathf.Clamp(_pitch - delta.y, -maxLookAngle, maxLookAngle);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleDashAndBlank()
        {
            if (_dashCooldownTimer > 0f)
                _dashCooldownTimer -= Time.deltaTime;

            if (_dashTimer > 0f)
                _dashTimer -= Time.deltaTime;

            bool dashPressed = Keyboard.current.leftShiftKey.wasPressedThisFrame ||
                               Mouse.current.rightButton.wasPressedThisFrame;

            if (dashPressed && _dashCooldownTimer <= 0f && _dashTimer <= 0f)
            {
                Vector2 input = ReadMoveInput();
                Vector3 wishDir = (transform.right * input.x + transform.forward * input.y);
                if (wishDir.sqrMagnitude < 0.01f)
                    wishDir = transform.forward;

                _dashDir = wishDir.normalized;
                _dashTimer = dashDuration;
                _dashCooldownTimer = dashCooldown;

                ScreenEffects.Instance?.TriggerDashEffect(input.x);
                ScreenEffects.Instance?.TriggerFlash(new Color(0.3f, 0.9f, 1f), 0.15f);
                ScreenEffects.Instance?.AddShake(0.06f);
                RetroAudio.Instance?.Play(SfxType.Dash);
            }

            bool blankPressed = Keyboard.current.qKey.wasPressedThisFrame ||
                                Keyboard.current.eKey.wasPressedThisFrame;
            if (blankPressed && BlanksCount > 0)
            {
                TriggerBlank();
            }
        }

        public void AddBlank(int amount)
        {
            BlanksCount += Mathf.Max(0, amount);
        }

        private void TriggerBlank()
        {
            BlanksCount--;
            EnemyBullet.ClearAllBullets();
            ScreenEffects.Instance?.TriggerFlash(new Color(0.65f, 0.9f, 1f), 0.7f);
            ScreenEffects.Instance?.AddShake(0.28f);
            RetroAudio.Instance?.Play(SfxType.Blank);
            VfxSpawner.Instance?.SpawnBurst(transform.position + Vector3.up * 0.8f, new Color(0.6f, 0.95f, 1f), 20, 12f, 0.25f);

            var hits = Physics.OverlapSphere(transform.position, blankRadius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var dmg) && hit.gameObject != gameObject)
                {
                    Vector3 dir = (hit.transform.position - transform.position).normalized;
                    dir.y = 0f;
                    dmg.TakeDamage(0f, dir, blankKnockback);
                }
            }
        }

        private void HandleMovement()
        {
            Vector2 input = Cursor.lockState == CursorLockMode.Locked ? ReadMoveInput() : Vector2.zero;

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            if (_cc.isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame && _dashTimer <= 0f)
                _verticalVelocity = jumpForce;

            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 horizontalMove;
            if (_dashTimer > 0f)
            {
                horizontalMove = _dashDir * dashSpeed;
            }
            else
            {
                Vector3 moveDir = (transform.right * input.x + transform.forward * input.y);
                if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();
                horizontalMove = moveDir * moveSpeed;
            }

            CurrentHorizontalSpeed = new Vector2(horizontalMove.x, horizontalMove.z).magnitude;
            Vector3 velocity = horizontalMove + Vector3.up * _verticalVelocity;
            _cc.Move(velocity * Time.deltaTime);
        }

        private Vector2 ReadMoveInput()
        {
            float x = 0f;
            float y = 0f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.wKey.isPressed) y += 1f;
            if (Keyboard.current.sKey.isPressed) y -= 1f;
            return new Vector2(x, y);
        }
    }
}
