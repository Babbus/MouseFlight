using UnityEngine;
using DomeClash.Core;
using DomeClash.Ships;

namespace DomeClash.Core
{
    /// <summary>
    /// Test script to demonstrate ItemManager functionality
    /// This script shows how to create and equip engine equipment
    /// </summary>
    public class ItemManagerTest : MonoBehaviour
    {
        [Header("Test Equipment")]
        [SerializeField] private EngineEquipment testEngine;
        
        [Header("References")]
        [SerializeField] private PrototypeShip ship;
        [SerializeField] private ItemManager itemManager;
        
        [Header("Test Controls")]
        [SerializeField] private bool createTestEngine = false;
        [SerializeField] private bool equipTestEngine = false;
        [SerializeField] private bool removeEngine = false;
        [SerializeField] private bool printStats = false;
        
        private void Start()
        {
            FindReferences();
        }
        
        private void Update()
        {
            HandleTestInputs();
        }
        
        private void FindReferences()
        {
            if (ship == null)
                ship = FindFirstObjectByType<PrototypeShip>();
                
            if (itemManager == null && ship != null)
                itemManager = ship.GetComponent<ItemManager>();
        }
        
        private void HandleTestInputs()
        {
            if (createTestEngine)
            {
                createTestEngine = false;
                CreateTestEngine();
            }
            
            if (equipTestEngine)
            {
                equipTestEngine = false;
                EquipTestEngine();
            }
            
            if (removeEngine)
            {
                removeEngine = false;
                RemoveEngine();
            }
            
            if (printStats)
            {
                printStats = false;
                PrintCurrentStats();
            }
        }
        
        private void CreateTestEngine()
        {
            if (testEngine != null) return;
            
            // Create a test engine for Bastion
            testEngine = EngineEquipment.CreateDefaultEngine("Test Bastion Engine", PrototypeShip.ShipType.Bastion);
            
            // Add some custom stat modifiers
            testEngine.AddStatModifier("thrust", 50f, EquipmentData.ModifierType.Additive);
            testEngine.AddStatModifier("maxspeed", 0.2f, EquipmentData.ModifierType.Multiplicative);
            testEngine.AddStatModifier("acceleration", 0.3f, EquipmentData.ModifierType.Multiplicative);
            
            Debug.Log($"Created test engine: {testEngine.equipmentName}");
            Debug.Log($"Performance Rating: {testEngine.GetPerformanceRating()}");
            Debug.Log($"Efficiency: {testEngine.GetEfficiencyRating():F1}%");
        }
        
        private void EquipTestEngine()
        {
            if (itemManager == null || testEngine == null)
            {
                Debug.LogError("ItemManager or test engine not found!");
                return;
            }
            
            bool success = itemManager.SetEquipment(ItemManager.SlotType.Engine, testEngine);
            
            if (success)
            {
                Debug.Log($"Successfully equipped {testEngine.equipmentName}");
                PrintCurrentStats();
            }
            else
            {
                Debug.LogError($"Failed to equip {testEngine.equipmentName}");
            }
        }
        
        private void RemoveEngine()
        {
            if (itemManager == null)
            {
                Debug.LogError("ItemManager not found!");
                return;
            }
            
            bool success = itemManager.RemoveEquipment(ItemManager.SlotType.Engine);
            
            if (success)
            {
                Debug.Log("Engine removed successfully");
                PrintCurrentStats();
            }
            else
            {
                Debug.LogError("Failed to remove engine");
            }
        }
        
        private void PrintCurrentStats()
        {
            if (itemManager == null)
            {
                Debug.LogError("ItemManager not found!");
                return;
            }
            
            var stats = itemManager.GetShipStatistics();
            
            Debug.Log("=== Current Ship Statistics ===");
            Debug.Log($"Mass: {stats.mass:F1}");
            Debug.Log($"Thrust: {stats.thrust:F1}");
            Debug.Log($"Max Speed: {stats.maxSpeed:F1}");
            Debug.Log($"Acceleration: {stats.acceleration:F1}");
            Debug.Log($"Turn Rate: {stats.turnRate:F1}");
            Debug.Log($"Strafe Speed: {stats.strafeSpeed:F1}");
            Debug.Log($"Flight Speed: {stats.flightSpeed:F1}");
            Debug.Log($"Boost Duration: {stats.boostDuration:F1}");
            Debug.Log($"Engine Thrust: {stats.engineThrust:F1}");
            Debug.Log($"Maneuver Rate: {stats.maneuverRate:F1}");
            Debug.Log($"Strafe Thrust: {stats.strafeThrust:F1}");
            Debug.Log("================================");
            
            // Print equipment info
            var engineSlot = itemManager.GetSlot(ItemManager.SlotType.Engine);
            if (engineSlot != null && engineSlot.equipment != null)
            {
                var engine = engineSlot.equipment as EngineEquipment;
                if (engine != null)
                {
                    Debug.Log($"=== Engine Information ===");
                    Debug.Log($"Name: {engine.equipmentName}");
                    Debug.Log($"Performance Rating: {engine.GetPerformanceRating()}");
                    Debug.Log($"Efficiency: {engine.GetEfficiencyRating():F1}%");
                    Debug.Log($"Heat Generation: {engine.GetHeatRating():F1}%");
                    Debug.Log($"Max Thrust: {engine.maxThrust:F0}");
                    Debug.Log($"Top Speed: {engine.topSpeed:F0}");
                    Debug.Log($"Acceleration: {engine.acceleration:F1}");
                    Debug.Log($"Turn Rate: {engine.turnRate:F0}°/s");
                    Debug.Log($"Boost Duration: {engine.boostDuration:F1}s");
                    Debug.Log("==========================");
                }
            }
            else
            {
                Debug.Log("No engine equipped");
            }
        }
        
        /// <summary>
        /// Create and equip a default engine for the current ship
        /// </summary>
        [ContextMenu("Create and Equip Default Engine")]
        public void CreateAndEquipDefaultEngine()
        {
            if (ship == null || itemManager == null) return;
            
            // Create default engine for current ship type
            var defaultEngine = EngineEquipment.CreateDefaultEngine(
                $"Default {ship.shipType} Engine", 
                ship.shipType
            );
            
            // Equip the engine
            itemManager.SetEquipment(ItemManager.SlotType.Engine, defaultEngine);
            
            Debug.Log($"Created and equipped default engine for {ship.shipType}");
        }
        
        /// <summary>
        /// Print all equipment slots information
        /// </summary>
        [ContextMenu("Print All Equipment Slots")]
        public void PrintAllEquipmentSlots()
        {
            if (itemManager == null) return;
            
            var slots = itemManager.GetAllSlots();
            
            Debug.Log("=== All Equipment Slots ===");
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                string status = slot.IsOccupied ? $"Occupied: {slot.EquipmentName}" : "Empty";
                Debug.Log($"Slot {i} ({slot.slotType}): {status}");
            }
            Debug.Log("===========================");
        }
        
        /// <summary>
        /// Test equipment compatibility
        /// </summary>
        [ContextMenu("Test Equipment Compatibility")]
        public void TestEquipmentCompatibility()
        {
            if (itemManager == null || ship == null) return;
            
            bool isValid = itemManager.ValidateEquipmentCompatibility();
            Debug.Log($"Equipment compatibility check: {(isValid ? "PASSED" : "FAILED")}");
            
            // Test each slot
            var slots = itemManager.GetAllSlots();
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot.equipment != null)
                {
                    bool compatible = slot.equipment.IsCompatibleWithShip(ship.shipType);
                    Debug.Log($"Slot {i} ({slot.slotType}): {(compatible ? "Compatible" : "Incompatible")}");
                }
            }
        }
    }
} 