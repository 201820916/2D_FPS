using UnityEngine;

namespace GungeonFPS.Dungeon
{
    public class DoorController : MonoBehaviour
    {
        [SerializeField] private float openOffsetY = 4.2f;
        [SerializeField] private float slideSpeed = 8f;

        private Vector3 _closedPos;
        private Vector3 _openPos;
        private bool _isOpen = true;
        private Collider _collider;

        private void Awake()
        {
            _closedPos = transform.position;
            _openPos = _closedPos + Vector3.up * openOffsetY;
            _collider = GetComponent<Collider>();
            SetOpen(true, instant: true);
        }

        private void Update()
        {
            Vector3 target = _isOpen ? _openPos : _closedPos;
            if ((transform.position - target).sqrMagnitude > 0.0001f)
                transform.position = Vector3.MoveTowards(transform.position, target, slideSpeed * Time.deltaTime);
        }

        public void SetOpen(bool open, bool instant = false)
        {
            _isOpen = open;
            if (_collider != null)
                _collider.enabled = !open;

            if (instant)
                transform.position = _isOpen ? _openPos : _closedPos;
        }
    }
}
