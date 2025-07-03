using UnityEngine;
using DomeClash.Core;
using DomeClash.Ships;

namespace DomeClash.Weapons.BastionWeapons
{
    /// <summary>
    /// Gravburst Shells - Bastion Secondary Weapon
    /// Fires arcing explosive shells that detonate on impact, dealing area damage over cover.
    /// Designed for siege and crowd control.
    /// </summary>
    public class GravburstShells : WeaponSystem
    {
        [Header("Gravburst Specific")]
        [SerializeField] private GameObject gravburstShellPrefab;
        [SerializeField] private ParticleSystem mortarMuzzleFlash;
        [SerializeField] private AudioClip mortarFireSound;
        [SerializeField] private float arcHeight = 10f; // Height of projectile arc
        [SerializeField] private LayerMask explosionLayers = -1;

        protected override void Awake()
        {
            base.Awake();
            
            // Set Gravburst stats according to GDD
            weaponName = "Gravburst Shells";
            weaponType = WeaponType.ProjectileGun; // Custom type for mortar
            
            // Configure stats for heavy mortar
            stats.damage = 200f; // Very High damage
            stats.fireRate = 0.4f; // Very Low rate of fire
            stats.range = 250f; // Short to Mid range
            stats.projectileSpeed = 40f; // Slow arcing projectiles
            stats.damageType = DamageType.Explosive;
            
            // Physical properties
            stats.mass = 500f; // Very heavy weapon (from GDD)
            stats.aoeRadius = 8f; // Area-of-effect damage
            stats.knockbackForce = 30f; // High knockback from explosion
            
            // Heat management
            stats.heatGeneration = 35f; // Very high heat per shot
            stats.maxHeat = 100f;
            stats.heatDissipation = 6f; // Very slow cooling
            stats.overheatCooldown = 4f;
            
            // Energy consumption
            stats.energyConsumption = 30f; // Very high energy cost
            
            // Combat properties
            stats.criticalChance = 0.05f;
            stats.criticalMultiplier = 1.8f;
        }

        protected override void Fire()
        {
            base.Fire();
            Debug.Log("Gravburst Shells fired - arcing explosive round!");
        }

        protected override void CreateProjectile(float damage = -1f, bool isCritical = false)
        {
            float finalDamage = damage > 0f ? damage : stats.damage;
            
            if (gravburstShellPrefab != null)
            {
                GameObject shell = Instantiate(gravburstShellPrefab, firePoint.position, firePoint.rotation);
                
                // Get or add GravburstProjectile component
                GravburstProjectile gravburstProj = shell.GetComponent<GravburstProjectile>();
                if (gravburstProj == null)
                {
                    gravburstProj = shell.AddComponent<GravburstProjectile>();
                }
                
                // Calculate arc trajectory to target
                Vector3 targetDirection = CalculateArcTrajectory();
                
                gravburstProj.Initialize(firePoint.position, targetDirection, finalDamage, stats.projectileSpeed, ownerShip);
                gravburstProj.SetExplosionProperties(stats.aoeRadius, stats.knockbackForce, explosionLayers);
                gravburstProj.SetArcHeight(arcHeight);
                gravburstProj.SetCritical(isCritical);
            }
            
            // Play effects
            PlayMortarEffects();
        }

        private Vector3 CalculateArcTrajectory()
        {
            // For now, use forward direction with slight upward angle
            // In a full implementation, this would calculate proper ballistic trajectory
            Vector3 direction = firePoint.forward;
            direction.y += 0.3f; // Add upward angle for arc
            return direction.normalized;
        }

        private void PlayMortarEffects()
        {
            // Play mortar muzzle flash
            if (mortarMuzzleFlash != null)
            {
                mortarMuzzleFlash.Play();
            }
            
            // Play mortar fire sound
            if (mortarFireSound != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.8f, 1.0f); // Lower pitch for heavy mortar
                audioSource.PlayOneShot(mortarFireSound);
            }
        }

        protected override GameObject CreateProjectileObject()
        {
            return gravburstShellPrefab;
        }

        // Public methods
        public void SetGravburstShellPrefab(GameObject prefab)
        {
            gravburstShellPrefab = prefab;
        }

        public void SetArcHeight(float height)
        {
            arcHeight = Mathf.Max(0f, height);
        }

        public float GetAOERadius()
        {
            return stats.aoeRadius;
        }

        public void SetAOERadius(float radius)
        {
            stats.aoeRadius = Mathf.Max(0f, radius);
        }
    }

    /// <summary>
    /// Custom projectile for Gravburst Shells with arcing trajectory and area damage
    /// </summary>
    public class GravburstProjectile : Projectile
    {
        [Header("Gravburst Properties")]
        [SerializeField] private float explosionRadius = 8f;
        [SerializeField] private new float knockbackForce = 30f;
        [SerializeField] private float arcHeight = 10f;
        [SerializeField] private LayerMask explosionLayers = -1;
        [SerializeField] private GameObject explosionEffect;
        
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float arcProgress = 0f;
        private bool isArcing = true;

        public override void Initialize(Vector3 startPos, Vector3 dir, float dmg, float spd, PrototypeShip ship)
        {
            base.Initialize(startPos, dir, dmg, spd, ship);
            
            startPosition = startPos;
            // Calculate target position based on range and direction
            targetPosition = startPos + dir * speed * lifetime * 0.8f; // 80% of max range
            arcProgress = 0f;
            isArcing = true;
        }

        protected override void Update()
        {
            if (isArcing)
            {
                UpdateArcMovement();
            }
            else
            {
                base.Update();
            }
        }

        private void UpdateArcMovement()
        {
            // Check lifetime
            if (Time.time - spawnTime > lifetime)
            {
                ExplodeOnImpact(transform.position, Vector3.up);
                return;
            }

            // Update arc progress
            arcProgress += speed * Time.deltaTime / Vector3.Distance(startPosition, targetPosition);
            
            if (arcProgress >= 1f)
            {
                // Reached target, explode
                ExplodeOnImpact(targetPosition, Vector3.up);
                return;
            }

            // Calculate arc position
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, arcProgress);
            
            // Add arc height using sine curve
            float heightMultiplier = Mathf.Sin(arcProgress * Mathf.PI);
            currentPos.y += heightMultiplier * arcHeight;
            
            transform.position = currentPos;
            
            // Update rotation to face movement direction
            if (arcProgress > 0.01f)
            {
                Vector3 lastPos = Vector3.Lerp(startPosition, targetPosition, arcProgress - 0.01f);
                lastPos.y += Mathf.Sin((arcProgress - 0.01f) * Mathf.PI) * arcHeight;
                
                Vector3 moveDirection = (currentPos - lastPos).normalized;
                if (moveDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(moveDirection);
                }
            }
        }

        protected override void OnHit(RaycastHit hit)
        {
            ExplodeOnImpact(hit.point, hit.normal);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;
            
            // Explode on any collision
            ExplodeOnImpact(transform.position, Vector3.up);
        }

        private void ExplodeOnImpact(Vector3 explosionPos, Vector3 normal)
        {
            if (hasHit) return;
            hasHit = true;
            
            // Create explosion effect
            CreateExplosionEffect(explosionPos, normal);
            
            // Apply area damage
            ApplyAreaDamage(explosionPos);
            
            DestroyProjectile();
        }

        private void CreateExplosionEffect(Vector3 position, Vector3 normal)
        {
            if (explosionEffect != null)
            {
                GameObject effect = Instantiate(explosionEffect, position, Quaternion.LookRotation(normal));
                Destroy(effect, 3f);
            }
            else
            {
                // Create simple explosion effect
                GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                explosion.transform.position = position;
                explosion.transform.localScale = Vector3.one * explosionRadius * 0.5f;
                
                Renderer renderer = explosion.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.red;
                }
                
                Destroy(explosion.GetComponent<Collider>());
                Destroy(explosion, 1f);
            }
        }

        private void ApplyAreaDamage(Vector3 explosionPos)
        {
            Collider[] hitColliders = Physics.OverlapSphere(explosionPos, explosionRadius, explosionLayers);
            
            foreach (Collider col in hitColliders)
            {
                PrototypeShip targetShip = col.GetComponent<PrototypeShip>();
                if (targetShip != null && targetShip != owner)
                {
                    // Calculate distance-based damage falloff
                    float distance = Vector3.Distance(explosionPos, col.transform.position);
                    float damageMultiplier = 1f - (distance / explosionRadius);
                    damageMultiplier = Mathf.Clamp01(damageMultiplier);
                    
                    float finalDamage = damage * damageMultiplier;
                    
                    // Apply damage
                    DamageSystem targetDamageSystem = targetShip.GetComponent<DamageSystem>();
                    if (targetDamageSystem != null)
                    {
                        targetDamageSystem.TakeDamage(finalDamage, DamageType.Explosive, owner);
                    }
                    
                    // Apply knockback
                    if (knockbackForce > 0f)
                    {
                        Rigidbody targetRb = col.GetComponent<Rigidbody>();
                        if (targetRb != null)
                        {
                            Vector3 knockbackDirection = (col.transform.position - explosionPos).normalized;
                            float knockbackMultiplier = damageMultiplier; // Same falloff as damage
                            targetRb.AddForce(knockbackDirection * knockbackForce * knockbackMultiplier, ForceMode.Impulse);
                        }
                    }
                }
            }
        }

        // Public setters
        public void SetExplosionProperties(float radius, float knockback, LayerMask layers)
        {
            explosionRadius = radius;
            knockbackForce = knockback;
            explosionLayers = layers;
        }

        public void SetArcHeight(float height)
        {
            arcHeight = height;
        }
    }
} 