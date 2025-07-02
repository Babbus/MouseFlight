using UnityEngine;
using DomeClash.Ships;

namespace DomeClash.Core
{
    /// <summary>
    /// Optimized 3rd Person Camera Controller
    /// Follows the ship from behind with smooth movement
    /// Always shows where the ship is facing in normal flight
    /// </summary>
    public class ShipCameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        
        [Header("Camera Settings")]
        [SerializeField] private float distance = 50f;
        [SerializeField] private float height = 8f;
        [SerializeField] private float smoothSpeed = 1f;
        [SerializeField] public bool useSmoothCamera = false; // Disabled for direct response
        
        [Header("Enhanced Turn Camera")]
        [SerializeField] private float turnLookAheadAngle = 30f; // How much extra to rotate into turns
        [SerializeField] private float pitchLookAheadAngle = 20f; // How much extra to rotate for pitch
        
        [Header("Camera Movement Settings")]
        [SerializeField] private float truckDistance = 15f; // How far to move camera sideways for turns
        [SerializeField] private float pedestalDistance = 8f; // How far to move camera up/down for pitch
        [SerializeField] private float movementSensitivity = 1f; // Overall movement sensitivity multiplier
        
        [Header("Stall Transition Settings")]
        [SerializeField] private float stallTransitionSpeed = 2f; // How fast to transition between stall/normal modes
        [SerializeField] private float weathervaneStrength = 0.8f; // How much the camera follows velocity during recovery
        [SerializeField] private float recoveryLookAheadTime = 1.5f; // How far ahead to anticipate ship orientation during recovery
        
        [Header("Look Ahead Settings")]
        [SerializeField] private float lookAheadDistance = 30f; // How far ahead of the ship to look
        [SerializeField] private float lookAheadHeight = 5f; // Height offset for the look-ahead point
        [SerializeField] private bool useVelocityForLookAhead = false; // Use velocity direction instead of ship heading
        
        [Header("Ray Intersection Settings")]
        [SerializeField] private float maxRayDistance = 1000f; // Maximum distance for ray casting
        [SerializeField] private float fallbackDistance = 50f; // Fallback distance if rays don't intersect
        [SerializeField] private bool debugRays = false; // Show debug rays in scene view
        
        [Header("Ship Ray Tip Settings")]
        [SerializeField] private float shipRayTipDistance = 100f; // Distance along ship's ray to look at
        [SerializeField] private float tipHeightOffset = 0f; // Additional height offset for the tip point
        
        [Header("Camera Components")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Camera cam;
        
        [Header("Cursor Settings")]
        [SerializeField] private Texture2D customCursor;
        [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
        [SerializeField] private bool hideCursorInFlight = false;
        
        [Header("Stall Camera Effects")]
        [SerializeField] private float stallShakeIntensity = 0.5f;
        [SerializeField] private float stallFovIncrease = 5f;
        [SerializeField] private float stallEffectSmoothSpeed = 2f;
        
        // Cached values for performance
        private ShipFlightController flightController;
        private Vector3 currentVelocity;
        private float smoothTime;
        private bool isInitialized = false;
        
        // Rotation tracking
        private Quaternion smoothedCameraTarget;
        private float originalFov;
        private float currentFov;
        
        // Stall transition tracking
        private bool wasStalled = false;
        private float stallBlendFactor = 0f; // 0 = normal mode, 1 = stall mode
        private Quaternion lastStallRotation;
        private Vector3 lastStallPosition;
        private bool isTransitioning = false;
        

        
        private void Start()
        {
            InitializeCamera();
        }
        
        private void InitializeCamera()
        {
            // Find target if not assigned
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
            
            if (target == null)
            {
                return;
            }

            // Get flight controller from target
            flightController = target.GetComponent<ShipFlightController>();
            
            // Simplify camera setup - make sure we're using the right objects
            if (cameraRig == null)
            {
                // Find existing cameraRig or use this transform directly
                Transform existingRig = transform.Find("CameraRig");
                if (existingRig != null)
                {
                    cameraRig = existingRig;
                }
                else
                {
                    // Use this transform as the camera rig directly
                    cameraRig = transform;
                }
            }
            
            // Find camera
            if (cam == null)
            {
                // Try to find camera as child first
                cam = GetComponentInChildren<Camera>();
                if (cam == null)
                {
                    // Fall back to main camera
                    cam = Camera.main;
                }
                
                if (cam != null)
                {
                    originalFov = cam.fieldOfView;
                    currentFov = originalFov;
                }
            }
            
            // Initialize camera position and rotation immediately
            Vector3 initialPosition = target.position - (target.forward * distance) + (Vector3.up * height);
            transform.position = initialPosition;
            transform.rotation = target.rotation;
            
            // Initialize tracking variables
            isInitialized = true;
            smoothedCameraTarget = target.rotation;
            
            // Cache smooth time calculation
            smoothTime = 1f / smoothSpeed;
            
            // Initialize cursor
            SetCustomCursor();
        }
        
        private void LateUpdate()
        {
            if (!isInitialized || target == null) return;
            
            // TEST: Simple rotation test - uncomment to test if camera can rotate
            // transform.Rotate(0, 20 * Time.deltaTime, 0); // Should rotate camera slowly
            
            UpdateCameraPositionAndRotation();
            UpdateStallEffects();
        }
        
        private void UpdateCameraPositionAndRotation()
        {
            if (target == null) return;

            bool isCurrentlyStalled = flightController != null && flightController.IsStalled() && flightController.CurrentVelocity.sqrMagnitude > 0.1f;
            
            // Detect stall state changes and manage transitions
            if (isCurrentlyStalled != wasStalled)
            {
                wasStalled = isCurrentlyStalled;
                isTransitioning = true;
                
                if (!isCurrentlyStalled)
                {
                    // Capture the current stall camera state for smooth transition
                    lastStallRotation = transform.rotation;
                    lastStallPosition = transform.position;
                }
            }
            
            // Update blend factor for smooth transitions
            float targetBlend = isCurrentlyStalled ? 1f : 0f;
            stallBlendFactor = Mathf.MoveTowards(stallBlendFactor, targetBlend, stallTransitionSpeed * Time.deltaTime);
            
            // Stop transitioning when blend is complete
            if (Mathf.Approximately(stallBlendFactor, targetBlend))
            {
                isTransitioning = false;
            }

            // Calculate both camera modes using POSITION-BASED approach
            Vector3 stallPosition, normalPosition;
            
            // STALL MODE CALCULATION: Camera positioned to show ship between heading and velocity
            Vector3 shipHeading = target.forward;
            Vector3 velocityDirection = flightController != null ? flightController.CurrentVelocity.normalized : target.forward;
            
            // Find the average direction between ship heading and velocity (weathervane effect)
            Vector3 averageDirection = (shipHeading + velocityDirection).normalized;
            Vector3 stallCameraBackDirection = -averageDirection;
            stallPosition = target.position + (stallCameraBackDirection * distance) + (Vector3.up * height);
            
            // NORMAL MODE CALCULATION: Camera moves based on turn/pitch inputs (TRUCK/PEDESTAL)
            Vector3 shipEuler = target.eulerAngles;
            float shipYaw = shipEuler.y;
            float shipPitch = shipEuler.x;
            if (shipPitch > 180f) shipPitch -= 360f;
            
            // Get ship inputs for camera movement
            float yawInput = 0f;
            float pitchInput = 0f;
            if (flightController != null)
            {
                yawInput = flightController.GetYawInput();
                pitchInput = flightController.GetPitchInput();
            }
            
            // Calculate base camera position (behind ship)
            Vector3 baseCameraPosition = target.position - (target.forward * distance) + (Vector3.up * height);
            
            // TRUCK MOVEMENT: Move camera sideways based on yaw input (WORLD-SPACE, not affected by banking)
            // Use world-space right direction projected onto horizontal plane to avoid banking influence
            Vector3 shipForwardHorizontal = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
            Vector3 worldRight = Vector3.Cross(Vector3.up, shipForwardHorizontal).normalized;
            Vector3 truckOffset = worldRight * (yawInput * truckDistance * movementSensitivity);
            
            // PEDESTAL MOVEMENT: Move camera up/down based on pitch input (pure world-space vertical)
            // Use pure world up/down movement, completely independent of ship orientation
            Vector3 pedestalOffset = Vector3.up * (-pitchInput * pedestalDistance * movementSensitivity);
            
            // Combine base position with truck and pedestal movements
            normalPosition = baseCameraPosition + truckOffset + pedestalOffset;
            
            // ENHANCED STALL-TO-NORMAL TRANSITION: Weathervane effect during recovery
            Vector3 finalPosition;
            
            if (stallBlendFactor > 0f && stallBlendFactor < 1f && !isCurrentlyStalled)
            {
                // RECOVERY PHASE: Blend from stall mode to normal with weathervane anticipation
                
                // Predict where the ship will be oriented based on current angular velocity
                Vector3 currentAngularVelocity = Vector3.zero;
                if (flightController != null)
                {
                    // Use ship's rotational inputs to predict future orientation
                    currentAngularVelocity = new Vector3(pitchInput, yawInput, 0) * recoveryLookAheadTime;
                }
                
                // Create an anticipated target rotation that accounts for ship's rotation trend
                Quaternion anticipatedShipRotation = target.rotation * Quaternion.Euler(currentAngularVelocity);
                Vector3 anticipatedForward = anticipatedShipRotation * Vector3.forward;
                
                // Weathervane effect: blend velocity direction influence during recovery
                Vector3 weathervaneDirection = Vector3.Slerp(velocityDirection, anticipatedForward, weathervaneStrength);
                Vector3 weathervaneCameraBackDirection = -weathervaneDirection;
                Vector3 weathervanePosition = target.position + (weathervaneCameraBackDirection * distance) + (Vector3.up * height);
                
                // Blend from stall position through weathervane to normal position
                float recoveryProgress = 1f - stallBlendFactor; // 0 at start of recovery, 1 at end
                
                // Two-phase recovery:
                // Phase 1 (first 60% of recovery): Stall -> Weathervane
                // Phase 2 (last 40% of recovery): Weathervane -> Normal
                if (recoveryProgress < 0.6f)
                {
                    float phase1Progress = recoveryProgress / 0.6f;
                    finalPosition = Vector3.Lerp(stallPosition, weathervanePosition, phase1Progress);
                }
                else
                {
                    float phase2Progress = (recoveryProgress - 0.6f) / 0.4f;
                    finalPosition = Vector3.Lerp(weathervanePosition, normalPosition, phase2Progress);
                }
            }
            else
            {
                // NORMAL BLENDING: Simple interpolation between modes
                finalPosition = Vector3.Lerp(normalPosition, stallPosition, stallBlendFactor);
            }



            // Apply final camera transform
            Vector3 oldPosition = transform.position;
            
            // SET POSITION: Move camera to calculated position
            transform.position = finalPosition;
            
            // SET ROTATION: Look ahead of the ship based on its heading (or velocity)
            Vector3 lookAheadPoint = CalculateLookAheadPoint();
            Vector3 directionToLookAhead = (lookAheadPoint - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(directionToLookAhead, Vector3.up);

            // Camera rig handling
            if (cameraRig != null && cameraRig != transform)
            {
                cameraRig.localPosition = Vector3.zero;
            }
        }
        
        private Vector3 CalculateLookAheadPoint()
        {
            if (target == null) return Vector3.zero;
            
            // RAY 1: Camera forward ray (for debug visualization)
            Vector3 cameraPosition = transform.position;
            Vector3 cameraForward = transform.forward;
            
            // RAY 2: Ship forward ray - this is what we'll look at
            Vector3 shipPosition = target.position;
            Vector3 shipForward = target.forward;
            
            // Calculate the tip of the ship's ray
            Vector3 shipRayTip = shipPosition + (shipForward * shipRayTipDistance) + (Vector3.up * tipHeightOffset);
            

            return shipRayTip;
        }
        
        private void UpdateStallEffects()
        {
            if (flightController == null || cam == null) return;

            bool isStalled = flightController.IsStalled();
            float targetFov = isStalled ? originalFov + stallFovIncrease : originalFov;
            
            // Smoothly adjust FOV
            currentFov = Mathf.Lerp(currentFov, targetFov, stallEffectSmoothSpeed * Time.deltaTime);
            cam.fieldOfView = currentFov;

            // Apply camera shake
            if (isStalled)
            {
                // Use Perlin noise for a natural-feeling shake
                float time = Time.time * 10f; // Shake speed
                float shakeX = (Mathf.PerlinNoise(time, 0) - 0.5f) * stallShakeIntensity;
                float shakeY = (Mathf.PerlinNoise(0, time) - 0.5f) * stallShakeIntensity;
                
                // Apply shake as a local rotation offset to the camera itself
                cam.transform.localRotation = Quaternion.Euler(shakeX, shakeY, 0);
            }
            else
            {
                // Smoothly return to zero rotation when not stalled
                cam.transform.localRotation = Quaternion.Slerp(cam.transform.localRotation, Quaternion.identity, stallEffectSmoothSpeed * Time.deltaTime);
            }
        }
        
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null && !isInitialized)
            {
                InitializeCamera();
            }
        }
        
        public Transform GetTarget() => target;
        
        /// <summary>
        /// Set custom cursor for the game
        /// </summary>
        private void SetCustomCursor()
        {
            if (customCursor != null)
            {
                Cursor.SetCursor(customCursor, cursorHotspot, CursorMode.Auto);
            }
            
            if (hideCursorInFlight)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        
        /// <summary>
        /// Show cursor (useful for UI interactions)
        /// </summary>
        public void ShowCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        /// <summary>
        /// Hide cursor (useful for flight mode)
        /// </summary>
        public void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        // Camera now always uses direct response - no smoothing
    }
} 