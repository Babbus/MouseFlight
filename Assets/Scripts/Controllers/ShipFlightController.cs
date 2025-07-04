using UnityEngine;
using DomeClash.Ships;

namespace DomeClash.Core
{
    /// <summary>
    /// Ship Flight Controller - Transform-based flight movement system
    /// Handles all ship movement, banking, and flight physics
    /// NO PHYSICS - Direct transform control for responsive arcade flight
    /// </summary>
    [RequireComponent(typeof(ShipManager))]
    public class ShipFlightController : MonoBehaviour
    {
        [Header("System Toggles")]
        [SerializeField] private bool systemEnabled = true;

        [Header("Core Components")]
        [SerializeField] private ItemManager itemManager;
        
        [Header("Current Movement State")]
        [SerializeField] private float throttle = 0.8f;
        [SerializeField] private Vector3 currentVelocity = Vector3.zero;
        private float currentSpeed = 0f;

        [Header("Banking System")]
        [SerializeField] private float currentBankAngle = 0f;
        [SerializeField] private float currentPitch = 0f;
        [SerializeField] private float currentYaw = 0f;
        
        [Header("Stall & Gravity System")]
        [SerializeField] private float universalGravity = 9.81f;
        [SerializeField] private bool isStalled = false;
        [SerializeField] private float smoothedStallControlMultiplier = 1f;
        [SerializeField] private float dynamicStallThreshold = 0f;
        
        // --- Private Flight Control State ---
        private float pitchInput = 0f;
        private float yawInput = 0f;
        private float rollInput = 0f;
        private float strafeInput = 0f;
        
        // --- Cached References ---
        private ShipManager shipClass;
        private ShipStatistics stats; // Cached stats from ItemManager

        // --- Public Properties for UI/Debugging ---
        public float CurrentSpeed => currentSpeed;
        public float Throttle => throttle;
        public Vector3 CurrentVelocity => currentVelocity;
        public float ActualSpeed => currentVelocity.magnitude;
        public float ForwardSpeed { get; private set; }
        
        [Header("Mouse Flight Input")] 
        // Mouse input settings are now hardcoded for a consistent feel.
        private const float MAX_PITCH_ANGLE = 15f;
        private const float MAX_YAW_ANGLE = 20f;
        private const float MOUSE_RESPONSIVENESS = 0.8f;
        private const bool INSTANT_INPUT_MODE = true;
        
        // --- Private Input State ---
        private float currentStrafeInput = 0f;
        private float targetStrafeInput = 0f;
        private Vector2 mouseInput = Vector2.zero, targetMouseInput = Vector2.zero, lastMouseInput = Vector2.zero;
        private Transform mouseAim = null;
        private Transform aircraft = null;
        
        private float lastBankInput = 0f;
        private bool isBraking = false;
        private float stallInfluence = 0f;
        
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
            shipClass = GetComponent<ShipManager>();
            
            Vector3 currentEuler = transform.eulerAngles;
            currentPitch = currentEuler.x;
            currentYaw = currentEuler.y;
            currentBankAngle = currentEuler.z;

            FindMissingReferences();
            ResetMouseAim();
            
            if (itemManager == null)
            {
                itemManager = GetComponent<ItemManager>();
            }
        }
        
        private void OnEnable()
        {
            ShipEvents.OnStatsChanged += UpdateStats;
        }

        private void OnDisable()
        {
            ShipEvents.OnStatsChanged -= UpdateStats;
        }

        private void UpdateStats(ShipStatistics newStats)
        {
            this.stats = newStats;
        }

        private void Start()
        {
            if (itemManager != null)
            {
                // Initialize with stats from ItemManager
                stats = itemManager.GetShipStatistics();
                currentSpeed = stats.maxSpeed * throttle;
                currentVelocity = transform.forward * currentSpeed;
            }
        }
        
        private void Update()
        {
            if (!systemEnabled || itemManager == null) return;

            if (stats == null) return;

            HandleInput();
            ProcessProgressiveStrafeInput();
            ConvertMouseInputToShipControl_Enhanced();
            
            UpdateTransformMovement();
        }

        private float GetEffectiveTurnSpeed()
        {
            if (stats.maxSpeed <= 0) return 60f;

            float massFactor = Mathf.Max(0.1f, stats.mass / 100f);
            float baseTurnRate = stats.turnRate / massFactor;
            float speedRatio = currentVelocity.magnitude / stats.maxSpeed;
            
            float minimumTurnRatio = 0.25f;
            speedRatio = Mathf.Max(minimumTurnRatio, speedRatio);

            return baseTurnRate * speedRatio;
        }
        
        private void UpdateTransformMovement()
        {
            float deltaTime = Time.deltaTime;

            dynamicStallThreshold = CalculateStallThreshold();
            
            Vector3 forwardVelocity = Vector3.Project(currentVelocity, transform.forward);
            this.ForwardSpeed = Vector3.Dot(forwardVelocity, transform.forward);

            float transitionRange = stats.maxSpeed * 0.2f;
            float stallBeginThreshold = dynamicStallThreshold + (transitionRange * 0.5f);
            float stallEndThreshold = dynamicStallThreshold - (transitionRange * 0.5f);
            
            stallInfluence = 1f - Mathf.Clamp01(Mathf.InverseLerp(stallEndThreshold, stallBeginThreshold, this.ForwardSpeed));

            isStalled = stallInfluence > 0.01f;
            smoothedStallControlMultiplier = CalculateStallControlMultiplier();
            
            Vector3 gravityDirection = Vector3.down;
            float gravityMultiplier = 1f + (2f * stallInfluence);
            float gravityMagnitude = universalGravity * gravityMultiplier;
            currentVelocity += gravityDirection * gravityMagnitude * deltaTime;

            Vector3 thrustForce = transform.forward * stats.acceleration * throttle;
            currentVelocity += thrustForce * deltaTime;

            Vector3 strafeVelocity = Vector3.zero;

            if (isStalled)
            {
                float pitchAngle = currentPitch;
                float forwardMomentumFactor = Mathf.InverseLerp(-90f, 90f, pitchAngle);
                Vector3 fallDirection = Vector3.down;
                Vector3 glideDirection = transform.forward;
                Vector3 blendedDirection = Vector3.Slerp(fallDirection, glideDirection, forwardMomentumFactor).normalized;
                
                float stallSpeedLimitFactor = Mathf.Lerp(0.1f, 1.0f, forwardMomentumFactor);
                float targetSpeed = stats.maxSpeed * stallSpeedLimitFactor;

                Vector3 targetVelocity = blendedDirection * targetSpeed;
                currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, stats.acceleration * 2f * deltaTime);
            }
            else
            {
                float maxDragForce = stats.acceleration * 0.25f;
                float throttleInfluence = 1.0f - throttle;
                float currentDrag = maxDragForce * throttleInfluence;
                
                if (currentVelocity.sqrMagnitude > 0.1f)
                {
                    Vector3 dragForce = -currentVelocity.normalized * currentDrag;
                    currentVelocity += dragForce * Time.deltaTime;
                }

                if (isBraking)
                {
                    float brakePower = stats.acceleration * 1.5f;
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

                float totalManeuverBudget = stats.maneuverRate;
                float availableStrafeBudget = Mathf.Max(0, totalManeuverBudget - driftCorrectionSpeedCost);
                Vector3 strafeDirection = transform.right;
                strafeDirection.y = 0;
                strafeDirection.Normalize();

                float pitchRadians = currentPitch * Mathf.Deg2Rad;
                float strafePitchFactor = Mathf.Abs(Mathf.Cos(pitchRadians));
                strafeVelocity = strafeDirection * availableStrafeBudget * currentStrafeInput * strafePitchFactor;
            }
            
            if (!isStalled && currentVelocity.magnitude > stats.maxSpeed)
            {
                currentVelocity = currentVelocity.normalized * stats.maxSpeed;
            }

            Vector3 finalMovement = currentVelocity + strafeVelocity;
            if (!float.IsNaN(finalMovement.x) && !float.IsNaN(finalMovement.y) && !float.IsNaN(finalMovement.z))
            {
                transform.position += finalMovement * deltaTime;
            }
            currentSpeed = finalMovement.magnitude;
            
            Quaternion oldRotation = transform.rotation;
            
            transform.rotation = ApplyRotation(deltaTime);

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

            Quaternion yawRotation = Quaternion.AngleAxis(yawChange, Vector3.up);
            
            Quaternion rotationAfterYaw = yawRotation * currentRotation;
            
            Vector3 pitchAxis = Camera.main != null ? Camera.main.transform.right : Vector3.right;
            Quaternion pitchRotation = Quaternion.AngleAxis(pitchChange, pitchAxis);
            
            Quaternion newDirectionRotation = pitchRotation * rotationAfterYaw;

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

            float maxBankAngle = stats.maxBankAngle;
            float speedRatio = Mathf.Clamp01(currentSpeed / stats.maxSpeed);
            float minSpeedMultiplier = 0.2f;
            float speedMultiplier = Mathf.Lerp(minSpeedMultiplier, 1f, speedRatio);
            float targetBankAngle = targetBankInput * maxBankAngle * speedMultiplier;
            
            float bankSmoothing = stats.bankSmoothing;
            
            Vector3 fwd = newDirectionRotation * Vector3.forward;
            Quaternion noRoll = Quaternion.LookRotation(fwd, Vector3.up);
            float currentVisualRoll = Vector3.SignedAngle(noRoll * Vector3.up, newDirectionRotation * Vector3.up, fwd);
            
            float smoothedBankAngle = Mathf.LerpAngle(currentVisualRoll, targetBankAngle, bankSmoothing * deltaTime);

            float rollChange = smoothedBankAngle - currentVisualRoll;
            Quaternion bankRotation = Quaternion.AngleAxis(rollChange, fwd);

            Quaternion rotationWithVisualBank = bankRotation * newDirectionRotation;
            
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
            
            float massFactor = (stats.mass > 0) ? Mathf.Clamp(stats.mass / 350f, 0.5f, 2.5f) : 1f;
            float targetDegreesPerSecond = 72f;
            float maxSlerpFactor = (targetDegreesPerSecond * Mathf.Deg2Rad * deltaTime);
            float massInfluencedStallFactor = stallInfluence / massFactor;
            float finalStallFactor = Mathf.SmoothStep(0f, 1f, massInfluencedStallFactor);

            Quaternion finalRotation = Quaternion.Slerp(
                rotationWithVisualBank,
                weathervaneTarget,
                finalStallFactor
            );

            Vector3 finalEuler = finalRotation.eulerAngles;
            currentPitch = finalEuler.x > 180 ? finalEuler.x - 360 : finalEuler.x;
            currentYaw = finalEuler.y;
            currentBankAngle = finalEuler.z > 180 ? finalEuler.z - 360 : finalEuler.z;
            
            return finalRotation;
        }

        private float CalculateStallThreshold()
        {
            float stallThreshold = stats.maxSpeed * 0.4f;
            return Mathf.Max(stallThreshold, 15f);
        }
        
        private float CalculateStallControlMultiplier()
        {
            if (dynamicStallThreshold <= 0f) return 1f;
            
            float speedRatio = currentSpeed / dynamicStallThreshold;
            
            if (speedRatio < 1f)
            {
                return Mathf.Clamp(speedRatio, 0.3f, 1f);
            }
            return 1f;
        }
        
        public void SetPitchInput(float value) => pitchInput = Mathf.Clamp(value, -1f, 1f);
        public void SetYawInput(float value) => yawInput = Mathf.Clamp(value, -1f, 1f);
        public void SetRollInput(float value) => rollInput = Mathf.Clamp(value, -1f, 1f);
        public void SetStrafeInput(float value) => strafeInput = Mathf.Clamp(value, -1f, 1f);
        
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
        
        public float GetPitchInput() => pitchInput;
        public float GetYawInput() => yawInput;
        public float GetRollInput() => rollInput;
        public float GetStrafeInput() => strafeInput;
        public float GetCurrentBankAngle() => currentBankAngle;
        public float GetCurrentPitch() => currentPitch;
        public float GetCurrentYaw() => currentYaw;
        public float GetEffectiveFlightSpeedPublic() => stats.flightSpeed;
        public float GetEffectiveTurnSpeedPublic() => GetEffectiveTurnSpeed();
        public bool IsStalled() => isStalled;
        public float GetStallControlMultiplier() => smoothedStallControlMultiplier;
        public float GetDynamicStallThreshold() => dynamicStallThreshold;
        public float GetActualSpeed() => ActualSpeed;
        
        private void FindMissingReferences() {
            if (itemManager == null) itemManager = GetComponent<ItemManager>();
            
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) aircraft = playerObj.transform;
            
            GameObject mouseAimObj = GameObject.Find("MouseAim");
            if (mouseAimObj != null) mouseAim = mouseAimObj.transform;
        }
        
        private void ResetMouseAim() {
            if (mouseAim != null) {
                mouseAim.position = transform.position + (transform.forward * 100f);
                mouseAim.rotation = transform.rotation;
            }

            if (mouseAim != null && aircraft != null) {
                try { mouseAim.rotation = aircraft.rotation; } catch { }
            }
        }
        
        private void ProcessProgressiveStrafeInput() {
            currentStrafeInput = Mathf.Lerp(currentStrafeInput, targetStrafeInput, Time.deltaTime * 10f);
        }
        
        private void ConvertMouseInputToShipControl_Enhanced() {
            if (shipClass == null || mouseAim == null || aircraft == null) return;
            
            float pitchInputVal = mouseInput.y / MAX_PITCH_ANGLE;
            float yawInputVal = mouseInput.x / MAX_YAW_ANGLE;
            
            pitchInputVal = Mathf.Clamp(pitchInputVal, -1f, 1f);
            yawInputVal = Mathf.Clamp(yawInputVal, -1f, 1f);
            
            SetYawInput(yawInputVal);
            SetPitchInput(pitchInputVal);
        }
        
        private void HandleInput() {
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 mouseOffset = new Vector2(Input.mousePosition.x, Input.mousePosition.y) - screenCenter;
            
            float yawAngle = (mouseOffset.x / screenCenter.x) * MAX_YAW_ANGLE;
            float pitchAngle = -(mouseOffset.y / screenCenter.y) * MAX_PITCH_ANGLE;
            
            mouseInput = new Vector2(yawAngle, pitchAngle);
            
            targetMouseInput = new Vector2(yawAngle, pitchAngle);
            
            if (mouseAim != null && aircraft != null) {
                Vector3 aircraftForward = aircraft.forward;
                Vector3 aircraftUp = aircraft.up;
                Vector3 pitchAxis = Camera.main != null ? Camera.main.transform.right : Vector3.right;
                Quaternion yawOffset = Quaternion.AngleAxis(mouseInput.x, aircraftUp);
                Quaternion pitchOffset = Quaternion.AngleAxis(mouseInput.y, pitchAxis);
                Vector3 mouseLookDirection = yawOffset * pitchOffset * aircraftForward;
                mouseAim.rotation = Quaternion.LookRotation(mouseLookDirection, aircraftUp);
            }

            targetStrafeInput = Input.GetAxis("Horizontal");
            
            HandleBrakingInput();
        }
        
        private void HandleBrakingInput()
        {
            isBraking = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.JoystickButton1);
        }
    }
} 