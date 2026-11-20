using UnityEngine;
using UnityEngine.UI;

namespace GungeonFPS.UI
{
    public class DamageIndicatorUI : MonoBehaviour
    {
        public static DamageIndicatorUI Instance { get; private set; }

        [SerializeField] private RectTransform indicatorPivot;
        [SerializeField] private Image indicatorImage;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float fadeDuration = 0.75f;

        private float _timer;
        private Vector3 _sourceDirection;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (indicatorImage != null)
                indicatorImage.color = new Color(1f, 0.15f, 0.25f, 0f);
        }

        public void ShowDamageFrom(Vector3 incomingHitDirection)
        {
            if (indicatorImage == null || indicatorPivot == null) return;
            if (incomingHitDirection.sqrMagnitude < 0.0001f) return;

            _sourceDirection = -incomingHitDirection;
            _sourceDirection.y = 0f;
            if (_sourceDirection.sqrMagnitude < 0.0001f) return;
            _sourceDirection.Normalize();

            _timer = fadeDuration;
            UpdateOrientationAndAlpha();
        }

        private void Update()
        {
            if (_timer <= 0f) return;
            _timer -= Time.deltaTime;
            UpdateOrientationAndAlpha();
        }

        private void UpdateOrientationAndAlpha()
        {
            if (playerTransform == null && Camera.main != null)
                playerTransform = Camera.main.transform;

            if (playerTransform != null)
            {
                Vector3 fwd = playerTransform.forward;
                fwd.y = 0f;
                fwd.Normalize();
                float signedAngle = Vector3.SignedAngle(fwd, _sourceDirection, Vector3.up);
                indicatorPivot.localEulerAngles = new Vector3(0f, 0f, -signedAngle);
            }

            float alpha = Mathf.Clamp01(_timer / Mathf.Max(0.05f, fadeDuration));
            indicatorImage.color = new Color(1f, 0.15f, 0.25f, alpha * 0.9f);
        }
    }
}
