using UnityEngine;
using DomeClash.Weapons;

namespace DomeClash.Core
{
    /// <summary>
    /// Unity 6000.1.9f1 Compatible Ship Class System
    /// Transform-Based Movement (NO PHYSICS)
    /// </summary>
    public enum ShipType
    {
        Bastion,
        Breacher,
        Razor,
        Haven
    }

    public enum DamageType
    {
        Kinetic,
        Energy,
        Explosive
    }

    [System.Serializable]
    public class ShipStats
    {
        [Header("Flight - Transform Based")]
        public float maxSpeed = 100f;
        public float acceleration = 10f;
        public float turnRate = 30f;
        public float strafeSpeed = 20f;
        public float boostDuration = 5f;
        
        [Header("Combat")]
        public float maxHealth = 100f;
        public float health = 100f;
        public float maxShields = 100f;
        public float shields = 100f;
        public float maxEnergy = 100f;
        public float energy = 100f;
        public float shieldCapacity = 100f;
        public float armorRating = 50f;
        public float energyCapacity = 100f;
        
        [Header("Physics - Minimal")]
        public float mass = 100f;  // Sadece referans için - physics kullanılmıyor
    }

    public abstract class ShipClass : MonoBehaviour
    {
        [Header("Ship Identity")]
        public ShipType shipType;
        public string shipName = "Prototype Ship";
        
        [Header("Stats")]
        public ShipStats stats;
        
        [Header("Components")]
        [SerializeField] protected Rigidbody rb;  // Sadece collision detection için
        [SerializeField] protected DomeClashFlightController flightController;
        [SerializeField] protected WeaponSystem[] weapons;
        
        [Header("Current State - Transform Based")]
        [SerializeField] protected float currentHealth = 100f;
        [SerializeField] protected float currentShield = 100f;
        [SerializeField] protected float currentEnergy = 100f;
        [System.NonSerialized] protected float currentSpeed = 0f;  // Transform-based speed - not serialized

        // Events
        public System.Action<float> OnHealthChanged;
        public System.Action<float> OnShieldChanged;
        public System.Action<float> OnArmorChanged;
        public System.Action<float> OnEnergyChanged;
        public System.Action OnDestroyed;

        protected virtual void Awake()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody>();
                
            if (flightController == null)
                flightController = FindFirstObjectByType<DomeClashFlightController>();
                
            if (weapons == null || weapons.Length == 0)
                weapons = GetComponentsInChildren<WeaponSystem>();
                
            InitializeShip();
        }

        protected virtual void Start()
        {
            // Initialize current values
            currentHealth = stats.health;
            currentShield = stats.shields;
            currentEnergy = stats.energy;
        }

        protected virtual void Update()
        {
            UpdateShipState();
            // NO STALL HANDLING - transform-based system doesn't need it
        }

        protected virtual void FixedUpdate() { }

        protected virtual void InitializeShip()
        {
            // Minimal rigidbody setup - ONLY for collision detection
            if (rb != null)
            {
                rb.isKinematic = true;  // NO PHYSICS - only collision detection
                rb.useGravity = false;
                Debug.Log($"{shipName}: Rigidbody set to kinematic - NO PHYSICS mode");
            }
        }

        protected virtual void UpdateShipState()
        {
            // Transform-based systems update their own currentSpeed
            // Base class doesn't need to do anything here
        }

        public virtual void TakeDamage(float damage, DamageType damageType)
        {
            float remainingDamage = damage;
            
            // Apply damage to shields first
            if (currentShield > 0)
            {
                float shieldDamage = Mathf.Min(currentShield, remainingDamage);
                currentShield -= shieldDamage;
                remainingDamage -= shieldDamage;
                OnShieldChanged?.Invoke(currentShield);
            }
            
            // Apply remaining damage to health
            if (remainingDamage > 0)
            {
                currentHealth -= remainingDamage;
                OnHealthChanged?.Invoke(currentHealth);
                OnArmorChanged?.Invoke(currentHealth);
                
                if (currentHealth <= 0)
                {
                    DestroyShip();
                }
            }
        }

        public virtual bool ConsumeEnergy(float amount)
        {
            if (currentEnergy >= amount)
            {
                currentEnergy -= amount;
                OnEnergyChanged?.Invoke(currentEnergy);
                return true;
            }
            return false;
        }

        public virtual void RestoreShield(float amount)
        {
            currentShield = Mathf.Min(currentShield + amount, stats.shieldCapacity);
            OnShieldChanged?.Invoke(currentShield);
        }

        public virtual void RestoreEnergy(float amount)
        {
            currentEnergy = Mathf.Min(currentEnergy + amount, stats.energyCapacity);
            OnEnergyChanged?.Invoke(currentEnergy);
        }

        protected virtual void DestroyShip()
        {
            OnDestroyed?.Invoke();
            // Add destruction effects here
            Debug.Log($"{shipName} has been destroyed!");
        }

        // Getters
        public float GetHealthPercent() => currentHealth / stats.maxHealth;
        public float GetShieldPercent() => currentShield / stats.maxShields;
        public float GetArmorPercent() => currentHealth / stats.armorRating;
        public float GetEnergyPercent() => currentEnergy / stats.maxEnergy;
        public virtual float GetCurrentSpeed() => currentSpeed;  // Virtual - override in derived classes
        public ShipType GetShipType() => shipType;
        public string GetShipName() => shipName;
        
        // Additional getters for DebugHUD
        public float GetCurrentHealth() => currentHealth;
        public float GetCurrentEnergy() => currentEnergy;
        public float GetMaxSpeed() => stats.maxSpeed;
        public float GetAcceleration() => stats.acceleration;
        public float GetTurnRate() => stats.turnRate;
        public float GetBoostDuration() => stats.boostDuration;
        public float GetEnergyCapacity() => stats.energyCapacity;

        // Input methods - implemented by derived classes
        public virtual void SetPitchInput(float value) { }
        public virtual void SetYawInput(float value) { }
        public virtual void SetRollInput(float value) { }
        public virtual void SetStrafeInput(float value) { }
    }
} 