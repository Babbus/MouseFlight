using UnityEngine;
using DomeClash.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DomeClash.Ships
{
    /// <summary>
    /// PrototypeShip - Standalone Flight System
    /// Standalone ship class with weapon system compatibility
    /// </summary>
    public class PrototypeShip : MonoBehaviour
    {
        [Header("Ship Identity")]
        public string shipName = "Prototype Ship";
        public enum ShipType { PrototypeShip, Bastion, Breacher, Razor, Haven }
        public ShipType shipType = ShipType.PrototypeShip;

        [System.Serializable]
        public class ShipStats
        {
            [Header("Core Stats")]
            public float mass = 50f;
            public float thrust = 75f;
            public float maxSpeed = 250f;
            public float acceleration = 10f;
            public float deceleration = 20f;
            public float turnRate = 2000f;
            public float strafeSpeed = 60f;
            
            [Header("Legacy & Other")]
            public float boostDuration = 2.8f;
            public float engineThrust = 11772f;
            public float maneuverRate = 50f;
            public float strafeThrust = 60f;
        }
        
        [Header("Ship Stats")]
        public ShipStats stats = new ShipStats();

        [Header("Components")]
        [SerializeField] public ShipFlightController flightMovement;

        // Flight system delegation - movement handled by ShipFlightController

        protected virtual void Awake()
        {
            InitializeShip();
        }

        protected virtual void InitializeShip()
        {
            // Set PrototypeShip-specific identity
            shipType = ShipType.PrototypeShip;
            shipName = "Prototype Ship";
            
            // Configure ship movement stats for a "heavy" but powerful feel
            stats.mass = 40f;           // A solid baseline mass.
            stats.thrust = 75f;         // Raw engine power.
            stats.maxSpeed = 250f;      // Top speed of the ship.
            stats.acceleration = 120f;  // Extremely high thrust to maintain speed when climbing at any angle
            stats.deceleration = 10f;   // How quickly the ship slows down.
            stats.turnRate = 80f;       // Increased turn rate for better responsiveness (was 4000f, using more reasonable value)
            stats.strafeSpeed = 60f;
            stats.boostDuration = 2.8f;
            stats.engineThrust = 11772f;
            stats.maneuverRate = 50f;
            stats.strafeThrust = 60f;

            // Auto-find or create ShipFlightController
            if (flightMovement == null)
            {
                flightMovement = GetComponent<ShipFlightController>();
                if (flightMovement == null)
                {
                    flightMovement = gameObject.AddComponent<ShipFlightController>();
                }
            }

            // Flight profile should already be assigned - no need to create new one
            if (flightMovement != null)
            {
                // No console logging
            }
        }

        protected virtual void Update()
        {
<<<<<<< HEAD
            HandleThrottleInput();
=======
            base.Update();
            HandleThrottleInput();
            // Mouse1 (Fire1) ile ateş et
            if (Input.GetButton("Fire1"))
            {
                if (_weaponManager == null)
                    _weaponManager = GetComponent<DomeClash.Weapons.WeaponManager>();
                if (_weaponManager != null)
                    _weaponManager.FirePrimary();
            }
>>>>>>> parent of ea08b60 (Missle System Without radar:)
        }

        private void HandleThrottleInput()
        {
            if (flightMovement == null) return;
            if (Input.GetKey(KeyCode.W))
<<<<<<< HEAD
            {
                SetThrottle(1.0f);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                SetThrottle(0.0f);
            }
=======
                IncreaseThrottle(0.5f * Time.deltaTime);
            if (Input.GetKey(KeyCode.S))
                DecreaseThrottle(0.5f * Time.deltaTime);
>>>>>>> parent of ea08b60 (Missle System Without radar:)
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

        // Weapon system compatibility methods
        public virtual bool HasEnoughEnergy(float amount)
        {
            // Simple implementation - always return true for now
            // Can be enhanced later with proper energy system
            return true;
        }

        public virtual void ConsumeEnergy(float amount)
        {
            // Simple implementation - does nothing for now
            // Can be enhanced later with proper energy system
        }

        public virtual Vector3 GetVelocity()
        {
            return flightMovement != null ? flightMovement.CurrentVelocity : Vector3.zero;
        }

        public virtual bool IsDestroyed()
        {
            // Simple implementation - never destroyed for now
            // Can be enhanced later with proper damage system
            return false;
        }

        public virtual float GetHealthPercent()
        {
            // Simple implementation - always full health for now
            // Can be enhanced later with proper damage system
            return 1f;
        }

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