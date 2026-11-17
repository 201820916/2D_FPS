using UnityEngine;

namespace GungeonFPS.Core
{
    public class BillboardSprite : MonoBehaviour
    {
        [SerializeField] private bool lockYAxis = true;
        private Transform _cameraTransform;

        private void Start()
        {
            if (Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null)
            {
                if (Camera.main == null) return;
                _cameraTransform = Camera.main.transform;
            }

            if (lockYAxis)
            {
                Vector3 dir = transform.position - _cameraTransform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }
            else
            {
                transform.rotation = _cameraTransform.rotation;
            }
        }
    }
}
