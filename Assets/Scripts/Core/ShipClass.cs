using UnityEngine;
using DomeClash.Core;

namespace DomeClash.Core
{
    /// <summary>
    /// Base ship class - provides common ship functionality
    /// All ships inherit from this base class
    /// </summary>
    public class ShipClass : MonoBehaviour
    {
        [Header("Ship Identity")]
        public string shipName = "Ship";
        public enum ShipType { PrototypeShip, Bastion, Breacher, Razor, Haven }
        public ShipType shipType = ShipType.PrototypeShip;

        [System.Serializable]
        public class ShipStats
        {
            [Header("Core Stats")]
            public float mass = 50f;
            public float maxSpeed = 100f;
            public float acceleration = 10f;
            public float turnRate = 30f;
            public float strafeSpeed = 20f;
            public float boostDuration = 5f;
        }

        [Header("Ship Stats")]
        public ShipStats stats = new ShipStats();

        [Header("Components")]
        public ShipFlightController flightMovement;
        public DamageSystem damageSystem;
        public EnergySystem energySystem;

        protected virtual void Awake()
        {
            InitializeShip();
        }

        protected virtual void InitializeShip()
        {
            // Auto-find or create required components
            if (flightMovement == null)
            {
                flightMovement = GetComponent<ShipFlightController>();
                if (flightMovement == null)
                {
                    flightMovement = gameObject.AddComponent<ShipFlightController>();
                }
            }

            if (damageSystem == null)
            {
                damageSystem = GetComponent<DamageSystem>();
                if (damageSystem == null)
                {
                    damageSystem = gameObject.AddComponent<DamageSystem>();
                }
            }

            if (energySystem == null)
            {
                energySystem = GetComponent<EnergySystem>();
                if (energySystem == null)
                {
                    energySystem = gameObject.AddComponent<EnergySystem>();
                }
            }
        }

        // Public methods for weapon systems
        public virtual bool HasEnoughEnergy(float amount)
        {
            return energySystem != null && energySystem.HasEnoughEnergy(amount);
        }

        public virtual void ConsumeEnergy(float amount)
        {
            if (energySystem != null)
            {
                energySystem.ConsumeWeaponEnergy(amount);
            }
        }

        public virtual float GetCurrentSpeed()
        {
            return flightMovement != null ? flightMovement.CurrentSpeed : 0f;
        }

        public virtual Vector3 GetVelocity()
        {
            return flightMovement != null ? flightMovement.CurrentVelocity : Vector3.zero;
        }

        // Ship state queries
        public virtual bool IsDestroyed()
        {
            return damageSystem != null && damageSystem.IsDestroyed;
        }

        public virtual float GetHealthPercent()
        {
            return damageSystem != null ? damageSystem.GetOverallHPPercent() : 1f;
        }
    }
} 