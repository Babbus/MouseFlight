using UnityEngine;
using DomeClash.Core;

namespace DomeClash.Weapons.BastionWeapons
{
    /// <summary>
    /// Hammercoil - Bastion Primary Weapon
    /// A heavy kinetic cannon that launches slow, high-impact rounds capable of knocking enemies back.
    /// Ideal for area denial and frontline control.
    /// </summary>
    public class Hammercoil : ProjectileGun
    {
        [Header("Hammercoil Specific")]
        [SerializeField] private GameObject hammercoilProjectilePrefab;
        [SerializeField] private ParticleSystem heavyMuzzleFlash;
        [SerializeField] private AudioClip hammercoilFireSound;
        [SerializeField] private float screenShakeIntensity = 0.3f;
        [SerializeField] private float screenShakeDuration = 0.2f;

        protected override void Awake()
        {
            base.Awake();
            
            // Set Hammercoil stats according to GDD
            weaponName = "Hammercoil";
            weaponType = WeaponType.ProjectileGun;
            
            // Configure stats for heavy kinetic cannon
            stats.damage = 150f; // Very High damage
            stats.fireRate = 0.8f; // Low rate of fire
            stats.range = 300f; // Medium range
            stats.projectileSpeed = 60f; // Slow projectiles
            stats.damageType = DamageType.Kinetic;
            
            // Physical properties
            stats.mass = 420f; // Heavy weapon (from GDD)
            stats.knockbackForce = 25f; // Generates knockback on hit
            stats.recoil = 15f; // High recoil
            
            // Heat management
            stats.heatGeneration = 25f; // High heat per shot
            stats.maxHeat = 100f;
            stats.heatDissipation = 8f; // Slow cooling
            stats.overheatCooldown = 3f;
            
            // Energy consumption
            stats.energyConsumption = 20f; // High energy cost
            
            // Combat properties
            stats.criticalChance = 0.08f;
            stats.criticalMultiplier = 2.0f;
            
            // Set projectile prefab
            if (hammercoilProjectilePrefab != null)
            {
                SetProjectilePrefab(hammercoilProjectilePrefab);
            }
        }

        protected override void Fire()
        {
            base.Fire();
            
            // Apply screen shake for heavy weapon feel
            ApplyScreenShake();
            
            // Apply recoil to ship
            ApplyRecoilToShip();
            
            Debug.Log("Hammercoil fired - heavy kinetic round!");
        }

        private void ApplyScreenShake()
        {
            // TODO: Implement screen shake system
            // CameraShake.Instance?.Shake(screenShakeIntensity, screenShakeDuration);
            Debug.Log($"Screen shake: {screenShakeIntensity} for {screenShakeDuration}s");
        }

        private void ApplyRecoilToShip()
        {
            if (ownerShip != null && stats.recoil > 0f)
            {
                // Apply backward force to ship when firing
                Rigidbody shipRb = ownerShip.GetComponent<Rigidbody>();
                if (shipRb != null)
                {
                    Vector3 recoilForce = -firePoint.forward * stats.recoil;
                    shipRb.AddForce(recoilForce, ForceMode.Impulse);
                }
            }
        }

        protected override void PlayFireEffects()
        {
            // Play heavy muzzle flash
            if (heavyMuzzleFlash != null)
            {
                heavyMuzzleFlash.Play();
            }
            else if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
            
            // Play heavy fire sound
            if (hammercoilFireSound != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f); // Slight pitch variation
                audioSource.PlayOneShot(hammercoilFireSound);
            }
            else if (fireSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(fireSound);
            }
        }

        protected override GameObject CreateProjectileObject()
        {
            return hammercoilProjectilePrefab != null ? hammercoilProjectilePrefab : base.CreateProjectileObject();
        }

        // Public methods for customization
        public void SetHammercoilProjectile(GameObject prefab)
        {
            hammercoilProjectilePrefab = prefab;
            SetProjectilePrefab(prefab);
        }

        public float GetKnockbackForce()
        {
            return stats.knockbackForce;
        }

        public void SetKnockbackForce(float force)
        {
            stats.knockbackForce = force;
        }
    }
} 