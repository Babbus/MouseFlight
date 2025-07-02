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
        
        [Header("Movement")]
        [Tooltip("Base acceleration rate")]
        public float acceleration = 40f;

        [Tooltip("Base deceleration rate")]
        public float deceleration = 20f;

        [Tooltip("Stall threshold speed")]
        public float stallThreshold = 25f;

        [Header("Visual & Audio")]
        [Tooltip("Engine sound profile name")]
        public string engineSoundProfile = "default";
        
        [Tooltip("Thruster visual effect intensity")]
        [Range(0f, 2f)]
        public float thrusterEffectIntensity = 1f;

        [Tooltip("Maneuver rate (max strafe speed, m/s)")]
        public float maneuverRate = 3f;

        [Header("Maneuvering")]
        [Tooltip("Maximum strafe acceleration (m/s^2)")]
        public float strafeThrust = 60f;

        [Header("Core Stats")]
        [Tooltip("The ship's mass. Affects gravity and turning agility.")]
        public float mass = 1f;
        [Tooltip("The raw power of the main thrusters. Determines max achievable speed.")]
        public float thrust = 100f;

        [Header("Retro Thrusters")]
        [Tooltip("Retro (braking) thrust, used for slowing down. Defaults to strafeThrust.")]
        public float retroThrust = 60f;

        /// <summary>
        /// Create a flight profile by reading stats from a ship
        /// This is the main way to create profiles - no hardcoded ship types
        /// </summary>
        public static FlightProfile CreateFromShip(PrototypeShip ship)
        {
            if (ship == null) 
            {
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
            profile.turnSpeed = 80f;  // Increased from 60f for better default turning
            profile.bankingAmount = 45f;
            profile.maxBankAngle = 60f;
            profile.bankSmoothing = 8f;
            profile.autoLevelRate = 4f;
            profile.speedBankingMultiplier = 1.0f;
            profile.mousePositionBankingSensitivity = 0.6f;
            profile.stallThreshold = 25f;
            profile.acceleration = 40f;
            profile.deceleration = 20f;
            profile.engineSoundProfile = "default";
            profile.thrusterEffectIntensity = 1.0f;
            profile.maneuverRate = 3f;
            profile.strafeThrust = 60f;
            profile.retroThrust = 60f;
            profile.mass = 1f;
            profile.thrust = 100f;
            
            return profile;
        }
        
        /// <summary>
        /// Load flight stats from a ship - the main method for dynamic profile creation
        /// </summary>
        public void LoadFromShip(PrototypeShip ship)
        {
            if (ship == null || ship.stats == null) 
            {
                return;
            }
            
            // Set profile identity from ship
            profileName = $"{ship.shipName} Flight Profile";
            description = $"Dynamic flight profile for {ship.shipName} - generated from ship stats";
            
            // Load all flight-relevant stats directly from ship
            maxSpeed = ship.stats.maxSpeed;
            acceleration = ship.stats.acceleration;
            deceleration = acceleration * 0.5f;
            
            // Set other values
            flightSpeed = maxSpeed * 0.8f;
            minSpeed = maxSpeed * 0.15f;
            speedSmoothing = 12f;
            strafeSpeed = ship.stats.strafeSpeed;
            turnSpeed = ship.stats.turnRate;
            bankingAmount = 45f;
            maxBankAngle = 60f;
            bankSmoothing = 8f;
            autoLevelRate = 4f;
            speedBankingMultiplier = 1.0f;
            mousePositionBankingSensitivity = 0.6f;
            stallThreshold = maxSpeed * 0.2f; // 20% of max speed
            engineSoundProfile = "default";
            thrusterEffectIntensity = 1f;
            maneuverRate = ship.stats.maneuverRate;
            strafeThrust = ship.stats.strafeThrust;
            retroThrust = ship.stats.strafeThrust;
            mass = ship.stats.mass;
            thrust = ship.stats.thrust;
        }
        
        /// <summary>
        /// Refresh the profile by reloading from the ship
        /// Useful if ship stats change during runtime
        /// </summary>
        public void RefreshFromShip(PrototypeShip ship)
        {
            LoadFromShip(ship);
        }
    }
} 