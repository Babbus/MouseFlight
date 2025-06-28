using UnityEngine;

namespace DomeClash.Core
{
    /// <summary>
    /// Flight Profile ScriptableObject - GDD Class-Based Flight Behavior
    /// Defines unique flight characteristics for each ship class
    /// Bastion, Breacher, Razor, Haven profiles
    /// </summary>
    [CreateAssetMenu(fileName = "New Flight Profile", menuName = "DomeClash/Flight Profile")]
    public class FlightProfile : ScriptableObject
    {
        [Header("Profile Identity")]
        public string profileName = "Default Profile";
        public ShipType targetShipClass = ShipType.Bastion;
        [TextArea(3, 5)]
        public string description = "Flight behavior description";
        
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
        public float bankSmoothing = 8f;
        
        [Tooltip("Auto-level rate when no input")]
        public float autoLevelRate = 4f;
        
        [Tooltip("Speed-based banking multiplier")]
        public float speedBankingMultiplier = 1.0f;
        
        [Tooltip("Mouse position banking sensitivity")]
        public float mousePositionBankingSensitivity = 0.6f;
        
        [Header("Mass & Physics (GDD System)")]
        [Tooltip("Ship mass - affects collision and inertia")]
        public float mass = 350f;
        
        [Tooltip("Inertia factor - affects momentum")]
        public float inertiaFactor = 1f;
        
        [Tooltip("Stall threshold speed")]
        public float stallThreshold = 15f;
        
        [Header("Energy & Performance")]
        [Tooltip("Boost duration in seconds")]
        public float boostDuration = 3f;
        
        [Tooltip("Energy consumption multiplier")]
        public float energyConsumptionRate = 1f;
        
        [Tooltip("Dodge energy cost")]
        public float dodgeEnergyCost = 25f;
        
        [Header("Visual & Audio")]
        [Tooltip("Engine sound profile name")]
        public string engineSoundProfile = "default";
        
        [Tooltip("Thruster visual effect intensity")]
        [Range(0f, 2f)]
        public float thrusterEffectIntensity = 1f;
        
        /// <summary>
        /// Create default profiles for GDD ship classes
        /// </summary>
        public static FlightProfile CreateDefaultProfile(ShipType shipType)
        {
            var profile = CreateInstance<FlightProfile>();
            
            switch (shipType)
            {
                case ShipType.Bastion:
                    SetupBastionProfile(profile);
                    break;
                case ShipType.Breacher:
                    SetupBreacherProfile(profile);
                    break;
                case ShipType.Razor:
                    SetupRazorProfile(profile);
                    break;
                case ShipType.Haven:
                    SetupHavenProfile(profile);
                    break;
            }
            
            return profile;
        }
        
        private static void SetupBastionProfile(FlightProfile profile)
        {
            // GDD: "Ground-skimming VTOL tank. Feels heavy, slow to turn"
            profile.profileName = "Bastion Heavy Tank";
            profile.targetShipClass = ShipType.Bastion;
            profile.description = "Heavy VTOL tank - slow but stable. Ground-skimming flight profile.";
            
            // Speed settings - slowest class (INCREASED)
            profile.flightSpeed = 75f;   // 65 → 75 (+15%)
            profile.maxSpeed = 105f;     // 90 → 105 (+15%)
            profile.minSpeed = 15f;
            profile.speedSmoothing = 8f;
            profile.strafeSpeed = 15f;
            
            // Maneuverability - least agile (BALANCED: 80 → 40 deg/s)
            profile.turnSpeed = 40f;  // Ağır tank - çok yavaş dönüş
            profile.bankingAmount = 30f;  // 20 → 30 (stronger banking)
            profile.maxBankAngle = 45f;   // 35 → 45 (more max bank)
            profile.bankSmoothing = 5f;
            profile.autoLevelRate = 3f;
            profile.speedBankingMultiplier = 0.5f;
            profile.mousePositionBankingSensitivity = 0.3f;
            
            // Mass - heaviest
            profile.mass = 490f;
            profile.inertiaFactor = 1.8f;
            profile.stallThreshold = 12f;
            
            // Energy
            profile.boostDuration = 3.4f;
            profile.energyConsumptionRate = 0.8f;
            profile.dodgeEnergyCost = 40f;
            
            // Audio/Visual
            profile.engineSoundProfile = "heavy_vtol";
            profile.thrusterEffectIntensity = 1.5f;
        }
        
        private static void SetupBreacherProfile(FlightProfile profile)
        {
            // GDD: "Mid-weight ship with punchy afterburner bursts"
            profile.profileName = "Breacher Assault";
            profile.targetShipClass = ShipType.Breacher;
            profile.description = "Frontline aggressor - burst movement with aggressive dive angles.";
            
            // Speed settings - medium (INCREASED)
            profile.flightSpeed = 100f;  // 85 → 100 (+15%)  
            profile.maxSpeed = 135f;     // 120 → 135 (+15%)
            profile.minSpeed = 20f;
            profile.speedSmoothing = 10f;
            profile.strafeSpeed = 22f;
            
            // Maneuverability - balanced (BALANCED: 100 → 50 deg/s)
            profile.turnSpeed = 50f;   // Orta seviye çeviklik
            profile.bankingAmount = 50f;  // 35 → 50 (stronger banking)
            profile.maxBankAngle = 65f;   // 50 → 65 (more max bank)
            profile.bankSmoothing = 8f;
            profile.autoLevelRate = 5f;
            profile.speedBankingMultiplier = 1.0f;
            profile.mousePositionBankingSensitivity = 0.6f;
            
            // Mass - medium
            profile.mass = 360f;
            profile.inertiaFactor = 1.2f;
            profile.stallThreshold = 18f;
            
            // Energy
            profile.boostDuration = 2.8f;
            profile.energyConsumptionRate = 1.2f;
            profile.dodgeEnergyCost = 30f;
            
            // Audio/Visual
            profile.engineSoundProfile = "burst_assault";
            profile.thrusterEffectIntensity = 1.3f;
        }
        
        private static void SetupRazorProfile(FlightProfile profile)
        {
            // GDD: "High-speed interceptor. Twitchy, light, and highly reactive"
            profile.profileName = "Razor Interceptor";
            profile.targetShipClass = ShipType.Razor;
            profile.description = "Ultra-light interceptor - maximum agility and speed with stall risk.";
            
            // Speed settings - fastest (INCREASED)
            profile.flightSpeed = 120f;  // 100 → 120 (+20%)
            profile.maxSpeed = 160f;     // 140 → 160 (+20%)
            profile.minSpeed = 25f;
            profile.speedSmoothing = 15f;
            profile.strafeSpeed = 35f;
            
            // Maneuverability - most agile (BALANCED: 120 → 60 deg/s)
            profile.turnSpeed = 60f;   // Daha makul: saniyede 60 derece
            profile.bankingAmount = 60f;  // 45 → 60 (stronger banking)
            profile.maxBankAngle = 85f;   // 75 → 85 (more max bank)
            profile.bankSmoothing = 12f;
            profile.autoLevelRate = 8f;
            profile.speedBankingMultiplier = 1.5f;
            profile.mousePositionBankingSensitivity = 1.0f;
            
            // Mass - lightest
            profile.mass = 220f;
            profile.inertiaFactor = 0.7f;
            profile.stallThreshold = 22f;
            
            // Energy
            profile.boostDuration = 2.4f;
            profile.energyConsumptionRate = 1.5f;
            profile.dodgeEnergyCost = 20f;
            
            // Audio/Visual
            profile.engineSoundProfile = "light_interceptor";
            profile.thrusterEffectIntensity = 0.8f;
        }
        
        private static void SetupHavenProfile(FlightProfile profile)
        {
            // GDD: "Floaty and precise. Designed for mid-air stasis and stable positioning"
            profile.profileName = "Haven Support";
            profile.targetShipClass = ShipType.Haven;
            profile.description = "Support craft - stable hovering and precise positioning over speed.";
            
            // Speed settings - moderate (INCREASED)
            profile.flightSpeed = 90f;   // 75 → 90 (+20%)
            profile.maxSpeed = 125f;     // 105 → 125 (+20%)
            profile.minSpeed = 18f;
            profile.speedSmoothing = 12f;
            profile.strafeSpeed = 28f;
            
            // Maneuverability - stable and precise (BALANCED: 90 → 45 deg/s)
            profile.turnSpeed = 45f;   // Hassas ve dengeli
            profile.bankingAmount = 40f;  // 25 → 40 (stronger banking)
            profile.maxBankAngle = 55f;   // 40 → 55 (more max bank)
            profile.bankSmoothing = 10f;
            profile.autoLevelRate = 6f;
            profile.speedBankingMultiplier = 0.8f;
            profile.mousePositionBankingSensitivity = 0.5f;
            
            // Mass - light-medium
            profile.mass = 250f;
            profile.inertiaFactor = 0.9f;
            profile.stallThreshold = 15f;
            
            // Energy
            profile.boostDuration = 2.7f;
            profile.energyConsumptionRate = 0.9f;
            profile.dodgeEnergyCost = 25f;
            
            // Audio/Visual
            profile.engineSoundProfile = "support_hover";
            profile.thrusterEffectIntensity = 1.0f;
        }
        
        /// <summary>
        /// Static factory methods for individual profiles
        /// </summary>
        public static FlightProfile CreateRazorProfile()
        {
            var profile = CreateInstance<FlightProfile>();
            SetupRazorProfile(profile);
            return profile;
        }
        
        public static FlightProfile CreateBastionProfile()
        {
            var profile = CreateInstance<FlightProfile>();
            SetupBastionProfile(profile);
            return profile;
        }
        
        public static FlightProfile CreateBreacherProfile()
        {
            var profile = CreateInstance<FlightProfile>();
            SetupBreacherProfile(profile);
            return profile;
        }
        
        public static FlightProfile CreateHavenProfile()
        {
            var profile = CreateInstance<FlightProfile>();
            SetupHavenProfile(profile);
            return profile;
        }
    }
} 