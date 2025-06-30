using UnityEngine;
using DomeClash.Ships;

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
        public float bankingAmount = 45f;
        
        [Header("Advanced Banking")]
        [Tooltip("Maximum bank angle in degrees")]
        public float maxBankAngle = 60f;
        
        [Tooltip("Bank smoothing factor (higher = more responsive)")]
        [Range(1f, 50f)]
        public float bankSmoothing = 8f;  // Lowered from 35f to 8f for more responsive banking
        
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
        public float stallThreshold = 25f;
        
        [Header("Visual & Audio")]
        [Tooltip("Engine sound profile name")]
        public string engineSoundProfile = "default";
        
        [Tooltip("Thruster visual effect intensity")]
        [Range(0f, 2f)]
        public float thrusterEffectIntensity = 1f;
        
        [Header("Engine")]
        [Tooltip("Engine thrust (N)")]
        public float engineThrust = 5400f;

        // --- Drag tuning ---
        [Header("Drag Model")]
        [Tooltip("Base drag (minimum)")]
        public float baseDrag = 0f;
        [Tooltip("Drag per kg of mass")]
        public float dragPerKg = 0.05f;

        // --- Max speed is now calculated ---
        // maxSpeed = engineThrust / (baseDrag + mass * dragPerKg)

        [Tooltip("Maneuver rate (max strafe speed, m/s)")]
        public float maneuverRate = 3f;

        /// <summary>
        /// Create a flight profile by reading stats from a ship
        /// This is the main way to create profiles - no hardcoded ship types
        /// </summary>
        public static FlightProfile CreateFromShip(PrototypeShip ship)
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
            profile.bankingAmount = 45f;  // Increased from 30f to 45f
            profile.maxBankAngle = 60f;
            profile.bankSmoothing = 8f;  // Lowered from 35f to 8f for more responsive banking
            profile.autoLevelRate = 4f;
            profile.speedBankingMultiplier = 1.0f;
            profile.mousePositionBankingSensitivity = 0.6f;
            profile.mass = 350f;
            profile.inertiaFactor = 1.0f;
            profile.stallThreshold = 25f;
            profile.engineSoundProfile = "default";
            profile.thrusterEffectIntensity = 1.0f;
            profile.baseDrag = 0f;
            profile.dragPerKg = 0.05f; // Reduced to 0.05f for 30 m/s² drag
            
            return profile;
        }
        
        /// <summary>
        /// Load flight stats from a ship - the main method for dynamic profile creation
        /// </summary>
        public void LoadFromShip(PrototypeShip ship)
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
            engineThrust = ship.stats.engineThrust;
            mass = ship.stats.mass;
            baseDrag = 0f;
            dragPerKg = 0.05f; // Reduced to 0.05f for 30 m/s² drag (600kg × 0.05 = 30 m/s²)
            float drag = baseDrag + (mass * dragPerKg);
            maxSpeed = engineThrust / drag;
            flightSpeed = maxSpeed * 0.8f;
            minSpeed = maxSpeed * 0.15f;
            speedSmoothing = 12f;
            strafeSpeed = ship.stats.strafeSpeed;
            turnSpeed = ship.stats.turnRate;
            bankingAmount = 45f;
            maxBankAngle = CalculateMaxBankAngle(mass);
            bankSmoothing = 8f;
            autoLevelRate = 4f;
            speedBankingMultiplier = 1.0f;
            mousePositionBankingSensitivity = 0.6f;
            inertiaFactor = CalculateInertiaFactor(mass);
            stallThreshold = maxSpeed * 0.25f; // More reasonable: 25% of max speed instead of 15%
            engineSoundProfile = DetermineEngineSound(mass);
            thrusterEffectIntensity = DetermineThrusterIntensity(mass);
            maneuverRate = ship.stats.maneuverRate;
            
            Debug.Log($"Loaded flight profile from {ship.shipName}: maxSpeed={maxSpeed}, engineThrust={engineThrust}, mass={mass}, drag={drag}");
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
        public void RefreshFromShip(PrototypeShip ship)
        {
            LoadFromShip(ship);
        }

        // --- TEMPLATE FOR OTHER SHIPS ---
        // To tune another ship:
        // 1. Set stats.mass = [desired mass in kg]
        // 2. Set stats.engineThrust = [desired thrust in N]
        // 3. maxSpeed will be calculated as engineThrust / (baseDrag + mass * dragPerKg)
        // 4. Adjust baseDrag and dragPerKg in FlightProfile if you want global drag changes
    }
} 