using UnityEngine;
using DomeClash.Ships;

namespace DomeClash.Weapons
{
    /// <summary>
    /// Simple prototype weapon system for testing
    /// </summary>
    public class PrototypeWeapon : MonoBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private string weaponName = "Prototype Gun";
        [SerializeField] private float damage = 25f;
        [SerializeField] private float fireRate = 10f; // Rounds per second
        [SerializeField] private float projectileSpeed = 100f;
        [SerializeField] private float range = 200f;
        
        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        
        [Header("Effects")]
        [SerializeField] private GameObject muzzleFlash;
        [SerializeField] private AudioClip fireSound;
        
        [Header("State")]
        [SerializeField] private float lastFireTime = 0f;
        [SerializeField] private bool canFire = true;
        
        // Components
        private AudioSource audioSource;
        private PrototypeShip ship;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
                
            ship = GetComponentInParent<PrototypeShip>();
            
            if (firePoint == null)
                firePoint = transform;
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            // Fire with left mouse button
            if (Input.GetMouseButton(0))
            {
                TryFire();
            }
        }

        private void TryFire()
        {
            if (!canFire) return;
            
            float timeSinceLastFire = Time.time - lastFireTime;
            float fireInterval = 1f / fireRate;
            
            if (timeSinceLastFire >= fireInterval)
            {
                Fire();
            }
        }

        private void Fire()
        {
            lastFireTime = Time.time;
            
            // Create projectile
            CreateProjectile();
            
            // Play effects
            PlayMuzzleFlash();
            PlayFireSound();
            
            Debug.Log($"{weaponName} fired!");
        }

        private void CreateProjectile()
        {
            if (projectilePrefab == null)
            {
                // Create simple projectile if no prefab
                CreateSimpleProjectile();
                return;
            }
            
            // Spawn projectile from prefab
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            
            // Set projectile properties
            Rigidbody projRb = projectile.GetComponent<Rigidbody>();
            if (projRb != null)
            {
                projRb.linearVelocity = firePoint.forward * projectileSpeed;
            }
            
            // Add projectile script if not present
            PrototypeProjectile projScript = projectile.GetComponent<PrototypeProjectile>();
            if (projScript == null)
            {
                projScript = projectile.AddComponent<PrototypeProjectile>();
            }
            
            projScript.Initialize(damage, ship);
            
            // Destroy projectile after range time
            float destroyTime = range / projectileSpeed;
            Destroy(projectile, destroyTime);
        }

        private void CreateSimpleProjectile()
        {
            // Create a simple sphere projectile
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.transform.position = firePoint.position;
            projectile.transform.rotation = firePoint.rotation;
            projectile.transform.localScale = Vector3.one * 0.2f; // Small projectile
            
            // Add rigidbody
            Rigidbody rb = projectile.AddComponent<Rigidbody>();
            rb.linearVelocity = firePoint.forward * projectileSpeed;
            rb.useGravity = false;
            
            // Add projectile script
            PrototypeProjectile projScript = projectile.AddComponent<PrototypeProjectile>();
            projScript.Initialize(damage, ship);
            
            // Set material
            Renderer renderer = projectile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.red;
            }
            
            // Destroy after range time
            float destroyTime = range / projectileSpeed;
            Destroy(projectile, destroyTime);
        }

        private void PlayMuzzleFlash()
        {
            if (muzzleFlash != null)
            {
                GameObject flash = Instantiate(muzzleFlash, firePoint.position, firePoint.rotation);
                Destroy(flash, 0.1f);
            }
        }

        private void PlayFireSound()
        {
            if (fireSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(fireSound);
            }
        }

        // Public methods
        public void SetFireRate(float newFireRate)
        {
            fireRate = newFireRate;
        }

        public void SetDamage(float newDamage)
        {
            damage = newDamage;
        }

        public float GetFireRate()
        {
            return fireRate;
        }

        public float GetDamage()
        {
            return damage;
        }

        public bool CanFire()
        {
            return canFire;
        }

        public void SetCanFire(bool canFire)
        {
            this.canFire = canFire;
        }
    }

    /// <summary>
    /// Simple projectile for prototype testing
    /// </summary>
    public class PrototypeProjectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private float damage = 25f;
        [SerializeField] private float lifetime = 5f;
        
        private PrototypeShip owner;
        private float spawnTime;
        private bool hasHit = false;

        public void Initialize(float dmg, PrototypeShip ship)
        {
            damage = dmg;
            owner = ship;
            spawnTime = Time.time;
            hasHit = false;
        }

        private void Update()
        {
            // Check lifetime
            if (Time.time - spawnTime > lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            // Check if we hit another ship
            PrototypeShip targetShip = other.GetComponent<PrototypeShip>();
            if (targetShip != null && targetShip != owner)
            {
                hasHit = true;
                
                // Apply damage (simple implementation)
                Debug.Log($"Hit {targetShip.name} for {damage} damage!");
                
                // Create hit effect
                CreateHitEffect();
                
                // Destroy projectile
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasHit) return;

            // Check if we hit another ship
            PrototypeShip targetShip = collision.gameObject.GetComponent<PrototypeShip>();
            if (targetShip != null && targetShip != owner)
            {
                hasHit = true;
                
                // Apply damage
                Debug.Log($"Hit {targetShip.name} for {damage} damage!");
                
                // Create hit effect
                CreateHitEffect();
            }
            
            // Destroy projectile on any collision
            Destroy(gameObject);
        }

        private void CreateHitEffect()
        {
            // Create simple hit effect
            GameObject hitEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitEffect.transform.position = transform.position;
            hitEffect.transform.localScale = Vector3.one * 0.5f;
            
            // Set material
            Renderer renderer = hitEffect.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.yellow;
            }
            
            // Remove collider
            Destroy(hitEffect.GetComponent<Collider>());
            
            // Destroy after short time
            Destroy(hitEffect, 0.2f);
        }
    }
} 