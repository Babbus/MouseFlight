using UnityEngine;
using DomeClash.Core;
using DomeClash.Weapons;
using DomeClash.Weapons.PrototypeRazorWeapons;

namespace DomeClash.Ships
{
    /// <summary>
    /// Prototype Razor Ship with Arc Splitter and Hunter Seeker weapons
    /// </summary>
    public class PrototypeRazorShip : ShipClass
    {
        [Header("Razor Prototype Weapons")]
        [SerializeField] private ArcSplitter arcSplitter;
        [SerializeField] private HunterSeeker hunterSeeker;
        
        [Header("Weapon Fire Points")]
        [SerializeField] private Transform[] primaryFirePoints; // Twin barrels for Arc Splitter
        [SerializeField] private Transform[] missileFirePoints; // Missile pods for Hunter Seeker
        
        [Header("Energy System")]
        [SerializeField] private EnergySystem energySystem;
        
        // Weapon management
        private WeaponSystem currentPrimaryWeapon;
        private WeaponSystem currentSecondaryWeapon;
        
        protected override void Awake()
        {
            base.Awake();
            
            // Set ship stats for Razor
            shipName = "Razor Prototype";
            shipType = ShipType.Razor;
            
            // Initialize weapon systems
            InitializeWeaponSystems();
            
            // Initialize energy system
            InitializeEnergySystem();
        }

        private void InitializeWeaponSystems()
        {
            // Setup Arc Splitter (Primary)
            if (arcSplitter == null)
                arcSplitter = GetComponentInChildren<ArcSplitter>();
            
            if (arcSplitter != null)
            {
                currentPrimaryWeapon = arcSplitter;
                
                // Setup twin barrels
                if (primaryFirePoints != null && primaryFirePoints.Length > 0)
                {
                    arcSplitter.TwinBarrels = primaryFirePoints;
                }
                
                // Subscribe to weapon events
                arcSplitter.OnWeaponOverheated += OnPrimaryOverheated;
                arcSplitter.OnWeaponCooledDown += OnPrimaryCooled;
                arcSplitter.OnEnergyConsumed += OnEnergyConsumed;
                
                Debug.Log("Arc Splitter initialized successfully!");
            }
            
            // Setup Hunter Seeker (Secondary)
            if (hunterSeeker == null)
                hunterSeeker = GetComponentInChildren<HunterSeeker>();
                
            if (hunterSeeker != null)
            {
                currentSecondaryWeapon = hunterSeeker;
                
                // Setup missile pods
                if (missileFirePoints != null && missileFirePoints.Length > 0)
                {
                    hunterSeeker.MissilePods = missileFirePoints;
                }
                
                // Subscribe to weapon events
                hunterSeeker.OnWeaponOverheated += OnSecondaryOverheated;
                hunterSeeker.OnWeaponCooledDown += OnSecondaryCooled;
                hunterSeeker.OnEnergyConsumed += OnEnergyConsumed;
                hunterSeeker.OnTargetLocked += OnMissileTargetLocked;
                hunterSeeker.OnTargetLost += OnMissileTargetLost;
                
                Debug.Log("Hunter Seeker initialized successfully!");
            }
        }

        private void InitializeEnergySystem()
        {
            if (energySystem == null)
                energySystem = GetComponent<EnergySystem>();
                
            if (energySystem == null)
            {
                // Add energy system if not present
                energySystem = gameObject.AddComponent<EnergySystem>();
                Debug.Log("Energy system added to ship");
            }
            
            // Configure energy stats for Razor
            energySystem.Stats.maxEnergy = 120f; // High energy for agile ship
            energySystem.Stats.energyRegenRate = 15f; // Fast regen
            energySystem.Stats.energyRegenDelay = 1f;
            energySystem.Stats.weaponEnergyMultiplier = 0.8f; // Efficient weapons
            
            // Subscribe to energy events
            energySystem.OnEnergyDepleted += OnEnergyDepleted;
            energySystem.OnEnergyFull += OnEnergyRestored;
            
            Debug.Log($"Energy system initialized: {energySystem.MaxEnergy} max energy");
        }

        // Weapon event handlers
        private void OnPrimaryOverheated()
        {
            Debug.Log("Arc Splitter overheated!");
            // Add visual/audio feedback here
        }

        private void OnPrimaryCooled()
        {
            Debug.Log("Arc Splitter cooled down");
        }

        private void OnSecondaryOverheated()
        {
            Debug.Log("Hunter Seeker overheated!");
        }

        private void OnSecondaryCooled()
        {
            Debug.Log("Hunter Seeker cooled down");
        }

        private void OnMissileTargetLocked(Transform target)
        {
            Debug.Log($"Hunter Seeker locked onto: {target.name}");
        }

        private void OnMissileTargetLost()
        {
            Debug.Log("Hunter Seeker lost target lock");
        }

        private void OnEnergyConsumed(float amount)
        {
            if (energySystem != null)
            {
                energySystem.ConsumeWeaponEnergy(amount);
            }
        }

        private void OnEnergyDepleted()
        {
            Debug.Log("Ship energy depleted! Weapons offline!");
        }

        private void OnEnergyRestored()
        {
            Debug.Log("Ship energy restored");
        }

        // Public methods for external systems
        public WeaponSystem GetPrimaryWeapon() => currentPrimaryWeapon;
        public WeaponSystem GetSecondaryWeapon() => currentSecondaryWeapon;
        public EnergySystem GetEnergySystem() => energySystem;
        
        public bool HasEnergyForWeapon(float energyCost)
        {
            return energySystem != null && energySystem.HasEnoughEnergy(energyCost);
        }

        // Debug methods for testing
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugOverheatPrimary()
        {
            if (currentPrimaryWeapon != null)
                currentPrimaryWeapon.ForceOverheat();
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugDrainEnergy()
        {
            if (energySystem != null)
                energySystem.DebugDrainEnergy();
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugRestoreEnergy()
        {
            if (energySystem != null)
                energySystem.DebugRestoreEnergy();
        }
    }
} 