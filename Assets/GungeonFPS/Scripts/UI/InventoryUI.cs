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

        [Header("Header & Equipped")]
        [SerializeField] private Text coreCurrencyText;
        [SerializeField] private Text equippedMainText;
        [SerializeField] private Text equippedSecText;
        [SerializeField] private Text bagHeaderText;

        [Header("Backpack Slots")]
        [SerializeField] private List<Button> slotButtons = new();
        [SerializeField] private List<Text> slotTexts = new();
        [SerializeField] private List<Button> dismantleButtons = new();

        [Header("Upgrade Frame (2-Branch Skeleton)")]
        [SerializeField] private Button selectMainUpgradeButton;
        [SerializeField] private Button selectSecUpgradeButton;
        [SerializeField] private Text upgradeTargetText;
        [SerializeField] private Text routeATitleText;
        [SerializeField] private Text routeADescText;
        [SerializeField] private Button routeAButton;
        [SerializeField] private Text routeBTitleText;
        [SerializeField] private Text routeBDescText;
        [SerializeField] private Button routeBButton;

        private bool _inspectingMain = true;

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

            for (int i = 0; i < dismantleButtons.Count; i++)
            {
                int slotIndex = i;
                if (dismantleButtons[i] != null)
                    dismantleButtons[i].onClick.AddListener(() => OnDismantleClicked(slotIndex));
            }

            if (selectMainUpgradeButton != null)
                selectMainUpgradeButton.onClick.AddListener(() => { _inspectingMain = true; RefreshUI(); });
            if (selectSecUpgradeButton != null)
                selectSecUpgradeButton.onClick.AddListener(() => { _inspectingMain = false; RefreshUI(); });

            // Upgrade buttons are skeleton placeholders until per-weapon routes are designed
            if (routeAButton != null) routeAButton.interactable = false;
            if (routeBButton != null) routeBButton.interactable = false;
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

        private void OnDismantleClicked(int index)
        {
            if (inventorySystem == null) return;
            inventorySystem.DismantleSlot(index);
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (coreCurrencyText != null)
            {
                int core = inventorySystem != null ? inventorySystem.CoreCurrency : 0;
                coreCurrencyText.text = $"CORE: {core}";
            }

            string mainName = weaponController != null && weaponController.CurrentWeapon != null
                ? weaponController.CurrentWeapon.weaponName
                : "NONE";
            string secName = secondaryController != null && secondaryController.CurrentSecondary != null
                ? secondaryController.CurrentSecondary.weaponName
                : "NONE";

            if (equippedMainText != null)
                equippedMainText.text = $"[MAIN]  {mainName}";
            if (equippedSecText != null)
                equippedSecText.text = $"[SEC]   {secName}";

            int count = inventorySystem != null ? inventorySystem.Items.Count : 0;
            int cap = inventorySystem != null ? inventorySystem.MaxCapacity : slotButtons.Count;
            if (bagHeaderText != null)
                bagHeaderText.text = $"BACKPACK ({count}/{cap})  —  CLICK SLOT TO EQUIP / DISMANTLE FOR CORE";

            for (int i = 0; i < slotButtons.Count; i++)
            {
                bool hasItem = inventorySystem != null && i < inventorySystem.Items.Count;
                if (slotButtons[i] != null)
                    slotButtons[i].interactable = hasItem;
                if (i < dismantleButtons.Count && dismantleButtons[i] != null)
                    dismantleButtons[i].interactable = hasItem;

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

            // Refresh 2-Branch Upgrade Frame skeleton
            string targetLabel = _inspectingMain ? $"[MAIN] {mainName}" : $"[SEC] {secName}";
            if (upgradeTargetText != null)
                upgradeTargetText.text = $"UPGRADE MODULE  |  TARGET: {targetLabel}";

            if (routeATitleText != null)
                routeATitleText.text = "ROUTE A — [TBD]";
            if (routeADescText != null)
                routeADescText.text = $"{targetLabel}\nRoute A specialization\n(Design Pending)";

            if (routeBTitleText != null)
                routeBTitleText.text = "ROUTE B — [TBD]";
            if (routeBDescText != null)
                routeBDescText.text = $"{targetLabel}\nRoute B specialization\n(Design Pending)";
        }
    }
}
