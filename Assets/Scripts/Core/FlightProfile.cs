using UnityEngine;

namespace DomeClash.Core
{
    /// <summary>
    /// Flight Profile ScriptableObject - Dynamic Ship-Based Flight Behavior
    /// Reads flight characteristics directly from the attached ship's stats
    /// No hardcoded ship types - truly modular and data-driven
    /// </summary>
    [CreateAssetMenu(fileName = "New Flight Profile", menuName = "DomeClash/Flight Profile")]
    public class FlightProfile : ScriptableObject
    {
        [Header("Profile Identity")]
        public string profileName = "Dynamic Flight Profile";
        [TextArea(3, 5)]
        public string description = "Dynamically generated flight profile from ship stats";
        
        [Header("Speed & Acceleration")]
        [Tooltip("Base flight speed")]
        public float flightSpeed = 80f;
        
        [Tooltip("Maximum flight speed")]
        public float maxSpeed = 150f;
        
        [Tooltip("Minimum flight speed (stall threshold)")]
        public float minSpeed = 20f;
        
        [Tooltip("Speed change smoothing")]
        public float speedSmoothing = 12f;
        
        [Tooltip("Strafe movement speed")]
        public float strafeSpeed = 25f;
        
        [Header("Maneuverability")]
        [Tooltip("Turn speed for pitch/yaw/roll")]
        public float turnSpeed = 250f;
        
        [Tooltip("Banking amount when turning")]
        public float bankingAmount = 30f;
        
        [Header("Advanced Banking")]
        [Tooltip("Maximum bank angle in degrees")]
        public float maxBankAngle = 60f;
        
        [Tooltip("Bank smoothing factor (higher = more responsive)")]
        [Range(1f, 50f)]
        public float bankSmoothing = 16f;
        
        [Tooltip("Auto-level rate when no input")]
        public float autoLevelRate = 4f;
        
        [Tooltip("Speed-based banking multiplier")]
        public float speedBankingMultiplier = 1.0f;
        
        [Tooltip("Mouse position banking sensitivity")]
        public float mousePositionBankingSensitivity = 0.6f;
        
        [Header("Mass & Physics")]
        [Tooltip("Ship mass - affects collision and inertia")]
        public float mass = 350f;
        
        [Tooltip("Inertia factor - affects momentum")]
        public float inertiaFactor = 1f;
        
        [Tooltip("Stall threshold speed")]
        public float stallThreshold = 15f;
        
        [Header("Visual & Audio")]
        [Tooltip("Engine sound profile name")]
        public string engineSoundProfile = "default";
        
        [Tooltip("Thruster visual effect intensity")]
        [Range(0f, 2f)]
        public float thrusterEffectIntensity = 1f;
        
        /// <summary>
        /// Create a flight profile by reading stats from a ship
        /// This is the main way to create profiles - no hardcoded ship types
        /// </summary>
        public static FlightProfile CreateFromShip(ShipClass ship)
        {
            if (ship == null) 
            {
                Debug.LogWarning("Cannot create flight profile from null ship");
                return CreateDefaultProfile();
            }
            
            var profile = CreateInstance<FlightProfile>();
            profile.LoadFromShip(ship);
            return profile;
        }
        
        /// <summary>
        /// Create a default profile for fallback cases
        /// </summary>
        public static FlightProfile CreateDefaultProfile()
        {
            var profile = CreateInstance<FlightProfile>();
            profile.profileName = "Default Flight Profile";
            profile.description = "Default flight profile for fallback cases";
            
            // Sensible defaults
            profile.flightSpeed = 100f;
            profile.maxSpeed = 150f;
            profile.minSpeed = 20f;
            profile.speedSmoothing = 12f;
            profile.strafeSpeed = 25f;
            profile.turnSpeed = 60f;
            profile.bankingAmount = 30f;
            profile.maxBankAngle = 60f;
            profile.bankSmoothing = 16f;
            profile.autoLevelRate = 4f;
            profile.speedBankingMultiplier = 1.0f;
            profile.mousePositionBankingSensitivity = 0.6f;
            profile.mass = 350f;
            profile.inertiaFactor = 1.0f;
            profile.stallThreshold = 15f;
            profile.engineSoundProfile = "default";
            profile.thrusterEffectIntensity = 1.0f;
            
            return profile;
        }
        
        /// <summary>
        /// Load flight stats from a ship - the main method for dynamic profile creation
        /// </summary>
        public void LoadFromShip(ShipClass ship)
        {
            if (ship == null || ship.stats == null) 
            {
                Debug.LogWarning("Cannot load flight profile from null ship or ship stats");
                return;
            }
            
            // Set profile identity from ship
            profileName = $"{ship.shipName} Flight Profile";
            description = $"Dynamic flight profile for {ship.shipName} - generated from ship stats";
            
            // Load all flight-relevant stats directly from ship
            flightSpeed = ship.stats.maxSpeed * 0.8f;  // Cruise at 80% of max speed
            maxSpeed = ship.stats.maxSpeed;
            minSpeed = ship.stats.maxSpeed * 0.15f;    // Stall at 15% of max speed
            speedSmoothing = 12f;  // Default smoothing
            strafeSpeed = ship.stats.strafeSpeed;
            
            // Maneuverability from ship stats
            turnSpeed = ship.stats.turnRate;
            bankingAmount = 30f;  // Default banking amount
            maxBankAngle = CalculateMaxBankAngle(ship.stats.mass);
            bankSmoothing = 16f;   // Default smoothing
            autoLevelRate = 4f;   // Default auto-level
            speedBankingMultiplier = 1.0f;  // Default multiplier
            mousePositionBankingSensitivity = 0.6f;  // Default sensitivity
            
            // Mass and physics from ship
            mass = ship.stats.mass;
            inertiaFactor = CalculateInertiaFactor(ship.stats.mass);
            stallThreshold = ship.stats.maxSpeed * 0.15f;  // Automatically 15% of max speed
            
            // Visual and audio based on ship characteristics
            engineSoundProfile = DetermineEngineSound(ship.stats.mass);
            thrusterEffectIntensity = DetermineThrusterIntensity(ship.stats.mass);
            
            Debug.Log($"Loaded flight profile from {ship.shipName}: maxSpeed={maxSpeed}, turnSpeed={turnSpeed}, mass={mass}");
        }
        
        /// <summary>
        /// Calculate max bank angle based on ship mass
        /// Heavier ships have more limited banking
        /// </summary>
        private float CalculateMaxBankAngle(float shipMass)
        {
            if (shipMass > 400f) return 60f;      // Heavy ships: limited banking
            if (shipMass > 250f) return 75f;      // Medium ships: moderate banking
            return 90f;                           // Light ships: full banking
        }
        
        /// <summary>
        /// Calculate inertia factor based on ship mass
        /// Heavier ships have higher inertia
        /// </summary>
        private float CalculateInertiaFactor(float shipMass)
        {
            if (shipMass > 400f) return 1.8f;     // Heavy ships: high inertia
            if (shipMass > 250f) return 1.2f;     // Medium ships: moderate inertia
            return 0.7f;                          // Light ships: low inertia
        }
        
        /// <summary>
        /// Determine engine sound based on ship mass
        /// </summary>
        private string DetermineEngineSound(float shipMass)
        {
            if (shipMass > 400f) return "heavy_vtol";
            if (shipMass > 250f) return "medium_thrust";
            return "light_interceptor";
        }
        
        /// <summary>
        /// Determine thruster intensity based on ship mass
        /// </summary>
        private float DetermineThrusterIntensity(float shipMass)
        {
            if (shipMass > 400f) return 1.5f;     // Heavy ships: powerful thrusters
            if (shipMass > 250f) return 1.2f;     // Medium ships: moderate thrusters
            return 0.8f;                          // Light ships: subtle thrusters
        }
        
        /// <summary>
        /// Refresh the profile by reloading from the ship
        /// Useful if ship stats change during runtime
        /// </summary>
        public void RefreshFromShip(ShipClass ship)
        {
            LoadFromShip(ship);
        }
    }
} 