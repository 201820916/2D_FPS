using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GungeonFPS.Core;
using GungeonFPS.Player;

namespace GungeonFPS.UI
{
    public class GameOverUI : MonoBehaviour
    {
        public static GameOverUI Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text statsText;
        [SerializeField] private Button retryButton;
        [SerializeField] private InventorySystem inventorySystem;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            IsOpen = false;

            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                IsOpen = false;
            }
        }

        public void ShowGameOver()
        {
            if (IsOpen) return;
            IsOpen = true;

            if (panelRoot != null)
                panelRoot.SetActive(true);

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (statsText != null)
            {
                int core = inventorySystem != null ? inventorySystem.CoreCurrency : 0;
                statsText.text =
                    $"FLOOR REACHED:  {RunManager.CurrentFloor}\n" +
                    $"ENEMIES ELIMINATED:  {RunManager.TotalKills}\n" +
                    $"CORE COLLECTED:  {core}";
            }
        }

        private void OnRetryClicked()
        {
            IsOpen = false;
            Time.timeScale = 1f;
            RunManager.ResetRun();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
