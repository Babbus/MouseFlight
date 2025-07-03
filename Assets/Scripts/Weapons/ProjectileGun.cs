using UnityEngine;
using DomeClash.Core;

namespace DomeClash.Weapons
{
    /// <summary>
    /// Projectile Gun - Physical ammo with drop and spread
    /// Heat-based, requires burst control. Good for sustained pressure.
    /// </summary>
    public class ProjectileGun : WeaponSystem
    {
        [Header("Projectile Gun Settings")]
        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected float spread = 0f;
        [SerializeField] protected float recoil = 0f;
        [SerializeField] protected ParticleSystem muzzleFlash;
        [SerializeField] protected AudioClip fireSound;
        [SerializeField] protected Transform[] barrels;
        
        protected new AudioSource audioSource;
        protected int currentBarrelIndex = 0;

        protected override void Awake()
        {
            base.Awake();
            
            weaponType = WeaponType.ProjectileGun;
            
            // Setup audio
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
                
            // Setup barrels
            if (barrels == null || barrels.Length == 0)
            {
                barrels = new Transform[] { firePoint };
            }
        }

        protected override void Fire()
        {
            base.Fire();
            
            // Create projectile from current barrel
            Transform currentBarrel = barrels[currentBarrelIndex];
            CreateProjectileFromBarrel(currentBarrel);
            
            // Apply recoil if any
            if (recoil > 0f)
            {
                ApplyRecoil();
            }
            
            // Cycle to next barrel
            currentBarrelIndex = (currentBarrelIndex + 1) % barrels.Length;
            
            // Play effects
            PlayFireEffects();
        }
        
        protected virtual void ApplyRecoil()
        {
            if (ownerShip != null && recoil > 0f)
            {
                Rigidbody shipRb = ownerShip.GetComponent<Rigidbody>();
                if (shipRb != null)
                {
                    Vector3 recoilForce = -firePoint.forward * recoil;
                    shipRb.AddForce(recoilForce, ForceMode.Impulse);
                }
            }
        }

        private void CreateProjectileFromBarrel(Transform barrel)
        {
            if (projectilePrefab == null) return;
            
            // Calculate spread
            Vector3 direction = barrel.forward;
            if (spread > 0f)
            {
                direction = ApplySpread(direction, spread);
            }
            
            // Create projectile
            GameObject projectile = Instantiate(projectilePrefab, barrel.position, Quaternion.LookRotation(direction));
            
            // Initialize projectile
            Projectile proj = projectile.GetComponent<Projectile>();
            if (proj != null)
            {
                float splitDamage = barrels.Length > 1 ? stats.damage / barrels.Length : stats.damage;
                proj.Initialize(barrel.position, direction, splitDamage, stats.projectileSpeed, ownerShip);
            }
        }

        protected override Vector3 ApplySpread(Vector3 direction, float spreadAngle)
        {
            // Apply random spread within cone
            float randomX = Random.Range(-spreadAngle, spreadAngle);
            float randomY = Random.Range(-spreadAngle, spreadAngle);
            
            Quaternion spreadRotation = Quaternion.Euler(randomX, randomY, 0);
            return spreadRotation * direction;
        }

        protected override void PlayFireEffects()
        {
            // Play sound
            if (fireSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(fireSound);
            }
            
            // Play muzzle flash
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
        }

        protected override GameObject CreateProjectileObject()
        {
            return projectilePrefab;
        }

        // Public methods for customization
        public void SetProjectilePrefab(GameObject prefab)
        {
            projectilePrefab = prefab;
        }

        public void SetSpread(float newSpread)
        {
            spread = newSpread;
        }

        public void SetBarrels(Transform[] newBarrels)
        {
            if (newBarrels != null && newBarrels.Length > 0)
            {
                barrels = newBarrels;
                currentBarrelIndex = 0;
            }
        }

        public float GetSpread()
        {
            return spread;
        }

        public int GetBarrelCount()
        {
            return barrels?.Length ?? 0;
        }
    }
} 