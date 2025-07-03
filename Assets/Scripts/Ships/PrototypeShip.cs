using UnityEngine;
using DomeClash.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DomeClash.Ships
{
    /// <summary>
    /// PrototypeShip - Standalone Flight System
    /// Inherits from ShipClass base functionality
    /// </summary>
    public class PrototypeShip : ShipClass
    {
        [Header("Prototype Ship Specific")]
        [System.Serializable]
        public class PrototypeShipStats
        {
            [Header("Extended Stats")]
            public float thrust = 75f;
            public float deceleration = 20f;
            public float engineThrust = 11772f;
            public float maneuverRate = 50f;
            public float strafeThrust = 60f;
        }
        
        [Header("Extended Stats")]
        public PrototypeShipStats prototypeStats = new PrototypeShipStats();

        // Flight system delegation - movement handled by ShipFlightController

        protected override void Awake()
        {
            base.Awake(); // Call ShipClass initialization first
            InitializePrototypeShip();
        }

        protected virtual void InitializePrototypeShip()
        {
            // Set PrototypeShip-specific identity
            shipType = ShipType.PrototypeShip;
            shipName = "Prototype Ship";
            
            // Configure ship movement stats for a "heavy" but powerful feel
            stats.mass = 40f;           // A solid baseline mass.
            stats.maxSpeed = 250f;      // Top speed of the ship.
            stats.acceleration = 15f;   // How quickly the ship reaches its target speed.
            stats.turnRate = 80f;       // Increased turn rate for better responsiveness
            stats.strafeSpeed = 60f;
            stats.boostDuration = 2.8f;
            
            // Configure prototype-specific stats
            prototypeStats.thrust = 75f;         // Raw engine power.
            prototypeStats.deceleration = 10f;   // How quickly the ship slows down.
            prototypeStats.engineThrust = 11772f;
            prototypeStats.maneuverRate = 50f;
            prototypeStats.strafeThrust = 60f;

            // flightMovement is now handled by base ShipClass
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
        public float GetFlightSpeed() => flightMovement?.GetFlightProfile()?.flightSpeed ?? 0f;
        public float GetTurnSpeed() => flightMovement?.GetFlightProfile()?.turnSpeed ?? 0f;

        // Banking system getters
        public float GetCurrentBankAngle() => flightMovement?.GetCurrentBankAngle() ?? 0f;
        public float GetCurrentPitch() => flightMovement?.GetCurrentPitch() ?? 0f;
        public float GetCurrentYaw() => flightMovement?.GetCurrentYaw() ?? 0f;

        // Ship stats getters (for compatibility)
        public float GetMaxSpeed() => stats.maxSpeed;
        public float GetAcceleration() => stats.acceleration;
        public float GetTurnRate() => stats.turnRate;
        public float GetBoostDuration() => stats.boostDuration;



#if UNITY_EDITOR
    [ContextMenu("Refresh Flight Profile From Inspector")]
    public void RefreshFlightProfile()
    {
        var flightController = GetComponent<DomeClash.Core.ShipFlightController>();
        if (flightController != null)
        {
            flightController.SetFlightProfile(DomeClash.Core.FlightProfile.CreateFromShip(this));
            Debug.Log("Flight profile refreshed from inspector values!");
        }
    }
#endif
    }
} 