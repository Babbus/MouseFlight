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
        [SerializeField] private bool autoFindProfile = true;
        
        [Header("Item Manager Integration")]
        [SerializeField] private ItemManager itemManager;
        [SerializeField] private bool useItemManager = true;
        
        [Header("Current Movement State")]
        [SerializeField] private float currentSpeed = 0f;
        [SerializeField] private float throttle = 0.8f;
        [SerializeField] private Vector3 currentVelocity = Vector3.zero;
        
        [Header("Profile Override (Optional)")]
        [SerializeField] private bool useOverrideSettings = false;
        [SerializeField] private float overrideFlightSpeed = 100f;
        [SerializeField] private float overrideTurnSpeed = 80f;  // Increased from 60f for better override turning
        
        [Header("Banking System")]
        [SerializeField] private float currentBankAngle = 0f;
        [SerializeField] private float currentPitch = 0f;
        [SerializeField] private float currentYaw = 0f;
        
        [Header("Stall & Gravity System")]
        [SerializeField] private float universalGravity = 30f;
        [SerializeField] private bool isStalled = false;
        [SerializeField] private float smoothedStallControlMultiplier = 1f;
        [SerializeField] private float dynamicStallThreshold = 0f;
        
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
        public float ForwardSpeed { get; private set; }
        
        [Header("Mouse Flight Input")] 
        [SerializeField] public bool systemEnabled = true;
        [SerializeField] private bool enableInputConversion = true;
        [SerializeField] private float maxPitchAngle = 15f;
        [SerializeField] private float maxYawAngle = 20f;
        [SerializeField] private float mouseResponsiveness = 0.8f;
        [SerializeField] private bool instantInputMode = true;
        [SerializeField] private float doubleTapWindow = 0.3f;

        private float currentStrafeInput = 0f;
        private float targetStrafeInput = 0f;
        private bool isDodging = false;
        private float lastAPress = 0f, lastDPress = 0f, lastSPress = 0f;
        private Vector2 mouseInput = Vector2.zero, targetMouseInput = Vector2.zero, lastMouseInput = Vector2.zero;
        private Vector2 lastMouseScreenPosition = Vector2.zero;
        private Transform mouseAim = null;
        private Transform aircraft = null;

        private float lastBankInput = 0f;  // For smooth bank transitions
        private bool isBraking = false;
        private float stallInfluence = 0f;

        public enum DodgeDirection { Left, Right, Back }

        public Vector3 BoresightPos => aircraft == null ? transform.forward * 1000f : (aircraft.transform.forward * 1000f) + aircraft.transform.position;
        public bool IsUsingEnhancedControl => true;
        public Vector3 MouseAimPos {
            get {
                if (aircraft != null) {
                    try {
                        Vector3 aircraftForward = aircraft.forward;
                        Vector3 aircraftUp = aircraft.up;
                        Vector3 pitchAxis = Camera.main != null ? Camera.main.transform.right : Vector3.right;
                        Quaternion yawOffset = Quaternion.AngleAxis(mouseInput.x, aircraftUp);
                        Quaternion pitchOffset = Quaternion.AngleAxis(mouseInput.y, pitchAxis);
                        Vector3 aimDirection = yawOffset * pitchOffset * aircraftForward;
                        return aircraft.position + (aimDirection * 1000f);
                    } catch { return transform.position + (transform.forward * 1000f); }
                } else {
                    return transform.position + (transform.forward * 1000f);
                }
            }
        }

        private void Awake()
        {
            shipClass = GetComponent<PrototypeShip>();
            
            // Initialize rotation values from current transform
            Vector3 currentEuler = transform.eulerAngles;
            currentPitch = currentEuler.x;
            currentYaw = currentEuler.y;
            currentBankAngle = currentEuler.z;

            FindMissingReferences();
            ResetMouseAim();
            
            // Find ItemManager if not assigned
            if (itemManager == null)
            {
                itemManager = GetComponent<ItemManager>();
            }
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
            
            // Initialize with default speed
            if (flightProfile != null)
            {
                currentSpeed = flightProfile.maxSpeed * throttle;
                currentVelocity = transform.forward * currentSpeed;
            }
        }
        
        private void Update()
        {
            if (!systemEnabled) return;
            HandleInput();
            ProcessProgressiveStrafeInput();
            ConvertMouseInputToShipControl_Enhanced();
            
            // Update transform-based movement
            UpdateTransformMovement();
        }
        
        private void ApplyFlightProfile()
        {
            if (flightProfile == null) return;
            
            // Initialize with profile values
            throttle = 0.9f; // Higher default throttle for better initial movement
        }
        
        /// <summary>
        /// Automatic flight profile finding - based on ship names
        /// </summary>
        private void AutoFindFlightProfile()
        {
            // Get the ship class component to create a dynamic profile
            if (shipClass != null)
            {
                flightProfile = FlightProfile.CreateFromShip(shipClass);
            }
            else
            {
                // Fallback to default profile if no ship class found
                flightProfile = FlightProfile.CreateDefaultProfile();
            }
        }
        
        /// <summary>
        /// Effective flight speed - use override if available
        /// </summary>
        private float GetEffectiveFlightSpeed()
        {
            if (useOverrideSettings) return overrideFlightSpeed;
            
            // Use ItemManager if available and enabled
            if (useItemManager && itemManager != null)
            {
                var stats = itemManager.GetShipStatistics();
                return stats.flightSpeed > 0f ? stats.flightSpeed : 100f;
            }
            
            return flightProfile != null ? flightProfile.flightSpeed : 100f;
        }
        
        /// <summary>
        /// Effective turn speed - use override if available
        /// </summary>
        private float GetEffectiveTurnSpeed()
        {
            if (useOverrideSettings) return overrideTurnSpeed;
            
            // Use ItemManager if available and enabled
            if (useItemManager && itemManager != null)
            {
                var stats = itemManager.GetShipStatistics();
                if (stats.maxSpeed <= 0) return 60f; // Fallback

                // Dynamic turn speed calculation using ItemManager stats
                float itemManagerMassFactor = Mathf.Max(0.1f, stats.mass / 100f);
                float itemManagerBaseTurnRate = stats.turnRate / itemManagerMassFactor;
                float itemManagerSpeedRatio = currentVelocity.magnitude / stats.maxSpeed;
                
                // Ensure a minimum turn rate even at zero speed to allow for recovery.
                float itemManagerMinimumTurnRatio = 0.25f;
                itemManagerSpeedRatio = Mathf.Max(itemManagerMinimumTurnRatio, itemManagerSpeedRatio);

                return itemManagerBaseTurnRate * itemManagerSpeedRatio;
            }
            
            if (flightProfile == null || flightProfile.maxSpeed <= 0) return 60f; // Fallback

            // Dynamic turn speed calculation.
            // Slower and heavier ships turn slower, but with improved responsiveness.
            // Turn rate is proportional to current speed to prevent instant stalls.
            float massFactor = Mathf.Max(0.1f, flightProfile.mass / 100f); // Reduced mass impact for better turning (was mass directly)
            float baseTurnRate = flightProfile.turnSpeed / massFactor;
            float speedRatio = currentVelocity.magnitude / flightProfile.maxSpeed;
            
            // Ensure a minimum turn rate even at zero speed to allow for recovery.
            float minimumTurnRatio = 0.25f; // Increased from 0.15f for better low-speed turning
            speedRatio = Mathf.Max(minimumTurnRatio, speedRatio);

            return baseTurnRate * speedRatio;
        }
        
        private void UpdateTransformMovement()
        {
            // Use ItemManager if available and enabled
            if (useItemManager && itemManager != null)
            {
                var stats = itemManager.GetShipStatistics();
                if (stats == null) return;
                
                // Update flight profile with current stats
                UpdateFromItemManager(stats);
            }
            else if (flightProfile == null) return;
            
            float deltaTime = Time.deltaTime;

            // --- STATE CALCULATION ---
            dynamicStallThreshold = CalculateStallThreshold();
            
            Vector3 forwardVelocity = Vector3.Project(currentVelocity, transform.forward);
            this.ForwardSpeed = Vector3.Dot(forwardVelocity, transform.forward);

            float transitionRange = flightProfile.maxSpeed * 0.2f; // e.g., 20% of max speed
            float stallBeginThreshold = dynamicStallThreshold + (transitionRange * 0.5f);
            float stallEndThreshold = dynamicStallThreshold - (transitionRange * 0.5f);
            
            stallInfluence = 1f - Mathf.Clamp01(Mathf.InverseLerp(stallEndThreshold, stallBeginThreshold, this.ForwardSpeed));

            isStalled = stallInfluence > 0.01f;
            smoothedStallControlMultiplier = CalculateStallControlMultiplier();
            
            // --- UNIFIED GRAVITY & FALL (Always On) ---
            Vector3 gravityDirection = Vector3.down;
            float gravityMultiplier = 1f + (2f * stallInfluence);
            float gravityMagnitude = universalGravity * gravityMultiplier;
            currentVelocity += gravityDirection * gravityMagnitude * deltaTime;

            // --- FORWARD THRUST ---
            Vector3 thrustForce = transform.forward * flightProfile.acceleration * throttle;
            currentVelocity += thrustForce * deltaTime;

            Vector3 strafeVelocity = Vector3.zero;

            if (isStalled)
            {
                // --- STALL FLIGHT DYNAMICS ---
                float pitchAngle = currentPitch;
                float forwardMomentumFactor = Mathf.InverseLerp(-90f, 90f, pitchAngle);
                Vector3 fallDirection = Vector3.down;
                Vector3 glideDirection = transform.forward;
                Vector3 blendedDirection = Vector3.Slerp(fallDirection, glideDirection, forwardMomentumFactor).normalized;
                
                float stallSpeedLimitFactor = Mathf.Lerp(0.1f, 1.0f, forwardMomentumFactor);
                float targetSpeed = flightProfile.maxSpeed * stallSpeedLimitFactor;

                Vector3 targetVelocity = blendedDirection * targetSpeed;
                currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, flightProfile.acceleration * 2f * deltaTime);
            }
            else
            {
                // --- NORMAL FLIGHT DYNAMICS ---
                // --- DRAG ---
                float maxDragForce = flightProfile.acceleration * 0.25f;
                float throttleInfluence = 1.0f - throttle;
                float currentDrag = maxDragForce * throttleInfluence;
                
                if (currentVelocity.sqrMagnitude > 0.1f)
                {
                    Vector3 dragForce = -currentVelocity.normalized * currentDrag;
                    currentVelocity += dragForce * Time.deltaTime;
                }

                // --- ACTIVE BRAKING ---
                if (isBraking)
                {
                    float brakePower = flightProfile.acceleration * 1.5f;
                    Vector3 forwardVelocityComponent = Vector3.Project(currentVelocity, transform.forward);
                    
                    if (Vector3.Dot(forwardVelocityComponent, transform.forward) > 0)
                    {
                        Vector3 brakeForce = -forwardVelocityComponent.normalized * brakePower;
                        currentVelocity += brakeForce * Time.deltaTime;
                        
                        Vector3 finalForwardVelocity = Vector3.Project(currentVelocity, transform.forward);
                        if (Vector3.Dot(finalForwardVelocity, transform.forward) < 0)
                        {
                            currentVelocity -= finalForwardVelocity;
                        }
                    }
                }
                
                // --- DRIFT CORRECTION ---
                float originalSpeed = currentVelocity.magnitude;
                Vector3 forwardVel = Vector3.Project(currentVelocity, transform.forward);
                Vector3 sidewaysVel = currentVelocity - forwardVel;
                float driftCorrectionSpeedCost = sidewaysVel.magnitude;
                float correctionFactor = 0.05f;
                Vector3 correctedSidewaysVel = Vector3.Lerp(sidewaysVel, Vector3.zero, GetEffectiveTurnSpeed() * correctionFactor * deltaTime);
                currentVelocity = forwardVel + correctedSidewaysVel;
                
                if (currentVelocity.sqrMagnitude > 0.01f) {
                    currentVelocity = currentVelocity.normalized * originalSpeed;
                }

                // --- STRAFE ---
                float totalManeuverBudget = flightProfile.maneuverRate;
                float availableStrafeBudget = Mathf.Max(0, totalManeuverBudget - driftCorrectionSpeedCost);
                Vector3 strafeDirection = transform.right;
                strafeDirection.y = 0;
                strafeDirection.Normalize();

                // Reduce strafe as pitch increases (cosine falloff)
                float pitchRadians = currentPitch * Mathf.Deg2Rad;
                float strafePitchFactor = Mathf.Abs(Mathf.Cos(pitchRadians)); // 1 at level, 0 at ±90°
                strafeVelocity = strafeDirection * availableStrafeBudget * currentStrafeInput * strafePitchFactor;
            }
            
            // --- ENFORCE GLOBAL SPEED LIMIT (in normal flight) ---
            if (!isStalled && currentVelocity.magnitude > flightProfile.maxSpeed)
            {
                currentVelocity = currentVelocity.normalized * flightProfile.maxSpeed;
            }

            // --- APPLY MOVEMENT ---
            Vector3 finalMovement = currentVelocity + strafeVelocity;
            if (!float.IsNaN(finalMovement.x) && !float.IsNaN(finalMovement.y) && !float.IsNaN(finalMovement.z))
            {
                transform.position += finalMovement * deltaTime;
            }
            currentSpeed = finalMovement.magnitude;
            
            // --- CONTROL SYSTEM (Rotation) ---
            Quaternion oldRotation = transform.rotation;
            
            // All rotation logic is now handled in a single, clean method
            // which returns the final calculated orientation.
            transform.rotation = ApplyRotation(deltaTime);

            // Update velocity based on the change in orientation
            Quaternion rotationChange = transform.rotation * Quaternion.Inverse(oldRotation);
            Vector3 steeredVelocity = rotationChange * currentVelocity;
            currentVelocity = Vector3.Lerp(currentVelocity, steeredVelocity, 1f - stallInfluence);
        }
        
        private Quaternion ApplyRotation(float deltaTime)
        {
            float effectiveTurnSpeed = GetEffectiveTurnSpeed();
            float pitchChange = pitchInput * effectiveTurnSpeed * deltaTime;
            float yawChange = yawInput * effectiveTurnSpeed * deltaTime;

            Quaternion currentRotation = transform.rotation;

            // 1. Create a yaw rotation around the stable WORLD up axis.
            Quaternion yawRotation = Quaternion.AngleAxis(yawChange, Vector3.up);
            
            // 2. Apply this yaw to the current rotation.
            Quaternion rotationAfterYaw = yawRotation * currentRotation;
            
            // 3. Create a pitch rotation around the camera's right axis to keep it screen-relative.
            Vector3 pitchAxis = Camera.main != null ? Camera.main.transform.right : Vector3.right;
            Quaternion pitchRotation = Quaternion.AngleAxis(pitchChange, pitchAxis);
            
            // 4. Combine the pitch and yaw rotations. The order (pitch * yaw) is crucial.
            Quaternion newDirectionRotation = pitchRotation * rotationAfterYaw;

            // --- CALCULATE AND APPLY VISUAL BANKING ---
            float targetBankInput = 0f;
            Vector2 mouseScreenPos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height) - (Vector2.one * 0.5f);
            float turnIntensity = Mathf.Clamp01(Mathf.Abs(mouseScreenPos.x) * 2f);
            float turnDirection = Mathf.Sign(mouseScreenPos.x);

            if (Mathf.Abs(currentStrafeInput) > 0.05f)
            {
                targetBankInput = -currentStrafeInput;
            }
            else
            {
                targetBankInput = -turnDirection * turnIntensity;
            }
            
            float bankTransitionSpeed = 5f;
            targetBankInput = Mathf.MoveTowards(lastBankInput, targetBankInput, bankTransitionSpeed * deltaTime);
            lastBankInput = targetBankInput;

            float maxBankAngle = flightProfile != null ? flightProfile.maxBankAngle : 60f;
            float speedRatio = Mathf.Clamp01(currentSpeed / (flightProfile != null ? flightProfile.maxSpeed : 150f));
            float minSpeedMultiplier = 0.2f;
            float speedMultiplier = Mathf.Lerp(minSpeedMultiplier, 1f, speedRatio);
            float targetBankAngle = targetBankInput * maxBankAngle * speedMultiplier;
            
            float bankSmoothing = flightProfile != null ? flightProfile.bankSmoothing : 8f;
            
            // To get the current visual roll, we create a reference rotation with no roll,
            // and then measure the angle between its up vector and our final direction's up vector.
            Vector3 fwd = newDirectionRotation * Vector3.forward;
            Quaternion noRoll = Quaternion.LookRotation(fwd, Vector3.up);
            float currentVisualRoll = Vector3.SignedAngle(noRoll * Vector3.up, newDirectionRotation * Vector3.up, fwd);
            
            float smoothedBankAngle = Mathf.LerpAngle(currentVisualRoll, targetBankAngle, bankSmoothing * deltaTime);

            // Calculate the small change in roll needed and apply it.
            float rollChange = smoothedBankAngle - currentVisualRoll;
            Quaternion bankRotation = Quaternion.AngleAxis(rollChange, fwd);

            Quaternion rotationWithVisualBank = bankRotation * newDirectionRotation;
            
            // --- WEATHERVANE & STALL EFFECT ---
            Quaternion weathervaneTarget = rotationWithVisualBank;
            if (currentVelocity.sqrMagnitude > 0.1f)
            {
                Vector3 velocityDir = currentVelocity.normalized;
                Vector3 playerDesiredDir = rotationWithVisualBank * Vector3.forward;
                float freedomAngle = 45f;
                if (Vector3.Angle(playerDesiredDir, velocityDir) > freedomAngle)
                {
                    Vector3 rotationAxis = Vector3.Cross(velocityDir, playerDesiredDir).normalized;
                    Quaternion coneEdgeRotation = Quaternion.AngleAxis(freedomAngle, rotationAxis);
                    Vector3 targetDirection = coneEdgeRotation * velocityDir;
                    weathervaneTarget = Quaternion.LookRotation(targetDirection, rotationWithVisualBank * Vector3.up);
                }
            }
            
            float massFactor = (flightProfile != null && flightProfile.mass > 0) ? Mathf.Clamp(flightProfile.mass / 350f, 0.5f, 2.5f) : 1f;
            float targetDegreesPerSecond = 72f;
            float maxSlerpFactor = (targetDegreesPerSecond * Mathf.Deg2Rad * deltaTime);
            float massInfluencedStallFactor = stallInfluence / massFactor;
            float finalStallFactor = Mathf.SmoothStep(0f, 1f, massInfluencedStallFactor);

            Quaternion finalRotation = Quaternion.Slerp(
                rotationWithVisualBank,
                weathervaneTarget,
                finalStallFactor
            );

            // Sync state variables for UI/debugging
            Vector3 finalEuler = finalRotation.eulerAngles;
            currentPitch = finalEuler.x > 180 ? finalEuler.x - 360 : finalEuler.x;
            currentYaw = finalEuler.y;
            currentBankAngle = finalEuler.z > 180 ? finalEuler.z - 360 : finalEuler.z;
            
            return finalRotation;
        }

        // Calculate stall threshold based on max speed
        private float CalculateStallThreshold()
        {
            if (flightProfile == null) return 15f;
            
            // Stall threshold: 40% of max speed (increased from 20%)
            float stallThreshold = flightProfile.maxSpeed * 0.4f;
            
            return Mathf.Max(stallThreshold, 15f); // Minimum 15 units/sec
        }
        
        // Calculate stall control reduction based on forward speed ratio
        private float CalculateStallControlMultiplier()
        {
            if (dynamicStallThreshold <= 0f) return 1f;
            
            float speedRatio = currentSpeed / dynamicStallThreshold;
            
            if (speedRatio < 1f)
            {
                return Mathf.Clamp(speedRatio, 0.3f, 1f); // Allow at least 30% control when stalled
            }
            return 1f;
        }
        
        // Input methods - called by flight controller
        public void SetPitchInput(float value) => pitchInput = Mathf.Clamp(value, -1f, 1f);
        public void SetYawInput(float value) => yawInput = Mathf.Clamp(value, -1f, 1f);
        public void SetRollInput(float value) => rollInput = Mathf.Clamp(value, -1f, 1f);
        public void SetStrafeInput(float value) => strafeInput = Mathf.Clamp(value, -1f, 1f);
        
        // Throttle control
        public void SetThrottle(float newThrottle) 
        { 
            throttle = Mathf.Clamp01(newThrottle); // Allow 0 to 1 throttle
        }
        
        public void IncreaseThrottle(float amount = 0.1f) 
        { 
            throttle = Mathf.Clamp01(throttle + amount);
        }
        
        public void DecreaseThrottle(float amount = 0.1f) 
        { 
            throttle = Mathf.Clamp01(throttle - amount);
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
        
        /// <summary>
        /// Update flight controller from ItemManager statistics
        /// </summary>
        public void UpdateFromItemManager(ShipStatistics stats)
        {
            if (!useItemManager || stats == null) return;
            
            // Update flight profile with new statistics
            if (flightProfile == null)
            {
                flightProfile = FlightProfile.CreateDefaultProfile();
            }
            
            // Apply statistics to flight profile
            flightProfile.flightSpeed = stats.flightSpeed;
            flightProfile.maxSpeed = stats.maxSpeed;
            flightProfile.minSpeed = stats.minSpeed;
            flightProfile.speedSmoothing = stats.speedSmoothing;
            flightProfile.strafeSpeed = stats.strafeSpeed;
            flightProfile.turnSpeed = stats.turnRate;
            flightProfile.bankingAmount = stats.bankingAmount;
            flightProfile.maxBankAngle = stats.maxBankAngle;
            flightProfile.bankSmoothing = stats.bankSmoothing;
            flightProfile.autoLevelRate = stats.autoLevelRate;
            flightProfile.speedBankingMultiplier = stats.speedBankingMultiplier;
            flightProfile.mousePositionBankingSensitivity = stats.mousePositionBankingSensitivity;
            flightProfile.stallThreshold = stats.stallThreshold;
            flightProfile.acceleration = stats.acceleration;
            flightProfile.deceleration = stats.deceleration;
            flightProfile.engineSoundProfile = stats.engineSoundProfile.ToString();
            flightProfile.thrusterEffectIntensity = stats.thrusterEffectIntensity;
            flightProfile.maneuverRate = stats.maneuverRate;
            flightProfile.strafeThrust = stats.strafeThrust;
            flightProfile.retroThrust = stats.retroThrust;
            flightProfile.mass = stats.mass;
            flightProfile.thrust = stats.thrust;
            
            Debug.Log($"Flight controller updated from ItemManager for {gameObject.name}");
        }

        private void FindMissingReferences() {
            // Aircraft (Player tagged GameObject)
            if (aircraft == null) {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) aircraft = playerObj.transform;
            }
            // MouseAim - always create as child of this GameObject
            if (mouseAim == null) {
                GameObject mouseAimObj = transform.Find("MouseAim")?.gameObject;
                if (mouseAimObj == null) {
                    mouseAimObj = new GameObject("MouseAim");
                    mouseAimObj.transform.SetParent(transform);
                    mouseAimObj.transform.localPosition = Vector3.zero;
                    mouseAimObj.transform.localRotation = Quaternion.identity;
                }
                mouseAim = mouseAimObj.transform;
            }
        }

        private void ResetMouseAim() {
            mouseInput = Vector2.zero;
            targetMouseInput = Vector2.zero;
            lastMouseInput = Vector2.zero;
            if (mouseAim != null && aircraft != null) {
                try { mouseAim.rotation = aircraft.rotation; } catch { }
            }
        }

        private void HandleStrafeInput() {
            float rawStrafeInput = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) rawStrafeInput -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) rawStrafeInput += 1f;
            targetStrafeInput = Mathf.Clamp(rawStrafeInput, -1f, 1f);
        }

        private void ProcessProgressiveStrafeInput() {
            currentStrafeInput = targetStrafeInput;
            if (Mathf.Abs(currentStrafeInput) < 0.01f) currentStrafeInput = 0f;
        }

        private void HandleDodgeInput() {
            float currentTime = Time.time;
            if (Input.GetKeyDown(KeyCode.A)) {
                if (currentTime - lastAPress < doubleTapWindow) PerformDodge(DodgeDirection.Left);
                lastAPress = currentTime;
            } else if (Input.GetKeyDown(KeyCode.D)) {
                if (currentTime - lastDPress < doubleTapWindow) PerformDodge(DodgeDirection.Right);
                lastDPress = currentTime;
            } else if (Input.GetKeyDown(KeyCode.S)) {
                if (currentTime - lastSPress < doubleTapWindow) PerformDodge(DodgeDirection.Back);
                lastSPress = currentTime;
            }
            if (isDodging && currentTime - lastAPress > doubleTapWindow * 2f) ResetDodge();
        }

        private void PerformDodge(DodgeDirection direction) {
            if (isDodging) return;
            isDodging = true;
            Debug.Log($"{name}: Dodge performed - {direction}");
            // TODO: Implement dodge mechanics
        }
        private void ResetDodge() { isDodging = false; }

        private void ConvertMouseInputToShipControl_Enhanced() {
            if (shipClass == null || mouseAim == null || aircraft == null) return;
            float yawInputVal = mouseInput.x / maxYawAngle;
            float pitchInputVal = mouseInput.y / maxPitchAngle;
            yawInputVal = Mathf.Clamp(yawInputVal, -1f, 1f);
            pitchInputVal = Mathf.Clamp(pitchInputVal, -1f, 1f);
            Vector2 mouseScreenPos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
            mouseScreenPos -= Vector2.one * 0.5f;
            float rollInputVal = -mouseScreenPos.x * 1.2f;
            rollInputVal = Mathf.Clamp(rollInputVal, -1f, 1f);
            SetYawInput(yawInputVal);
            SetPitchInput(pitchInputVal);
            SetRollInput(rollInputVal);
        }

        private void HandleInput() {
            if (!enableInputConversion) return;
            Vector2 currentMouseScreenPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 mouseOffset = currentMouseScreenPos - screenCenter;
            float yawAngle = (mouseOffset.x / screenCenter.x) * maxYawAngle;
            float pitchAngle = -(mouseOffset.y / screenCenter.y) * maxPitchAngle;
            if (instantInputMode) {
                mouseInput = new Vector2(yawAngle, pitchAngle);
            } else {
                targetMouseInput = new Vector2(yawAngle, pitchAngle);
                mouseInput = Vector2.Lerp(mouseInput, targetMouseInput, mouseResponsiveness * Time.deltaTime);
            }
            if (mouseAim != null && aircraft != null) {
                try {
                    Vector3 aircraftForward = aircraft.forward;
                    Vector3 aircraftUp = aircraft.up;
                    Vector3 pitchAxis = Camera.main != null ? Camera.main.transform.right : Vector3.right;
                    Quaternion yawOffset = Quaternion.AngleAxis(mouseInput.x, aircraftUp);
                    Quaternion pitchOffset = Quaternion.AngleAxis(mouseInput.y, pitchAxis);
                    Vector3 mouseLookDirection = yawOffset * pitchOffset * aircraftForward;
                    mouseAim.rotation = Quaternion.LookRotation(mouseLookDirection, aircraftUp);
                } catch { }
            }
            HandleStrafeInput();
            HandleDodgeInput();
            HandleBrakingInput();
            HandleCameraSmoothingToggle();
            lastMouseScreenPosition = currentMouseScreenPos;
            lastMouseInput = mouseInput;
        }

        private void HandleBrakingInput()
        {
            isBraking = Input.GetKey(KeyCode.S) && throttle < 0.01f;
        }
        
        private void HandleCameraSmoothingToggle()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleCameraSmoothing();
            }
        }
        
        public void ToggleCameraSmoothing()
        {
            // Camera smoothing is now always disabled for direct response
            Debug.Log("Camera smoothing is permanently disabled for direct response");
        }
    }
} 