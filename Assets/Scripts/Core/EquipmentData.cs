using UnityEngine;
using System.Collections.Generic;
using DomeClash.Core;
using DomeClash.Ships;

namespace DomeClash.Core
{
    /// <summary>
    /// Base ScriptableObject for all equipment items
    /// Defines common properties and behavior for equipment
    /// </summary>
    [CreateAssetMenu(fileName = "New Equipment", menuName = "DomeClash/Equipment/Base Equipment")]
    public class EquipmentData : ScriptableObject
    {
        [Header("Equipment Identity")]
        public string equipmentName = "New Equipment";
        [TextArea(3, 5)]
        public string description = "Equipment description";
        public Sprite icon;
        
        [Header("Equipment Type")]
        public EquipmentType equipmentType = EquipmentType.Core;
        public ItemManager.EquipmentCategory category = ItemManager.EquipmentCategory.Core;
        
        [Header("Base Properties")]
        public float mass = 0f;
        public float cost = 0f;
        public int rarity = 0; // 0 = Common, 1 = Uncommon, 2 = Rare, 3 = Epic, 4 = Legendary
        
        [Header("Class Compatibility")]
        public bool bastionCompatible = true;
        public bool breacherCompatible = true;
        public bool razorCompatible = true;
        public bool havenCompatible = true;
        
        [Header("Stat Modifiers")]
        [SerializeField] private List<StatModifier> statModifiers = new List<StatModifier>();
        
        [Header("Visual Effects")]
        public GameObject visualEffectPrefab;
        public AudioClip equipSound;
        public AudioClip unequipSound;
        
        // Equipment types
        public enum EquipmentType
        {
            Core,
            Armor,
            Engine,
            Radar,
            Weapon,
            Module,
            Utility
        }
        
        // Stat modifier types
        public enum ModifierType
        {
            Additive,       // Direct addition
            Multiplicative, // Percentage increase
            Override        // Replace value
        }
        
        /// <summary>
        /// Called when equipment is equipped
        /// </summary>
        public virtual void OnEquipped(ItemManager itemManager)
        {
            // Override in derived classes for specific behavior
        }
        
        /// <summary>
        /// Called when equipment is unequipped
        /// </summary>
        public virtual void OnUnequipped(ItemManager itemManager)
        {
            // Override in derived classes for specific behavior
        }
        
        /// <summary>
        /// Apply statistics to ship statistics
        /// </summary>
        public virtual void ApplyStatistics(ShipStatistics stats)
        {
            foreach (var modifier in statModifiers)
            {
                ApplyStatModifier(stats, modifier);
            }
        }
        
        /// <summary>
        /// Apply individual stat modifier
        /// </summary>
        protected virtual void ApplyStatModifier(ShipStatistics stats, StatModifier modifier)
        {
            switch (modifier.statName.ToLower())
            {
                case "mass":
                    ApplyModifier(ref stats.mass, modifier);
                    break;
                case "thrust":
                    ApplyModifier(ref stats.thrust, modifier);
                    break;
                case "maxspeed":
                    ApplyModifier(ref stats.maxSpeed, modifier);
                    break;
                case "acceleration":
                    ApplyModifier(ref stats.acceleration, modifier);
                    break;
                case "deceleration":
                    ApplyModifier(ref stats.deceleration, modifier);
                    break;
                case "turnrate":
                    ApplyModifier(ref stats.turnRate, modifier);
                    break;
                case "strafespeed":
                    ApplyModifier(ref stats.strafeSpeed, modifier);
                    break;
                case "boostduration":
                    ApplyModifier(ref stats.boostDuration, modifier);
                    break;
                case "enginethrust":
                    ApplyModifier(ref stats.engineThrust, modifier);
                    break;
                case "maneuverrate":
                    ApplyModifier(ref stats.maneuverRate, modifier);
                    break;
                case "strafethrust":
                    ApplyModifier(ref stats.strafeThrust, modifier);
                    break;
                case "flightspeed":
                    ApplyModifier(ref stats.flightSpeed, modifier);
                    break;
                case "minspeed":
                    ApplyModifier(ref stats.minSpeed, modifier);
                    break;
                case "speedsmoothing":
                    ApplyModifier(ref stats.speedSmoothing, modifier);
                    break;
                case "bankingamount":
                    ApplyModifier(ref stats.bankingAmount, modifier);
                    break;
                case "maxbankangle":
                    ApplyModifier(ref stats.maxBankAngle, modifier);
                    break;
                case "banksmoothing":
                    ApplyModifier(ref stats.bankSmoothing, modifier);
                    break;
                case "autolevelrate":
                    ApplyModifier(ref stats.autoLevelRate, modifier);
                    break;
                case "speedbankingmultiplier":
                    ApplyModifier(ref stats.speedBankingMultiplier, modifier);
                    break;
                case "mousepositionbankingsensitivity":
                    ApplyModifier(ref stats.mousePositionBankingSensitivity, modifier);
                    break;
                case "stallthreshold":
                    ApplyModifier(ref stats.stallThreshold, modifier);
                    break;
                case "enginesoundprofile":
                    ApplyModifier(ref stats.engineSoundProfile, modifier);
                    break;
                case "thrustereffectintensity":
                    ApplyModifier(ref stats.thrusterEffectIntensity, modifier);
                    break;
                case "retrothrust":
                    ApplyModifier(ref stats.retroThrust, modifier);
                    break;
            }
        }
        
        /// <summary>
        /// Apply modifier to a float value
        /// </summary>
        private void ApplyModifier(ref float value, StatModifier modifier)
        {
            switch (modifier.modifierType)
            {
                case ModifierType.Additive:
                    value += modifier.value;
                    break;
                case ModifierType.Multiplicative:
                    value *= (1f + modifier.value);
                    break;
                case ModifierType.Override:
                    value = modifier.value;
                    break;
            }
        }
        
        /// <summary>
        /// Check if equipment has specific stat modifier
        /// </summary>
        public bool HasStatModifier(string statName)
        {
            return statModifiers.Exists(m => m.statName.ToLower() == statName.ToLower());
        }
        
        /// <summary>
        /// Get stat modifier value
        /// </summary>
        public float GetStatModifier(string statName)
        {
            var modifier = statModifiers.Find(m => m.statName.ToLower() == statName.ToLower());
            return modifier != null ? modifier.value : 0f;
        }
        
        /// <summary>
        /// Add stat modifier
        /// </summary>
        public void AddStatModifier(string statName, float value, ModifierType modifierType = ModifierType.Additive)
        {
            var existingModifier = statModifiers.Find(m => m.statName.ToLower() == statName.ToLower());
            
            if (existingModifier != null)
            {
                existingModifier.value = value;
                existingModifier.modifierType = modifierType;
            }
            else
            {
                statModifiers.Add(new StatModifier
                {
                    statName = statName,
                    value = value,
                    modifierType = modifierType
                });
            }
        }
        
        /// <summary>
        /// Remove stat modifier
        /// </summary>
        public void RemoveStatModifier(string statName)
        {
            statModifiers.RemoveAll(m => m.statName.ToLower() == statName.ToLower());
        }
        
        /// <summary>
        /// Clear all stat modifiers
        /// </summary>
        public void ClearStatModifiers()
        {
            statModifiers.Clear();
        }
        
        /// <summary>
        /// Get all stat modifiers
        /// </summary>
        public List<StatModifier> GetStatModifiers()
        {
            return new List<StatModifier>(statModifiers);
        }
        
        /// <summary>
        /// Check if equipment is compatible with a given ship type
        /// </summary>
        public bool IsCompatibleWithShip(ShipManager.ShipType shipType)
        {
            // Based on public compatibility flags
            switch (shipType)
            {
                case ShipManager.ShipType.Bastion:
                    return bastionCompatible;
                case ShipManager.ShipType.Breacher:
                    return breacherCompatible;
                case ShipManager.ShipType.Razor:
                    return razorCompatible;
                case ShipManager.ShipType.Haven:
                    return havenCompatible;
                default:
                    return true; // Compatible with Default/unspecified types
            }
        }
        
        /// <summary>
        /// Get rarity name
        /// </summary>
        public string GetRarityName()
        {
            switch (rarity)
            {
                case 0: return "Common";
                case 1: return "Uncommon";
                case 2: return "Rare";
                case 3: return "Epic";
                case 4: return "Legendary";
                default: return "Unknown";
            }
        }
        
        /// <summary>
        /// Get rarity color
        /// </summary>
        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case 0: return Color.white;      // Common
                case 1: return Color.green;      // Uncommon
                case 2: return Color.blue;       // Rare
                case 3: return Color.magenta;    // Epic
                case 4: return Color.yellow;     // Legendary
                default: return Color.gray;
            }
        }
        
        /// <summary>
        /// Create equipment instance
        /// </summary>
        public static EquipmentData CreateEquipment(string name, EquipmentType type, ItemManager.EquipmentCategory category)
        {
            var equipment = CreateInstance<EquipmentData>();
            equipment.equipmentName = name;
            equipment.equipmentType = type;
            equipment.category = category;
            return equipment;
        }
        
        /// <summary>
        /// Clone equipment
        /// </summary>
        public EquipmentData Clone()
        {
            var clone = CreateInstance<EquipmentData>();
            clone.equipmentName = this.equipmentName;
            clone.description = this.description;
            clone.icon = this.icon;
            clone.equipmentType = this.equipmentType;
            clone.category = this.category;
            clone.mass = this.mass;
            clone.cost = this.cost;
            clone.rarity = this.rarity;
            clone.bastionCompatible = this.bastionCompatible;
            clone.breacherCompatible = this.breacherCompatible;
            clone.razorCompatible = this.razorCompatible;
            clone.havenCompatible = this.havenCompatible;
            clone.visualEffectPrefab = this.visualEffectPrefab;
            clone.equipSound = this.equipSound;
            clone.unequipSound = this.unequipSound;
            
            // Clone stat modifiers
            foreach (var modifier in statModifiers)
            {
                clone.statModifiers.Add(new StatModifier
                {
                    statName = modifier.statName,
                    value = modifier.value,
                    modifierType = modifier.modifierType
                });
            }
            
            return clone;
        }
    }
    
    /// <summary>
    /// Stat modifier structure
    /// </summary>
    [System.Serializable]
    public class StatModifier
    {
        public string statName = "";
        public float value = 0f;
        public EquipmentData.ModifierType modifierType = EquipmentData.ModifierType.Additive;
        
        public StatModifier() { }
        
        public StatModifier(string statName, float value, EquipmentData.ModifierType modifierType = EquipmentData.ModifierType.Additive)
        {
            this.statName = statName;
            this.value = value;
            this.modifierType = modifierType;
        }
    }
} 