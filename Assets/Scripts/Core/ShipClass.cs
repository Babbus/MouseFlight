using UnityEngine;
using DomeClash.Weapons;

namespace DomeClash.Core
{
    /// <summary>
    /// Unity 6000.1.9f1 Compatible Ship Class System
    /// Transform-Based Movement (NO PHYSICS)
    /// </summary>
    public enum ShipType
    {
        PrototypeShip,
        Bastion,
        Breacher,
        Razor,
        Haven
    }

    public enum DamageType
    {
        Kinetic,
        Energy,
        Explosive
    }

    [System.Serializable]
    public class ShipStats
    {
        [Header("Flight - Transform Based")]
        [Tooltip("Maximum forward speed (m/s)")]
        [Range(10, 1000)]
        public float maxSpeed = 100f;

        [Tooltip("How quickly the ship accelerates (m/s^2)")]
        [Range(1, 100)]
        public float acceleration = 10f;

        [Tooltip("Turn rate (degrees/sec)")]
        [Range(10, 200)]
        public float turnRate = 30f;

        [Tooltip("Strafe speed (m/s)")]
        [Range(0, 100)]
        public float strafeSpeed = 20f;

        [Tooltip("Boost duration (seconds)")]
        [Range(0, 10)]
        public float boostDuration = 5f;

        [Header("Physics - Minimal")]
        [Tooltip("Ship mass (kg)")]
        [Range(10, 1000)]
        public float mass = 100f;
    }

    public abstract class ShipClass : MonoBehaviour
    {
        [Header("Ship Identity")]
        public ShipType shipType;
        public string shipName = "Prototype Ship";
        [Header("Stats")]
        public ShipStats stats;
        [Header("Components")]
        [SerializeField] protected Rigidbody rb;
        [SerializeField] protected MouseFlightController flightController;
        // Weapons and combat fields removed

        // Remove current state fields

        protected virtual void Awake()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody>();
            if (flightController == null)
                flightController = FindFirstObjectByType<MouseFlightController>();
            InitializeShip();
        }

        protected virtual void Start() { }
        protected virtual void Update() { UpdateShipState(); }
        protected virtual void FixedUpdate() { }
        protected virtual void InitializeShip()
        {
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                Debug.Log($"{shipName}: Rigidbody set to kinematic - NO PHYSICS mode");
            }
        }
        protected virtual void UpdateShipState() { }
        // Remove all combat/energy/damage methods
        // Remove all current state getters
        // Only keep flight-related methods
        public float GetMaxSpeed() => stats.maxSpeed;
        public float GetAcceleration() => stats.acceleration;
        public float GetTurnRate() => stats.turnRate;
        public float GetBoostDuration() => stats.boostDuration;
        // Input methods - implemented by derived classes
        public virtual void SetPitchInput(float value) { }
        public virtual void SetYawInput(float value) { }
        public virtual void SetRollInput(float value) { }
        public virtual void SetStrafeInput(float value) { }
    }
} 