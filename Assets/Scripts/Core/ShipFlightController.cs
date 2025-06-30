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
        
        // Input variables
        private float pitchInput = 0f;
        private float yawInput = 0f;
        private float rollInput = 0f;
        private float strafeInput = 0f;
        
        // Component references
        private PrototypeShip shipClass;
        
        public float CurrentSpeed => currentSpeed;
        public float Throttle => throttle;
        public Vector3 CurrentVelocity => currentVelocity;
        public float ActualSpeed => currentVelocity.magnitude; // Actual velocity magnitude for calculations
        
        // Add fields for altitude and vertical velocity
        private float lastAltitude = 0f;
        private float verticalVelocity = 0f;
        
        // Stall state variables
        [SerializeField] private bool isStalled = false;
        [SerializeField] private float smoothedStallControlMultiplier = 1f;
        [SerializeField] private float dynamicStallThreshold = 0f;
        private bool wasStalledLastFrame = false;
        
        // Store calculated terminal velocity
        private float terminalVelocity = 460f; // Default, will be recalculated
        
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
            
            // Starting speed and velocity
            float targetFlightSpeed = GetEffectiveFlightSpeed();
            currentSpeed = targetFlightSpeed * throttle;
            
            // Initialize velocity vector in the forward direction
            Vector3 flightDirection = CalculateFlightDirection();
            currentVelocity = flightDirection * currentSpeed;
            
            Debug.Log($"ShipFlightController initialized for {gameObject.name} with profile: {(flightProfile != null ? flightProfile.name : "None")}, " +
                     $"InitialSpeed: {currentSpeed:F1}m/s, InitialVelocity: {currentVelocity}");
        }
        
        private void Update()
        {
            // Handle input
            HandleMovementInput();
            
            // Update transform-based movement
            UpdateTransformMovement();

            // Track altitude and vertical velocity
            float currentAltitude = transform.position.y;
            verticalVelocity = (currentAltitude - lastAltitude) / Time.deltaTime;
            lastAltitude = currentAltitude;
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
            
            // Calculate dynamic stall threshold
            dynamicStallThreshold = CalculateDynamicStallThreshold();
            
            // --- MASS-BASED DRAG SYSTEM ---
            float baseDrag = flightProfile.baseDrag;
            float dragPerKg = flightProfile.dragPerKg;
            float drag = baseDrag + (flightProfile.mass * dragPerKg);
            
            // Speed-based drag system to achieve target characteristics:
            // - At 0 m/s: drag = 60 m/s² (for 60 m/s² deceleration at 0 thrust)
            // - At 460 m/s: drag = 90 m/s² (matches thrust for terminal velocity)
            // Calculate terminal velocity: thrust = drag * v_terminal => v_terminal = thrust / drag_at_terminal
            float dragAtTerminal = 90f; // Must match drag at terminal velocity
            terminalVelocity = flightProfile.engineThrust / (flightProfile.mass * dragAtTerminal); // Actually, use v = thrust / drag_at_terminal (drag in m/s²)
            terminalVelocity = 460f; // For now, hardcode to 460 for consistency with drag system
            
            float speedRatio = currentVelocity.magnitude / 460f; // Normalize to target max speed
            float speedBasedDrag = 60f + (speedRatio * 30f); // Linear increase from 60 to 90 m/s²
            speedBasedDrag = Mathf.Max(speedBasedDrag, 60f); // Don't go below 60 m/s²
            
            // --- THRUST SYSTEM - ALWAYS APPLIED REGARDLESS OF STALL STATE ---
            float thrust = flightProfile.engineThrust * throttle;
            
            // --- UNIFIED MOVEMENT SYSTEM - USE ACTUAL VELOCITY ---
            
            // Get current velocity magnitude
            float currentVelocityMagnitude = currentVelocity.magnitude;
            
            // Decompose current velocity into local axes
            Vector3 localVelocity = transform.InverseTransformDirection(currentVelocity);

            // --- FORWARD (Z) COMPONENT ---
            // Update forward speed using thrust, drag, and gravity only
            float forwardSpeed = localVelocity.z;
            // Apply drag to forward speed
            forwardSpeed -= speedBasedDrag * deltaTime * Mathf.Sign(forwardSpeed);
            // Apply thrust
            float thrustAcceleration = (flightProfile.engineThrust * throttle) / flightProfile.mass;
            forwardSpeed += thrustAcceleration * deltaTime;
            // Clamp to terminal velocity
            forwardSpeed = Mathf.Clamp(forwardSpeed, -terminalVelocity, terminalVelocity);

            // --- LATERAL (X) COMPONENT ---
            // Update lateral speed using strafe input, subject to drag and maneuverRate
            float lateralSpeed = localVelocity.x;
            float strafeAccel = flightProfile.maneuverRate * Mathf.Clamp(strafeInput, -1f, 1f); // Max strafe accel per second
            lateralSpeed += strafeAccel * deltaTime;
            // Apply drag to lateral speed
            float lateralDrag = speedBasedDrag * 0.7f; // Slightly less drag for lateral
            if (Mathf.Abs(lateralSpeed) > 0.01f)
                lateralSpeed -= lateralDrag * deltaTime * Mathf.Sign(lateralSpeed);
            // Clamp to max maneuver rate
            lateralSpeed = Mathf.Clamp(lateralSpeed, -flightProfile.maneuverRate, flightProfile.maneuverRate);

            // (Optional: clamp y if you want to restrict vertical movement)

            // Recombine to world space
            localVelocity.z = forwardSpeed;
            localVelocity.x = lateralSpeed;
            Vector3 totalVelocity = transform.TransformDirection(localVelocity);
            
            // Check for stall state (for control responsiveness only)
            bool wasStalled = isStalled;
            isStalled = currentVelocityMagnitude < dynamicStallThreshold;
            
            // Calculate stall control multiplier for responsiveness
            smoothedStallControlMultiplier = CalculateStallControlMultiplier();
            
            // Calculate movement direction
            Vector3 flightDirection = CalculateFlightDirection();
            
            // Create new velocity vector based on flight direction and magnitude
            Vector3 newVelocity = flightDirection * currentVelocityMagnitude;
            
            // Strafe movement (reduced responsiveness when stalled)
            Vector3 strafeVelocity = Vector3.zero;
            if (Mathf.Abs(strafeInput) > 0.05f)
            {
                float effectiveStrafeInput = strafeInput * smoothedStallControlMultiplier;
                Vector3 horizontalRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
                strafeVelocity = horizontalRight * effectiveStrafeInput * flightProfile.maneuverRate;
            }
            // Cap the lateral (strafe) velocity to maneuverRate
            float lateralSpeedCap = Vector3.Dot(strafeVelocity, transform.right);
            if (Mathf.Abs(lateralSpeedCap) > flightProfile.maneuverRate)
            {
                strafeVelocity = transform.right * Mathf.Sign(lateralSpeedCap) * flightProfile.maneuverRate;
            }
            
            // Combine forward and strafe velocities
            Vector3 totalVelocityFinal = newVelocity + strafeVelocity;

            // Decompose into local axes
            Vector3 localVelocityFinal = transform.InverseTransformDirection(totalVelocityFinal);
            // Ellipse limiting: (z/terminalVelocity)^2 + (x/maneuverRate)^2 <= 1
            float vF = localVelocityFinal.z;
            float vS = localVelocityFinal.x;
            float vFmax = terminalVelocity;
            float vSmax = flightProfile.maneuverRate;
            float ellipse = (vF * vF) / (vFmax * vFmax) + (vS * vS) / (vSmax * vSmax);
            if (ellipse > 1f && (vFmax > 0f && vSmax > 0f))
            {
                float scale = 1f / Mathf.Sqrt(ellipse);
                vF *= scale;
                vS *= scale;
                localVelocityFinal.z = vF;
                localVelocityFinal.x = vS;
            }
            // Clamp for safety
            localVelocityFinal.z = Mathf.Clamp(localVelocityFinal.z, -vFmax, vFmax);
            localVelocityFinal.x = Mathf.Clamp(localVelocityFinal.x, -vSmax, vSmax);
            totalVelocityFinal = transform.TransformDirection(localVelocityFinal);
            
            // Cap the total velocity magnitude to terminal velocity
            float maxSpeed = terminalVelocity;
            if (totalVelocityFinal.magnitude > maxSpeed)
            {
                totalVelocityFinal = totalVelocityFinal.normalized * maxSpeed;
            }
            
            // Add gravity effect - ship should fall when not moving forward or when stalled
            float gravity = 9.81f; // Standard gravity
            Vector3 gravityVelocity = Vector3.down * gravity * deltaTime;
            
            // Only apply gravity if ship is stalled or moving very slowly
            if (isStalled || currentVelocityMagnitude < 5f)
            {
                totalVelocityFinal += gravityVelocity;
            }
            
            // Apply movement
            Vector3 movement = totalVelocityFinal * deltaTime;
            
            // Debug logging for thrust and stall state
            if (Time.frameCount % 60 == 0) // Log every 60 frames (1 second at 60fps)
            {
                Debug.Log($"{gameObject.name}: Thrust={thrust:F0}N, Throttle={throttle:F2}, " +
                         $"EngineThrust={flightProfile.engineThrust:F0}N, Mass={flightProfile.mass:F0}kg, " +
                         $"BaseDrag={drag:F1}m/s², SpeedBasedDrag={speedBasedDrag:F1}m/s², " +
                         $"ThrustAccel={thrustAcceleration:F1}m/s², VelocityIncrease={thrustAcceleration * deltaTime:F1}m/s, " +
                         $"CurrentSpeed={currentSpeed:F1}m/s, ActualSpeed={ActualSpeed:F1}m/s, " +
                         $"StallThreshold={dynamicStallThreshold:F1}m/s, IsStalled={isStalled}, " +
                         $"VelocityMagnitude={currentVelocityMagnitude:F1}m/s, Gravity={gravity:F1}m/s², Movement={movement}");
            }
            
            if (!float.IsNaN(movement.x) && !float.IsNaN(movement.y) && !float.IsNaN(movement.z) &&
                !float.IsInfinity(movement.x) && !float.IsInfinity(movement.y) && !float.IsInfinity(movement.z))
            {
                transform.position += movement;
                currentVelocity = totalVelocityFinal;
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Invalid movement vector detected - skipping movement this frame");
                currentVelocity = Vector3.zero;
            }
            
            // Stall recovery - simple speed-based recovery
            if (isStalled && currentVelocityMagnitude >= dynamicStallThreshold * 1.1f) // 10% buffer above threshold
            {
                isStalled = false;
            }
            
            wasStalledLastFrame = isStalled;

            // CONTROL SYSTEM - Apply stall control reduction
            float responsiveMultiplier = 2.5f;
            float effectiveTurnSpeed = GetEffectiveTurnSpeed() * smoothedStallControlMultiplier;
            float pitchChange = pitchInput * effectiveTurnSpeed * deltaTime * responsiveMultiplier;
            if (!float.IsNaN(pitchChange) && !float.IsInfinity(pitchChange))
            {
                currentPitch += pitchChange;
                currentPitch = Mathf.Clamp(currentPitch, -89f, 89f);
            }
            float yawChange = yawInput * effectiveTurnSpeed * deltaTime * responsiveMultiplier;
            if (!float.IsNaN(yawChange) && !float.IsInfinity(yawChange))
            {
                currentYaw += yawChange;
            }
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
            float speedFactor = flightProfile != null ? Mathf.Clamp01((ActualSpeed - flightProfile.minSpeed) / (flightProfile.maxSpeed - flightProfile.minSpeed)) : 1f;
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
        
        // Calculate dynamic stall threshold based on terminal velocity, not static maxSpeed
        private float CalculateDynamicStallThreshold()
        {
            if (flightProfile == null) return 15f;
            float baseStallThreshold = terminalVelocity * 0.25f; // 25% of actual terminal velocity
            float pitchAngle = Mathf.Abs(currentPitch);
            float pitchMultiplier = 1f + (pitchAngle / 90f) * 0.3f;
            float dynamicThreshold = baseStallThreshold * pitchMultiplier;
            return dynamicThreshold;
        }
        
        // Calculate minimum thrust required to maintain altitude at current pitch and mass
        private float CalculateMinimumThrustRequired()
        {
            if (flightProfile == null) return 0f;
            
            // Base gravity force
            float gravityForce = flightProfile.mass * 9.81f;
            
            // Pitch affects how much thrust is needed to counteract gravity
            float pitchAngle = Mathf.Abs(currentPitch);
            float pitchMultiplier = 1f + (pitchAngle / 90f); // 0° = 1.0, 90° = 2.0
            
            // Minimum thrust needed to maintain altitude
            float minimumThrust = gravityForce * pitchMultiplier;
            
            return minimumThrust;
        }
        
        // Calculate stall control reduction based on speed ratio
        private float CalculateStallControlMultiplier()
        {
            if (dynamicStallThreshold <= 0f) return 1f;
            
            // Calculate speed ratio (current speed vs stall threshold)
            float speedRatio = currentSpeed / dynamicStallThreshold;
            
            // Exponential reduction when below stall threshold
            if (speedRatio < 1f)
            {
                // Exponential curve: more dramatic reduction at lower speeds
                float controlMultiplier = Mathf.Pow(speedRatio, 2f);
                return Mathf.Clamp01(controlMultiplier);
            }
            
            return 1f; // Full control when above stall threshold
        }
        
        // Input methods - called by flight controller
        public void SetPitchInput(float value) => pitchInput = Mathf.Clamp(value, -1f, 1f);
        public void SetYawInput(float value) => yawInput = Mathf.Clamp(value, -1f, 1f);
        public void SetRollInput(float value) => rollInput = Mathf.Clamp(value, -1f, 1f);
        public void SetStrafeInput(float value) => strafeInput = Mathf.Clamp(value, -1f, 1f);
        
        // Throttle control
        public void SetThrottle(float newThrottle) 
        { 
            float oldThrottle = throttle;
            throttle = Mathf.Clamp01(newThrottle);
            if (Mathf.Abs(oldThrottle - throttle) > 0.01f)
            {
                Debug.Log($"{gameObject.name}: SetThrottle called - {oldThrottle:F2} -> {throttle:F2}");
            }
        }
        public void IncreaseThrottle(float amount = 0.1f) 
        { 
            float oldThrottle = throttle;
            throttle = Mathf.Clamp01(throttle + amount);
            if (Mathf.Abs(oldThrottle - throttle) > 0.01f)
            {
                Debug.Log($"{gameObject.name}: IncreaseThrottle called - {oldThrottle:F2} -> {throttle:F2} (amount: {amount:F2})");
            }
        }
        public void DecreaseThrottle(float amount = 0.1f) 
        { 
            float oldThrottle = throttle;
            throttle = Mathf.Clamp01(throttle - amount);
            if (Mathf.Abs(oldThrottle - throttle) > 0.01f)
            {
                Debug.Log($"{gameObject.name}: DecreaseThrottle called - {oldThrottle:F2} -> {throttle:F2} (amount: {amount:F2})");
            }
        }
        
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
        public bool IsStalled() => isStalled;
        public float GetStallControlMultiplier() => smoothedStallControlMultiplier;
        public float GetDynamicStallThreshold() => dynamicStallThreshold;
        public float GetMinimumThrustRequired() => CalculateMinimumThrustRequired();
        public float GetActualSpeed() => ActualSpeed;
        
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