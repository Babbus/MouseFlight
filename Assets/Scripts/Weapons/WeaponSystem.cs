using UnityEngine;
using DomeClash.Core;
using DomeClash.Ships;

namespace DomeClash.Weapons
{
    public enum WeaponType
    {
        ProjectileGun,
        HomingMissile,
        PulseLaser
    }

    [System.Serializable]
    public class WeaponStats
    {
        [Header("Combat")]
        public float damage = 50f;
        public float fireRate = 1f;
        public float range = 200f;
        public float projectileSpeed = 100f;
        public DamageType damageType = DamageType.Kinetic;
        
        [Header("Lock-On")]
        public bool requiresLock = false;
        public float lockTime = 1.5f;
        public float lockRange = 150f;
        
        [Header("Heat & Energy")]
        public float heatGeneration = 10f;
        public float maxHeat = 100f;
        public float heatDissipation = 15f;
        public float energyConsumption = 5f; // Energy consumed per shot
        public float overheatCooldown = 2f; // Time to cool down from overheated state
        
        [Header("Physical Properties")]
        [Tooltip("Weapon mass in kg - affects ship handling")]
        public float mass = 100f;
        
        [Header("Weapon Behavior")]
        [Tooltip("Weapon spread in degrees (for projectile weapons)")]
        public float spread = 0f;
        [Tooltip("Recoil force applied to ship")]
        public float recoil = 0f;
        [Tooltip("Critical hit chance (0-1)")]
        [Range(0f, 1f)] public float criticalChance = 0.05f;
        [Tooltip("Critical hit damage multiplier")]
        public float criticalMultiplier = 1.5f;
        
        [Header("Special Effects")]
        [Tooltip("Knockback force applied to targets")]
        public float knockbackForce = 0f;
        [Tooltip("Area of effect radius for explosive weapons")]
        public float aoeRadius = 0f;
        [Tooltip("Penetration - can hit multiple targets")]
        public bool canPenetrate = false;
        public int maxPenetrations = 1;
    }

    public abstract class WeaponSystem : MonoBehaviour
    {
        [Header("Weapon Identity")]
        public string weaponName;
        public WeaponType weaponType;
        
        [Header("Stats")]
        public WeaponStats stats;
        
        [Header("Components")]
        [SerializeField] protected Transform firePoint;
        [SerializeField] protected PrototypeShip ownerShip;
        [SerializeField] protected MouseFlightController mouseFlightController;
        [SerializeField] protected ShipFlightController shipFlightController;
        protected AudioSource audioSource;
        
        [Header("State")]
        [SerializeField] protected float currentHeat = 0f;
        [SerializeField] protected float lastFireTime = 0f;
        [SerializeField] protected bool isLocked = false;
        [SerializeField] protected Transform lockedTarget = null;
        [SerializeField] protected float lockProgress = 0f;
        [SerializeField] protected bool isOverheated = false;
        [SerializeField] protected float overheatRecoveryTime = 0f;

        // Events
        public System.Action<float> OnHeatChanged;
        public System.Action<Transform> OnTargetLocked;
        public System.Action OnTargetLost;
        public System.Action OnWeaponOverheated;
        public System.Action OnWeaponCooledDown;
        public System.Action<float> OnEnergyConsumed;

        protected virtual void Awake()
        {
            if (firePoint == null)
                firePoint = transform;
            
            if (ownerShip == null)
                ownerShip = GetComponentInParent<PrototypeShip>();
                
            if (mouseFlightController == null)
                mouseFlightController = FindFirstObjectByType<MouseFlightController>();
                
            if (shipFlightController == null)
                shipFlightController = GetComponent<ShipFlightController>();
            if (shipFlightController == null && ownerShip != null)
                shipFlightController = ownerShip.GetComponent<ShipFlightController>();
                
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        protected virtual void Update()
        {
            UpdateHeat();
            UpdateLockOn();
            HandleInput();
        }

        protected virtual void UpdateHeat()
        {
            // Handle overheat recovery
            if (isOverheated)
            {
                overheatRecoveryTime -= Time.deltaTime;
                if (overheatRecoveryTime <= 0f)
                {
                    isOverheated = false;
                    OnWeaponCooledDown?.Invoke();
                    Debug.Log($"{weaponName} has cooled down from overheating");
                }
            }
            
            // Dissipate heat
            if (currentHeat > 0)
            {
                currentHeat -= stats.heatDissipation * Time.deltaTime;
                currentHeat = Mathf.Max(0, currentHeat);
                OnHeatChanged?.Invoke(currentHeat);
            }
        }

        protected virtual void UpdateLockOn()
        {
            if (!stats.requiresLock) return;

            // Find potential targets
            Transform target = FindTarget();
            
            if (target != null)
            {
                if (lockedTarget != target)
                {
                    // Start locking new target
                    lockedTarget = target;
                    lockProgress = 0f;
                }
                
                // Progress lock
                lockProgress += Time.deltaTime / stats.lockTime;
                
                if (lockProgress >= 1f && !isLocked)
                {
                    isLocked = true;
                    OnTargetLocked?.Invoke(lockedTarget);
                    Debug.Log($"{weaponName}: Target LOCKED onto {lockedTarget.name}!");
                }
            }
            else
            {
                if (lockedTarget != null)
                {
                    Debug.Log($"{weaponName}: Target LOST!");
                    lockedTarget = null;
                    isLocked = false;
                    lockProgress = 0f;
                    OnTargetLost?.Invoke();
                }
            }
        }

        protected virtual Transform FindTarget()
        {
            // Enhanced target finding using MouseAim direction
            Vector3 aimDirection = GetAimDirection();
            
            Collider[] colliders = Physics.OverlapSphere(transform.position, stats.lockRange);
            
            foreach (Collider col in colliders)
            {
                PrototypeShip ship = col.GetComponent<PrototypeShip>();
                if (ship != null && ship != ownerShip)
                {
                    // Check if target is in aim direction
                    Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
                    float angle = Vector3.Angle(aimDirection, directionToTarget);
                    
                    if (angle < 30f) // 30 degree cone from aim direction
                    {
                        return col.transform;
                    }
                }
            }
            
            return null;
        }
        
        protected virtual Vector3 GetAimDirection()
        {
            // Use MouseAim direction if available
            if (mouseFlightController != null)
            {
                Vector3 mouseAimPos = mouseFlightController.MouseAimPos;
                return (mouseAimPos - transform.position).normalized;
            }
            
            // Fallback to ship forward direction
            return transform.forward;
        }

        protected virtual void HandleInput()
        {
            // Primary fire
            if (Input.GetMouseButton(0))
            {
                TryFire();
            }
            
            // Secondary fire (missiles)
            if (Input.GetMouseButton(1) && weaponType == WeaponType.HomingMissile)
            {
                TryFireMissile();
            }
        }

        protected virtual void TryFire()
        {
            if (CanFire())
            {
                Fire();
            }
        }

        protected virtual void TryFireMissile()
        {
            if (CanFireMissile())
            {
                FireMissile();
            }
        }

        protected virtual bool CanFire()
        {
            if (Time.time - lastFireTime < 1f / stats.fireRate) return false;
            if (isOverheated) return false;
            if (stats.requiresLock && !isLocked) return false;
            
            // TODO: Check ship energy when energy system is implemented
            // if (ownerShip != null && !ownerShip.HasEnoughEnergy(stats.energyConsumption)) return false;
            
            return true;
        }

        protected virtual bool CanFireMissile()
        {
            if (Time.time - lastFireTime < 1f / stats.fireRate) return false;
            if (!isLocked) return false;
            
            return true;
        }

        protected virtual void Fire()
        {
            lastFireTime = Time.time;
            
            // Generate heat
            currentHeat += stats.heatGeneration;
            
            // Check for overheating
            if (currentHeat >= stats.maxHeat && !isOverheated)
            {
                isOverheated = true;
                overheatRecoveryTime = stats.overheatCooldown;
                OnWeaponOverheated?.Invoke();
                Debug.Log($"{weaponName} has overheated!");
            }
            
            // Consume energy
            if (ownerShip != null)
            {
                // TODO: Implement ship energy system
                OnEnergyConsumed?.Invoke(stats.energyConsumption);
            }
            
            // Apply critical hit chance
            float finalDamage = stats.damage;
            bool isCritical = UnityEngine.Random.value < stats.criticalChance;
            if (isCritical)
            {
                finalDamage *= stats.criticalMultiplier;
                Debug.Log($"Critical hit! {finalDamage} damage");
            }
            
            OnHeatChanged?.Invoke(currentHeat);

            // Apply weapon recoil to ship
            ApplyWeaponRecoil();

            // Create projectile with final damage
            CreateProjectile(finalDamage, isCritical);
        }

        protected virtual void FireMissile()
        {
            lastFireTime = Time.time;
            
            // Create homing missile
            CreateHomingMissile();
        }

        protected virtual void CreateProjectile(float damage = -1f, bool isCritical = false)
        {
            // Use provided damage or default to stats damage
            float finalDamage = damage > 0f ? damage : stats.damage;
            
            // Base projectile creation - override in specific weapon types
            GameObject projectile = CreateProjectileObject();
            
            if (projectile != null)
            {
                Projectile proj = projectile.GetComponent<Projectile>();
                if (proj != null)
                {
                    Vector3 direction = firePoint.forward;
                    
                    // Apply spread if weapon has it
                    if (stats.spread > 0f)
                    {
                        direction = ApplySpread(direction, stats.spread);
                    }
                    
                    proj.Initialize(firePoint.position, direction, finalDamage, stats.projectileSpeed, ownerShip);
                    proj.SetDamageType(stats.damageType);
                    proj.SetCritical(isCritical);
                    
                    // Apply knockback and other effects
                    if (stats.knockbackForce > 0f)
                    {
                        proj.SetKnockbackForce(stats.knockbackForce);
                    }
                }
            }
        }
        
        protected virtual Vector3 ApplySpread(Vector3 direction, float spreadAngle)
        {
            // Apply random spread within cone
            float randomX = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
            float randomY = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
            
            Quaternion spreadRotation = Quaternion.Euler(randomX, randomY, 0);
            return spreadRotation * direction;
        }
        
        protected virtual void PlayFireEffects()
        {
            // Base fire effects - override in specific weapon types
        }

        protected virtual void CreateHomingMissile()
        {
            if (lockedTarget == null) return;
            
            GameObject missile = CreateMissileObject();
            
            if (missile != null)
            {
                HomingMissile homingMissile = missile.GetComponent<HomingMissile>();
                if (homingMissile != null)
                {
                    homingMissile.Initialize(firePoint.position, lockedTarget, stats.damage, stats.projectileSpeed, ownerShip);
                }
            }
        }

        protected virtual GameObject CreateProjectileObject()
        {
            // Override in specific weapon implementations
            return null;
        }

        protected virtual GameObject CreateMissileObject()
        {
            // Override in specific weapon implementations
            return null;
        }

        public virtual float GetHeatPercent()
        {
            return currentHeat / stats.maxHeat;
        }

        public virtual bool IsOverheated()
        {
            return isOverheated;
        }

        public virtual Transform GetLockedTarget()
        {
            return lockedTarget;
        }

        public virtual float GetLockProgress()
        {
            return lockProgress;
        }
        
        public virtual float GetMass()
        {
            return stats.mass;
        }
        
        public virtual float GetEnergyConsumption()
        {
            return stats.energyConsumption;
        }
        
        public virtual float GetOverheatRecoveryProgress()
        {
            if (!isOverheated) return 1f;
            return 1f - (overheatRecoveryTime / stats.overheatCooldown);
        }
        
        public virtual WeaponStats GetStats()
        {
            return stats;
        }
        
        public virtual void ForceOverheat()
        {
            isOverheated = true;
            overheatRecoveryTime = stats.overheatCooldown;
            OnWeaponOverheated?.Invoke();
        }
        
        public virtual void ForceCooldown()
        {
            isOverheated = false;
            overheatRecoveryTime = 0f;
            currentHeat = 0f;
            OnWeaponCooledDown?.Invoke();
        }
        
        // Heat getter methods for debugging
        public virtual float GetCurrentHeat()
        {
            return currentHeat;
        }
        
        public virtual float GetMaxHeat()
        {
            return stats.maxHeat;
        }
        
        /// <summary>
        /// Apply weapon recoil to ship flight system
        /// </summary>
        protected virtual void ApplyWeaponRecoil()
        {
            if (stats.recoil <= 0f) return;
            
            // Apply recoil to ship flight controller if available
            if (shipFlightController != null)
            {
                // Recoil affects pitch (pushes nose up slightly)
                float recoilPitch = stats.recoil * 0.01f; // Convert to pitch input
                float currentPitch = shipFlightController.GetPitchInput();
                shipFlightController.SetPitchInput(Mathf.Clamp(currentPitch + recoilPitch, -1f, 1f));
            }
            
            // Visual screen shake could be added here
            // CameraShake.Instance?.Shake(stats.recoil * 0.1f);
        }
    }
} 