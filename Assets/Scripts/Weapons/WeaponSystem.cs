using UnityEngine;
using DomeClash.Core;

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
        
        [Header("Lock-On")]
        public bool requiresLock = false;
        public float lockTime = 1.5f;
        public float lockRange = 150f;
        
        [Header("Heat")]
        public float heatGeneration = 10f;
        public float maxHeat = 100f;
        public float heatDissipation = 15f;
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
        [SerializeField] protected ShipClass ownerShip;
        [SerializeField] protected DomeClashFlightController flightController;
        
        [Header("State")]
        [SerializeField] protected float currentHeat = 0f;
        [SerializeField] protected float lastFireTime = 0f;
        [SerializeField] protected bool isLocked = false;
        [SerializeField] protected Transform lockedTarget = null;
        [SerializeField] protected float lockProgress = 0f;

        // Events
        public System.Action<float> OnHeatChanged;
        public System.Action<Transform> OnTargetLocked;
        public System.Action OnTargetLost;

        protected virtual void Awake()
        {
            if (firePoint == null)
                firePoint = transform;
            
            if (ownerShip == null)
                ownerShip = GetComponentInParent<ShipClass>();
                
            if (flightController == null)
                flightController = FindFirstObjectByType<DomeClashFlightController>();
        }

        protected virtual void Update()
        {
            UpdateHeat();
            UpdateLockOn();
            HandleInput();
        }

        protected virtual void UpdateHeat()
        {
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
                }
            }
            else
            {
                if (lockedTarget != null)
                {
                    lockedTarget = null;
                    isLocked = false;
                    lockProgress = 0f;
                    OnTargetLost?.Invoke();
                }
            }
        }

        protected virtual Transform FindTarget()
        {
            // Simple target finding - can be expanded with radar system
            Collider[] colliders = Physics.OverlapSphere(transform.position, stats.lockRange);
            
            foreach (Collider col in colliders)
            {
                ShipClass ship = col.GetComponent<ShipClass>();
                if (ship != null && ship != ownerShip)
                {
                    // Check if target is in front of us
                    Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, directionToTarget);
                    
                    if (angle < 30f) // 30 degree cone
                    {
                        return col.transform;
                    }
                }
            }
            
            return null;
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
            if (currentHeat >= stats.maxHeat) return false;
            if (stats.requiresLock && !isLocked) return false;
            
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
            currentHeat += stats.heatGeneration;
            OnHeatChanged?.Invoke(currentHeat);

            // Create projectile
            CreateProjectile();
        }

        protected virtual void FireMissile()
        {
            lastFireTime = Time.time;
            
            // Create homing missile
            CreateHomingMissile();
        }

        protected virtual void CreateProjectile()
        {
            // Base projectile creation - override in specific weapon types
            GameObject projectile = CreateProjectileObject();
            
            if (projectile != null)
            {
                Projectile proj = projectile.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.Initialize(firePoint.position, firePoint.forward, stats.damage, stats.projectileSpeed, ownerShip);
                }
            }
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
            return currentHeat >= stats.maxHeat;
        }

        public virtual Transform GetLockedTarget()
        {
            return lockedTarget;
        }

        public virtual float GetLockProgress()
        {
            return lockProgress;
        }
    }
} 