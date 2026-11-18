using UnityEngine;

namespace GungeonFPS.Weapons
{
    public enum SecondaryWeaponType
    {
        EnergyBlade,
        AegisShield
    }

    [CreateAssetMenu(fileName = "NewSecondaryWeapon", menuName = "GungeonFPS/Secondary Weapon Data")]
    public class SecondaryWeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "NEON BLADE";
        public SecondaryWeaponType secondaryType = SecondaryWeaponType.EnergyBlade;
        public Color accentColor = new Color(0.25f, 0.95f, 1f);

        [Header("Blade Settings")]
        public float cooldown = 0.55f;
        public float damage = 32f;
        public float range = 3.2f;
        public float knockback = 12f;
        public float bulletClearRadius = 2.6f;

        [Header("Shield Settings")]
        public float maxEnergy = 100f;
        public float drainPerSecond = 32f;
        public float regenPerSecond = 22f;
    }
}
