using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using GungeonFPS.Core;
using GungeonFPS.UI;

namespace GungeonFPS.Dungeon
{
    public class FloorPortal : MonoBehaviour
    {
        [SerializeField] private float interactRadius = 3.2f;
        [SerializeField] private Transform visualRing;

        private Transform _player;
        private bool _triggered;

        private void Start()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }

        private void Update()
        {
            if (visualRing != null)
                visualRing.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);

            if (_triggered || _player == null || Keyboard.current == null) return;
            if (InventoryUI.IsOpen || GameOverUI.IsOpen) return;

            float distSq = (_player.position - transform.position).sqrMagnitude;
            if (distSq <= interactRadius * interactRadius)
            {
                int nextFloor = RunManager.CurrentFloor + 1;
                GameHUD.Instance?.RequestInteractPrompt($"[E] ENTER FLOOR {nextFloor}");

                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    _triggered = true;
                    RetroAudio.Instance?.Play(SfxType.DoorOpen, 1.1f);
                    RunManager.AdvanceToNextFloor(_player.gameObject);
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            }
        }
    }
}
