using UnityEngine;
using DomeClash.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
            // Set PrototypeShip-specific identity
            shipType = ShipType.PrototypeShip;
            shipName = "Prototype Ship";
            
            // Configure stats as overall characteristics from all ship types
            stats.maxSpeed = 380f;
            stats.acceleration = 18f;
            stats.turnRate = 65f;
            stats.strafeSpeed = 60f;
            stats.boostDuration = 2.8f;
            stats.mass = 300f;

            // Transform-based system - NO RIGIDBODY NEEDED!
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                Debug.Log($"{name}: Rigidbody kinematic yapıldı - fizik devre dışı");
            }

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

            // Flight profile should already be assigned - no need to create new one
            if (flightMovement != null)
            {
                Debug.Log($"{name}: Using existing flight profile: {(flightMovement.GetFlightProfile()?.profileName ?? "None")}");
            }

            Debug.Log($"PrototypeShip initialized with balanced characteristics from all ship types");
        }

        protected override void Update()
        {
            base.Update();
            HandleThrottleInput();
        }

        private void HandleThrottleInput()
        {
            if (flightMovement == null) return;
            if (Input.GetKey(KeyCode.W))
                IncreaseThrottle(0.5f * Time.deltaTime);
            if (Input.GetKey(KeyCode.S))
                DecreaseThrottle(0.5f * Time.deltaTime);
        }

        // Input set functions - delegate to ShipFlightController
        public override void SetPitchInput(float value) { if (flightMovement != null) flightMovement.SetPitchInput(value); }
        public override void SetYawInput(float value) { if (flightMovement != null) flightMovement.SetYawInput(value); }
        public override void SetRollInput(float value) { if (flightMovement != null) flightMovement.SetRollInput(value); }
        public override void SetStrafeInput(float value) { if (flightMovement != null) flightMovement.SetStrafeInput(value); }

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