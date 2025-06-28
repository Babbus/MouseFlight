using UnityEngine;
using DomeClash.Core;

namespace DomeClash.Ships
{
    /// <summary>
    /// PrototypeShip - Modern Modular Flight System
    /// Uses FlightMovementComponent for all movement
    /// Clean separation of concerns - flight vs ship systems
    /// </summary>
    public class PrototypeShip : ShipClass
    {
        [Header("Modular Flight System")]
        [Tooltip("Flight movement component reference")]
        public FlightMovementComponent flightMovement;

        // Flight system delegation - movement handled by FlightMovementComponent

        // Camera level reference (public so it can be assigned)
        [Header("Camera Control")]
        public Transform cameraTransform;       // Reference to camera for level keeping
        [Tooltip("Camera follow distance behind ship")]
        public float cameraFollowDistance = 25f;
        [Tooltip("Camera height above ship")]
        public float cameraHeight = 8f;
        [Tooltip("Camera follow smoothing")]
        public float cameraFollowSpeed = 5f;

        protected override void InitializeShip()
        {
            // Transform-based system - NO RIGIDBODY NEEDED!
            if (rb != null)
            {
                // Rigidbody'yi kinematic yap - fizik hesabı yok
                rb.isKinematic = true;
                rb.useGravity = false;
                Debug.Log($"{name}: Rigidbody kinematic yapıldı - fizik devre dışı");
            }

            // Initialize ship stats
            stats.health = stats.maxHealth;
            stats.energy = stats.maxEnergy;
            stats.shields = stats.maxShields;

            // Auto-find or create FlightMovementComponent
            if (flightMovement == null)
            {
                flightMovement = GetComponent<FlightMovementComponent>();
                if (flightMovement == null)
                {
                    flightMovement = gameObject.AddComponent<FlightMovementComponent>();
                    Debug.Log($"{name}: FlightMovementComponent added automatically");
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

            // Auto-find camera if not assigned
            if (cameraTransform == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    cameraTransform = mainCam.transform;
                    Debug.Log($"Auto-assigned camera: {cameraTransform.name}");
                }
            }

            Debug.Log($"PrototypeShip initialized with Modular Flight System");
        }

        protected override void Update()
        {
            // Base class Update'i çağır
            base.Update();

            // Manual camera control for stable following
            if (cameraTransform != null)
            {
                UpdateCameraFollow();
            }
        }

        // Movement now handled by FlightMovementComponent

        private void UpdateCameraFollow()
        {
            if (cameraTransform == null || flightMovement == null) return;

            // Calculate camera position - behind and above ship
            // Use horizontal directions only (no roll influence)
            Vector3 horizontalForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 horizontalBack = -horizontalForward;

            // Target camera position
            Vector3 targetCameraPos = transform.position 
                + horizontalBack * cameraFollowDistance    // Behind ship
                + Vector3.up * cameraHeight;               // Above ship

            // Smooth camera position movement
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetCameraPos, 
                cameraFollowSpeed * Time.deltaTime);

            // Camera rotation - follows pitch and yaw, but stays level (no roll)
            Vector3 cameraEuler = new Vector3(flightMovement.GetCurrentPitch(), flightMovement.GetCurrentYaw(), 0f);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, 
                Quaternion.Euler(cameraEuler), cameraFollowSpeed * Time.deltaTime);
        }

        // Banking and rotation now handled by FlightMovementComponent

        // Input set functions - delegate to FlightMovementComponent
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

        // Getter methods for DebugHUD - delegate to FlightMovementComponent
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

        // Camera system getters
        public float GetCameraFollowDistance() => cameraFollowDistance;
        public float GetCameraHeight() => cameraHeight;
        public bool HasCameraAssigned() => cameraTransform != null;

        // Throttle control methods - delegate to FlightMovementComponent
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
                // Shields absorb damage first
                float shieldDamage = Mathf.Min(damage, stats.shields);
                stats.shields -= shieldDamage;
                damage -= shieldDamage;
            }

            if (damage > 0)
            {
                // Remaining damage goes to health
                stats.health -= damage;
                stats.health = Mathf.Max(0f, stats.health);

                if (stats.health <= 0)
                {
                    Debug.Log("PrototypeShip destroyed!");
                    // TODO: Handle ship destruction
                }
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

        // Debug information - updated for FlightMovementComponent
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

                // Draw camera position indicator
                if (cameraTransform != null)
                {
                    Gizmos.color = Color.orange;
                    Gizmos.DrawWireSphere(cameraTransform.position, 2f);
                    Gizmos.DrawLine(pos, cameraTransform.position);
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