using UnityEngine;
using DomeClash.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DomeClash.Ships
{
    /// <summary>
    /// ShipManager - The base class and central hub for a ship entity.
    /// Owns and coordinates all other components like ItemManager and ShipFlightController.
    /// </summary>
    public class ShipManager : MonoBehaviour
    {
        [Header("Ship Identity")]
        public string shipName = "Default Ship";
        public enum ShipType { Default, Bastion, Breacher, Razor, Haven }
        public ShipType shipType = ShipType.Default;

        [Header("Components")]
        [SerializeField] public ShipFlightController flightMovement;
        [SerializeField] public ItemManager itemManager;

        [Header("Appearance")]
        [Tooltip("The parent transform where the ship model will be instantiated. If null, this transform is used.")]
        [SerializeField] private Transform chassisParent;
        
        private GameObject currentChassisInstance;

        // Flight system delegation - movement handled by ShipFlightController

        protected virtual void Awake()
        {
            InitializeShip();
        }

        protected virtual void InitializeShip()
        {
            // Set default identity based on the class, can be overridden in subclasses
            if (this.GetType() == typeof(ShipManager))
            {
                shipType = ShipType.Default;
                shipName = "Default Ship";
            }

            // Auto-find or create ShipFlightController
            if (flightMovement == null)
            {
                flightMovement = GetComponent<ShipFlightController>();
                if (flightMovement == null)
                {
                    flightMovement = gameObject.AddComponent<ShipFlightController>();
                }
            }
            
            // Auto-find or create ItemManager
            if (itemManager == null)
            {
                itemManager = GetComponent<ItemManager>();
                if (itemManager == null)
                {
                    itemManager = gameObject.AddComponent<ItemManager>();
                }
            }

            // If no parent is specified, use this manager's transform as the parent.
            if (chassisParent == null)
            {
                chassisParent = transform;
            }
        }

        protected virtual void Update()
        {
            HandleThrottleInput();
        }

        private void HandleThrottleInput()
        {
            if (flightMovement == null) return;
            if (Input.GetKey(KeyCode.W))
            {
                SetThrottle(1.0f);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                SetThrottle(0.0f);
            }
        }

        // Input set functions - delegate to ShipFlightController
        public virtual void SetPitchInput(float value) { if (flightMovement != null) flightMovement.SetPitchInput(value); }
        public virtual void SetYawInput(float value) { if (flightMovement != null) flightMovement.SetYawInput(value); }
        public virtual void SetRollInput(float value) { if (flightMovement != null) flightMovement.SetRollInput(value); }
        public virtual void SetStrafeInput(float value) { if (flightMovement != null) flightMovement.SetStrafeInput(value); }

        // Throttle control methods - delegate to ShipFlightController
        public void SetThrottle(float newThrottle) { if (flightMovement != null) flightMovement.SetThrottle(newThrottle); }
        public void IncreaseThrottle(float amount = 0.1f) { if (flightMovement != null) flightMovement.IncreaseThrottle(amount); }
        public void DecreaseThrottle(float amount = 0.1f) { if (flightMovement != null) flightMovement.DecreaseThrottle(amount); }

        // Getter methods for DebugHUD - delegate to ShipFlightController
        public float GetPitchInput() => flightMovement?.GetPitchInput() ?? 0f;
        public float GetYawInput() => flightMovement?.GetYawInput() ?? 0f;
        public float GetRollInput() => flightMovement?.GetRollInput() ?? 0f;
        public float GetStrafeInput() => flightMovement?.GetStrafeInput() ?? 0f;
        public float GetThrottle() => flightMovement?.Throttle ?? 0f;
        public float GetCurrentSpeed() => flightMovement?.CurrentSpeed ?? 0f;
        public float GetFlightSpeed() => itemManager?.GetShipStatistics()?.flightSpeed ?? 0f;
        public float GetTurnSpeed() => itemManager?.GetShipStatistics()?.turnRate ?? 0f;

        // Banking system getters
        public float GetCurrentBankAngle() => flightMovement?.GetCurrentBankAngle() ?? 0f;
        public float GetCurrentPitch() => flightMovement?.GetCurrentPitch() ?? 0f;
        public float GetCurrentYaw() => flightMovement?.GetCurrentYaw() ?? 0f;

        // Ship stats getters sourced from ItemManager
        public float GetMaxSpeed() => itemManager?.GetShipStatistics()?.maxSpeed ?? 0f;
        public float GetAcceleration() => itemManager?.GetShipStatistics()?.acceleration ?? 0f;
        public float GetTurnRate() => itemManager?.GetShipStatistics()?.turnRate ?? 0f;
        public float GetBoostDuration() => itemManager?.GetShipStatistics()?.boostDuration ?? 0f;

        /// <summary>
        /// Sets the ship's chassis to a new model.
        /// </summary>
        /// <param name="chassisPrefab">The prefab of the 3D model to use. Pass null to remove the current model.</param>
        public void SetChassis(GameObject chassisPrefab)
        {
            Debug.Log($"ShipManager: SetChassis called with prefab '{(chassisPrefab != null ? chassisPrefab.name : "null")}'.");
            // Destroy the old chassis if it exists
            if (currentChassisInstance != null)
            {
                Debug.Log("ShipManager: Destroying old chassis instance.");
                Destroy(currentChassisInstance);
            }

            // If a new prefab is provided, instantiate it
            if (chassisPrefab != null)
            {
                Debug.Log("ShipManager: Instantiating new chassis prefab.");
                currentChassisInstance = Instantiate(chassisPrefab, chassisParent);
                currentChassisInstance.transform.localPosition = Vector3.zero;
                currentChassisInstance.transform.localRotation = Quaternion.identity;
            }
        }

#if UNITY_EDITOR
    [ContextMenu("Recalculate Ship Statistics")]
    public void RecalculateStats()
    {
        if (itemManager != null)
        {
            itemManager.RecalculateStatistics();
            Debug.Log("Ship statistics recalculated from Item Manager!");
        }
    }
#endif
    }
} 