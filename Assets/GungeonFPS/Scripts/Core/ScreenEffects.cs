using UnityEngine;
using UnityEngine.UI;

namespace GungeonFPS.Core
{
    public class ScreenEffects : MonoBehaviour
    {
        public static ScreenEffects Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform cameraShakeTarget;
        [SerializeField] private Image flashOverlay;

        [Header("FOV Settings")]
        [SerializeField] private float baseFov = 80f;
        [SerializeField] private float dashFovBoost = 12f;
        [SerializeField] private float fovReturnSpeed = 10f;

        private float _shakeIntensity;
        private float _shakeDecay = 12f;
        private float _targetFovOffset;
        private float _currentTilt;
        private float _targetTilt;
        private Color _flashColor;
        private Vector3 _baseLocalPos;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (targetCamera == null) targetCamera = Camera.main;
            if (cameraShakeTarget != null) _baseLocalPos = cameraShakeTarget.localPosition;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Camera shake
            if (cameraShakeTarget != null)
            {
                if (_shakeIntensity > 0.001f)
                {
                    Vector2 offset = Random.insideUnitCircle * _shakeIntensity;
                    cameraShakeTarget.localPosition = _baseLocalPos + new Vector3(offset.x, offset.y, 0f);
                    _shakeIntensity = Mathf.MoveTowards(_shakeIntensity, 0f, _shakeDecay * dt);
                }
                else
                {
                    cameraShakeTarget.localPosition = _baseLocalPos;
                }

                // Smooth camera tilt
                _currentTilt = Mathf.Lerp(_currentTilt, _targetTilt, dt * 16f);
                _targetTilt = Mathf.Lerp(_targetTilt, 0f, dt * 10f);
                Vector3 euler = cameraShakeTarget.localEulerAngles;
                cameraShakeTarget.localEulerAngles = new Vector3(euler.x, euler.y, _currentTilt);
            }

            // FOV punch
            if (targetCamera != null)
            {
                _targetFovOffset = Mathf.Lerp(_targetFovOffset, 0f, dt * fovReturnSpeed);
                targetCamera.fieldOfView = baseFov + _targetFovOffset;
            }

            // Screen flash
            if (flashOverlay != null && _flashColor.a > 0.001f)
            {
                _flashColor.a = Mathf.MoveTowards(_flashColor.a, 0f, dt * 3.5f);
                flashOverlay.color = _flashColor;
            }
        }

        public void AddShake(float intensity)
        {
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
        }

        public void TriggerDashEffect(float strafeDir)
        {
            _targetFovOffset = dashFovBoost;
            _targetTilt = -strafeDir * 6f;
        }

        public void TriggerFlash(Color color, float alpha = 0.45f)
        {
            if (flashOverlay == null) return;
            _flashColor = new Color(color.r, color.g, color.b, alpha);
            flashOverlay.color = _flashColor;
        }
    }
}
