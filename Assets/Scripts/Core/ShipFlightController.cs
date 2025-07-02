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

        private float currentStrafeSpeed = 0f;
        private float lastBankInput = 0f;  // For smooth bank transitions
        private bool wasStalledLastFrame = false;

        public enum DodgeDirection { Left, Right, Back }

        public Vector3 BoresightPos => aircraft == null ? transform.forward * 1000f : (aircraft.transform.forward * 1000f) + aircraft.transform.position;
        public bool IsUsingEnhancedControl => true;
        public Vector3 MouseAimPos {
            get {
                if (aircraft != null) {
                    try {
                        Vector3 aircraftForward = aircraft.forward;
                        Vector3 aircraftUp = aircraft.up;
                        Vector3 aircraftRight = aircraft.right;
                        Quaternion yawOffset = Quaternion.AngleAxis(mouseInput.x, aircraftUp);
                        Quaternion pitchOffset = Quaternion.AngleAxis(mouseInput.y, aircraftRight);
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
        /// Otomatik flight profile bulma - gemi ismlerine göre
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
            if (flightProfile == null || flightProfile.maxSpeed <= 0) return 60f; // Fallback

            // Dynamic turn speed calculation.
            // Slower and heavier ships turn slower.
            // Turn rate is proportional to current speed to prevent instant stalls.
            float massFactor = Mathf.Max(0.1f, flightProfile.mass); // Prevent division by zero
            float baseTurnRate = flightProfile.turnSpeed / massFactor;
            float speedRatio = currentVelocity.magnitude / flightProfile.maxSpeed;
            
            // Ensure a minimum turn rate even at zero speed to allow for recovery.
            float minimumTurnRatio = 0.15f;
            speedRatio = Mathf.Max(minimumTurnRatio, speedRatio);

            return baseTurnRate * speedRatio;
        }
        
        private void UpdateTransformMovement()
        {
            if (flightProfile == null) return;
            
            float deltaTime = Time.deltaTime;

            // --- STATE CALCULATION ---
            dynamicStallThreshold = CalculateStallThreshold();
            isStalled = currentVelocity.magnitude < dynamicStallThreshold;
            smoothedStallControlMultiplier = CalculateStallControlMultiplier();
            
            // --- UNIFIED GRAVITY & FALL (Always On) ---
            // This is now the primary force acting on the ship each frame.
            float pitchAngle = transform.eulerAngles.x;
            if (pitchAngle > 180f) pitchAngle -= 360f;
            float pitchInfluence = pitchAngle / 90f; 

            Vector3 gravityDirection;
            if (Mathf.Abs(pitchInfluence) < 0.01f) {
                gravityDirection = Vector3.down;
            } else {
                gravityDirection = Vector3.Lerp(Vector3.down, transform.forward * Mathf.Sign(pitchInfluence), Mathf.Abs(pitchInfluence));
            }

            if (isStalled)
            {
                float upAlignment = Vector3.Dot(transform.up, Vector3.up);
                float flatFactor = 1 - Mathf.Abs(upAlignment);
                float terminalFallSpeed = Mathf.Lerp(flightProfile.maxSpeed, flightProfile.maxSpeed * 0.5f, flatFactor);
                currentVelocity = Vector3.MoveTowards(currentVelocity, gravityDirection * terminalFallSpeed, universalGravity * deltaTime);
            }
            else
            {
                currentVelocity += gravityDirection * universalGravity * deltaTime;
            }

            // --- FORWARD THRUST ---
            // The ship's engines now apply force to fight against gravity.
            Vector3 thrustForce = transform.forward * flightProfile.acceleration * throttle;
            currentVelocity += thrustForce * deltaTime;
            
            // --- DRIFT CORRECTION ---
            Vector3 forwardVel = Vector3.Project(currentVelocity, transform.forward);
            Vector3 sidewaysVel = currentVelocity - forwardVel;
            float correctionFactor = 0.05f;
            sidewaysVel = Vector3.Lerp(sidewaysVel, Vector3.zero, GetEffectiveTurnSpeed() * correctionFactor * deltaTime);
            currentVelocity = forwardVel + sidewaysVel;

            // --- TERMINAL VELOCITY (Stall Speed Cap) ---
            if (isStalled)
            {
                float upAlignment = Vector3.Dot(transform.up, Vector3.up);
                float flatFactor = 1 - Mathf.Abs(upAlignment);
                
                float terminalVelocity = flightProfile.maxSpeed;
                float currentTerminalVelocity = Mathf.Lerp(terminalVelocity, terminalVelocity * 0.5f, flatFactor);
                
                if (currentVelocity.magnitude > currentTerminalVelocity)
                {
                    currentVelocity = currentVelocity.normalized * currentTerminalVelocity;
                }
            }
            
            // --- ENFORCE GLOBAL SPEED LIMIT ---
            if (currentVelocity.magnitude > flightProfile.maxSpeed)
            {
                currentVelocity = currentVelocity.normalized * flightProfile.maxSpeed;
            }

            // --- LATERAL MOVEMENT (STRAFE) ---
            Vector3 strafeVelocity = transform.right * flightProfile.maneuverRate * strafeInput;
            Vector3 finalMovement = currentVelocity + strafeVelocity;
            // We no longer add strafe to the persistent velocity to avoid unwanted momentum buildup.
            // It's a direct, responsive translation for the current frame.

            // --- APPLY MOVEMENT ---
            if (!float.IsNaN(finalMovement.x) && !float.IsNaN(finalMovement.y) && !float.IsNaN(finalMovement.z))
            {
                transform.position += finalMovement * deltaTime;
            }
            currentSpeed = currentVelocity.magnitude;
            
            // --- CONTROL SYSTEM (Rotation) ---
            float effectiveTurnSpeed = GetEffectiveTurnSpeed();
            
            // Store the rotation from before the turn is applied
            Quaternion oldRotation = transform.rotation;

            float pitchChange = pitchInput * effectiveTurnSpeed * deltaTime;
            float yawChange = yawInput * effectiveTurnSpeed * deltaTime;
            
            currentPitch += pitchChange;
            currentPitch = Mathf.Clamp(currentPitch, -89f, 89f);
            currentYaw += yawChange;
            
            // Apply banking
            ApplyBankingRotation(); // This method now sets transform.rotation

            // --- STEER VELOCITY ---
            // Calculate the change in rotation and apply it to the velocity vector.
            // This ensures the ship's momentum follows its new orientation.
            Quaternion rotationChange = transform.rotation * Quaternion.Inverse(oldRotation);
            currentVelocity = rotationChange * currentVelocity;
        }
        
        private void ApplyBankingRotation()
        {
            // SAFETY CHECK - Prevent NaN values
            if (float.IsNaN(currentPitch) || float.IsInfinity(currentPitch)) currentPitch = 0f;
            if (float.IsNaN(currentYaw) || float.IsInfinity(currentYaw)) currentYaw = 0f;
            if (float.IsNaN(pitchInput) || float.IsInfinity(pitchInput)) pitchInput = 0f;
            if (float.IsNaN(yawInput) || float.IsInfinity(yawInput)) yawInput = 0f;

            // --- BANKING SYSTEM - SPEED-AWARE BANKING ---
            float targetBankInput = 0f;
            
            // Calculate mouse-based bank
            Vector2 mouseScreenPos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
            mouseScreenPos -= Vector2.one * 0.5f;  // Center at 0,0
            float turnIntensity = Mathf.Abs(mouseScreenPos.x) * 2f; // *2 to reach 1 at screen edges
            turnIntensity = Mathf.Clamp01(turnIntensity);
            float turnDirection = Mathf.Sign(mouseScreenPos.x);
            
            // Compare forward speed vs strafe speed
            float absCurrentSpeed = Mathf.Abs(currentSpeed);
            float absStrafeSpeed = Mathf.Abs(currentStrafeSpeed);
            bool strafeIsFaster = absStrafeSpeed > absCurrentSpeed;
            
            // Check if strafe and turn are in the same direction
            bool isStrafing = Mathf.Abs(currentStrafeInput) > 0.05f;
            bool isTurning = turnIntensity > 0.05f;
            bool sameDirection = (turnDirection > 0 && currentStrafeInput > 0) || (turnDirection < 0 && currentStrafeInput < 0);
            
            if (isStrafing)
            {
                if (strafeIsFaster)
                {
                    // When strafing faster than forward speed, strafe controls banking
                    // Keep consistent banking direction with normal flight
                    targetBankInput = -currentStrafeInput;  // Same direction as normal flight
                }
                else if (isTurning && sameDirection)
                {
                    // When moving faster forward and inputs align, use turn direction
                    targetBankInput = -turnDirection;
                }
                else
                {
                    // When moving faster forward but inputs don't align, use strafe
                    targetBankInput = -currentStrafeInput;  // Same banking direction
                }
            }
            else
            {
                // Pure mouse turning
                targetBankInput = -turnDirection * turnIntensity;
            }
            
            // Smooth transitions (faster)
            float transitionSpeed = 5f;  // Increased from 2f for snappier response
            targetBankInput = Mathf.MoveTowards(lastBankInput, targetBankInput, transitionSpeed * Time.deltaTime);
            lastBankInput = targetBankInput;
            
            targetBankInput = Mathf.Clamp(targetBankInput, -1f, 1f);

            // Calculate speed-based bank angle limit
            float maxBankAngle = flightProfile != null ? flightProfile.maxBankAngle : 60f;
            float speedRatio = currentSpeed / (flightProfile != null ? flightProfile.maxSpeed : 150f);
            speedRatio = Mathf.Clamp01(speedRatio);  // Ensure ratio is between 0 and 1
            
            // Allow minimum banking even at low speeds (20% of max bank)
            float minSpeedMultiplier = 0.2f;
            float speedMultiplier = Mathf.Lerp(minSpeedMultiplier, 1f, speedRatio);
            
            // Apply speed-based limit to bank angle
            float speedLimitedBankAngle = maxBankAngle * speedMultiplier;
            float targetBankAngle = targetBankInput * speedLimitedBankAngle;

            // Smooth the bank angle
            float bankSmoothing = flightProfile != null ? flightProfile.bankSmoothing : 8f;
            currentBankAngle = Mathf.Lerp(currentBankAngle, targetBankAngle, bankSmoothing * Time.deltaTime);

            // Apply rotation
            Vector3 eulerAngles = new Vector3(currentPitch, currentYaw, currentBankAngle);
            
            // Final safety check before applying rotation
            if (!float.IsNaN(eulerAngles.x) && !float.IsNaN(eulerAngles.y) && !float.IsNaN(eulerAngles.z) &&
                !float.IsInfinity(eulerAngles.x) && !float.IsInfinity(eulerAngles.y) && !float.IsInfinity(eulerAngles.z))
            {
                transform.rotation = Quaternion.Euler(eulerAngles);
            }
            else
            {
                currentPitch = 0f;
                currentYaw = 0f;
                currentBankAngle = 0f;
                transform.rotation = Quaternion.identity;
            }
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
                    Vector3 aircraftRight = aircraft.right;
                    Quaternion yawOffset = Quaternion.AngleAxis(mouseInput.x, aircraftUp);
                    Quaternion pitchOffset = Quaternion.AngleAxis(mouseInput.y, aircraftRight);
                    Vector3 mouseLookDirection = yawOffset * pitchOffset * aircraftForward;
                    mouseAim.rotation = Quaternion.LookRotation(mouseLookDirection, aircraftUp);
                } catch { }
            }
            HandleStrafeInput();
            HandleDodgeInput();
            lastMouseScreenPosition = currentMouseScreenPos;
            lastMouseInput = mouseInput;
        }
    }
} 