using UnityEngine;
using DomeClash.Ships;

namespace DomeClash.Core
{
    /// <summary>
    /// Ship Flight Controller - Transform-based flight movement system
    /// Handles all ship movement, banking, and flight physics
    /// NO PHYSICS - Direct transform control for responsive arcade flight
    /// </summary>
    public class ShipFlightController : MonoBehaviour
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
        
        // Banking system
        private float currentBankInput = 0f;
        
        // Input variables
        private float pitchInput = 0f;
        private float yawInput = 0f;
        private float rollInput = 0f;
        private float strafeInput = 0f;
        private float thrustInput = 0f;
        
        // Component references
        private PrototypeShip shipClass;
        
        public float CurrentSpeed => currentSpeed;
        public float Throttle => throttle;
        public Vector3 CurrentVelocity => currentVelocity;
        
        private void Awake()
        {
            shipClass = GetComponent<PrototypeShip>();
            
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
            
            Debug.Log($"ShipFlightController initialized for {gameObject.name} with profile: {(flightProfile != null ? flightProfile.name : "None")}");
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
            // Get the ship class component to create a dynamic profile
            if (shipClass != null)
            {
                flightProfile = FlightProfile.CreateFromShip(shipClass);
                Debug.Log($"Auto-created dynamic flight profile for {gameObject.name} from ship stats");
            }
            else
            {
                // Fallback to default profile if no ship class found
                flightProfile = FlightProfile.CreateDefaultProfile();
                Debug.Log($"Auto-assigned default flight profile to {gameObject.name} (no ship class found)");
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
            // W key thrust input removed - now handled by PrototypeShip throttle system
            // if (Input.GetKey(KeyCode.W))
            //     thrustInput = 1f;
            // else
            //     thrustInput = 0f;

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
            
            float deltaTime = Time.deltaTime;
            
            // Speed control - throttle + thrust input
            float effectiveFlightSpeed = GetEffectiveFlightSpeed();
            float targetSpeed = effectiveFlightSpeed * throttle;
            if (thrustInput > 0f)
            {
                float maxSpeed = flightProfile != null ? flightProfile.maxSpeed : effectiveFlightSpeed * 1.5f;
                targetSpeed = Mathf.Lerp(targetSpeed, maxSpeed, thrustInput * 0.5f);
            }

            // GRAVITY SYSTEM - affects speed based on pitch and mass
            float gravityEffect = CalculateGravityEffect(deltaTime);
            targetSpeed += gravityEffect;

            // Calculate terminal velocity based on mass
            float terminalVelocity = CalculateTerminalVelocity();
            
            // Smooth speed changes
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, flightProfile.speedSmoothing * deltaTime);
            
            // STALL MECHANICS - check if ship is stalled
            // Use ship-specific stall threshold instead of minSpeed
            bool isStalled = currentSpeed < flightProfile.stallThreshold;
            
            // RECOVERY SYSTEM - ship can recover if speed > threshold AND aligned with flight direction
            if (isStalled && currentSpeed >= flightProfile.stallThreshold)
            {
                // Check if ship is aligned with its flight direction
                if (IsShipAlignedWithFlightDirection())
                {
                    isStalled = false; // Ship recovers from stall
                    Debug.Log($"{gameObject.name}: Recovered from stall - speed: {currentSpeed:F1}, aligned: true");
                }
                else
                {
                    // Ship has speed but not aligned - keep stalling
                    isStalled = true;
                    Debug.Log($"{gameObject.name}: Still stalled - speed: {currentSpeed:F1}, not aligned with flight direction");
                }
            }
            
            // Clamp to terminal velocity (can exceed maxSpeed due to gravity)
            currentSpeed = Mathf.Clamp(currentSpeed, 0f, terminalVelocity);

            // Calculate flight direction - includes pitch but excludes roll effect
            Vector3 flightDirection = CalculateFlightDirection();

            // MOMENTUM-BASED STALL MECHANICS
            Vector3 forwardMovement;
            if (isStalled)
            {
                // When stalled, gravity pulls ship down while maintaining forward momentum
                forwardMovement = CalculateStallMovement(deltaTime);
            }
            else
            {
                // Normal flight movement
                forwardMovement = flightDirection * currentSpeed * deltaTime;
            }

            // Strafe movement - horizontal only
            Vector3 horizontalRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            Vector3 strafeMovement = Vector3.zero;
            if (Mathf.Abs(strafeInput) > 0.05f)
            {
                strafeMovement = horizontalRight * strafeInput * flightProfile.strafeSpeed * deltaTime;
            }

            // Apply movement - SAFE POSITION UPDATE
            Vector3 totalMovement = forwardMovement + strafeMovement;
            
            // Safety check for NaN positions
            if (!float.IsNaN(totalMovement.x) && !float.IsNaN(totalMovement.y) && !float.IsNaN(totalMovement.z) &&
                !float.IsInfinity(totalMovement.x) && !float.IsInfinity(totalMovement.y) && !float.IsInfinity(totalMovement.z))
            {
                transform.position += totalMovement;
                currentVelocity = totalMovement / deltaTime;
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Invalid movement vector detected - skipping movement this frame");
                currentVelocity = Vector3.zero;
            }

            // DIRECT RESPONSIVE CONTROL - Reduced smoothing for instant feel
            float responsiveMultiplier = 2.5f; // 2.5x more responsive

            // Direct pitch control with enhanced responsiveness - SAFE CALCULATION
            float effectiveTurnSpeed = GetEffectiveTurnSpeed();
            float pitchChange = pitchInput * effectiveTurnSpeed * deltaTime * responsiveMultiplier;
            if (!float.IsNaN(pitchChange) && !float.IsInfinity(pitchChange))
            {
                currentPitch += pitchChange;
                currentPitch = Mathf.Clamp(currentPitch, -89f, 89f);
            }

            // Direct yaw control with enhanced responsiveness - SAFE CALCULATION
            float yawChange = yawInput * effectiveTurnSpeed * deltaTime * responsiveMultiplier;
            if (!float.IsNaN(yawChange) && !float.IsInfinity(yawChange))
            {
                currentYaw += yawChange;
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
            // SAFETY CHECK - Prevent NaN values
            if (float.IsNaN(currentPitch) || float.IsInfinity(currentPitch)) currentPitch = 0f;
            if (float.IsNaN(currentYaw) || float.IsInfinity(currentYaw)) currentYaw = 0f;
            if (float.IsNaN(pitchInput) || float.IsInfinity(pitchInput)) pitchInput = 0f;
            if (float.IsNaN(yawInput) || float.IsInfinity(yawInput)) yawInput = 0f;

            // Determine target bank input instantly
            float targetBankInput = 0f;
            if (Mathf.Abs(strafeInput) > 0.05f)
            {
                // Check if ship is turning in the same direction as strafe
                if (Mathf.Abs(yawInput) > 0.05f && Mathf.Sign(yawInput) == Mathf.Sign(strafeInput))
                {
                    // Strafe + turn in same direction: blend between strafe banking and turn banking
                    float strafeBanking = Mathf.Sign(strafeInput);
                    float turnBanking = -yawInput; // Opposite of turn direction for correct banking
                    float turnInfluence = Mathf.Abs(yawInput); // How much turning affects the blend
                    
                    // Blend: more turning = more turn-based banking, less strafe banking
                    targetBankInput = Mathf.Lerp(strafeBanking, turnBanking, turnInfluence);
                }
                else
                {
                    // Normal strafe banking (no turning or turning in opposite direction)
                    targetBankInput = Mathf.Sign(strafeInput);
                }
            }
            else
            {
                // No strafe input: use pure turn-based banking
                targetBankInput = -yawInput; // Opposite of turn direction for correct banking
            }
            targetBankInput = Mathf.Clamp(targetBankInput, -1f, 1f);

            // Calculate target bank angle using speed-based banking
            float minBankAngle = 35f;
            float maxBankAngle = flightProfile != null ? flightProfile.maxBankAngle : 60f;
            float speedFactor = flightProfile != null ? Mathf.Clamp01((currentSpeed - flightProfile.minSpeed) / (flightProfile.maxSpeed - flightProfile.minSpeed)) : 1f;
            float speedBasedBankAngle = Mathf.Lerp(minBankAngle, maxBankAngle, speedFactor);
            float targetBankAngle = targetBankInput * speedBasedBankAngle;

            // Smooth only the bank angle, not the input
            float bankSmoothing = flightProfile != null ? flightProfile.bankSmoothing : 8f;
            currentBankAngle = Mathf.Lerp(currentBankAngle, targetBankAngle, bankSmoothing * Time.deltaTime);

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
        
        // Calculate gravity effect on speed based on pitch and mass
        private float CalculateGravityEffect(float deltaTime)
        {
            if (flightProfile == null) return 0f;
            
            // Convert pitch to radians for calculations
            float pitchRadians = currentPitch * Mathf.Deg2Rad;
            
            // Calculate vertical component of flight direction
            float verticalComponent = Mathf.Sin(pitchRadians);
            
            // Gravity strength based on mass (heavier ships = stronger gravity effect)
            // Reduced for better observation of recovery system
            float gravityStrength = flightProfile.mass * 0.5f; // Much weaker gravity for testing
            
            // STALL MECHANICS - enhance gravity effects when stalled
            // When stalled, gravity effects are much stronger for dramatic fall
            float stallMultiplier = 1f;
            if (currentSpeed < flightProfile.stallThreshold)
            {
                stallMultiplier = 2f; // Reduced from 3x to 2x for testing
            }
            
            // Gravity effect: negative when pitching up (decelerating), positive when pitching down (accelerating)
            float gravityEffect = verticalComponent * gravityStrength * stallMultiplier * deltaTime;
            
            return gravityEffect;
        }
        
        // Calculate terminal velocity based on mass
        private float CalculateTerminalVelocity()
        {
            if (flightProfile == null) return flightProfile.maxSpeed;
            
            // Terminal velocity increases with mass (heavier ships can go faster when diving)
            // Much higher terminal velocity for dramatic falls
            float baseTerminalVelocity = flightProfile.maxSpeed * 3.0f; // Increased from 1.5x to 3x
            float massMultiplier = 1f + (flightProfile.mass / 500f); // Heavier ships get even higher terminal velocity
            
            return baseTerminalVelocity * massMultiplier;
        }
        
        // Calculate momentum-based stall movement with gravity-driven fall
        private Vector3 CalculateStallMovement(float deltaTime)
        {
            if (flightProfile == null) return Vector3.zero;
            
            // Calculate how much forward momentum we should maintain
            float forwardMomentum = currentSpeed; // Current speed represents forward momentum
            
            // Get the ship's current forward direction (last known flight direction)
            Vector3 flightDirection = CalculateFlightDirection();
            
            // Calculate horizontal component (forward momentum)
            Vector3 horizontalMovement = flightDirection * forwardMomentum * deltaTime;
            
            // Calculate vertical fall using gravity (no separate fall system)
            // Gravity pulls ship down when stalled
            float gravityFallSpeed = flightProfile.mass * 0.5f * 2f * deltaTime; // Reduced gravity for testing
            Vector3 verticalMovement = Vector3.down * gravityFallSpeed;
            
            // Combine horizontal momentum with vertical fall
            Vector3 stallMovement = horizontalMovement + verticalMovement;
            
            return stallMovement;
        }
        
        // Check if ship is aligned with its flight direction for recovery
        private bool IsShipAlignedWithFlightDirection()
        {
            if (flightProfile == null) return false;
            
            // Get ship's current forward direction
            Vector3 shipForward = transform.forward;
            
            // Get flight direction (where ship should be pointing)
            Vector3 flightDirection = CalculateFlightDirection();
            
            // Calculate alignment angle (how much ship is pointing in flight direction)
            float alignmentAngle = Vector3.Angle(shipForward, flightDirection);
            
            // Ship is aligned if pointing within 30 degrees of flight direction
            float maxAlignmentAngle = 30f;
            
            return alignmentAngle <= maxAlignmentAngle;
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
        public bool IsStalled() => currentSpeed < (flightProfile?.stallThreshold ?? 0f);
        public float GetCurrentFallVelocity() => flightProfile?.mass * 0.5f * 2f ?? 0f; // Current gravity fall speed for debugging
        public float GetStallThreshold() => flightProfile?.stallThreshold ?? 0f; // Ship's stall speed threshold
        public bool IsAlignedWithFlightDirection() => IsShipAlignedWithFlightDirection(); // Flight direction alignment for recovery
        
        // Public method to change flight profile at runtime
        public void SetFlightProfile(FlightProfile newProfile)
        {
            flightProfile = newProfile;
            if (flightProfile != null)
            {
                ApplyFlightProfile();
            }
        }
        
        // Profile switching methods - now use dynamic approach
        public void SwitchToRazorProfile() 
        { 
            if (shipClass != null)
            {
                // Temporarily modify ship stats to Razor-like characteristics
                var originalStats = shipClass.stats;
                shipClass.stats.maxSpeed = 160f;
                shipClass.stats.turnRate = 60f;
                shipClass.stats.mass = 220f;
                shipClass.stats.strafeSpeed = 35f;
                
                SetFlightProfile(FlightProfile.CreateFromShip(shipClass));
                
                // Restore original stats
                shipClass.stats = originalStats;
            }
        }
        
        public void SwitchToBastionProfile() 
        { 
            if (shipClass != null)
            {
                // Temporarily modify ship stats to Bastion-like characteristics
                var originalStats = shipClass.stats;
                shipClass.stats.maxSpeed = 105f;
                shipClass.stats.turnRate = 40f;
                shipClass.stats.mass = 490f;
                shipClass.stats.strafeSpeed = 15f;
                
                SetFlightProfile(FlightProfile.CreateFromShip(shipClass));
                
                // Restore original stats
                shipClass.stats = originalStats;
            }
        }
        
        public void SwitchToBreacherProfile() 
        { 
            if (shipClass != null)
            {
                // Temporarily modify ship stats to Breacher-like characteristics
                var originalStats = shipClass.stats;
                shipClass.stats.maxSpeed = 135f;
                shipClass.stats.turnRate = 50f;
                shipClass.stats.mass = 360f;
                shipClass.stats.strafeSpeed = 22f;
                
                SetFlightProfile(FlightProfile.CreateFromShip(shipClass));
                
                // Restore original stats
                shipClass.stats = originalStats;
            }
        }
        
        public void SwitchToHavenProfile() 
        { 
            if (shipClass != null)
            {
                // Temporarily modify ship stats to Haven-like characteristics
                var originalStats = shipClass.stats;
                shipClass.stats.maxSpeed = 125f;
                shipClass.stats.turnRate = 45f;
                shipClass.stats.mass = 250f;
                shipClass.stats.strafeSpeed = 28f;
                
                SetFlightProfile(FlightProfile.CreateFromShip(shipClass));
                
                // Restore original stats
                shipClass.stats = originalStats;
            }
        }
        
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