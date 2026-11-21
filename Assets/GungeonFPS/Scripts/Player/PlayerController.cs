using UnityEngine;
using UnityEngine.InputSystem;
using GungeonFPS.Core;
using GungeonFPS.Enemies;
using GungeonFPS.UI;

namespace GungeonFPS.Player
{
    /// 1인칭 플레이어의 이동, 마우스 시점 회전, 무적 대시, 공포탄(Blank)을 담당

    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement (High-Speed Run & Gun)")]
        [SerializeField] private float moveSpeed = 11f;          // 기본 수평 이동 속도
        [SerializeField] private float gravity = -25f;           // 중력 가속도 (음수)
        [SerializeField] private float jumpForce = 7.5f;         // 점프 시 위로 가해지는 초기 속도

        [Header("Mouse Look")]
        [SerializeField] private Transform cameraPivot;          // 상하 시점(Pitch)만 담당하는 눈높이 피벗 오브젝트
        [SerializeField] private float mouseSensitivity = 0.14f; // 마우스 감도
        [SerializeField] private float maxLookAngle = 85f;       // 고개를 위/아래로 꺾을 수 있는 최대 각도

        [Header("Invincible Dash")]
        [SerializeField] private float dashSpeed = 28f;          // 대시 중 이동 속도
        [SerializeField] private float dashDuration = 0.18f;     // 대시 지속 시간 (= 무적 프레임 시간)
        [SerializeField] private float dashCooldown = 0.75f;     // 대시 재사용 대기 시간

        [Header("Blank (Clear Enemy Bullets)")]
        [SerializeField] private int startingBlanks = 2;         // 시작 시 보유한 공포탄 개수
        [SerializeField] private float blankRadius = 25f;        // 공포탄 사용 시 적을 밀어내는 반경
        [SerializeField] private float blankKnockback = 14f;     // 공포탄 충격파의 넉백 세기

        // 대시 타이머가 남아있으면 무적 상태로 판정 (PlayerHealth에서 참조)
        public bool IsDashInvincible => _dashTimer > 0f;
        // HUD 대시 게이지 바용 0~1 진행도 (1이면 사용 가능)
        public float DashCooldownProgress => dashCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(_dashCooldownTimer / dashCooldown);
        public int BlanksCount { get; private set; }
        // 무기 흔들림(Bobbing) 계산을 위해 현재 수평 이동 속도를 외부에 공개
        public float CurrentHorizontalSpeed { get; private set; }

        private CharacterController _cc;
        private float _pitch;               // 누적된 상하 시점 각도 (X축 회전)
        private float _verticalVelocity;    // 점프·중력으로 변하는 Y축 속도
        private float _dashTimer;           // 남은 대시 지속 시간
        private float _dashCooldownTimer;   // 남은 대시 쿨타임
        private Vector3 _dashDir;           // 대시 시작 시 고정된 이동 방향

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            BlanksCount = startingBlanks;

            // 게임 시작 시 마우스 커서를 화면 중앙에 잠그고 숨김
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // 인벤토리 UI가 열려 있을 때는 플레이어 조작을 완전히 멈춤
            if (InventoryUI.IsOpen) return;
            if (Keyboard.current == null || Mouse.current == null) return;

            HandleCursorToggle();

            // 커서가 잠겨 있을 때만 시점 회전 및 대시/공포탄 입력을 받음
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                HandleLook();
                HandleDashAndBlank();
            }

            HandleMovement();
        }

        /// 테스트용 : ESC로 커서 잠금을 해제/복구
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

        /// 마우스 이동량에 따라 좌우(Player 본체)와 상하(CameraPivot)를 분리해 회전.
        private void HandleLook()
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            // 좌우(Yaw)는 Player 몸통(Y축)을 회전시켜 이동 방향(forward/right)과 일치시킴
            // Mouse.current.delta.ReadValue() : 이번 프레임 동안 마우스가 움직인 변화량 (x,y) 반환
            // delta.x = 마우스 좌우 이동량
            // delta.y = 마우스 상하 이동량
            // mouseSensitivity : 픽셀 단위 이동량이 너무 커서 감도(0.14)를 곱해 회전 각도로 축소.



            transform.Rotate(Vector3.up * delta.x);
            // 상하(Pitch)는 최대 각도로 제한한 뒤 CameraPivot(X축)만 회전시켜 몸통이 눕지 않게 함
            // Vector3.up (0, 1, 0) * delta.x (0, delta.x, 0) = (0, delta.x, 0);
            // 마우스 오른쪽 -> 몸통 오른쪽으로 회전
            // 마우스 왼쪽 -> 몸통 왼쪽 회전



            _pitch = Mathf.Clamp(_pitch - delta.y, -maxLookAngle, maxLookAngle);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            // 현재 상하각도(_pitch)에 마우스 상하 이동량 반영
            // 마우스 위로 -> _pitch 감소 -> 위를 봄 ( 좌표 0,0은 관점에 따라 다름 )
            // 마우스 아래로 -> _pitch 증가 -> 아래를 봄

            // Mathf.Clamp
            // -maxLookAngle = -85
            // maxLookAngle = 85
            // 사이로 시야각 제한

            // cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            // 일반 각도를 유니티 회전 형식 Quaternion으로 변환
            // localRotation : 부모 기준 x축만 _pitch로 설정, Y-Z는 0으로 고정
        }




        /// Left Shift 무적 대시와 Q 공포탄 입력을 처리.
        private void HandleDashAndBlank()
        {
            if (_dashCooldownTimer > 0f)
                _dashCooldownTimer -= Time.deltaTime;

            if (_dashTimer > 0f)
                _dashTimer -= Time.deltaTime;

            bool dashPressed = Keyboard.current.leftShiftKey.wasPressedThisFrame;

            if (dashPressed && _dashCooldownTimer <= 0f && _dashTimer <= 0f)
            {
                // 현재 WASD 입력 방향으로 대시 (입력이 없으면 정면으로 대시)
                Vector2 input = ReadMoveInput();
                Vector3 wishDir = (transform.right * input.x + transform.forward * input.y);
                if (wishDir.sqrMagnitude < 0.01f)
                    wishDir = transform.forward;

                _dashDir = wishDir.normalized;
                _dashTimer = dashDuration;
                _dashCooldownTimer = dashCooldown;

                // 대시 연출: 좌우 기울기/FOV 확장, 청록색 화면 플래시, 약한 흔들림, 효과음
                ScreenEffects.Instance?.TriggerDashEffect(input.x);
                ScreenEffects.Instance?.TriggerFlash(new Color(0.3f, 0.9f, 1f), 0.15f);
                ScreenEffects.Instance?.AddShake(0.06f);
                RetroAudio.Instance?.Play(SfxType.Dash);
            }

            bool blankPressed = Keyboard.current.qKey.wasPressedThisFrame;
            if (blankPressed && BlanksCount > 0)
            {
                TriggerBlank();
            }
        }



        /// 아이템 습득 등으로 공포탄 개수를 추가합니다.
        public void AddBlank(int amount)
        {
            BlanksCount += Mathf.Max(0, amount);
        }

        /// <summary>
        /// 공포탄 발동: 화면의 모든 적 탄막을 지우고, 주변 적을 강하게 밀어냅니다.
        /// </summary>
        private void TriggerBlank()
        {
            BlanksCount--;
            EnemyBullet.ClearAllBullets();
            ScreenEffects.Instance?.TriggerFlash(new Color(0.65f, 0.9f, 1f), 0.7f);
            ScreenEffects.Instance?.AddShake(0.28f);
            RetroAudio.Instance?.Play(SfxType.Blank);
            VfxSpawner.Instance?.SpawnBurst(transform.position + Vector3.up * 0.8f, new Color(0.6f, 0.95f, 1f), 20, 12f, 0.25f);

            // 반경 내의 모든 적에게 데미지 0 + 강한 넉백 적용
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



        /// 수평 이동(일반/대시), 점프, 중력을 합쳐 CharacterController를 이동시킵니다.
        private void HandleMovement()
        {
            Vector2 input = Cursor.lockState == CursorLockMode.Locked ? ReadMoveInput() : Vector2.zero;

            // 바닥에 붙어 있을 때 살짝 아래로 눌러주어 경사로/계단에서도 isGrounded 유지
            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            // 대시 중이 아닐 때만 점프 허용
            if (_cc.isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame && _dashTimer <= 0f)
                _verticalVelocity = jumpForce;

            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 horizontalMove;
            if (_dashTimer > 0f)
            {
                // 대시 중에는 고정된 대시 방향으로 고속 이동
                horizontalMove = _dashDir * dashSpeed;
            }
            else
            {
                // 대각선 이동 시 속도가 빨라지지 않도록 벡터 크기를 1로 정규화
                Vector3 moveDir = (transform.right * input.x + transform.forward * input.y);
                if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();
                horizontalMove = moveDir * moveSpeed;
            }

            CurrentHorizontalSpeed = new Vector2(horizontalMove.x, horizontalMove.z).magnitude;
            Vector3 velocity = horizontalMove + Vector3.up * _verticalVelocity;
            _cc.Move(velocity * Time.deltaTime);
        }



        /// New Input System에서 WASD 키 상태를 읽어 2D 방향 벡터로 반환합니다.
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
