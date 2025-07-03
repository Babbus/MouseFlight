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
        PrototypeShip,
        Bastion,
        Breacher,
        Razor,
        Haven
    }

    // DamageType moved to DamageSystem.cs to avoid duplication

    [System.Serializable]
    public class ShipStats
    {
        [Header("Flight - Transform Based")]
        [Tooltip("Maximum forward speed (m/s)")]
        [Range(10, 1000)]
        public float maxSpeed = 100f;

        [Tooltip("How quickly the ship accelerates (m/s^2)")]
        [Range(1, 100)]
        public float acceleration = 10f;

        [Tooltip("Turn rate (degrees/sec)")]
        [Range(10, 200)]
        public float turnRate = 30f;

        [Tooltip("Strafe speed (m/s)")]
        [Range(0, 100)]
        public float strafeSpeed = 20f;

        [Tooltip("Boost duration (seconds)")]
        [Range(0, 10)]
        public float boostDuration = 5f;

        [Header("Physics - Minimal")]
        [Tooltip("Ship mass (kg)")]
        [Range(10, 1000)]
        public float mass = 100f;
    }

    public abstract class ShipClass : MonoBehaviour
    {
        [Header("Ship Identity")]
        public ShipType shipType;
        public string shipName = "Prototype Ship";
        [Header("Stats")]
        public ShipStats stats;
        [Header("Weapons")]
        [Tooltip("Bu geminin varsayılan birincil silahı")] 
        public DomeClash.Weapons.PrimaryWeaponData primaryWeaponData;
        [SerializeField, Tooltip("Bu geminin silah yöneticisi")] 
        protected DomeClash.Weapons.WeaponManager weaponManager;
        [Header("Components")]
        [SerializeField] protected Rigidbody rb;
        [SerializeField] protected MouseFlightController flightController;
        [SerializeField] protected DamageSystem damageSystem;
        
        // Health and status
        public bool IsAlive => damageSystem != null ? damageSystem.IsAlive : true;

        protected virtual void Awake()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody>();
            if (flightController == null)
                flightController = FindFirstObjectByType<MouseFlightController>();
            if (weaponManager == null)
                weaponManager = GetComponent<DomeClash.Weapons.WeaponManager>();
            if (damageSystem == null)
                damageSystem = GetComponent<DamageSystem>();
            
            // Setup weapon manager
            if (weaponManager != null && primaryWeaponData != null)
                weaponManager.EquipPrimary(primaryWeaponData);
                
            // Setup damage system events
            if (damageSystem != null)
            {
                damageSystem.OnShipDestroyed += OnShipDestroyed;
                damageSystem.OnCriticalFailure += OnCriticalFailure;
            }
            
            InitializeShip();
        }

        protected virtual void Start() { }
        protected virtual void Update() { UpdateShipState(); }
        protected virtual void FixedUpdate() { }
        protected virtual void InitializeShip()
        {
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                Debug.Log($"{shipName}: Rigidbody set to kinematic - NO PHYSICS mode");
            }
        }
        
        protected virtual void UpdateShipState() { }
        
        // Damage system event handlers
        protected virtual void OnShipDestroyed()
        {
            Debug.Log($"{shipName} has been destroyed!");
            // Handle ship destruction (respawn, game over, etc.)
        }

        protected virtual void OnCriticalFailure(string failureType)
        {
            Debug.Log($"{shipName} suffered critical failure: {failureType}");
            // Handle critical system failures
        }
        
        // Flight-related methods
        public float GetMaxSpeed() => stats.maxSpeed;
        public float GetAcceleration() => stats.acceleration;
        public float GetTurnRate() => stats.turnRate;
        public float GetBoostDuration() => stats.boostDuration;
        
        // Health and damage methods
        public virtual float GetHealthPercent()
        {
            return damageSystem != null ? damageSystem.TotalHPPercent : 1f;
        }

        public virtual float GetShieldPercent()
        {
            return damageSystem != null ? damageSystem.Shield.HPPercent : 1f;
        }

        public virtual float GetArmorPercent()
        {
            return damageSystem != null ? damageSystem.Armor.HPPercent : 1f;
        }

        public virtual float GetCorePercent()
        {
            return damageSystem != null ? damageSystem.Core.HPPercent : 1f;
        }
        
        // Input methods - implemented by derived classes
        public virtual void SetPitchInput(float value) { }
        public virtual void SetYawInput(float value) { }
        public virtual void SetRollInput(float value) { }
        public virtual void SetStrafeInput(float value) { }
    }
} 