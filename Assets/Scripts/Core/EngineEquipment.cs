using UnityEngine;
using DomeClash.Core;
using DomeClash.Ships;

namespace DomeClash.Core
{
    /// <summary>
    /// Engine Equipment - Specific equipment type for ship engines
    /// Provides engine-specific statistics and behavior
    /// </summary>
    [CreateAssetMenu(fileName = "New Engine", menuName = "DomeClash/Equipment/Engine")]
    public class EngineEquipment : EquipmentData
    {
        [Header("Engine Specific Properties")]
        [Tooltip("Engine sound profile ID")]
        public int engineSoundProfileID = 0;
        
        [Tooltip("Engine visual effect intensity")]
        [Range(0f, 2f)]
        public float engineEffectIntensity = 1f;
        
        [Tooltip("Engine heat generation rate")]
        public float heatGenerationRate = 1f;
        
        [Tooltip("Engine efficiency (affects energy consumption)")]
        [Range(0.1f, 2f)]
        public float efficiency = 1f;
        
        [Header("Engine Performance")]
        [Tooltip("Maximum thrust output of the engine")]
        public float maxThrust = 100f;
        
        [Tooltip("How quickly the ship decelerates when reducing throttle")]
        public float deceleration = 20f;
        
        [Tooltip("How fast the ship can turn (degrees per second)")]
        public float turnRate = 2000f;
        
        [Tooltip("Maximum strafe speed for lateral movement")]
        public float strafeSpeed = 60f;
        
        [Tooltip("Duration of boost ability in seconds")]
        public float boostDuration = 2.8f;
        
        [Tooltip("Maximum speed the engine can achieve")]
        public float topSpeed = 250f;
        
        [Header("Boost Properties")]
        [Tooltip("Boost duration multiplier")]
        public float boostDurationMultiplier = 1f;
        
        [Tooltip("Boost speed multiplier")]
        public float boostSpeedMultiplier = 1f;
        
        [Tooltip("Boost energy consumption multiplier")]
        public float boostEnergyMultiplier = 1f;
        
        [Header("Maneuvering & Handling")]
        [Tooltip("Rate at which the ship can strafe and maneuver laterally")]
        public float maneuverRate = 35f;
        
        [Tooltip("Maximum visual bank angle in degrees")]
        public float maxBankAngle = 45f;
        
        [Tooltip("How quickly the ship banks into turns")]
        public float bankSmoothing = 8f;
        
        [Header("Class-Specific Bonuses")]
        [Tooltip("Bastion-specific engine bonus")]
        public float bastionBonus = 0f;
        
        [Tooltip("Breacher-specific engine bonus")]
        public float breacherBonus = 0f;
        
        [Tooltip("Razor-specific engine bonus")]
        public float razorBonus = 0f;
        
        [Tooltip("Haven-specific engine bonus")]
        public float havenBonus = 0f;
        
        public override void OnEquipped(ItemManager itemManager)
        {
            base.OnEquipped(itemManager);
            
            // Apply engine-specific effects
            ApplyEngineStatistics(itemManager);
        }
        
        public override void OnUnequipped(ItemManager itemManager)
        {
            base.OnUnequipped(itemManager);
            
            // Remove engine-specific effects
            // This could include stopping engine sounds, removing visual effects, etc.
        }
        
        public override void ApplyStatistics(ShipStatistics stats)
        {
            base.ApplyStatistics(stats);
            
            // Apply engine-specific statistics
            ApplyEngineSpecificStats(stats);
        }
        
        /// <summary>
        /// Apply engine-specific statistics
        /// </summary>
        private void ApplyEngineSpecificStats(ShipStatistics stats)
        {
            // Apply base engine stats directly
            stats.thrust = maxThrust;
            // stats.maxSpeed = topSpeed; // This will be calculated dynamically in ItemManager
            // stats.acceleration = acceleration; // This will also be calculated dynamically
            stats.deceleration = deceleration;
            stats.turnRate = turnRate;
            stats.strafeSpeed = strafeSpeed;
            stats.boostDuration = boostDuration;
            stats.mass = mass;
            stats.engineThrust = maxThrust;
            
            // Apply efficiency effects
            stats.thrusterEffectIntensity = engineEffectIntensity;
            stats.engineSoundProfile = engineSoundProfileID;
            
            // Apply maneuvering stats
            stats.maneuverRate = maneuverRate;
            stats.maxBankAngle = maxBankAngle;
            stats.bankSmoothing = bankSmoothing;
            
            // Apply boost properties
            stats.flightSpeed = topSpeed * 0.8f; // Default flight speed calculation
            stats.minSpeed = topSpeed * 0.15f;   // Default min speed calculation
        }
        
        /// <summary>
        /// Apply engine statistics to item manager
        /// </summary>
        private void ApplyEngineStatistics(ItemManager itemManager)
        {
            // Get ship class for class-specific bonuses
            var shipClass = itemManager.GetComponent<ShipManager>();
            if (shipClass != null)
            {
                float classBonus = 0f;
                
                switch (shipClass.shipType)
                {
                    case ShipManager.ShipType.Bastion:
                        classBonus = bastionBonus;
                        break;
                    case ShipManager.ShipType.Breacher:
                        classBonus = breacherBonus;
                        break;
                    case ShipManager.ShipType.Razor:
                        classBonus = razorBonus;
                        break;
                    case ShipManager.ShipType.Haven:
                        classBonus = havenBonus;
                        break;
                }
                
                // Apply class-specific bonus
                if (classBonus != 0f)
                {
                    AddStatModifier("thrust", classBonus, ModifierType.Additive);
                    AddStatModifier("maxspeed", classBonus * 0.1f, ModifierType.Additive);
                }
            }
        }
        
        /// <summary>
        /// Get engine performance rating
        /// </summary>
        public float GetPerformanceRating()
        {
            float rating = 0f;
            rating += maxThrust * 0.01f;
            rating += topSpeed * 0.1f;
            rating += deceleration * 2f;
            rating += turnRate * 0.01f;
            rating += efficiency * 25f;
            rating += boostDuration * 10f;
            rating += boostSpeedMultiplier * 20f;
            
            return Mathf.Round(rating);
        }
        
        /// <summary>
        /// Get engine efficiency rating
        /// </summary>
        public float GetEfficiencyRating()
        {
            return efficiency * 100f;
        }
        
        /// <summary>
        /// Get engine heat rating
        /// </summary>
        public float GetHeatRating()
        {
            return heatGenerationRate * 100f;
        }
        
        /// <summary>
        /// Check if engine is compatible with ship class
        /// </summary>
        public bool IsCompatibleWithShipClass(ShipManager.ShipType shipType)
        {
            switch (shipType)
            {
                case ShipManager.ShipType.Bastion:
                    return bastionCompatible && bastionBonus >= 0f;
                case ShipManager.ShipType.Breacher:
                    return breacherCompatible && breacherBonus >= 0f;
                case ShipManager.ShipType.Razor:
                    return razorCompatible && razorBonus >= 0f;
                case ShipManager.ShipType.Haven:
                    return havenCompatible && havenBonus >= 0f;
                default:
                    return true;
            }
        }
        
        /// <summary>
        /// Get engine description with performance details
        /// </summary>
        public string GetDetailedDescription()
        {
            string desc = description + "\n\n";
            desc += $"Performance Rating: {GetPerformanceRating()}\n";
            desc += $"Efficiency: {GetEfficiencyRating():F1}%\n";
            desc += $"Heat Generation: {GetHeatRating():F1}%\n";
            desc += $"Max Thrust: {maxThrust:F0}\n";
            desc += $"Deceleration: {deceleration:F1}\n";
            desc += $"Turn Rate: {turnRate:F0}°/s\n";
            desc += $"Strafe Speed: {strafeSpeed:F0}\n";
            desc += $"Boost Duration: {boostDuration:F1}s\n";
            desc += $"Mass: {mass:F0}\n";
            
            return desc;
        }
        
        /// <summary>
        /// Create engine equipment with default values
        /// </summary>
        public static EngineEquipment CreateDefaultEngine(string name, ShipManager.ShipType shipType)
        {
            var engine = CreateInstance<EngineEquipment>();
            engine.equipmentName = name;
            engine.equipmentType = EquipmentType.Engine;
            engine.category = ItemManager.EquipmentCategory.Core;
            
            // Set default values based on ship type
            switch (shipType)
            {
                case ShipManager.ShipType.Bastion:
                    engine.maxThrust = 75f;
                    engine.topSpeed = 200f;
                    engine.deceleration = 15f;
                    engine.turnRate = 1400f;
                    engine.strafeSpeed = 30f;
                    engine.boostDuration = 3.5f;
                    engine.mass = 490f;
                    engine.bastionBonus = 25f;
                    engine.maneuverRate = 25f;
                    engine.maxBankAngle = 40f;
                    break;
                    
                case ShipManager.ShipType.Breacher:
                    engine.maxThrust = 85f;
                    engine.topSpeed = 225f;
                    engine.deceleration = 18f;
                    engine.turnRate = 1600f;
                    engine.strafeSpeed = 42f;
                    engine.boostDuration = 2.8f;
                    engine.mass = 360f;
                    engine.breacherBonus = 15f;
                    engine.maneuverRate = 38f;
                    engine.maxBankAngle = 50f;
                    break;
                    
                case ShipManager.ShipType.Razor:
                    engine.maxThrust = 95f;
                    engine.topSpeed = 300f;
                    engine.deceleration = 22f;
                    engine.turnRate = 2200f;
                    engine.strafeSpeed = 60f;
                    engine.boostDuration = 2.2f;
                    engine.mass = 220f;
                    engine.razorBonus = 20f;
                    engine.maneuverRate = 45f;
                    engine.maxBankAngle = 60f;
                    break;
                    
                case ShipManager.ShipType.Haven:
                    engine.maxThrust = 80f;
                    engine.topSpeed = 225f;
                    engine.deceleration = 16f;
                    engine.turnRate = 1600f;
                    engine.strafeSpeed = 48f;
                    engine.boostDuration = 2.8f;
                    engine.mass = 250f;
                    engine.havenBonus = 10f;
                    engine.maneuverRate = 35f;
                    engine.maxBankAngle = 45f;
                    break;
            }
            
            return engine;
        }
        
        /// <summary>
        /// Clone engine equipment
        /// </summary>
        public new EngineEquipment Clone()
        {
            var clone = (EngineEquipment)base.Clone();
            clone.engineSoundProfileID = this.engineSoundProfileID;
            clone.engineEffectIntensity = this.engineEffectIntensity;
            clone.heatGenerationRate = this.heatGenerationRate;
            clone.efficiency = this.efficiency;
            clone.maxThrust = this.maxThrust;
            clone.deceleration = this.deceleration;
            clone.turnRate = this.turnRate;
            clone.strafeSpeed = this.strafeSpeed;
            clone.boostDuration = this.boostDuration;
            clone.topSpeed = this.topSpeed;
            clone.boostSpeedMultiplier = this.boostSpeedMultiplier;
            clone.boostEnergyMultiplier = this.boostEnergyMultiplier;
            clone.bastionBonus = this.bastionBonus;
            clone.breacherBonus = this.breacherBonus;
            clone.razorBonus = this.razorBonus;
            clone.havenBonus = this.havenBonus;
            clone.maneuverRate = this.maneuverRate;
            clone.maxBankAngle = this.maxBankAngle;
            clone.bankSmoothing = this.bankSmoothing;
            
            // Copy stat modifiers
            foreach (var modifier in GetStatModifiers())
            {
                clone.AddStatModifier(modifier.statName, modifier.value, modifier.modifierType);
            }
            
            return clone;
        }
    }
} 