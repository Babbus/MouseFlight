using UnityEngine;
using DomeClash.Core;
using DomeClash.Ships;

namespace DomeClash.Weapons.PrototypeRazorWeapons
{
    /// <summary>
    /// Hunter Seeker - Prototype Razor Secondary
    /// Smart micro-missile system with persistent tracking
    /// Arcade: Fast lock-on, agile missiles, satisfying pursuit
    /// Realistic: Limited fuel, can be dodged, requires lock
    /// Sci-Fi: Advanced tracking, ignores flares at close range
    /// </summary>
    public class HunterSeeker : WeaponSystem
    {
        [Header("Hunter Seeker Prototype")]
        [SerializeField] private GameObject hunterMissilePrefab;
        [SerializeField] private int missilesPerVolley = 6;
        [SerializeField] private float volleyDelay = 0.15f; // Time between missiles in volley
        [SerializeField] protected Transform[] missilePods; // Multiple launch points
        [SerializeField] private ParticleSystem podFlash;
        [SerializeField] private AudioClip[] launchSounds;
        [SerializeField] private float closeRangeFlareIgnore = 15f; // Ignores flares at close range
        
        private int currentMissileInVolley = 0;
        private bool isFiringVolley = false;
        private float nextMissileTime = 0f;
        private int currentPod = 0;

        protected override void Awake()
        {
            base.Awake();
            
            // Hunter Seeker Stats (Prototype focused)
            weaponName = "Hunter Seeker [PROTOTYPE]";
            weaponType = WeaponType.HomingMissile;
            
            // Combat stats - low damage per missile, high volume
            stats.damage = 25f; // Low damage per missile
            stats.fireRate = 1.5f; // 1.5 volleys per second max
            stats.range = 500f; // Long range tracking
            stats.projectileSpeed = 80f; // Fast missiles
            stats.damageType = DamageType.Explosive;
            
            // Lock-on properties - disabled for blind-fire mode (radar will enable this)
            stats.requiresLock = false; // No lock required for blind-fire
            stats.lockTime = 1.2f; // Will be used when radar is available
            stats.lockRange = 350f; // Will be used when radar is available
            
            // Physical properties - lightweight launcher
            stats.mass = 190f; // Light weapon (from GDD)
            stats.recoil = 1f; // Very light recoil per missile
            
            // Heat management - moderate
            stats.heatGeneration = 8f; // Heat per missile (48 per volley)
            stats.maxHeat = 120f; // Higher capacity for volleys
            stats.heatDissipation = 25f; // Fast cooling
            stats.overheatCooldown = 2f;
            
            // Energy consumption
            stats.energyConsumption = 5f; // Per missile (30 per volley)
            
            // Combat properties
            stats.criticalChance = 0.08f;
            stats.criticalMultiplier = 1.6f;
            
            // Setup missile pods
            if (missilePods == null || missilePods.Length == 0)
            {
                missilePods = new Transform[] { firePoint };
            }
        }

        protected override void HandleInput()
        {
            // Handle volley firing
            if (isFiringVolley)
            {
                if (Time.time >= nextMissileTime)
                {
                    // Check if we're in blind-fire mode or radar mode
                    if (lockedTarget == null)
                        FireNextBlindMissile();
                    else
                        FireNextMissile();
                }
                return;
            }
            
            // Start new volley on input - blind-fire mode (no lock required)
            if (Input.GetMouseButtonDown(1) && CanFire())
            {
                StartBlindFireVolley();
            }
        }

        private void StartBlindFireVolley()
        {
            Debug.Log("Hunter Seeker: Starting BLIND-FIRE volley (no radar lock)");
            
            isFiringVolley = true;
            currentMissileInVolley = 0;
            currentPod = 0;
            FireNextBlindMissile();
        }
        
        // Legacy method for when radar system is implemented
        private void StartVolley()
        {
            if (lockedTarget == null) return;
            
            isFiringVolley = true;
            currentMissileInVolley = 0;
            currentPod = 0;
            FireNextMissile();
        }

        private void FireNextBlindMissile()
        {
            if (!CanFire())
            {
                EndVolley();
                return;
            }
            
            // Fire from current pod
            Transform currentPodTransform = missilePods[currentPod];
            
            // Create blind-fire missile (no target)
            CreateBlindFireMissile(currentPodTransform);
            
            // Play effects
            PlayMissileEffects(currentPodTransform);
            
            // Update heat and energy
            currentHeat += stats.heatGeneration;
            if (ownerShip != null)
            {
                OnEnergyConsumed?.Invoke(stats.energyConsumption);
            }
            
            // Check for overheating
            if (currentHeat >= stats.maxHeat && !isOverheated)
            {
                isOverheated = true;
                overheatRecoveryTime = stats.overheatCooldown;
                OnWeaponOverheated?.Invoke();
                Debug.Log($"{weaponName} overheated during blind-fire volley!");
                EndVolley();
                return;
            }
            
            // Move to next pod
            currentPod = (currentPod + 1) % missilePods.Length;
            currentMissileInVolley++;
            
            // Check if volley is complete
            if (currentMissileInVolley >= missilesPerVolley)
            {
                EndVolley();
            }
            else
            {
                nextMissileTime = Time.time + volleyDelay;
            }
            
            OnHeatChanged?.Invoke(currentHeat);
        }

        private void FireNextMissile()
        {
            if (!CanFire() || lockedTarget == null)
            {
                EndVolley();
                return;
            }
            
            // Fire from current pod
            Transform currentPodTransform = missilePods[currentPod];
            
            // Create hunter missile
            CreateHunterMissile(currentPodTransform);
            
            // Play effects
            PlayMissileEffects(currentPodTransform);
            
            // Update heat and energy
            currentHeat += stats.heatGeneration;
            if (ownerShip != null)
            {
                OnEnergyConsumed?.Invoke(stats.energyConsumption);
            }
            
            // Check for overheating
            if (currentHeat >= stats.maxHeat && !isOverheated)
            {
                isOverheated = true;
                overheatRecoveryTime = stats.overheatCooldown;
                OnWeaponOverheated?.Invoke();
                Debug.Log($"{weaponName} overheated during volley!");
                EndVolley();
                return;
            }
            
            // Move to next pod
            currentPod = (currentPod + 1) % missilePods.Length;
            currentMissileInVolley++;
            
            // Check if volley is complete
            if (currentMissileInVolley >= missilesPerVolley)
            {
                EndVolley();
            }
            else
            {
                nextMissileTime = Time.time + volleyDelay;
            }
            
            OnHeatChanged?.Invoke(currentHeat);
        }

        private void CreateHunterMissile(Transform launchPod)
        {
            if (hunterMissilePrefab == null)
            {
                Debug.LogError($"Hunter Seeker: No missile prefab assigned! Cannot fire missiles.");
                return;
            }
            
            if (lockedTarget == null)
            {
                Debug.LogError($"Hunter Seeker: No locked target! Cannot fire missiles.");
                return;
            }
            
            Debug.Log($"Hunter Seeker: Firing missile from {launchPod.name} at {lockedTarget.name}");
            
            GameObject missile = Instantiate(hunterMissilePrefab, launchPod.position, launchPod.rotation);
            
            HunterMissile hunterMissile = missile.GetComponent<HunterMissile>();
            if (hunterMissile == null)
            {
                hunterMissile = missile.AddComponent<HunterMissile>();
                Debug.Log("Hunter Seeker: Added HunterMissile component to instantiated missile");
            }
            
            // Calculate initial launch direction with slight spread
            Vector3 launchDirection = (lockedTarget.position - launchPod.position).normalized;
            launchDirection = ApplyMissileSpread(launchDirection, 5f); // Small spread for realism
            
            hunterMissile.Initialize(launchPod.position, lockedTarget, stats.damage, stats.projectileSpeed, ownerShip);
            hunterMissile.SetTrackingProperties(closeRangeFlareIgnore, stats.range);
            hunterMissile.SetLaunchDirection(launchDirection);
            
            Debug.Log($"Hunter Seeker: Missile launched successfully! Speed: {stats.projectileSpeed}, Damage: {stats.damage}");
        }

        private void CreateBlindFireMissile(Transform launchPod)
        {
            if (hunterMissilePrefab == null)
            {
                Debug.LogError($"Hunter Seeker: No missile prefab assigned! Cannot fire blind-fire missiles.");
                return;
            }
            
            Debug.Log($"Hunter Seeker: Firing BLIND-FIRE missile from {launchPod.name} (no target tracking)");
            
            GameObject missile = Instantiate(hunterMissilePrefab, launchPod.position, launchPod.rotation);
            
            // Use SimpleMissile for reliable behavior
            SimpleMissile simpleMissile = missile.GetComponent<SimpleMissile>();
            if (simpleMissile == null)
            {
                simpleMissile = missile.AddComponent<SimpleMissile>();
                Debug.Log("Hunter Seeker: Added SimpleMissile component for straight-line flight");
            }
            
            // Get aim direction from weapon system
            Vector3 aimDirection = GetAimDirection();
            
            // Apply slight spread for realism
            aimDirection = ApplyMissileSpread(aimDirection, 3f); // Smaller spread for blind-fire
            
            simpleMissile.Initialize(launchPod.position, aimDirection, stats.damage, stats.projectileSpeed, ownerShip);
            
            Debug.Log($"Hunter Seeker: Blind-fire missile launched! Direction: {aimDirection}, Speed: {stats.projectileSpeed}");
        }

        private Vector3 ApplyMissileSpread(Vector3 direction, float spreadAngle)
        {
            float randomX = Random.Range(-spreadAngle, spreadAngle);
            float randomY = Random.Range(-spreadAngle, spreadAngle);
            
            Quaternion spreadRotation = Quaternion.Euler(randomX, randomY, 0);
            return spreadRotation * direction;
        }

        private void PlayMissileEffects(Transform pod)
        {
            // Play pod flash
            if (podFlash != null)
            {
                podFlash.transform.position = pod.position;
                podFlash.transform.rotation = pod.rotation;
                podFlash.Play();
            }
            
            // Play launch sound with variation
            if (launchSounds != null && launchSounds.Length > 0 && audioSource != null)
            {
                int soundIndex = currentMissileInVolley % launchSounds.Length;
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(launchSounds[soundIndex]);
            }
        }

        private void EndVolley()
        {
            isFiringVolley = false;
            currentMissileInVolley = 0;
            lastFireTime = Time.time; // Set for fire rate cooldown
            currentPod = 0;
        }

        protected override bool CanFire()
        {
            if (isFiringVolley) return true; // Allow volley continuation
            if (Time.time - lastFireTime < 1f / stats.fireRate) return false;
            if (isOverheated) return false;
            
            // Blind-fire mode: no lock required
            // When radar system is added, this will check for radar availability
            return true;
        }

        protected override GameObject CreateProjectileObject()
        {
            return hunterMissilePrefab;
        }

        // Public methods for tweaking
        public void SetMissilesPerVolley(int count)
        {
            missilesPerVolley = Mathf.Max(1, count);
        }

        public void SetVolleyDelay(float delay)
        {
            volleyDelay = Mathf.Max(0.05f, delay);
        }

        public void SetCloseRangeFlareIgnore(float range)
        {
            closeRangeFlareIgnore = Mathf.Max(0f, range);
        }

        // Public properties for setup
        public Transform[] MissilePods
        {
            get => missilePods;
            set => missilePods = value;
        }

        // Debug info
        public bool IsFiringVolley() => isFiringVolley;
        public int GetCurrentMissileInVolley() => currentMissileInVolley;
        public int GetMissilesPerVolley() => missilesPerVolley;
    }

    /// <summary>
    /// Enhanced Hunter Missile with advanced tracking and smart behavior
    /// </summary>
    public class HunterMissile : MonoBehaviour
    {
        [Header("Hunter Missile Properties")]
        [SerializeField] private float speed = 80f;
        [SerializeField] private float rotateSpeed = 250f; // Faster turning than basic missiles
        [SerializeField] private float damage = 25f;
        [SerializeField] private float lifeTime = 8f; // Longer life for persistent tracking
        [SerializeField] private float fuel = 6f; // Fuel-limited tracking
        [SerializeField] private float closeRangeFlareIgnore = 15f;
        [SerializeField] private float maxTrackingRange = 500f;
        
        private Rigidbody rb;
        private Transform target;
        private Vector3 initialDirection;
        private Vector3 launchDirection;
        private PrototypeShip owner;
        private bool hasFuel = true;
        private float spawnTime;
        private bool hasExploded = false;
        
        // Tracking state
        private bool isTracking = false;
        private float trackingStartTime;

        public void Initialize(Vector3 startPos, Transform tgt, float dmg, float spd, PrototypeShip ship)
        {
            transform.position = startPos;
            target = tgt;
            damage = dmg;
            speed = spd;
            owner = ship;
            spawnTime = Time.time;
            
            rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
                
            rb.useGravity = false;
            
            // Start with launch direction, then switch to tracking
            initialDirection = launchDirection.normalized;
            rb.linearVelocity = initialDirection * speed;
            
            Destroy(gameObject, lifeTime);
        }

        public void SetTrackingProperties(float flareIgnoreRange, float maxRange)
        {
            closeRangeFlareIgnore = flareIgnoreRange;
            maxTrackingRange = maxRange;
        }

        public void SetLaunchDirection(Vector3 direction)
        {
            launchDirection = direction;
        }

        void FixedUpdate()
        {
            if (rb == null) return; // Null check
            
            // Check fuel
            if (Time.time - spawnTime > fuel)
            {
                hasFuel = false;
            }
            
            // Smart tracking behavior
            if (hasFuel && target != null)
            {
                UpdateSmartTracking();
            }
            else
            {
                // No fuel or target - ballistic flight
                rb.linearVelocity = rb.linearVelocity.normalized * speed;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void UpdateSmartTracking()
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            // Check if target is in tracking range
            if (distanceToTarget > maxTrackingRange)
            {
                // Too far - ballistic flight
                rb.linearVelocity = transform.forward * speed;
                return;
            }
            
            // Start tracking after brief initial flight
            if (!isTracking && Time.time - spawnTime > 0.3f)
            {
                isTracking = true;
                trackingStartTime = Time.time;
            }
            
            if (isTracking)
            {
                // Enhanced tracking with lead calculation
                Vector3 targetPosition = CalculateLeadTarget();
                Vector3 direction = (targetPosition - transform.position).normalized;
                
                // Smooth rotation toward target
                Vector3 rotateAmount = Vector3.Cross(transform.forward, direction);
                float rotateIntensity = rotateSpeed;
                
                // Close range - ignore flares and increase agility
                if (distanceToTarget < closeRangeFlareIgnore)
                {
                    rotateIntensity *= 1.5f; // More agile at close range
                    Debug.Log("Hunter missile in close range - flare immunity active!");
                }
                
                rb.angularVelocity = rotateAmount * rotateIntensity * Mathf.Deg2Rad;
                rb.linearVelocity = transform.forward * speed;
            }
        }

        private Vector3 CalculateLeadTarget()
        {
            if (target == null) return transform.position;
            
            // Simple lead calculation for moving targets
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                Vector3 targetVelocity = targetRb.linearVelocity;
                float timeToTarget = Vector3.Distance(transform.position, target.position) / speed;
                return target.position + targetVelocity * timeToTarget * 0.5f; // 50% lead
            }
            
            return target.position;
        }

        void OnTriggerEnter(Collider other)
        {
            if (hasExploded) return;
            
            // Check if we hit a ship
            PrototypeShip targetShip = other.GetComponent<PrototypeShip>();
            if (targetShip != null && targetShip != owner)
            {
                ApplyDamage(targetShip);
                Explode();
            }
            else if (other.gameObject.layer != gameObject.layer) // Hit environment
            {
                Explode();
            }
        }

        private void ApplyDamage(PrototypeShip targetShip)
        {
            DamageSystem targetDamageSystem = targetShip.GetComponent<DamageSystem>();
            if (targetDamageSystem != null)
            {
                targetDamageSystem.TakeDamage(damage, DamageType.Explosive, owner);
                Debug.Log($"Hunter Seeker hit {targetShip.name} for {damage} explosive damage!");
            }
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;
            
            // Create simple explosion effect
            GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            explosion.transform.position = transform.position;
            explosion.transform.localScale = Vector3.one * 1.5f;
            
            Renderer renderer = explosion.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.orange;
            }
            
            Destroy(explosion.GetComponent<Collider>());
            Destroy(explosion, 0.8f);
            
            Destroy(gameObject);
        }

        // Debug info
        public bool IsTracking() => isTracking;
        public bool HasFuel() => hasFuel;
        public float GetDistanceToTarget() => target != null ? Vector3.Distance(transform.position, target.position) : 0f;
    }
} 