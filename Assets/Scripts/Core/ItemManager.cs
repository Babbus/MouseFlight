using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DomeClash.Ships;

namespace DomeClash.Core
{
    /// <summary>
    /// Item Manager - Central equipment management system for ships
    /// Manages all 8 equipment slots and provides statistics to other systems
    /// Replaces FlightProfile system with dynamic equipment-based stats
    /// </summary>
    public class ItemManager : MonoBehaviour
    {
        [Header("Equipment Slots")]
        [SerializeField] private EquipmentSlot[] equipmentSlots = new EquipmentSlot[8];
        
        [Header("Cached Statistics")]
        [SerializeField] private ShipStatistics cachedStats;
        [SerializeField] private bool statsDirty = true;
        
        [Header("References")]
        [SerializeField] private PrototypeShip shipClass;
        [SerializeField] private ShipFlightController flightController;
        
        // Events for UI and other systems
        public System.Action<EquipmentSlot, EquipmentData> OnEquipmentChanged;
        public System.Action<ShipStatistics> OnStatisticsUpdated;
        
        // Equipment slot types
        public enum SlotType
        {
            Armor = 0,
            Engine = 1,
            Radar = 2,
            PrimaryWeapon = 3,
            SecondaryWeapon = 4,
            Utility = 5,
            OffensiveModule = 6,
            DefensiveModule = 7
        }
        
        // Equipment categories
        public enum EquipmentCategory
        {
            Core,       // Armor, Engine, Radar
            Weapon,     // Primary, Secondary
            Module      // Utility, Offensive, Defensive
        }
        
        private void Awake()
        {
            InitializeSlots();
            FindReferences();
        }
        
        private void Start()
        {
            // Initialize with default equipment if slots are empty
            InitializeDefaultEquipment();
            
            // Calculate initial statistics
            RecalculateStatistics();
        }
        
        private void InitializeSlots()
        {
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                if (equipmentSlots[i] == null)
                {
                    equipmentSlots[i] = new EquipmentSlot();
                    equipmentSlots[i].slotType = (SlotType)i;
                }
            }
        }
        
        private void FindReferences()
        {
            if (shipClass == null)
                shipClass = GetComponent<PrototypeShip>();
                
            if (flightController == null)
                flightController = GetComponent<ShipFlightController>();
        }
        
        private void InitializeDefaultEquipment()
        {
            // Initialize with default equipment based on ship class
            if (shipClass != null)
            {
                // This will be implemented when equipment ScriptableObjects are created
                // For now, slots remain empty
            }
        }
        
        /// <summary>
        /// Get equipment from specific slot
        /// </summary>
        public EquipmentData GetEquipment(SlotType slotType)
        {
            int slotIndex = (int)slotType;
            if (slotIndex >= 0 && slotIndex < equipmentSlots.Length)
            {
                return equipmentSlots[slotIndex].equipment;
            }
            return null;
        }
        
        /// <summary>
        /// Set equipment to specific slot
        /// </summary>
        public bool SetEquipment(SlotType slotType, EquipmentData equipment)
        {
            int slotIndex = (int)slotType;
            if (slotIndex >= 0 && slotIndex < equipmentSlots.Length)
            {
                // Validate equipment compatibility
                if (equipment != null && !IsEquipmentCompatible(slotType, equipment))
                {
                    Debug.LogWarning($"Equipment {equipment.equipmentName} is not compatible with slot {slotType}");
                    return false;
                }
                
                // Remove old equipment
                EquipmentData oldEquipment = equipmentSlots[slotIndex].equipment;
                if (oldEquipment != null)
                {
                    oldEquipment.OnUnequipped(this);
                }
                
                // Set new equipment
                equipmentSlots[slotIndex].equipment = equipment;
                
                // Initialize new equipment
                if (equipment != null)
                {
                    equipment.OnEquipped(this);
                }
                
                // Mark statistics as dirty
                statsDirty = true;
                
                // Notify systems
                OnEquipmentChanged?.Invoke(equipmentSlots[slotIndex], equipment);
                
                // Recalculate statistics
                RecalculateStatistics();
                
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Check if equipment is compatible with slot
        /// </summary>
        private bool IsEquipmentCompatible(SlotType slotType, EquipmentData equipment)
        {
            if (equipment == null) return false;
            
            switch (slotType)
            {
                case SlotType.Armor:
                    return equipment.category == EquipmentCategory.Core && equipment.equipmentType == EquipmentData.EquipmentType.Armor;
                case SlotType.Engine:
                    return equipment.category == EquipmentCategory.Core && equipment.equipmentType == EquipmentData.EquipmentType.Engine;
                case SlotType.Radar:
                    return equipment.category == EquipmentCategory.Core && equipment.equipmentType == EquipmentData.EquipmentType.Radar;
                case SlotType.PrimaryWeapon:
                case SlotType.SecondaryWeapon:
                    return equipment.category == EquipmentCategory.Weapon;
                case SlotType.Utility:
                case SlotType.OffensiveModule:
                case SlotType.DefensiveModule:
                    return equipment.category == EquipmentCategory.Module;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Get all equipment of specific category
        /// </summary>
        public List<EquipmentData> GetEquipmentByCategory(EquipmentCategory category)
        {
            List<EquipmentData> result = new List<EquipmentData>();
            
            foreach (var slot in equipmentSlots)
            {
                if (slot.equipment != null && slot.equipment.category == category)
                {
                    result.Add(slot.equipment);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Get cached ship statistics
        /// </summary>
        public ShipStatistics GetShipStatistics()
        {
            if (statsDirty)
            {
                RecalculateStatistics();
            }
            return cachedStats;
        }
        
        /// <summary>
        /// Force recalculation of statistics
        /// </summary>
        public void RecalculateStatistics()
        {
            ShipStatistics newStats = new ShipStatistics();
            
            // Calculate statistics from all equipped items
            foreach (var slot in equipmentSlots)
            {
                if (slot.equipment != null)
                {
                    slot.equipment.ApplyStatistics(newStats);
                }
            }
            
            // Apply ship class base statistics
            if (shipClass != null && shipClass.stats != null)
            {
                ApplyShipClassBaseStats(newStats);
            }
            
            cachedStats = newStats;
            statsDirty = false;
            
            // Notify systems of statistics update
            OnStatisticsUpdated?.Invoke(cachedStats);
            
            // Update flight controller if available
            if (flightController != null)
            {
                flightController.UpdateFromItemManager(cachedStats);
            }
        }
        
        /// <summary>
        /// Apply ship class base statistics
        /// </summary>
        private void ApplyShipClassBaseStats(ShipStatistics stats)
        {
            // Apply base stats from ship class
            stats.mass += shipClass.stats.mass;
            stats.thrust += shipClass.stats.thrust;
            stats.maxSpeed += shipClass.stats.maxSpeed;
            stats.acceleration += shipClass.stats.acceleration;
            stats.deceleration += shipClass.stats.deceleration;
            stats.turnRate += shipClass.stats.turnRate;
            stats.strafeSpeed += shipClass.stats.strafeSpeed;
            stats.boostDuration += shipClass.stats.boostDuration;
            stats.engineThrust += shipClass.stats.engineThrust;
            stats.maneuverRate += shipClass.stats.maneuverRate;
            stats.strafeThrust += shipClass.stats.strafeThrust;
        }
        
        /// <summary>
        /// Get equipment slot information
        /// </summary>
        public EquipmentSlot GetSlot(SlotType slotType)
        {
            int slotIndex = (int)slotType;
            if (slotIndex >= 0 && slotIndex < equipmentSlots.Length)
            {
                return equipmentSlots[slotIndex];
            }
            return null;
        }
        
        /// <summary>
        /// Get all equipment slots
        /// </summary>
        public EquipmentSlot[] GetAllSlots()
        {
            return equipmentSlots;
        }
        
        /// <summary>
        /// Check if slot is occupied
        /// </summary>
        public bool IsSlotOccupied(SlotType slotType)
        {
            EquipmentSlot slot = GetSlot(slotType);
            return slot != null && slot.equipment != null;
        }
        
        /// <summary>
        /// Remove equipment from slot
        /// </summary>
        public bool RemoveEquipment(SlotType slotType)
        {
            return SetEquipment(slotType, null);
        }
        
        /// <summary>
        /// Get total mass from all equipment
        /// </summary>
        public float GetTotalMass()
        {
            float totalMass = 0f;
            
            foreach (var slot in equipmentSlots)
            {
                if (slot.equipment != null)
                {
                    totalMass += slot.equipment.mass;
                }
            }
            
            return totalMass;
        }
        
        /// <summary>
        /// Get equipment by name
        /// </summary>
        public EquipmentData GetEquipmentByName(string equipmentName)
        {
            foreach (var slot in equipmentSlots)
            {
                if (slot.equipment != null && slot.equipment.equipmentName == equipmentName)
                {
                    return slot.equipment;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Get slot containing specific equipment
        /// </summary>
        public SlotType? GetSlotForEquipment(EquipmentData equipment)
        {
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                if (equipmentSlots[i].equipment == equipment)
                {
                    return (SlotType)i;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Validate all equipment compatibility
        /// </summary>
        public bool ValidateEquipmentCompatibility()
        {
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                var slot = equipmentSlots[i];
                if (slot.equipment != null && !IsEquipmentCompatible((SlotType)i, slot.equipment))
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Clear all equipment
        /// </summary>
        public void ClearAllEquipment()
        {
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                SetEquipment((SlotType)i, null);
            }
        }
        
        /// <summary>
        /// Get equipment count by category
        /// </summary>
        public int GetEquipmentCount(EquipmentCategory category)
        {
            return GetEquipmentByCategory(category).Count;
        }
        
        /// <summary>
        /// Check if any equipment provides specific stat
        /// </summary>
        public bool HasStatModifier(string statName)
        {
            foreach (var slot in equipmentSlots)
            {
                if (slot.equipment != null && slot.equipment.HasStatModifier(statName))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Get total stat modifier value
        /// </summary>
        public float GetTotalStatModifier(string statName)
        {
            float total = 0f;
            
            foreach (var slot in equipmentSlots)
            {
                if (slot.equipment != null)
                {
                    total += slot.equipment.GetStatModifier(statName);
                }
            }
            
            return total;
        }
        
        /// <summary>
        /// Mark statistics as dirty (force recalculation)
        /// </summary>
        public void MarkStatsDirty()
        {
            statsDirty = true;
        }
        
        /// <summary>
        /// Get equipment slots as dictionary for easy access
        /// </summary>
        public Dictionary<SlotType, EquipmentSlot> GetSlotsDictionary()
        {
            Dictionary<SlotType, EquipmentSlot> slots = new Dictionary<SlotType, EquipmentSlot>();
            
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                slots.Add((SlotType)i, equipmentSlots[i]);
            }
            
            return slots;
        }
        
        /// <summary>
        /// Get equipment slots info for UI
        /// </summary>
        public EquipmentSlotInfo[] GetSlotsInfo()
        {
            EquipmentSlotInfo[] slotsInfo = new EquipmentSlotInfo[equipmentSlots.Length];
            
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                slotsInfo[i] = new EquipmentSlotInfo
                {
                    slotType = (SlotType)i,
                    isOccupied = equipmentSlots[i].equipment != null,
                    equipmentName = equipmentSlots[i].equipment?.equipmentName ?? "",
                    equipmentIcon = equipmentSlots[i].equipment?.icon,
                    category = equipmentSlots[i].equipment?.category ?? EquipmentCategory.Core
                };
            }
            
            return slotsInfo;
        }
    }
    
    /// <summary>
    /// Equipment slot data structure
    /// </summary>
    [System.Serializable]
    public class EquipmentSlot
    {
        public ItemManager.SlotType slotType;
        public EquipmentData equipment;
        
        public bool IsOccupied => equipment != null;
        public string EquipmentName => equipment?.equipmentName ?? "";
    }
    
    /// <summary>
    /// Equipment slot information for UI
    /// </summary>
    [System.Serializable]
    public class EquipmentSlotInfo
    {
        public ItemManager.SlotType slotType;
        public bool isOccupied;
        public string equipmentName;
        public Sprite equipmentIcon;
        public ItemManager.EquipmentCategory category;
    }
    
    /// <summary>
    /// Ship statistics structure - replaces FlightProfile
    /// </summary>
    [System.Serializable]
    public class ShipStatistics
    {
        [Header("Core Stats")]
        public float mass = 0f;
        public float thrust = 0f;
        public float maxSpeed = 0f;
        public float acceleration = 0f;
        public float deceleration = 0f;
        public float turnRate = 0f;
        public float strafeSpeed = 0f;
        
        [Header("Legacy & Other")]
        public float boostDuration = 0f;
        public float engineThrust = 0f;
        public float maneuverRate = 0f;
        public float strafeThrust = 0f;
        
        [Header("Flight Profile Compatible")]
        public float flightSpeed = 0f;
        public float minSpeed = 0f;
        public float speedSmoothing = 12f;
        public float bankingAmount = 45f;
        public float maxBankAngle = 60f;
        public float bankSmoothing = 8f;
        public float autoLevelRate = 4f;
        public float speedBankingMultiplier = 1.0f;
        public float mousePositionBankingSensitivity = 0.6f;
        public float stallThreshold = 25f;
        public float engineSoundProfile = 0f;
        public float thrusterEffectIntensity = 1f;
        public float retroThrust = 60f;
        
        /// <summary>
        /// Reset all statistics to zero
        /// </summary>
        public void Reset()
        {
            mass = 0f;
            thrust = 0f;
            maxSpeed = 0f;
            acceleration = 0f;
            deceleration = 0f;
            turnRate = 0f;
            strafeSpeed = 0f;
            boostDuration = 0f;
            engineThrust = 0f;
            maneuverRate = 0f;
            strafeThrust = 0f;
            flightSpeed = 0f;
            minSpeed = 0f;
            speedSmoothing = 12f;
            bankingAmount = 45f;
            maxBankAngle = 60f;
            bankSmoothing = 8f;
            autoLevelRate = 4f;
            speedBankingMultiplier = 1.0f;
            mousePositionBankingSensitivity = 0.6f;
            stallThreshold = 25f;
            engineSoundProfile = 0f;
            thrusterEffectIntensity = 1f;
            retroThrust = 60f;
        }
        
        /// <summary>
        /// Add statistics from another source
        /// </summary>
        public void Add(ShipStatistics other)
        {
            mass += other.mass;
            thrust += other.thrust;
            maxSpeed += other.maxSpeed;
            acceleration += other.acceleration;
            deceleration += other.deceleration;
            turnRate += other.turnRate;
            strafeSpeed += other.strafeSpeed;
            boostDuration += other.boostDuration;
            engineThrust += other.engineThrust;
            maneuverRate += other.maneuverRate;
            strafeThrust += other.strafeThrust;
            flightSpeed += other.flightSpeed;
            minSpeed += other.minSpeed;
            speedSmoothing += other.speedSmoothing;
            bankingAmount += other.bankingAmount;
            maxBankAngle += other.maxBankAngle;
            bankSmoothing += other.bankSmoothing;
            autoLevelRate += other.autoLevelRate;
            speedBankingMultiplier += other.speedBankingMultiplier;
            mousePositionBankingSensitivity += other.mousePositionBankingSensitivity;
            stallThreshold += other.stallThreshold;
            engineSoundProfile += other.engineSoundProfile;
            thrusterEffectIntensity += other.thrusterEffectIntensity;
            retroThrust += other.retroThrust;
        }
        
        /// <summary>
        /// Multiply statistics by a factor
        /// </summary>
        public void Multiply(float factor)
        {
            mass *= factor;
            thrust *= factor;
            maxSpeed *= factor;
            acceleration *= factor;
            deceleration *= factor;
            turnRate *= factor;
            strafeSpeed *= factor;
            boostDuration *= factor;
            engineThrust *= factor;
            maneuverRate *= factor;
            strafeThrust *= factor;
            flightSpeed *= factor;
            minSpeed *= factor;
            speedSmoothing *= factor;
            bankingAmount *= factor;
            maxBankAngle *= factor;
            bankSmoothing *= factor;
            autoLevelRate *= factor;
            speedBankingMultiplier *= factor;
            mousePositionBankingSensitivity *= factor;
            stallThreshold *= factor;
            engineSoundProfile *= factor;
            thrusterEffectIntensity *= factor;
            retroThrust *= factor;
        }
    }
} 