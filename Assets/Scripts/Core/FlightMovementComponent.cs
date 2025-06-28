using UnityEngine;

namespace DomeClash.Core
{
    /// <summary>
    /// Modular Flight Movement Component - GDD Class-Based Flight Behavior System
    /// Transform-based movement with class-specific profiles
    /// NO PHYSICS - Direct transform control for responsive arcade flight
    /// </summary>
    public class FlightMovementComponent : MonoBehaviour
    {
        [Header("Flight Profile")]
        [SerializeField] private FlightProfile flightProfile;
        [SerializeField] private bool autoFindProfile = true; // Otomatik profil bulma
        
        [Header("Current Movement State")]
        [SerializeField] private float currentSpeed = 0f;
        [SerializeField] private float throttle = 0.8f;
        [SerializeField] private Vector3 currentVelocity = Vector3.zero;
        
        [Header("Profile Override (Optional)")]
        [SerializeField] private bool useOverrideSettings = false;
        [SerializeField] private float overrideFlightSpeed = 100f;
        [SerializeField] private float overrideTurnSpeed = 60f;
        
        [Header("Banking System")]
        [SerializeField] private float currentBankAngle = 0f;
        [SerializeField] private float currentPitch = 0f;
        [SerializeField] private float currentYaw = 0f;
        
        // Input variables
        private float pitchInput = 0f;
        private float yawInput = 0f;
        private float rollInput = 0f;
        private float strafeInput = 0f;
        private float thrustInput = 0f;
        
        // Component references
        private ShipClass shipClass;
        
        public float CurrentSpeed => currentSpeed;
        public float Throttle => throttle;
        public Vector3 CurrentVelocity => currentVelocity;
        
        private void Awake()
        {
            shipClass = GetComponent<ShipClass>();
            
            // Initialize rotation values from current transform
            Vector3 currentEuler = transform.eulerAngles;
            currentPitch = currentEuler.x;
            currentYaw = currentEuler.y;
            currentBankAngle = currentEuler.z;
        }
        
        private void Start()
        {
            // Auto-find flight profile if enabled and not assigned
            if (autoFindProfile && flightProfile == null)
            {
                AutoFindFlightProfile();
            }
            
            // Apply flight profile if assigned
            if (flightProfile != null)
            {
                ApplyFlightProfile();
            }
            
            // Starting speed
            float targetFlightSpeed = GetEffectiveFlightSpeed();
            currentSpeed = targetFlightSpeed * throttle;
            
            Debug.Log($"FlightMovementComponent initialized for {gameObject.name} with profile: {(flightProfile != null ? flightProfile.name : "None")}");
        }
        
        private void Update()
        {
            // Handle input
            HandleMovementInput();
            
            // Update transform-based movement
            UpdateTransformMovement();
        }
        
        private void ApplyFlightProfile()
        {
            if (flightProfile == null) return;
            
            // Initialize with profile values
            throttle = 0.8f; // Default throttle
            Debug.Log($"Applied flight profile: {flightProfile.name} to {gameObject.name}");
        }
        
        /// <summary>
        /// Otomatik flight profile bulma - gemi ismlerine göre
        /// </summary>
        private void AutoFindFlightProfile()
        {
            string shipName = gameObject.name.ToLower();
            
            // Gemi türüne göre profil ara
            if (shipName.Contains("razor") || shipName.Contains("interceptor"))
            {
                flightProfile = FlightProfile.CreateRazorProfile();
                Debug.Log($"Auto-assigned Razor profile to {gameObject.name}");
            }
            else if (shipName.Contains("bastion") || shipName.Contains("tank") || shipName.Contains("heavy"))
            {
                flightProfile = FlightProfile.CreateBastionProfile();
                Debug.Log($"Auto-assigned Bastion profile to {gameObject.name}");
            }
            else if (shipName.Contains("breacher") || shipName.Contains("assault"))
            {
                flightProfile = FlightProfile.CreateBreacherProfile();
                Debug.Log($"Auto-assigned Breacher profile to {gameObject.name}");
            }
            else if (shipName.Contains("haven") || shipName.Contains("support"))
            {
                flightProfile = FlightProfile.CreateHavenProfile();
                Debug.Log($"Auto-assigned Haven profile to {gameObject.name}");
            }
            else
            {
                // Default: Razor profile for unknown ships
                flightProfile = FlightProfile.CreateRazorProfile();
                Debug.Log($"Auto-assigned default Razor profile to {gameObject.name}");
            }
        }
        
        /// <summary>
        /// Effective flight speed - override varsa onu kullan
        /// </summary>
        private float GetEffectiveFlightSpeed()
        {
            if (useOverrideSettings) return overrideFlightSpeed;
            return flightProfile != null ? flightProfile.flightSpeed : 100f;
        }
        
        /// <summary>
        /// Effective turn speed - override varsa onu kullan
        /// </summary>
        private float GetEffectiveTurnSpeed()
        {
            if (useOverrideSettings) return overrideTurnSpeed;
            return flightProfile != null ? flightProfile.turnSpeed : 60f;
        }
        
        private void HandleMovementInput()
        {
            // W tuşu ile thrust artır
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
        }
        
        private void UpdateTransformMovement()
        {
            if (flightProfile == null) return;
            
            // Speed control - throttle + thrust input
            float effectiveFlightSpeed = GetEffectiveFlightSpeed();
            float targetSpeed = effectiveFlightSpeed * throttle;
            if (thrustInput > 0f)
            {
                float maxSpeed = flightProfile != null ? flightProfile.maxSpeed : effectiveFlightSpeed * 1.5f;
                targetSpeed = Mathf.Lerp(targetSpeed, maxSpeed, thrustInput * 0.5f);
            }

            // Smooth speed changes
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, flightProfile.speedSmoothing * Time.deltaTime);
            currentSpeed = Mathf.Clamp(currentSpeed, flightProfile.minSpeed, flightProfile.maxSpeed);

            // Calculate flight direction - includes pitch but excludes roll effect
            Vector3 flightDirection = CalculateFlightDirection();

            // Forward movement - follows pitch but roll doesn't affect altitude
            Vector3 forwardMovement = flightDirection * currentSpeed * Time.deltaTime;

            // Strafe movement - horizontal only
            Vector3 horizontalRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            Vector3 strafeMovement = Vector3.zero;
            if (Mathf.Abs(strafeInput) > 0.05f)
            {
                strafeMovement = horizontalRight * strafeInput * flightProfile.strafeSpeed * Time.deltaTime;
            }

            // Apply movement - SAFE POSITION UPDATE
            Vector3 totalMovement = forwardMovement + strafeMovement;
            
            // Safety check for NaN positions
            if (!float.IsNaN(totalMovement.x) && !float.IsNaN(totalMovement.y) && !float.IsNaN(totalMovement.z) &&
                !float.IsInfinity(totalMovement.x) && !float.IsInfinity(totalMovement.y) && !float.IsInfinity(totalMovement.z))
            {
                transform.position += totalMovement;
                currentVelocity = totalMovement / Time.deltaTime;
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Invalid movement vector detected - skipping movement this frame");
                currentVelocity = Vector3.zero;
            }

            // Rotation control with banking system
            ApplyBankingRotation();
        }
        
        private Vector3 CalculateFlightDirection()
        {
            // SAFETY CHECK - Prevent NaN quaternion creation
            if (float.IsNaN(currentPitch) || float.IsInfinity(currentPitch)) currentPitch = 0f;
            if (float.IsNaN(currentYaw) || float.IsInfinity(currentYaw)) currentYaw = 0f;
            
            // Create flight direction using only pitch and yaw (ignoring roll)
            Vector3 eulerAngles = new Vector3(currentPitch, currentYaw, 0f);
            
            // Double check euler angles are valid
            if (float.IsNaN(eulerAngles.x) || float.IsNaN(eulerAngles.y) || float.IsNaN(eulerAngles.z))
            {
                Debug.LogWarning($"{gameObject.name}: Invalid euler angles in CalculateFlightDirection - using forward");
                return transform.forward;
            }
            
            Quaternion flightRotation = Quaternion.Euler(eulerAngles);
            return flightRotation * Vector3.forward;
        }
        
        private void ApplyBankingRotation()
        {
            if (flightProfile == null) return;
            
            float deltaTime = Time.deltaTime;
            
            // SAFETY CHECK - Prevent NaN values
            if (float.IsNaN(currentPitch) || float.IsInfinity(currentPitch)) currentPitch = 0f;
            if (float.IsNaN(currentYaw) || float.IsInfinity(currentYaw)) currentYaw = 0f;
            if (float.IsNaN(pitchInput) || float.IsInfinity(pitchInput)) pitchInput = 0f;
            if (float.IsNaN(yawInput) || float.IsInfinity(yawInput)) yawInput = 0f;

            // DIRECT RESPONSIVE CONTROL - Reduced smoothing for instant feel
            float responsiveMultiplier = 2.5f; // 2.5x daha responsive
            
            // Direct pitch control with enhanced responsiveness - SAFE CALCULATION
            float effectiveTurnSpeed = GetEffectiveTurnSpeed();
            float pitchChange = pitchInput * effectiveTurnSpeed * deltaTime * responsiveMultiplier;
            if (!float.IsNaN(pitchChange) && !float.IsInfinity(pitchChange))
            {
                currentPitch += pitchChange;
                currentPitch = Mathf.Clamp(currentPitch, -90f, 90f);
            }

            // Direct yaw control with enhanced responsiveness - SAFE CALCULATION
            float yawChange = yawInput * effectiveTurnSpeed * deltaTime * responsiveMultiplier;
            if (!float.IsNaN(yawChange) && !float.IsInfinity(yawChange))
            {
                currentYaw += yawChange;
            }

            // Advanced banking system
            float finalBankAngle = CalculateAdvancedBanking();

            // FAST SMOOTH bank angle transition - prevent jumps
            float bankSmoothingMultiplier = 8f; // Much faster banking transition
            currentBankAngle = Mathf.Lerp(currentBankAngle, finalBankAngle, 
                flightProfile.bankSmoothing * bankSmoothingMultiplier * deltaTime);

            // Apply rotation - SAFE ROTATION
            Vector3 eulerAngles = new Vector3(currentPitch, currentYaw, currentBankAngle);
            
            // Final safety check before applying rotation
            if (!float.IsNaN(eulerAngles.x) && !float.IsNaN(eulerAngles.y) && !float.IsNaN(eulerAngles.z) &&
                !float.IsInfinity(eulerAngles.x) && !float.IsInfinity(eulerAngles.y) && !float.IsInfinity(eulerAngles.z))
            {
                transform.rotation = Quaternion.Euler(eulerAngles);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Invalid euler angles detected - resetting rotation");
                currentPitch = 0f;
                currentYaw = 0f;
                currentBankAngle = 0f;
                transform.rotation = Quaternion.identity;
            }
        }
        
        private float CalculateAdvancedBanking()
        {
            if (flightProfile == null) return 0f;
            
            // SMOOTH LINEAR BANKING SYSTEM - All inputs get smooth transitions
            
            float targetBanking = 0f;
            
            // 1. STRAFE BANKING - A/D keys (SMOOTH, no instant jump)
            if (Mathf.Abs(strafeInput) > 0.1f)
            {
                targetBanking = -strafeInput * flightProfile.maxBankAngle; // A = +max (sola bank), D = -max (sağa bank)
            }
            // 2. MOUSE POSITION BANKING - When no strafe input
            else
            {
                // Roll input is calculated from mouse screen position in DomeClashFlightController
                targetBanking = rollInput * flightProfile.maxBankAngle;
            }
            
            // 3. SPEED-BASED MULTIPLIER for banking intensity (subtle effect)
            float speedFactor = Mathf.Clamp01(currentSpeed / flightProfile.maxSpeed);
            float speedMultiplier = 0.9f + (speedFactor * 0.2f); // Range: 0.9 to 1.1 (subtle)
            targetBanking *= speedMultiplier;
            
            // 4. CLAMP to profile limits
            targetBanking = Mathf.Clamp(targetBanking, -flightProfile.maxBankAngle, flightProfile.maxBankAngle);

            // DEBUG: Smooth banking system (ALL INPUTS SMOOTH)
            if (Time.frameCount % 30 == 0)  // Every 0.5 seconds for debugging
            {
                string inputSource = Mathf.Abs(strafeInput) > 0.1f ? "STRAFE" : "MOUSE";
                Debug.Log($"SMOOTH BANKING DEBUG - Source: {inputSource} | RollInput: {rollInput:F3} | StrafeInput: {strafeInput:F3} | " +
                         $"TargetBanking: {targetBanking:F1}° | CurrentBank: {currentBankAngle:F1}° | SpeedMultiplier: {speedMultiplier:F2}");
            }

            return targetBanking;
        }
        
        // Input methods - called by flight controller
        public void SetPitchInput(float value) => pitchInput = Mathf.Clamp(value, -1f, 1f);
        public void SetYawInput(float value) => yawInput = Mathf.Clamp(value, -1f, 1f);
        public void SetRollInput(float value) => rollInput = Mathf.Clamp(value, -1f, 1f);
        public void SetStrafeInput(float value) => strafeInput = Mathf.Clamp(value, -1f, 1f);
        
        // Throttle control
        public void SetThrottle(float newThrottle) => throttle = Mathf.Clamp01(newThrottle);
        public void IncreaseThrottle(float amount = 0.1f) => throttle = Mathf.Clamp01(throttle + amount);
        public void DecreaseThrottle(float amount = 0.1f) => throttle = Mathf.Clamp01(throttle - amount);
        
        // Getters for debugging/UI
        public float GetPitchInput() => pitchInput;
        public float GetYawInput() => yawInput;
        public float GetRollInput() => rollInput;
        public float GetStrafeInput() => strafeInput;
        public float GetCurrentBankAngle() => currentBankAngle;
        public float GetCurrentPitch() => currentPitch;
        public float GetCurrentYaw() => currentYaw;
        public FlightProfile GetFlightProfile() => flightProfile;
        public float GetEffectiveFlightSpeedPublic() => GetEffectiveFlightSpeed();
        public float GetEffectiveTurnSpeedPublic() => GetEffectiveTurnSpeed();
        public bool IsUsingOverrideSettings() => useOverrideSettings;
        
        // Public method to change flight profile at runtime
        public void SetFlightProfile(FlightProfile newProfile)
        {
            flightProfile = newProfile;
            if (flightProfile != null)
            {
                ApplyFlightProfile();
            }
        }
        
        // Runtime profil değiştirme için public metodlar
        public void SwitchToRazorProfile() => SetFlightProfile(FlightProfile.CreateRazorProfile());
        public void SwitchToBastionProfile() => SetFlightProfile(FlightProfile.CreateBastionProfile());
        public void SwitchToBreacherProfile() => SetFlightProfile(FlightProfile.CreateBreacherProfile());
        public void SwitchToHavenProfile() => SetFlightProfile(FlightProfile.CreateHavenProfile());
        
        // Override settings
        public void SetOverrideSettings(bool enable, float flightSpeed = 100f, float turnSpeed = 60f)
        {
            useOverrideSettings = enable;
            overrideFlightSpeed = flightSpeed;
            overrideTurnSpeed = turnSpeed;
            Debug.Log($"Override settings {(enable ? "enabled" : "disabled")} for {gameObject.name}");
        }
    }
} 