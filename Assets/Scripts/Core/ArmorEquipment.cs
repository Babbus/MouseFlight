using UnityEngine;
using DomeClash.Ships;

namespace DomeClash.Core
{
    /// <summary>
    /// Armor Equipment - Specific equipment type for ship chassis.
    /// Provides armor-specific statistics and defines the ship's 3D model.
    /// </summary>
    [CreateAssetMenu(fileName = "New Armor", menuName = "DomeClash/Equipment/Armor")]
    public class ArmorEquipment : EquipmentData
    {
        [Header("Armor Specific Properties")]
        [Tooltip("The 3D model prefab for the ship's chassis.")]
        public GameObject shipChassisPrefab;
        
        // In the future, you could add armor-specific stats here, like:
        // public float damageResistance = 0f;
        // public float energyResistance = 0f;

        public override void OnEquipped(ItemManager itemManager)
        {
            base.OnEquipped(itemManager);
            Debug.Log($"ArmorEquipment: '{equipmentName}' OnEquipped called.");

            // Find the ShipManager on the ship and tell it to change the chassis
            var shipManager = itemManager.GetComponent<ShipManager>();
            if (shipManager != null)
            {
                Debug.Log($"ArmorEquipment: Found ShipManager. Calling SetChassis with prefab '{(shipChassisPrefab != null ? shipChassisPrefab.name : "null")}'.");
                shipManager.SetChassis(shipChassisPrefab);
            }
            else
            {
                Debug.LogWarning("No ShipManager found on the ship. Cannot apply chassis model.");
            }
        }
        
        public override void OnUnequipped(ItemManager itemManager)
        {
            base.OnUnequipped(itemManager);

            // When unequipped, tell the ship manager to remove the chassis
            var shipManager = itemManager.GetComponent<ShipManager>();
            if (shipManager != null)
            {
                shipManager.SetChassis(null); // Passing null will clear the model
            }
        }
    }
} 