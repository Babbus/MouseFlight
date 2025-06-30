using UnityEngine;

namespace DomeClash.Weapons
{
    public abstract class WeaponData : ScriptableObject
    {
        public string weaponName;
        public float damage;
        public GameObject projectilePrefab;
        public AudioClip fireSfx;
        // Ortak alanlar
    }

    [CreateAssetMenu(fileName = "PrimaryWeaponData", menuName = "Weapons/Primary Weapon")]
    public class PrimaryWeaponData : WeaponData
    {
        // Primary silaha özel alanlar (ör. fireRate, spread, vb.)
        public float fireRate = 10f;
        public float projectileSpeed = 100f;
    }

    [CreateAssetMenu(fileName = "MissileWeaponData", menuName = "Weapons/Missile Weapon")]
    public class MissileWeaponData : WeaponData
    {
        public float lockRadius = 50f;
        public LayerMask targetMask;
        public float lockTime = 2f;
        // Missile'a özel alanlar
    }
} 