using UnityEngine;
using DomeClash.Ships;

namespace DomeClash.Core
{
    /// <summary>
    /// Dome Clash Flight Controller - MouseFlight sistemine dayalı
    /// Smooth camera tracking, framerate independent damping, titreme yok!
    /// Unity 6000.1.9f1 Compatible
    /// </summary>
    public class DomeClashFlightController : MonoBehaviour
    {
        [Header("System Control")]
        [SerializeField] [Tooltip("Enable/disable entire flight controller system")]
        public bool systemEnabled = true;
        [SerializeField] [Tooltip("Enable camera movement (set false for external camera control)")]
        public bool enableCameraMovement = true;

        [Header("Aircraft Components")]
        [SerializeField] [Tooltip("Transform of the aircraft the rig follows")]
        private Transform aircraft = null;
        [SerializeField] [Tooltip("Transform that mouse rotates to generate aim position")]
        private Transform mouseAim = null;
        [SerializeField] [Tooltip("Transform of the camera rig")]
        private Transform cameraRig = null;
        [SerializeField] [Tooltip("Main camera component")]
        private Camera cam = null;

        [Header("Camera Settings")]
        [SerializeField] [Tooltip("Follow aircraft using FixedUpdate")]
        private bool useFixed = true;
        [SerializeField] [Tooltip("Camera tracking speed - smooth movement")]
        private float camSmoothSpeed = 8f;
        [SerializeField] [Tooltip("Distance for aim calculations")]
        private float aimDistance = 1000f;
        
        [Header("Smart Hybrid Camera System")]
        [SerializeField] [Tooltip("Base camera follow distance behind aircraft")]
        private float cameraFollowDistance = 30f;
        [SerializeField] [Tooltip("Camera height above aircraft")]
        private float cameraHeight = 12f;
        [SerializeField] [Tooltip("Dynamic distance based on pitch (multiplier)")]
        private float pitchDistanceMultiplier = 1.5f;
        [SerializeField] [Tooltip("Minimum distance to aircraft")]
        private float minCameraDistance = 15f;
        [SerializeField] [Tooltip("Maximum distance to aircraft")]
        private float maxCameraDistance = 60f;

        [Header("Contextual Behavior")]
        [SerializeField] [Tooltip("Base aircraft view weight (calm flight)")]
        [Range(0f, 1f)] private float baseAircraftViewWeight = 0.2f;
        [SerializeField] [Tooltip("Max aircraft view weight (aggressive maneuvers)")]
        [Range(0f, 1f)] private float maxAircraftViewWeight = 0.6f;
        [SerializeField] [Tooltip("Predictive positioning factor")]
        [Range(0f, 2f)] private float predictiveFactor = 1.2f;
        [SerializeField] [Tooltip("Action-based offset multiplier")]
        [Range(0f, 3f)] private float actionOffsetMultiplier = 1.5f;

        [Header("Advanced Response")]
        [SerializeField] [Tooltip("Speed-based distance scaling")]
        [Range(0f, 2f)] private float speedDistanceScale = 0.8f;
        [SerializeField] [Tooltip("Maneuver intensity detection threshold")]
        [Range(0f, 2f)] private float maneuverThreshold = 0.5f;
        [SerializeField] [Tooltip("Look-ahead prediction time")]
        [Range(0f, 2f)] private float lookAheadTime = 0.8f;

        [Header("Flight Settings")]
        [SerializeField] [Tooltip("Autopilot sensitivity - enhanced for responsive flight")]
        private float autopilotSensitivity = 2.0f;  // 1.5 → 2.0 (daha responsive)
        [SerializeField] [Tooltip("Enable autopilot system")]
        private bool enableAutopilot = true;

        [Header("Input Settings")]
        [SerializeField] [Tooltip("Strafe speed multiplier")]
        private float strafeSpeedMultiplier = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        [Header("Mouse Control Limits")]
        [SerializeField] [Tooltip("Maximum pitch angle (up/down) in degrees")]
        private float maxPitchAngle = 20f;
        [SerializeField] [Tooltip("Maximum yaw angle (left/right) in degrees")]
        private float maxYawAngle = 30f;

        [SerializeField] [Tooltip("Mouse input responsiveness (much lower = much smoother)")]
        private float mouseResponsiveness = 5.0f;  // 3.0 → 5.0 (daha hızlı response)
        [SerializeField] [Tooltip("Mouse deadzone - ignore very small movements")]
        private float mouseDeadzone = 0.01f;


        // Mouse aim freeze system
        private Vector3 frozenDirection = Vector3.forward;
        private bool isMouseAimFrozen = false;

        // Debug throttling
        private float lastWarningTime = 0f;
        private float warningCooldown = 2f; // 2 saniyede bir warning

        // Ship reference
        private ShipClass shipClass = null;

        // Dodge system
        private bool isDodging = false;
        private float lastAPress = 0f;
        private float lastDPress = 0f;
        private float lastSPress = 0f;
        private float doubleTapWindow = 0.3f;

        // Mouse aim tracking - screen space tabanlı
        private Vector2 mouseInput = Vector2.zero;
        private Vector2 targetMouseInput = Vector2.zero;

        public enum DodgeDirection { Left, Right, Back }

        // Smart Camera System - Context Tracking
        private Vector3 lastAircraftPosition;
        private Vector3 lastAircraftVelocity;
        private Vector3 currentAircraftVelocity;
        private float maneuverIntensity = 0f;
        private float currentSpeedFactor = 0f;
        private float dynamicAircraftViewWeight = 0f;
        private Vector3 predictedAircraftPosition;
        private bool smartCameraInitialized = false;

        /// <summary>
        /// Aircraft's forward aim position - for crosshair
        /// </summary>
        public Vector3 BoresightPos
        {
            get
            {
                return aircraft == null
                     ? transform.forward * aimDistance
                     : (aircraft.transform.forward * aimDistance) + aircraft.transform.position;
            }
        }

        /// <summary>
        /// Mouse aim position - where player wants to fly (screen-space based)
        /// </summary>
        public Vector3 MouseAimPos
        {
            get
            {
                if (aircraft != null)
                {
                    try
                    {
                        if (isMouseAimFrozen)
                        {
                            return GetFrozenMouseAimPos();
                        }
                        else
                        {
                            // AIRCRAFT-RELATIVE calculation - mouse offset from aircraft direction
                            Vector3 aircraftForward = aircraft.forward;
                            Vector3 aircraftUp = aircraft.up;
                            Vector3 aircraftRight = aircraft.right;
                            
                            // Apply mouse input as offset from aircraft direction
                            Quaternion yawOffset = Quaternion.AngleAxis(mouseInput.x, aircraftUp);
                            Quaternion pitchOffset = Quaternion.AngleAxis(mouseInput.y, aircraftRight);
                            
                            Vector3 aimDirection = yawOffset * pitchOffset * aircraftForward;
                            return aircraft.position + (aimDirection * aimDistance);
                        }
                    }
                    catch (System.Exception)
                    {
                        // Referans bozuksa default değer döndür
                        if (Time.time - lastWarningTime > warningCooldown)
                        {
                            Debug.LogWarning($"{name}: Aircraft referansı bozuk - default değer kullanılıyor");
                            lastWarningTime = Time.time;
                        }
                        return transform.position + (transform.forward * aimDistance);
                    }
                }
                else
                {
                    return transform.position + (transform.forward * aimDistance);
                }
            }
        }

        private void Awake()
        {
            Debug.Log($"{name}: MouseFlight Controller başlatılıyor...");
        }



        private void Start()
        {
            // MouseFlight rule: rig must not be parented to anything
            if (transform.parent != null)
            {
                Debug.Log($"{name}: Parent'tan ayrılıyor: {transform.parent.name}");
                transform.parent = null;
            }
            
            // Find ship reference
            shipClass = FindFirstObjectByType<ShipClass>();
            if (shipClass == null)
            {
                Debug.LogError(name + ": No ShipClass found in scene!");
            }
            
            // Setup references
            FindMissingReferences();
            
            // Initialize mouse to center position
            ResetMouseAim();
            
            Debug.Log($"{name}: MouseFlight Controller hazır!");
        }

        private void FindMissingReferences()
        {
            try
            {
                // Aircraft (Player tagged GameObject)
                if (aircraft == null)
                {
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null)
                    {
                        aircraft = playerObj.transform;
                        Debug.Log($"{name}: Aircraft bulundu: {aircraft.name}");
                    }
                    else
                    {
                        Debug.LogError($"{name}: Player tagged GameObject bulunamadı!");
                    }
                }

                // MouseAim - always create as child of this GameObject
                if (mouseAim == null)
                {
                    GameObject mouseAimObj = transform.Find("MouseAim")?.gameObject;
                    if (mouseAimObj == null)
                    {
                        mouseAimObj = new GameObject("MouseAim");
                        mouseAimObj.transform.SetParent(transform);
                        mouseAimObj.transform.localPosition = Vector3.zero;
                        mouseAimObj.transform.localRotation = Quaternion.identity;
                    }
                    mouseAim = mouseAimObj.transform;
                }

                // CameraRig - always create as child of this GameObject
                if (cameraRig == null)
                {
                    GameObject cameraRigObj = transform.Find("CameraRig")?.gameObject;
                    if (cameraRigObj == null)
                    {
                        cameraRigObj = new GameObject("CameraRig");
                        cameraRigObj.transform.SetParent(transform);
                        cameraRigObj.transform.localPosition = Vector3.zero;
                        cameraRigObj.transform.localRotation = Quaternion.identity;
                    }
                    cameraRig = cameraRigObj.transform;
                }

                // Camera - find existing or create as child of CameraRig
                if (cam == null && cameraRig != null)
                {
                    // First try to find existing main camera
                    Camera existingCam = Camera.main;
                    if (existingCam != null)
                    {
                        // Move existing camera to CameraRig - third person position
                        existingCam.transform.SetParent(cameraRig);
                        existingCam.transform.localPosition = new Vector3(0, 2, -5); // Better 3rd person view
                        existingCam.transform.localRotation = Quaternion.identity;
                        cam = existingCam;
                    }
                    else
                    {
                        // Create new camera - third person position
                        GameObject cameraObj = new GameObject("Main Camera");
                        cameraObj.transform.SetParent(cameraRig);
                        cameraObj.transform.localPosition = new Vector3(0, 2, -5); // Better 3rd person view
                        cameraObj.transform.localRotation = Quaternion.identity;
                        
                        cam = cameraObj.AddComponent<Camera>();
                        cameraObj.AddComponent<AudioListener>();
                        cameraObj.tag = "MainCamera";
                        
                        // Camera settings - wider FOV for better aircraft visibility
                        cam.fieldOfView = 75f;  // Increased from 60° to 75°
                        cam.nearClipPlane = 1f;
                        cam.farClipPlane = 10000f;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{name}: FindMissingReferences hatası: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void Update()
        {
            // System enable/disable control
            if (!systemEnabled)
                return;

            // Runtime referans kontrolü - bozuk referansları düzelt
            bool needsRefresh = false;
            
            if (mouseAim == null || (mouseAim != null && mouseAim.gameObject == null))
            {
                mouseAim = null;
                needsRefresh = true;
            }
            
            if (cameraRig == null || (cameraRig != null && cameraRig.gameObject == null))
            {
                cameraRig = null;
                needsRefresh = true;
            }
            
            if (cam == null || (cam != null && cam.gameObject == null))
            {
                cam = null;
                needsRefresh = true;
            }
            
            if (aircraft == null || (aircraft != null && aircraft.gameObject == null))
            {
                aircraft = null;
                needsRefresh = true;
            }

            if (needsRefresh && Time.time - lastWarningTime > warningCooldown)
            {
                Debug.LogWarning($"{name}: Referanslar runtime'da kayboldu - yeniden bulunuyor...");
                FindMissingReferences();
                lastWarningTime = Time.time;
            }

            // Smart Camera Context Tracking (only if camera movement enabled)
            if (enableCameraMovement)
                UpdateSmartCameraContext();

            if (useFixed == false && enableCameraMovement)
                UpdateCameraPos();

            HandleInput();
            RotateRig();
            
            // Transform-based autopilot - run in Update (no physics timing needed)
            if (shipClass != null && enableAutopilot)
            {
                RunAutopilot();
            }
        }

        private void FixedUpdate()
        {
            // System enable/disable control
            if (!systemEnabled)
                return;

            // Transform-based system only needs camera updates in FixedUpdate
            if (useFixed == true && enableCameraMovement)
                UpdateCameraPos();
        }

        private void HandleInput()
        {
            // Free look toggle (C key)
            if (Input.GetKeyDown(KeyCode.C))
            {
                isMouseAimFrozen = true;
                frozenDirection = mouseAim.forward;
            }
            else if (Input.GetKeyUp(KeyCode.C))
            {
                isMouseAimFrozen = false;
                mouseAim.forward = frozenDirection;
            }

            // Reset mouse aim to center (R key)
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetMouseAim();
            }

            // Strafe input (A/D keys)
            HandleStrafeInput();

            // Dodge input (double-tap)
            HandleDodgeInput();
        }

        private void ResetMouseAim()
        {
            targetMouseInput = Vector2.zero;
            mouseInput = Vector2.zero;
            Debug.Log($"{name}: Mouse aim reset to center");
        }

        private void HandleStrafeInput()
        {
            if (shipClass == null) return;

            float strafeInput = 0f;

            if (Input.GetKey(KeyCode.A))
                strafeInput = -1f * strafeSpeedMultiplier; // Left strafe with multiplier
            else if (Input.GetKey(KeyCode.D))
                strafeInput = 1f * strafeSpeedMultiplier;  // Right strafe with multiplier

            shipClass.SetStrafeInput(strafeInput);
        }

        private void HandleDodgeInput()
        {
            if (isDodging) return;

            // Double A = Left barrel roll
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (Time.time - lastAPress < doubleTapWindow)
                {
                    PerformDodge(DodgeDirection.Left);
                }
                lastAPress = Time.time;
            }

            // Double D = Right barrel roll
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (Time.time - lastDPress < doubleTapWindow)
                {
                    PerformDodge(DodgeDirection.Right);
                }
                lastDPress = Time.time;
            }

            // Double S = Backflip
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (Time.time - lastSPress < doubleTapWindow)
                {
                    PerformDodge(DodgeDirection.Back);
                }
                lastSPress = Time.time;
            }
        }

        private void PerformDodge(DodgeDirection direction)
        {
            isDodging = true;
            Debug.Log($"Performing {direction} dodge!");

            // Reset dodge after 1 second
            Invoke(nameof(ResetDodge), 1f);
        }

        private void ResetDodge()
        {
            isDodging = false;
        }

        private void RotateRig()
        {
            // Güvenli referans kontrolü - herhangi biri null ise çık
            if (mouseAim == null || cam == null || cameraRig == null)
            {
                // Referansları yeniden bul
                if (Time.time - lastWarningTime > warningCooldown)
                {
                    Debug.LogWarning($"{name}: RotateRig referansları eksik - yeniden bulunuyor...");
                    FindMissingReferences();
                    lastWarningTime = Time.time;
                }
                return;
            }

            // Ek güvenlik kontrolü - GameObject'ler mevcut mu?
            if (mouseAim.gameObject == null || cam.gameObject == null || cameraRig.gameObject == null)
            {
                Debug.LogWarning($"{name}: RotateRig GameObject'leri null - referanslar sıfırlanıyor");
                mouseAim = null;
                cam = null;
                cameraRig = null;
                return;
            }

            try
            {
                // Absolute mouse position system - like classic MouseFlight
                if (!isMouseAimFrozen)
                {
                    // Get mouse position in screen space (0-1)
                    Vector2 mouseScreenPos = new Vector2(
                        Input.mousePosition.x / Screen.width,
                        Input.mousePosition.y / Screen.height
                    );
                    
                    // Convert to centered coordinates (-0.5 to +0.5)
                    mouseScreenPos -= Vector2.one * 0.5f;
                    
                    // Apply deadzone to screen position
                    if (Mathf.Abs(mouseScreenPos.x) < mouseDeadzone) mouseScreenPos.x = 0f;
                    if (Mathf.Abs(mouseScreenPos.y) < mouseDeadzone) mouseScreenPos.y = 0f;
                    
                    // Convert screen position to target angles - köşelerde tam limit
                    targetMouseInput.x = mouseScreenPos.x * 2f * maxYawAngle;   // Ekran kenarı = max yaw
                    targetMouseInput.y = mouseScreenPos.y * 2f * maxPitchAngle; // Ekran kenarı = max pitch (removed negative)
                    
                    // Clamp to limits (safety)
                    targetMouseInput.x = Mathf.Clamp(targetMouseInput.x, -maxYawAngle, maxYawAngle);
                    targetMouseInput.y = Mathf.Clamp(targetMouseInput.y, -maxPitchAngle, maxPitchAngle);
                    
                    // Debug position (active for troubleshooting)
                    if (Time.frameCount % 30 == 0)  // Every 0.5 seconds for debugging
                    {
                        float distanceToAircraft = aircraft != null ? Vector3.Distance(transform.position, aircraft.position) : 0f;
                        Debug.Log($"SMART CAMERA DEBUG - Mouse: ({mouseInput.x:F1}°, {mouseInput.y:F1}°) | Distance: {distanceToAircraft:F1} | ManeuverIntensity: {maneuverIntensity:F2} | ViewWeight: {dynamicAircraftViewWeight:F2} | Speed: {currentSpeedFactor:F2}");
                    }
                }

                // Smooth current input towards target
                mouseInput = Vector2.Lerp(mouseInput, targetMouseInput, mouseResponsiveness * Time.deltaTime);

                                    // HYBRID CAMERA SYSTEM - mouse control + aircraft visibility
                    if (aircraft != null)
                    {
                        CalculateHybridCameraRotation();
                    }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"{name}: RotateRig exception - {ex.Message}. Referanslar sıfırlanıyor.");
                mouseAim = null;
                cam = null;
                cameraRig = null;
            }
        }

        private void UpdateCameraPos()
        {
            if (aircraft != null && aircraft.gameObject != null)
            {
                try
                {
                    // Dynamic 3rd Person Camera: ensures aircraft always visible
                    CalculateOptimalCameraPosition();
                }
                catch (System.Exception)
                {
                    Debug.LogWarning($"{name}: Aircraft referansı bozuk - sıfırlanıyor");
                    aircraft = null;
                }
            }
        }

        private void UpdateSmartCameraContext()
        {
            if (aircraft == null) return;

            // Initialize on first frame
            if (!smartCameraInitialized)
            {
                lastAircraftPosition = aircraft.position;
                lastAircraftVelocity = Vector3.zero;
                smartCameraInitialized = true;
                return;
            }

            // Calculate aircraft velocity and acceleration
            Vector3 currentPos = aircraft.position;
            currentAircraftVelocity = (currentPos - lastAircraftPosition) / Time.deltaTime;
            Vector3 acceleration = (currentAircraftVelocity - lastAircraftVelocity) / Time.deltaTime;

            // Calculate maneuver intensity (0-1)
            float accelerationMagnitude = acceleration.magnitude;
            float velocityMagnitude = currentAircraftVelocity.magnitude;
            
            maneuverIntensity = Mathf.Clamp01(accelerationMagnitude / 100f); // Normalize acceleration
            currentSpeedFactor = Mathf.Clamp01(velocityMagnitude / 150f); // Normalize speed (assuming max ~150 units/sec)

            // Calculate dynamic view weight based on maneuver intensity
            float targetViewWeight = Mathf.Lerp(baseAircraftViewWeight, maxAircraftViewWeight, maneuverIntensity);
            dynamicAircraftViewWeight = Mathf.Lerp(dynamicAircraftViewWeight, targetViewWeight, 3f * Time.deltaTime);

            // Predict aircraft position for look-ahead
            predictedAircraftPosition = currentPos + (currentAircraftVelocity * lookAheadTime);

            // Update tracking variables
            lastAircraftPosition = currentPos;
            lastAircraftVelocity = currentAircraftVelocity;
        }

        private void CalculateOptimalCameraPosition()
        {
            // Get aircraft orientation and context
            Vector3 aircraftPos = aircraft.position;
            Vector3 aircraftForward = aircraft.forward;
            Vector3 aircraftUp = aircraft.up;
            
            // Calculate current pitch angle
            float currentPitch = Vector3.Angle(Vector3.up, aircraftUp) - 90f;
            currentPitch = Mathf.Clamp(currentPitch, -90f, 90f);
            
            // Dynamic distance based on multiple factors
            float pitchFactor = Mathf.Abs(currentPitch) / 90f; // 0-1
            float speedFactor = currentSpeedFactor * speedDistanceScale; // Speed influence
            float maneuverFactor = maneuverIntensity * actionOffsetMultiplier; // Maneuver influence

            // Combined distance calculation
            float dynamicDistance = cameraFollowDistance 
                + (pitchFactor * pitchDistanceMultiplier * cameraFollowDistance)
                + (speedFactor * cameraFollowDistance * 0.5f)
                + (maneuverFactor * cameraFollowDistance * 0.3f);
            
            dynamicDistance = Mathf.Clamp(dynamicDistance, minCameraDistance, maxCameraDistance);
            
            // Use horizontal direction for base positioning (no roll influence)
            Vector3 horizontalForward = Vector3.ProjectOnPlane(aircraftForward, Vector3.up).normalized;
            Vector3 horizontalBack = -horizontalForward;
            
            // Predictive positioning - use predicted position for high maneuvers
            Vector3 targetPos = Vector3.Lerp(aircraftPos, predictedAircraftPosition, maneuverIntensity * predictiveFactor);
            
            // Calculate base camera position with action-based offsets
            Vector3 baseCameraPos = targetPos 
                + horizontalBack * dynamicDistance    // Behind aircraft (dynamic distance)
                + Vector3.up * (cameraHeight + maneuverFactor * 5f); // Higher during maneuvers
            
            // Smart positioning: ensure aircraft stays in view
            Vector3 idealCameraPos = CalculateSmartCameraPosition(aircraftPos, baseCameraPos);
            
            // Smooth camera rig movement with dynamic speed
            float smoothSpeed = camSmoothSpeed * (1f + maneuverIntensity * 0.5f); // Faster during maneuvers
            transform.position = Vector3.Lerp(transform.position, idealCameraPos, 
                smoothSpeed * Time.deltaTime);
        }
        
        private Vector3 CalculateSmartCameraPosition(Vector3 aircraftPos, Vector3 baseCameraPos)
        {
            // Advanced visibility checking with predictive positioning
            if (cam != null && cam.gameObject != null)
            {
                // Calculate if aircraft would be in camera frustum from base position
                Vector3 dirToAircraft = (aircraftPos - baseCameraPos).normalized;
                Vector3 currentCameraForward = cameraRig != null ? cameraRig.forward : transform.forward;
                
                float dotProduct = Vector3.Dot(currentCameraForward, dirToAircraft);
                
                // Enhanced visibility thresholds based on maneuver intensity
                float visibilityThreshold = Mathf.Lerp(0.3f, 0.6f, maneuverIntensity); // Stricter during maneuvers
                
                // If aircraft would be behind camera or too far to the side, adjust position
                if (dotProduct < visibilityThreshold)
                {
                    // Smart offset calculation based on context
                    float offsetDistance = 10f + (maneuverIntensity * 15f); // More offset during maneuvers
                    Vector3 smartOffset = (baseCameraPos - aircraftPos).normalized * offsetDistance;
                    
                    // Also consider predictive positioning
                    Vector3 predictiveOffset = Vector3.zero;
                    if (maneuverIntensity > maneuverThreshold)
                    {
                        Vector3 predictiveDir = (predictedAircraftPosition - aircraftPos).normalized;
                        predictiveOffset = -predictiveDir * (predictiveFactor * 8f);
                    }
                    
                    Vector3 adjustedPos = baseCameraPos + smartOffset + predictiveOffset;
                    return Vector3.Lerp(baseCameraPos, adjustedPos, dynamicAircraftViewWeight);
                }
            }
            
            return baseCameraPos;
        }

        private void CalculateHybridCameraRotation()
        {
            if (mouseAim == null || cameraRig == null || aircraft == null) return;

            // Get aircraft orientation and context
            Vector3 aircraftForward = aircraft.forward;
            Vector3 aircraftUp = aircraft.up;
            Vector3 aircraftRight = aircraft.right;
            Vector3 aircraftPos = aircraft.position;
            
            // 1. Calculate mouse-based look direction (original behavior)
            Quaternion yawOffset = Quaternion.AngleAxis(mouseInput.x, aircraftUp);
            Quaternion pitchOffset = Quaternion.AngleAxis(mouseInput.y, aircraftRight);
            Vector3 mouseLookDirection = yawOffset * pitchOffset * aircraftForward;
            
            // Update mouseAim (for aiming systems)
            if (mouseAim != null)
            {
                mouseAim.rotation = Quaternion.LookRotation(mouseLookDirection, aircraftUp);
            }

            // 2. Calculate contextual look-at directions
            Vector3 cameraPos = transform.position;
            Vector3 dirToAircraft = (aircraftPos - cameraPos).normalized;
            
            // Predictive look-ahead for high maneuvers
            Vector3 dirToPredicted = Vector3.zero;
            if (maneuverIntensity > maneuverThreshold)
            {
                dirToPredicted = (predictedAircraftPosition - cameraPos).normalized;
            }
            
            // 3. Smart blending based on context
            Vector3 contextualLookDirection = dirToAircraft;
            
            // Use predictive direction during intense maneuvers
            if (maneuverIntensity > maneuverThreshold)
            {
                contextualLookDirection = Vector3.Slerp(dirToAircraft, dirToPredicted, 
                    maneuverIntensity * predictiveFactor * 0.5f);
            }
            
            // 4. Dynamic blend ratio based on maneuver intensity and speed
            float currentViewWeight = dynamicAircraftViewWeight;
            
            // Increase aircraft tracking during aggressive maneuvers
            if (maneuverIntensity > maneuverThreshold)
            {
                currentViewWeight = Mathf.Lerp(currentViewWeight, maxAircraftViewWeight, 
                    (maneuverIntensity - maneuverThreshold) * 2f);
            }
            
            // 5. Final look direction blend
            Vector3 finalLookDirection = Vector3.Slerp(mouseLookDirection, contextualLookDirection, currentViewWeight);
            
            // 6. Calculate proper up vector for natural camera behavior
            Vector3 upVector = Vector3.up;
            
            // Avoid gimbal lock when looking straight up/down
            if (Mathf.Abs(finalLookDirection.y) > 0.9f)
            {
                upVector = cameraRig.up; // Use current camera up when nearly vertical
            }
            
            // 7. Apply smooth camera rotation with dynamic responsiveness
            float rotationSpeed = camSmoothSpeed * (1f + maneuverIntensity * 0.3f); // Faster during maneuvers
            Quaternion targetRotation = Quaternion.LookRotation(finalLookDirection, upVector);
            cameraRig.rotation = Damp(cameraRig.rotation, targetRotation, rotationSpeed, Time.deltaTime);
        }

        private void RunAutopilot()
        {
            if (shipClass == null || mouseAim == null || aircraft == null) return;

            // GameObject kontrolü
            if (mouseAim.gameObject == null || aircraft.gameObject == null)
            {
                Debug.LogWarning($"{name}: Autopilot referansları null - sıfırlanıyor");
                mouseAim = null;
                aircraft = null;
                return;
            }

            try
            {
                // SIMPLE DIRECT CONTROL - mouse input directly to aircraft
                // Skip complex world space calculations - use mouse input directly
                float yawInput = mouseInput.x / maxYawAngle;    // Direct yaw from mouse X
                float pitchInput = -mouseInput.y / maxPitchAngle; // Direct pitch from mouse Y (inverted)

                // Clamp inputs to safe range
                yawInput = Mathf.Clamp(yawInput, -1f, 1f);
                pitchInput = Mathf.Clamp(pitchInput, -1f, 1f);

                // Simple roll control - just gentle banking
                float rollInput = -yawInput * 0.3f; // Gentle banking based on yaw
                
                // Simple deadzone for very small inputs to prevent jitter
                if (Mathf.Abs(yawInput) < 0.05f) yawInput = 0f;
                if (Mathf.Abs(pitchInput) < 0.05f) pitchInput = 0f;
                if (Mathf.Abs(rollInput) < 0.05f) rollInput = 0f;

                // Debug - active autopilot info for troubleshooting
                if (Time.frameCount % 30 == 0)  // Every 0.5 seconds
                {
                    Debug.Log($"AUTOPILOT DEBUG - Pitch: {pitchInput:F2}, Yaw: {yawInput:F2}, Roll: {rollInput:F2} | MouseInput: ({mouseInput.x:F1}, {mouseInput.y:F1}) | Active: {(Mathf.Abs(yawInput) > 0.05f || Mathf.Abs(pitchInput) > 0.05f)}");
                }

                // Send inputs to ship
                shipClass.SetPitchInput(pitchInput);
                shipClass.SetYawInput(yawInput);
                shipClass.SetRollInput(rollInput);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"{name}: Autopilot exception - {ex.Message}. Referanslar kontrol ediliyor.");
                if (mouseAim == null || mouseAim.gameObject == null) mouseAim = null;
                if (aircraft == null || aircraft.gameObject == null) aircraft = null;
            }
        }

        private Vector3 GetFrozenMouseAimPos()
        {
            if (mouseAim != null)
                return mouseAim.position + (frozenDirection * aimDistance);
            else
                return transform.forward * aimDistance;
        }

        // MouseFlight's framerate independent damping
        private Quaternion Damp(Quaternion a, Quaternion b, float lambda, float dt)
        {
            return Quaternion.Slerp(a, b, 1 - Mathf.Exp(-lambda * dt));
        }

        private void OnDrawGizmos()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            Color oldColor = Gizmos.color;

            // Draw boresight
            if (aircraft != null)
            {
                try
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireSphere(BoresightPos, 10f);
                }
                catch (System.Exception) { /* Ignore */ }
            }

            // Draw mouse aim - güvenli referans kontrolü
            if (mouseAim != null && mouseAim.gameObject != null)
            {
                try
                {
                    // Basit pozisyon kontrolü - exception fırlatırsa referans bozuk
                    Vector3 testPos = mouseAim.position;
                    
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(MouseAimPos, 10f);

                    // Draw mouse aim axes
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(mouseAim.position, mouseAim.forward * 50f);
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(mouseAim.position, mouseAim.up * 50f);
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(mouseAim.position, mouseAim.right * 50f);
                }
                catch (System.Exception)
                {
                    // Referans bozuksa hata vermeden geç - throttled warning
                    if (Time.time - lastWarningTime > warningCooldown)
                    {
                        Debug.LogWarning($"{name}: MouseAim referansı bozuk - Inspector'da yeniden ata");
                        lastWarningTime = Time.time;
                        
                        // Referansı null yap ki yeniden bulunabilsin
                        mouseAim = null;
                    }
                }
            }

            Gizmos.color = oldColor;
        }
    }
} 