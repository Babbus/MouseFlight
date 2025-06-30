using UnityEngine;
using DomeClash.Ships;

namespace DomeClash.Core
{
    /// <summary>
    /// Mouse Flight Controller - Handles mouse-based flight input conversion
    /// Converts mouse position to ship control commands
    /// Unity 6000.1.9f1 Compatible
    /// </summary>
    public class MouseFlightController : MonoBehaviour
    {
        [Header("System Control")]
        [SerializeField] [Tooltip("Enable/disable entire flight controller system")]
        public bool systemEnabled = true;

        [Header("Aircraft Components")]
        [SerializeField] [Tooltip("Transform of the aircraft the controller follows")]
        private Transform aircraft = null;
        [SerializeField] [Tooltip("Transform that mouse rotates to generate aim position")]
        private Transform mouseAim = null;

        [Header("Mouse Input Conversion")]
        [SerializeField] [Tooltip("Enable mouse input conversion system")]
        private bool enableInputConversion = true;

        [Header("Input Settings")]
        [SerializeField] [Tooltip("Strafe input transition speed (general value, will be motor-based)")]
        private float strafeTransitionSpeed = 8f;
        [SerializeField] [Tooltip("Current smooth strafe input value")]
        private float currentStrafeInput = 0f;
        [SerializeField] [Tooltip("Target strafe input value")]
        private float targetStrafeInput = 0f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        [Header("Enhanced Mouse Control")]
        [SerializeField] [Tooltip("Maximum pitch angle (up/down) in degrees")]
        private float maxPitchAngle = 15f;
        [SerializeField] [Tooltip("Maximum yaw angle (left/right) in degrees")]
        private float maxYawAngle = 20f;
        
        [Header("Flight Control Settings")]
        [SerializeField] [Tooltip("Mouse input responsiveness (higher = more responsive)")]
        private float mouseResponsiveness = 0.8f;
        [SerializeField] [Tooltip("Instant input mode - no smoothing (most responsive)")]
        private bool instantInputMode = true;

        // Debug throttling
        private float lastWarningTime = 0f;
        private float warningCooldown = 2f;

        // Ship reference
        private PrototypeShip shipClass = null;

        // Dodge system
        private bool isDodging = false;
        private float lastAPress = 0f;
        private float lastDPress = 0f;
        private float lastSPress = 0f;
        private float doubleTapWindow = 0.3f;

        // Mouse aim tracking
        private Vector2 mouseInput = Vector2.zero;
        private Vector2 targetMouseInput = Vector2.zero;
        private Vector2 lastMouseInput = Vector2.zero;
        
        // Enhanced Control Variables  
        private Vector2 lastMouseScreenPosition = Vector2.zero;

        public enum DodgeDirection { Left, Right, Back }

        /// <summary>
        /// Aircraft's forward aim position - for crosshair
        /// </summary>
        public Vector3 BoresightPos
        {
            get
            {
                return aircraft == null
                     ? transform.forward * 1000f
                     : (aircraft.transform.forward * 1000f) + aircraft.transform.position;
            }
        }
        
        /// <summary>
        /// Flight control system info for debugging
        /// </summary>
        public bool IsUsingEnhancedControl => true;

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
                        // AIRCRAFT-RELATIVE calculation - mouse offset from aircraft direction
                        Vector3 aircraftForward = aircraft.forward;
                        Vector3 aircraftUp = aircraft.up;
                        Vector3 aircraftRight = aircraft.right;
                        
                        // Apply mouse input as offset from aircraft direction
                        Quaternion yawOffset = Quaternion.AngleAxis(mouseInput.x, aircraftUp);
                        Quaternion pitchOffset = Quaternion.AngleAxis(mouseInput.y, aircraftRight);
                        
                        Vector3 aimDirection = yawOffset * pitchOffset * aircraftForward;
                        return aircraft.position + (aimDirection * 1000f);
                    }
                    catch (System.Exception)
                    {
                        if (Time.time - lastWarningTime > warningCooldown)
                        {
                            Debug.LogWarning($"{name}: Aircraft reference broken - using default value");
                            lastWarningTime = Time.time;
                        }
                        return transform.position + (transform.forward * 1000f);
                    }
                }
                else
                {
                    return transform.position + (transform.forward * 1000f);
                }
            }
        }

        private void Awake()
        {
            Debug.Log($"{name}: MouseFlight Controller initializing...");
        }

        private void Start()
        {
            // MouseFlight rule: rig must not be parented to anything
            if (transform.parent != null)
            {
                Debug.Log($"{name}: Separating from parent: {transform.parent.name}");
                transform.parent = null;
            }
            
            // Find ship reference
            shipClass = FindFirstObjectByType<PrototypeShip>();
            if (shipClass == null)
            {
                Debug.LogError(name + ": No PrototypeShip found in scene!");
            }
            
            // Setup references
            FindMissingReferences();
            
            // Initialize mouse to center position
            ResetMouseAim();
            
            // Initialize mouse position tracking for movement-based system
            lastMouseScreenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            
            Debug.Log($"{name}: MouseFlight Controller ready!");
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
                        Debug.Log($"{name}: Aircraft found: {aircraft.name}");
                    }
                    else
                    {
                        Debug.LogError($"{name}: Player tagged GameObject not found!");
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
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{name}: Error finding references - {ex.Message}");
            }
        }

        private void Update()
        {
            if (!systemEnabled) return;

            try
            {
                HandleInput();
                ProcessProgressiveStrafeInput();
                ConvertMouseInputToShipControl_Enhanced();
            }
            catch (System.Exception ex)
            {
                if (Time.time - lastWarningTime > warningCooldown)
                {
                    Debug.LogWarning($"{name}: Update exception - {ex.Message}");
                    lastWarningTime = Time.time;
                }
            }
        }

        private void HandleInput()
        {
            if (!enableInputConversion) return;

            // Enhanced mouse input processing
            Vector2 currentMouseScreenPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            
            // Calculate mouse movement from screen center
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 mouseOffset = currentMouseScreenPos - screenCenter;
            
            // Convert to normalized angles
            float yawAngle = (mouseOffset.x / screenCenter.x) * maxYawAngle;
            float pitchAngle = -(mouseOffset.y / screenCenter.y) * maxPitchAngle; // Inverted for natural feel
            
            // Apply responsiveness and instant input mode
            if (instantInputMode)
            {
                mouseInput = new Vector2(yawAngle, pitchAngle);
            }
            else
            {
                // Smooth input (not used currently)
                targetMouseInput = new Vector2(yawAngle, pitchAngle);
                mouseInput = Vector2.Lerp(mouseInput, targetMouseInput, mouseResponsiveness * Time.deltaTime);
            }
            
            // Update mouseAim rotation for aiming systems
            if (mouseAim != null && aircraft != null)
            {
                try
                {
                    Vector3 aircraftForward = aircraft.forward;
                    Vector3 aircraftUp = aircraft.up;
                    Vector3 aircraftRight = aircraft.right;
                    
                    Quaternion yawOffset = Quaternion.AngleAxis(mouseInput.x, aircraftUp);
                    Quaternion pitchOffset = Quaternion.AngleAxis(mouseInput.y, aircraftRight);
                    Vector3 mouseLookDirection = yawOffset * pitchOffset * aircraftForward;
                    
                    mouseAim.rotation = Quaternion.LookRotation(mouseLookDirection, aircraftUp);
                }
                catch (System.Exception)
                {
                    // Handle broken references
                    if (Time.time - lastWarningTime > warningCooldown)
                    {
                        Debug.LogWarning($"{name}: MouseAim rotation failed - reference issue");
                        lastWarningTime = Time.time;
                    }
                }
            }
            
            // Handle keyboard input
            HandleStrafeInput();
            HandleDodgeInput();
            
            // Update tracking variables
            lastMouseScreenPosition = currentMouseScreenPos;
            lastMouseInput = mouseInput;
        }

        private void ResetMouseAim()
        {
            mouseInput = Vector2.zero;
            targetMouseInput = Vector2.zero;
            lastMouseInput = Vector2.zero;
            
            if (mouseAim != null && aircraft != null)
            {
                try
                {
                    mouseAim.rotation = aircraft.rotation;
                }
                catch (System.Exception) { /* Ignore */ }
            }
        }

        private void HandleStrafeInput()
        {
            // Progressive strafe input system
            float rawStrafeInput = 0f;
            
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                rawStrafeInput -= 1f;
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                rawStrafeInput += 1f;
            }
            
            targetStrafeInput = Mathf.Clamp(rawStrafeInput, -1f, 1f);
        }

        private void ProcessProgressiveStrafeInput()
        {
            if (shipClass == null) return;

            // Remove smoothing: set currentStrafeInput directly
            currentStrafeInput = targetStrafeInput;
            // Apply deadzone to prevent drift
            if (Mathf.Abs(currentStrafeInput) < 0.01f)
            {
                currentStrafeInput = 0f;
            }
        }

        private float GetMotorBasedStrafeTransitionSpeed()
        {
            // Get flight controller for motor stats
            if (shipClass != null)
            {
                var flightController = shipClass.GetComponent<ShipFlightController>();
                if (flightController != null)
                {
                    var flightProfile = flightController.GetFlightProfile();
                    if (flightProfile != null)
                    {
                        // ENGINE-BASED STRAFE RESPONSE
                        // Higher turnSpeed = faster strafe response
                        float engineResponseFactor = flightProfile.turnSpeed / 60f; // Normalize around 60 deg/s
                        float massInertiaFactor = 400f / (flightProfile.mass * flightProfile.inertiaFactor); // Heavier = slower
                        float motorBasedSpeed = strafeTransitionSpeed * engineResponseFactor * massInertiaFactor;
                        
                        return Mathf.Clamp(motorBasedSpeed, 2f, 20f); // Clamp to reasonable range
                    }
                }
            }
            
            return strafeTransitionSpeed; // Fallback to default
        }

        private void HandleDodgeInput()
        {
            float currentTime = Time.time;
            
            // Double-tap detection for dodge
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (currentTime - lastAPress < doubleTapWindow)
                {
                    PerformDodge(DodgeDirection.Left);
                }
                lastAPress = currentTime;
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                if (currentTime - lastDPress < doubleTapWindow)
                {
                    PerformDodge(DodgeDirection.Right);
                }
                lastDPress = currentTime;
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                if (currentTime - lastSPress < doubleTapWindow)
                {
                    PerformDodge(DodgeDirection.Back);
                }
                lastSPress = currentTime;
            }
            
            // Reset dodge state after a delay
            if (isDodging && currentTime - lastAPress > doubleTapWindow * 2f)
            {
                ResetDodge();
            }
        }

        private void PerformDodge(DodgeDirection direction)
        {
            if (isDodging) return;
            
            isDodging = true;
            Debug.Log($"{name}: Dodge performed - {direction}");
            
            // TODO: Implement dodge mechanics
            // This could involve temporary speed boost, invulnerability, etc.
        }

        private void ResetDodge()
        {
            isDodging = false;
        }

        /// <summary>
        /// Enhanced Flight Control System
        /// Converts mouse position to ship control commands
        /// </summary>
        private void ConvertMouseInputToShipControl_Enhanced()
        {
            if (shipClass == null || mouseAim == null || aircraft == null) return;

            try
            {
                // OPTIMIZED MOUSE POSITION CONTROL
                // Direct mouse screen position to ship control
                float yawInput = mouseInput.x / maxYawAngle;      // Direct yaw from mouse X
                float pitchInput = mouseInput.y / maxPitchAngle;  // Direct pitch from mouse Y

                // Clamp inputs to safe range
                yawInput = Mathf.Clamp(yawInput, -1f, 1f);
                pitchInput = Mathf.Clamp(pitchInput, -1f, 1f);

                // SMOOTH MOUSE POSITION BANKING - Direct from mouse screen position
                // Calculate banking directly from mouse X position (not yaw input)
                Vector2 mouseScreenPos = new Vector2(
                    Input.mousePosition.x / Screen.width,
                    Input.mousePosition.y / Screen.height
                );
                mouseScreenPos -= Vector2.one * 0.5f; // Center coordinates (-0.5 to +0.5)
                
                // Direct banking from mouse X position - SMOOTH and LINEAR
                float rollInput = -mouseScreenPos.x * 1.2f; // Direct banking from screen position
                rollInput = Mathf.Clamp(rollInput, -1f, 1f);

                // DEBUG: Engine-based ship control inputs (MOTOR STAT INTEGRATION)
                // if (Time.frameCount % 30 == 0)  // Every 0.5 seconds for debugging
                // {
                //     Debug.Log($"ENGINE SHIP CONTROL DEBUG - YawInput: {yawInput:F3} | PitchInput: {pitchInput:F3} | " +
                //              $"RollInput: {rollInput:F3} (from mouse X: {mouseScreenPos.x:F3}) | " +
                //              $"MouseAngles: ({mouseInput.x:F1}°, {mouseInput.y:F1}°) | " +
                //              $"Ship: {(shipClass != null ? shipClass.name : "None")}");
                // }

                // Try to get ShipFlightController from shipClass
                if (shipClass != null)
                {
                    var flightMovement = shipClass.GetComponent<ShipFlightController>();
                    if (flightMovement != null)
                    {
                        // Send inputs to ShipFlightController
                        flightMovement.SetPitchInput(pitchInput);
                        flightMovement.SetYawInput(yawInput);
                        flightMovement.SetRollInput(rollInput);
                        flightMovement.SetStrafeInput(currentStrafeInput);
                        return;
                    }
                }

                // Fallback to legacy ship input if no ShipFlightController found
                if (shipClass != null)
                {
                    shipClass.SetPitchInput(pitchInput);
                    shipClass.SetYawInput(yawInput);
                    shipClass.SetRollInput(rollInput);
                    shipClass.SetStrafeInput(currentStrafeInput);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"{name}: Flight control exception - {ex.Message}");
                if (mouseAim == null || mouseAim.gameObject == null) mouseAim = null;
                if (aircraft == null || aircraft.gameObject == null) aircraft = null;
            }
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

            // Draw mouse aim
            if (mouseAim != null && mouseAim.gameObject != null)
            {
                try
                {
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
                    if (Time.time - lastWarningTime > warningCooldown)
                    {
                        Debug.LogWarning($"{name}: MouseAim reference broken - reassign in Inspector");
                        lastWarningTime = Time.time;
                        mouseAim = null;
                    }
                }
            }

            Gizmos.color = oldColor;
        }
    }
} 