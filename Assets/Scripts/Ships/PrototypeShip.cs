using UnityEngine;
using DomeClash.Core;

namespace DomeClash.Ships
{
    /// <summary>
    /// PrototypeShip - Modern Modular Flight System
    /// Uses ShipFlightController for all movement
    /// Clean separation of concerns - flight vs ship systems
    /// </summary>
    public class PrototypeShip : ShipClass
    {
        [Header("Modular Flight System")]
        [Tooltip("Flight movement component reference")]
        public ShipFlightController flightMovement;

        // Flight system delegation - movement handled by ShipFlightController

        protected override void InitializeShip()
        {
            // Transform-based system - NO RIGIDBODY NEEDED!
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                Debug.Log($"{name}: Rigidbody kinematic yapıldı - fizik devre dışı");
            }

            // Initialize ship stats
            stats.health = stats.maxHealth;
            stats.energy = stats.maxEnergy;
            stats.shields = stats.maxShields;

            // Auto-find or create ShipFlightController
            if (flightMovement == null)
            {
                flightMovement = GetComponent<ShipFlightController>();
                if (flightMovement == null)
                {
                    flightMovement = gameObject.AddComponent<ShipFlightController>();
                    Debug.Log($"{name}: ShipFlightController added automatically");
                }
            }

            // Auto-assign default flight profile if none assigned
            if (flightMovement != null && flightMovement.GetFlightProfile() == null)
            {
                // Create default profile for prototyping
                FlightProfile defaultProfile = FlightProfile.CreateDefaultProfile(ShipType.Razor); // Fast profile for prototype
                defaultProfile.name = "Auto_Prototype_Profile";
                
                flightMovement.SetFlightProfile(defaultProfile);
                Debug.Log($"{name}: Auto-assigned flight profile: {defaultProfile.name}");
            }

            Debug.Log($"PrototypeShip initialized with Modular Flight System");
        }

        protected override void Update()
        {
            // Base class Update'i çağır
            base.Update();
        }

        // Movement now handled by ShipFlightController

        // Input set functions - delegate to ShipFlightController
        public override void SetPitchInput(float value)
        {
            if (flightMovement != null)
                flightMovement.SetPitchInput(value);
        }

        public override void SetYawInput(float value)
        {
            if (flightMovement != null)
                flightMovement.SetYawInput(value);
        }

        public override void SetRollInput(float value)
        {
            if (flightMovement != null)
                flightMovement.SetRollInput(value);
        }

        public override void SetStrafeInput(float value)
        {
            if (flightMovement != null)
                flightMovement.SetStrafeInput(value);
        }

        // Getter methods for DebugHUD - delegate to ShipFlightController
        public float GetPitchInput() => flightMovement?.GetPitchInput() ?? 0f;
        public float GetYawInput() => flightMovement?.GetYawInput() ?? 0f;
        public float GetRollInput() => flightMovement?.GetRollInput() ?? 0f;
        public float GetStrafeInput() => flightMovement?.GetStrafeInput() ?? 0f;
        public float GetThrottle() => flightMovement?.Throttle ?? 0f;
        public new float GetCurrentSpeed() => flightMovement?.CurrentSpeed ?? 0f;
        public float GetFlightSpeed() => flightMovement?.GetFlightProfile()?.flightSpeed ?? 0f;
        public float GetTurnSpeed() => flightMovement?.GetFlightProfile()?.turnSpeed ?? 0f;

        // Banking system getters
        public float GetCurrentBankAngle() => flightMovement?.GetCurrentBankAngle() ?? 0f;
        public float GetCurrentPitch() => flightMovement?.GetCurrentPitch() ?? 0f;
        public float GetCurrentYaw() => flightMovement?.GetCurrentYaw() ?? 0f;

        // Throttle control methods - delegate to ShipFlightController
        public void SetThrottle(float newThrottle)
        {
            if (flightMovement != null)
                flightMovement.SetThrottle(newThrottle);
        }

        public void IncreaseThrottle(float amount = 0.1f)
        {
            if (flightMovement != null)
                flightMovement.IncreaseThrottle(amount);
        }

        public void DecreaseThrottle(float amount = 0.1f)
        {
            if (flightMovement != null)
                flightMovement.DecreaseThrottle(amount);
        }

        // Energy and health management
        public override bool ConsumeEnergy(float amount)
        {
            if (stats.energy >= amount)
            {
                stats.energy -= amount;
                stats.energy = Mathf.Max(0f, stats.energy);
                return true;
            }
            return false;
        }

        public override void TakeDamage(float damage, DamageType damageType)
        {
            if (stats.shields > 0)
            {
                stats.shields -= damage;
                if (stats.shields < 0)
                {
                    stats.health += stats.shields; // Subtract overflow from health
                    stats.shields = 0;
                }
            }
            else
            {
                stats.health -= damage;
            }
        }

        // Collision detection - kinematic rigidbody için
        private void OnTriggerEnter(Collider other)
        {
            // Collision handling for kinematic rigidbody
            if (other.CompareTag("Obstacle") || other.CompareTag("Ground"))
            {
                Debug.Log($"PrototypeShip collision with {other.name}");
                // Handle collision damage or effects
            }
        }

        // Debug information - updated for ShipFlightController
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && flightMovement != null)
            {
                Vector3 pos = transform.position;

                // Draw ship visual forward direction (blue) - includes all rotations
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(pos, transform.forward * 10f);

                // Draw ship up direction to show banking (cyan)
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(pos, transform.up * 8f);

                // Draw level reference (yellow)
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(pos, Vector3.up * 6f);

                // Draw banking indicator (red circle)
                float bankAngle = flightMovement.GetCurrentBankAngle();
                if (Mathf.Abs(bankAngle) > 5f)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(pos + Vector3.up * 12f, Mathf.Abs(bankAngle) / 10f);
                }

                // Draw pitch reference line (white)
                float pitch = flightMovement.GetCurrentPitch();
                if (Mathf.Abs(pitch) > 5f)
                {
                    Gizmos.color = Color.white;
                    Vector3 pitchDirection = Quaternion.Euler(pitch, flightMovement.GetCurrentYaw(), 0f) * Vector3.forward;
                    Gizmos.DrawRay(pos, pitchDirection * 12f);
                }

                // Draw input indicators
                Gizmos.color = Color.magenta;
                Vector3 inputPos = pos + Vector3.up * 15f;
                float pitchInput = flightMovement.GetPitchInput();
                float yawInput = flightMovement.GetYawInput();
                float rollInput = flightMovement.GetRollInput();
                
                if (Mathf.Abs(pitchInput) > 0.1f)
                    Gizmos.DrawRay(inputPos, Vector3.forward * pitchInput * 3f);
                if (Mathf.Abs(yawInput) > 0.1f)
                    Gizmos.DrawRay(inputPos, Vector3.right * yawInput * 3f);
                if (Mathf.Abs(rollInput) > 0.1f)
                    Gizmos.DrawRay(inputPos, Vector3.up * rollInput * 3f);
            }
        }
    }
} 