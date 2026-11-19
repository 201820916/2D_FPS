using System;
using System.Collections.Generic;
using UnityEngine;
using GungeonFPS.Core;
using GungeonFPS.Weapons;

namespace GungeonFPS.Player
{
    [Serializable]
    public class InventoryItem
    {
        public WeaponData mainWeapon;
        public SecondaryWeaponData secondaryWeapon;

        public bool IsMain => mainWeapon != null;
        public string DisplayName => mainWeapon != null ? mainWeapon.weaponName : (secondaryWeapon != null ? secondaryWeapon.weaponName : "EMPTY");
        public Color AccentColor => mainWeapon != null ? mainWeapon.projectileColor : (secondaryWeapon != null ? secondaryWeapon.accentColor : Color.gray);
    }

    public class InventorySystem : MonoBehaviour
    {
        [SerializeField] private int maxCapacity = 6;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private SecondaryWeaponController secondaryController;

        public int MaxCapacity => maxCapacity;
        public IReadOnlyList<InventoryItem> Items => _items;
        public bool HasSpace => _items.Count < maxCapacity;
        public int CoreCurrency { get; private set; }

        public event Action OnInventoryChanged;

        private readonly List<InventoryItem> _items = new();

        private void Awake()
        {
            if (weaponController == null) weaponController = GetComponent<WeaponController>();
            if (secondaryController == null) secondaryController = GetComponent<SecondaryWeaponController>();
        }

        public bool TryAddMain(WeaponData data)
        {
            if (data == null || !HasSpace) return false;
            _items.Add(new InventoryItem { mainWeapon = data });
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool TryAddSecondary(SecondaryWeaponData data)
        {
            if (data == null || !HasSpace) return false;
            _items.Add(new InventoryItem { secondaryWeapon = data });
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void EquipFromSlot(int index)
        {
            if (index < 0 || index >= _items.Count) return;

            InventoryItem item = _items[index];
            if (item.IsMain && weaponController != null)
            {
                WeaponData prev = weaponController.EquipMainWeapon(item.mainWeapon);
                if (prev != null)
                    item.mainWeapon = prev;
                else
                    _items.RemoveAt(index);

                RetroAudio.Instance?.Play(SfxType.Pickup, 0.85f);
                OnInventoryChanged?.Invoke();
            }
            else if (!item.IsMain && secondaryController != null)
            {
                SecondaryWeaponData prev = secondaryController.EquipSecondary(item.secondaryWeapon);
                if (prev != null)
                    item.secondaryWeapon = prev;
                else
                    _items.RemoveAt(index);

                RetroAudio.Instance?.Play(SfxType.Pickup, 0.85f);
                OnInventoryChanged?.Invoke();
            }
        }

        public void AddCurrency(int amount)
        {
            CoreCurrency = Mathf.Max(0, CoreCurrency + amount);
            OnInventoryChanged?.Invoke();
        }
    }
}
