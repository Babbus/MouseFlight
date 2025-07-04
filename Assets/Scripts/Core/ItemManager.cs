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
    [System.Serializable]
    public class ItemManager : MonoBehaviour
    {
        [Header("Equipment Slots")]
        [SerializeField] private EquipmentSlot[] equipmentSlots = new EquipmentSlot[System.Enum.GetValues(typeof(SlotType)).Length];
        
        [Header("Cached Statistics")]
        [SerializeField] private ShipStatistics cachedStats;
        [SerializeField] private bool statsDirty = true;
        
        [Header("References")]
        [SerializeField] private ShipFlightController flightController;
        [SerializeField] private ShipManager shipClass;
        
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
            FindReferences();
        }
        
        private void Start()
        {
            // Equip any items pre-assigned in the inspector slots
            InitializeDefaultEquipment();
            
            // Initial stat calculation
            RecalculateStatistics();
        }
        
        /// <summary>
        /// Finds and assigns references to other components on the ship.
        /// </summary>
        private void FindReferences()
        {
            if (flightController == null) flightController = GetComponent<ShipFlightController>();
            if (shipClass == null) shipClass = GetComponent<ShipManager>();
        }
        
        /// <summary>
        /// Loops through slots and formally equips any items assigned in the editor.
        /// </summary>
        private void InitializeDefaultEquipment()
        {
            // Create a temporary copy of the initial setup from the inspector.
            // This is crucial to avoid modifying the list while iterating over it.
            var initialSetup = new Dictionary<SlotType, EquipmentData>();
            foreach (var slot in equipmentSlots)
            {
                if (slot.equipment != null)
                {
                    initialSetup[slot.slotType] = slot.equipment;
                }
            }

            // Clear the runtime equipment slots to ensure a clean state before equipping.
            foreach (var slot in equipmentSlots)
            {
                slot.equipment = null;
            }

            // Now, iterate through the clean copy and equip each item.
            foreach (var entry in initialSetup)
            {
                SetEquipment(entry.Key, entry.Value);
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
            EquipmentSlot slot = GetSlot(slotType);
            if (slot == null)
            {
                Debug.LogError($"ItemManager: Slot of type {slotType} not found!");
                return false;
            }

            // Check for compatibility before equipping
            if (equipment != null && !IsEquipmentCompatible(slotType, equipment))
            {
                Debug.LogWarning($"Cannot equip {equipment.equipmentName}: not compatible with this ship or slot type.");
                return false;
            }

            // Unequip the old item first, if any
            if (slot.equipment != null)
            {
                Debug.Log($"ItemManager: Unequipping '{slot.equipment.equipmentName}' from {slotType} slot.");
                slot.equipment.OnUnequipped(this);
            }

            slot.equipment = equipment;

            // Equip the new item
            if (slot.equipment != null)
            {
                Debug.Log($"ItemManager: Equipping '{slot.equipment.equipmentName}' into {slotType} slot.");
                slot.equipment.OnEquipped(this);
            }
            
            MarkStatsDirty();
            OnEquipmentChanged?.Invoke(slot, equipment); // Fire event
            return true;
        }
        
        /// <summary>
        /// Check if equipment is compatible with slot
        /// </summary>
        /// <param name="equipment">The equipment to check.</param>
        /// <returns>True if the equipment is compatible.</returns>
        private bool IsEquipmentCompatible(SlotType slotType, EquipmentData equipment)
        {
            // 1. Check if the item's EquipmentType is allowed in the target SlotType.
            bool typeMatch = false;
            switch (slotType)
            {
                case SlotType.Armor:
                    typeMatch = (equipment.equipmentType == EquipmentData.EquipmentType.Armor);
                    break;
                case SlotType.Engine:
                    typeMatch = (equipment.equipmentType == EquipmentData.EquipmentType.Engine);
                    break;
                case SlotType.Radar:
                    typeMatch = (equipment.equipmentType == EquipmentData.EquipmentType.Radar);
                    break;
                case SlotType.PrimaryWeapon:
                case SlotType.SecondaryWeapon:
                    typeMatch = (equipment.equipmentType == EquipmentData.EquipmentType.Weapon);
                    break;
                case SlotType.Utility:
                case SlotType.OffensiveModule:
                case SlotType.DefensiveModule:
                    typeMatch = (equipment.equipmentType == EquipmentData.EquipmentType.Module || equipment.equipmentType == EquipmentData.EquipmentType.Utility);
                    break;
            }

            if (!typeMatch)
            {
                Debug.LogWarning($"Compatibility Fail: Item '{equipment.equipmentName}' of type '{equipment.equipmentType}' cannot go in a '{slotType}' slot.");
                return false;
            }

            // 2. Check if the equipment is compatible with the ship's class (e.g. Bastion, Razor).
            if (shipClass != null)
            {
                if (!equipment.IsCompatibleWithShip(shipClass.shipType))
                {
                    Debug.LogWarning($"Compatibility Fail: Item '{equipment.equipmentName}' is not compatible with ship type '{shipClass.shipType}'.");
                    return false;
                }
            }

            return true; // If all checks pass, it's compatible.
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
            if (shipClass == null || !statsDirty)
            {
                return;
            }

            cachedStats.Reset();
            
            // Apply stats from all equipped items
            foreach (var slot in equipmentSlots)
            {
                if (slot.equipment != null)
                {
                    ApplyEquipmentStatModifiers(slot.equipment, cachedStats);
                }
            }

            statsDirty = false;
            
            // Notify other systems that stats have been updated
            OnStatisticsUpdated?.Invoke(cachedStats);
        }
        
        /// <summary>
        /// Applies stat modifiers from a single piece of equipment
        /// </summary>
        private void ApplyEquipmentStatModifiers(EquipmentData equipment, ShipStatistics stats)
        {
            equipment.ApplyStatistics(stats);
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