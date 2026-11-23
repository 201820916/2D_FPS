using UnityEngine;

namespace GungeonFPS.Weapons
{
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "GungeonFPS/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity & HUD")]
        public string weaponName = "Rusty Pistol";
        public Sprite hudSprite;
        public Color projectileColor = new Color(1f, 0.9f, 0.3f, 1f);

        [Header("3D Viewmodel")]
        public GameObject viewmodelPrefab;
        public string viewmodelAddress; // Addressables key placeholder (install com.unity.addressables to use)
        public Vector3 viewmodelOffset = Vector3.zero;
        public Vector3 viewmodelEuler = new Vector3(0f, 180f, 0f);
        public float viewmodelScale = 0.55f;

        [Header("Fire Settings")]
        public float fireRate = 4f; // shots per second
        public int pelletsPerShot = 1;
        public float spreadAngle = 1.5f;
        public float damage = 15f;
        public float projectileSpeed = 38f;
        public float projectileScale = 0.35f;
        public float knockbackForce = 4f;

        [Header("Gimmick / Ammo")]
        public int maxBounces = 0;
        public int magazineSize = 12;
        public int maxTotalAmmo = -1; // -1 = infinite
        public float reloadTime = 0.85f;
        public bool automatic = true;

        [Header("Feel")]
        public float recoilKickPixels = 34f;
        public float screenShake = 0.04f;
    }
}
