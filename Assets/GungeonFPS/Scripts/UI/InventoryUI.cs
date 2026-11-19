using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GungeonFPS.Player;
using GungeonFPS.Weapons;

namespace GungeonFPS.UI
{
    public class InventoryUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private InventorySystem inventorySystem;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private SecondaryWeaponController secondaryController;

        [Header("UI Elements")]
        [SerializeField] private Text equippedMainText;
        [SerializeField] private Text equippedSecText;
        [SerializeField] private Text bagHeaderText;
        [SerializeField] private List<Button> slotButtons = new();
        [SerializeField] private List<Text> slotTexts = new();

        private void Awake()
        {
            IsOpen = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);

            for (int i = 0; i < slotButtons.Count; i++)
            {
                int slotIndex = i;
                if (slotButtons[i] != null)
                    slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotIndex));
            }
        }

        private void OnEnable()
        {
            if (inventorySystem != null)
                inventorySystem.OnInventoryChanged += RefreshUI;
        }

        private void OnDisable()
        {
            if (inventorySystem != null)
                inventorySystem.OnInventoryChanged -= RefreshUI;

            if (IsOpen)
                SetOpen(false);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                SetOpen(!IsOpen);
            }
            else if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
            }
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            if (panelRoot != null)
                panelRoot.SetActive(open);

            Time.timeScale = open ? 0f : 1f;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;

            if (open)
                RefreshUI();
        }

        private void OnSlotClicked(int index)
        {
            if (inventorySystem == null) return;
            inventorySystem.EquipFromSlot(index);
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (equippedMainText != null)
            {
                string mainName = weaponController != null && weaponController.CurrentWeapon != null
                    ? weaponController.CurrentWeapon.weaponName
                    : "NONE";
                equippedMainText.text = $"[MAIN]  {mainName}";
            }

            if (equippedSecText != null)
            {
                string secName = secondaryController != null && secondaryController.CurrentSecondary != null
                    ? secondaryController.CurrentSecondary.weaponName
                    : "NONE";
                equippedSecText.text = $"[SEC]   {secName}";
            }

            int count = inventorySystem != null ? inventorySystem.Items.Count : 0;
            int cap = inventorySystem != null ? inventorySystem.MaxCapacity : slotButtons.Count;
            if (bagHeaderText != null)
                bagHeaderText.text = $"BACKPACK ({count}/{cap})  —  CLICK TO EQUIP / SWAP";

            for (int i = 0; i < slotButtons.Count; i++)
            {
                bool hasItem = inventorySystem != null && i < inventorySystem.Items.Count;
                if (slotButtons[i] != null)
                    slotButtons[i].interactable = hasItem;

                if (i < slotTexts.Count && slotTexts[i] != null)
                {
                    if (hasItem)
                    {
                        var item = inventorySystem.Items[i];
                        string tag = item.IsMain ? "[MAIN]" : "[SEC]";
                        slotTexts[i].text = $"{tag}\n{item.DisplayName}";
                        slotTexts[i].color = item.AccentColor;
                    }
                    else
                    {
                        slotTexts[i].text = "— EMPTY —";
                        slotTexts[i].color = new Color(0.45f, 0.5f, 0.58f);
                    }
                }
            }
        }
    }
}
