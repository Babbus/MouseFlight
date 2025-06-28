using UnityEngine;
using DomeClash.Core;

namespace DomeClash.Ships
{
    /// <summary>
    /// PrototypeShip - MouseFlight Transform-Based System
    /// Direct transform control - NO PHYSICS!
    /// Enhanced with realistic banking mechanics - roll doesn't affect altitude
    /// </summary>
    public class PrototypeShip : ShipClass
    {
        [Header("Transform-Based Flight - Enhanced Performance")]
        [Tooltip("Flight speed - direct transform movement")] 
        public float flightSpeed = 120f;  // 80 → 120 (+50% hız)
        [Tooltip("Turn speed for pitch/yaw/roll")] 
        public float turnSpeed = 150f;   // 90 → 150 (+67% dönüş)
        [Tooltip("Banking amount when turning")]
        public float bankingAmount = 45f; // 30 → 45 (+50% banking)
        [Tooltip("Speed smoothing factor")]
        public float speedSmoothing = 8f;  // 5 → 8 (daha hızlı response)

        [Header("Advanced Control")]
        [Tooltip("Throttle control")] 
        [Range(0f, 1f)] public float throttle = 0.8f;
        [Tooltip("Minimum flight speed")]
        public float minSpeed = 30f;   // 20 → 30 (daha yüksek min hız)
        [Tooltip("Maximum flight speed")]
        public float maxSpeed = 200f;  // 150 → 200 (+33% max hız)

        [Header("Banking System")]
        [Tooltip("Maximum bank angle in degrees")]
        public float maxBankAngle = 60f;
        [Tooltip("Bank smoothing factor")]
        public float bankSmoothing = 4f;
        [Tooltip("Auto-level rate when no input")]
        public float autoLevelRate = 2f;

        // MouseFlight style input variables
        private float pitchInput = 0f;
        private float yawInput = 0f;
        private float rollInput = 0f;
        private float strafeInput = 0f;
        private float thrustInput = 0f;

        // Transform-based movement
        private new float currentSpeed;  // Hide inherited currentSpeed
        private Vector3 currentVelocity;

        // Banking system variables
        private float currentBankAngle = 0f;    // Current roll angle
        private float targetBankAngle = 0f;     // Target roll angle
        private float currentPitch = 0f;        // Current pitch
        private float currentYaw = 0f;          // Current yaw for smooth rotation

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

            // Starting speed
            currentSpeed = flightSpeed * throttle;

            // Initialize rotation values from current transform
            Vector3 currentEuler = transform.eulerAngles;
            currentPitch = currentEuler.x;
            currentYaw = currentEuler.y;
            currentBankAngle = currentEuler.z;

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

            Debug.Log($"PrototypeShip initialized with Enhanced Banking System (NO PHYSICS)");
        }

        protected override void Update()
        {
            // Base class Update'i çağır
            base.Update();

            // W tuşu ile throttle artır
            if (Input.GetKey(KeyCode.W))
                thrustInput = 1f;
            else
                thrustInput = 0f;

            // Throttle sistemi - scroll wheel ile kontrol
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
                IncreaseThrottle(0.1f);
            else if (scroll < 0f)
                DecreaseThrottle(0.1f);

            // Transform-based movement - her frame
            UpdateTransformMovement();

            // Debug için input değerlerini göster (active for troubleshooting)
            if (Time.frameCount % 30 == 0)  // Every 0.5 seconds
            {
                Vector3 flightDir = CalculateFlightDirection();
                float flightPitch = Mathf.Asin(-flightDir.y) * Mathf.Rad2Deg;
                Debug.Log($"SHIP DEBUG - Input: P:{pitchInput:F2}, Y:{yawInput:F2}, R:{rollInput:F2} | Angles: P:{currentPitch:F1}°, Y:{currentYaw:F1}°, Bank:{currentBankAngle:F1}° | FlightPitch:{flightPitch:F1}° | Speed: {currentSpeed:F1}");
            }

            // Manual camera control for stable following
            if (cameraTransform != null)
            {
                UpdateCameraFollow();
            }
        }

        private void UpdateTransformMovement()
        {
            // Speed control - throttle + thrust input
            float targetSpeed = flightSpeed * throttle;
            if (thrustInput > 0f)
                targetSpeed = Mathf.Lerp(targetSpeed, maxSpeed, thrustInput * 0.5f);

            // Smooth speed changes
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedSmoothing * Time.deltaTime);
            currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

            // Calculate flight direction - includes pitch but excludes roll effect
            // Create a forward vector using only pitch and yaw (no roll influence)
            Vector3 flightDirection = CalculateFlightDirection();

            // Forward movement - follows pitch but roll doesn't affect altitude
            Vector3 forwardMovement = flightDirection * currentSpeed * Time.deltaTime;

            // Strafe movement - horizontal only
            Vector3 horizontalRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            Vector3 strafeMovement = Vector3.zero;
            if (Mathf.Abs(strafeInput) > 0.05f)
            {
                strafeMovement = horizontalRight * strafeInput * stats.strafeSpeed * Time.deltaTime;
            }

            // Apply movement
            transform.position += forwardMovement + strafeMovement;

            // Rotation control with banking system
            ApplyBankingRotation();
        }

        private Vector3 CalculateFlightDirection()
        {
            // Create flight direction using only pitch and yaw (ignoring roll)
            // This allows pitch movement but prevents roll from affecting altitude
            Quaternion flightRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            return flightRotation * Vector3.forward;
        }

        private void UpdateCameraFollow()
        {
            if (cameraTransform == null) return;

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
            Vector3 cameraEuler = new Vector3(currentPitch, currentYaw, 0f);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, 
                Quaternion.Euler(cameraEuler), cameraFollowSpeed * Time.deltaTime);
        }

        private void ApplyBankingRotation()
        {
            float deltaTime = Time.deltaTime;

            // Smooth pitch control
            currentPitch += pitchInput * turnSpeed * deltaTime;
            currentPitch = Mathf.Clamp(currentPitch, -90f, 90f); // Limit pitch

            // Smooth yaw control
            currentYaw += yawInput * turnSpeed * deltaTime;

            // Banking system - calculate target bank angle
            float manualBank = rollInput * maxBankAngle;
            float autoBank = 0f;

            // Automatic banking when turning (coordinated turn)
            if (Mathf.Abs(yawInput) > 0.1f)
            {
                autoBank = -yawInput * bankingAmount;
            }

            targetBankAngle = manualBank + autoBank;
            targetBankAngle = Mathf.Clamp(targetBankAngle, -maxBankAngle, maxBankAngle);

            // Auto-level when no input
            if (Mathf.Abs(rollInput) < 0.1f && Mathf.Abs(yawInput) < 0.1f)
            {
                targetBankAngle = Mathf.Lerp(targetBankAngle, 0f, autoLevelRate * deltaTime);
            }

            // Smooth bank angle transition
            currentBankAngle = Mathf.Lerp(currentBankAngle, targetBankAngle, bankSmoothing * deltaTime);

            // Apply rotation using Euler angles for precise control
            Vector3 eulerAngles = new Vector3(currentPitch, currentYaw, currentBankAngle);
            transform.rotation = Quaternion.Euler(eulerAngles);
        }

        // Input set functions - called by DomeClashFlightController
        public override void SetPitchInput(float value)
        {
            pitchInput = Mathf.Clamp(value, -1f, 1f);
        }

        public override void SetYawInput(float value)
        {
            yawInput = Mathf.Clamp(value, -1f, 1f);
        }

        public override void SetRollInput(float value)
        {
            rollInput = Mathf.Clamp(value, -1f, 1f);
        }

        public override void SetStrafeInput(float value)
        {
            strafeInput = Mathf.Clamp(value, -1f, 1f);
        }

        // Getter methods for DebugHUD
        public float GetPitchInput() => pitchInput;
        public float GetYawInput() => yawInput;
        public float GetRollInput() => rollInput;
        public float GetStrafeInput() => strafeInput;
        public float GetThrottle() => throttle;
        public new float GetCurrentSpeed() => currentSpeed;  // Hide inherited method
        public float GetFlightSpeed() => flightSpeed;
        public float GetTurnSpeed() => turnSpeed;

        // Banking system getters
        public float GetCurrentBankAngle() => currentBankAngle;
        public float GetTargetBankAngle() => targetBankAngle;
        public float GetCurrentPitch() => currentPitch;
        public float GetCurrentYaw() => currentYaw;

        // Camera system getters
        public float GetCameraFollowDistance() => cameraFollowDistance;
        public float GetCameraHeight() => cameraHeight;
        public bool HasCameraAssigned() => cameraTransform != null;

        // Throttle control methods
        public void SetThrottle(float newThrottle)
        {
            throttle = Mathf.Clamp01(newThrottle);
        }

        public void IncreaseThrottle(float amount = 0.1f)
        {
            throttle = Mathf.Clamp01(throttle + amount);
        }

        public void DecreaseThrottle(float amount = 0.1f)
        {
            throttle = Mathf.Clamp01(throttle - amount);
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

        // Debug information
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Vector3 pos = transform.position;

                // Draw actual flight direction (green) - includes pitch, excludes roll
                Vector3 flightDirection = CalculateFlightDirection();
                Gizmos.color = Color.green;
                Gizmos.DrawRay(pos, flightDirection * (currentSpeed / 8f));

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
                if (Mathf.Abs(currentBankAngle) > 5f)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(pos + Vector3.up * 12f, Mathf.Abs(currentBankAngle) / 10f);
                }

                // Draw pitch reference line (white)
                if (Mathf.Abs(currentPitch) > 5f)
                {
                    Gizmos.color = Color.white;
                    Vector3 pitchDirection = Quaternion.Euler(currentPitch, currentYaw, 0f) * Vector3.forward;
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