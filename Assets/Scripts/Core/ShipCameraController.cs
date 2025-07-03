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
        
        [Header("Camera Movement Settings")]
        [SerializeField] private float truckDistance = 15f; // How far to move camera sideways for turns
        [SerializeField] private float pedestalDistance = 8f; // How far to move camera up/down for pitch
        [SerializeField] private float movementSensitivity = 1f; // Overall movement sensitivity multiplier
        
        [Header("Stall Transition Settings")]
        [SerializeField] private float stallTransitionSpeed = 2f; // How fast to transition between stall/normal modes
        [SerializeField] private float weathervaneStrength = 0.8f; // How much the camera follows velocity during recovery
        [SerializeField] private float recoveryLookAheadTime = 1.5f; // How far ahead to anticipate ship orientation during recovery
        
        [Header("Ship Ray Tip Settings")]
        [SerializeField] private float shipRayTipDistance = 100f; // Fallback distance if no collision
        [SerializeField] private float tipHeightOffset = 0f; // Additional height offset for the tip point
        [SerializeField] private float maxRayDistance = 1000f; // Maximum ray distance for collision detection
        [SerializeField] private LayerMask raycastLayers = -1; // Layers to check for collisions
        
        [Header("3D Crosshair Settings")]
        [SerializeField] private GameObject crosshairPrefab; // The crosshair GameObject to instantiate
        [SerializeField] private bool showCrosshair = true; // Whether to show the crosshair
        [SerializeField] private float crosshairScale = 1f; // Scale of the crosshair
        [SerializeField] private Color crosshairColor = Color.white; // Color of the crosshair
        [SerializeField] private float crosshairSurfaceOffset = 0.2f; // Offset above surface to avoid clipping
        [SerializeField] private float crosshairRotationSmoothSpeed = 10f; // Smoothing speed for crosshair rotation
        
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
        
        // Crosshair tracking
        private GameObject crosshairInstance;
        private Quaternion crosshairCurrentRotation;
        

        
        private void Start()
        {
            Debug.Log("ShipCameraController Start called");
            InitializeCamera();
        }
        
        private void InitializeCamera()
        {
            Debug.Log($"InitializeCamera called. crosshairPrefab assigned: {crosshairPrefab != null}, showCrosshair: {showCrosshair}");
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
            
            // Initialize crosshair
            InitializeCrosshair();
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
            
            // SET ROTATION: Always look at the max tip (never fallback tip)
            Vector3 maxTip = target.position + (target.forward * maxRayDistance) + (Vector3.up * tipHeightOffset);
            Vector3 directionToLookAhead = (maxTip - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(directionToLookAhead, Vector3.up);

            // Still update the crosshair (but ignore its return value for camera look)
            CalculateLookAheadPoint();

            // Camera rig handling
            if (cameraRig != null && cameraRig != transform)
            {
                cameraRig.localPosition = Vector3.zero;
            }
        }
        
        private Vector3 CalculateLookAheadPoint()
        {
            if (target == null) return Vector3.zero;
            
            Vector3 shipPosition = target.position;
            Vector3 shipForward = target.forward;
            
            RaycastHit hit;
            Vector3 rayStart = shipPosition + (Vector3.up * tipHeightOffset);
            Vector3 rayEnd = rayStart + (shipForward * maxRayDistance);
            
            bool hasCollision = Physics.Raycast(rayStart, shipForward, out hit, maxRayDistance, raycastLayers);
            
            Vector3 crosshairPosition;
            Vector3? hitNormal = null;
            if (hasCollision)
            {
                crosshairPosition = hit.point;
                hitNormal = hit.normal;
            }
            else
            {
                crosshairPosition = shipPosition + (shipForward * shipRayTipDistance) + (Vector3.up * tipHeightOffset);
            }
            
            // Update crosshair position and pass normal info
            UpdateCrosshair(crosshairPosition, hitNormal);
            
            return crosshairPosition;
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
        
        /// <summary>
        /// Initialize the 3D crosshair
        /// </summary>
        private void InitializeCrosshair()
        {
            Debug.Log($"InitializeCrosshair called. showCrosshair={showCrosshair}, crosshairPrefab assigned={crosshairPrefab != null}");
            if (!showCrosshair || crosshairPrefab == null) return;
            
            // Create crosshair instance
            crosshairInstance = Instantiate(crosshairPrefab);
            crosshairInstance.name = "ShipCrosshair";
            
            // Set initial scale and color
            crosshairInstance.transform.localScale = Vector3.one * crosshairScale;
            
            // Apply color if the crosshair has a renderer
            Renderer renderer = crosshairInstance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = crosshairColor;
            }
            
            // Set initial position (will be updated in UpdateCrosshair)
            crosshairInstance.transform.position = Vector3.zero;
            crosshairCurrentRotation = crosshairInstance.transform.rotation;
        }
        
        /// <summary>
        /// Update the crosshair position and orientation
        /// </summary>
        private void UpdateCrosshair(Vector3 targetPosition, Vector3? surfaceNormal)
        {
            if (!showCrosshair || crosshairInstance == null || cam == null) return;

            Quaternion targetRotation;
            if (surfaceNormal.HasValue)
            {
                // Offset the crosshair above the surface to avoid clipping
                Vector3 offsetPosition = targetPosition + surfaceNormal.Value * crosshairSurfaceOffset;
                crosshairInstance.transform.position = offsetPosition; // Always set position directly
                // Align the crosshair to the surface normal
                targetRotation = Quaternion.LookRotation(surfaceNormal.Value);
                // Flip 180° around Y if needed (for -Z front)
                targetRotation *= Quaternion.Euler(0, 180f, 0);
            }
            else
            {
                // No hit: fallback to old logic
                crosshairInstance.transform.position = targetPosition; // Always set position directly
                targetRotation = Quaternion.LookRotation(cam.transform.position - targetPosition, Vector3.up);
                targetRotation *= Quaternion.Euler(0, 180f, 0);
            }

            // Only smooth rotation for small changes, snap for large changes
            float angle = Quaternion.Angle(crosshairCurrentRotation, targetRotation);
            float snapThreshold = 30f; // degrees
            if (angle > snapThreshold)
            {
                crosshairCurrentRotation = targetRotation; // Snap
            }
            else
            {
                crosshairCurrentRotation = Quaternion.Slerp(crosshairCurrentRotation, targetRotation, Time.deltaTime * crosshairRotationSmoothSpeed);
            }
            crosshairInstance.transform.rotation = crosshairCurrentRotation;

            // Dynamically scale the crosshair based on distance to camera
            float distanceToCamera = Vector3.Distance(cam.transform.position, crosshairInstance.transform.position);
            float dynamicScale = crosshairScale * distanceToCamera * 0.05f;
            crosshairInstance.transform.localScale = Vector3.one * dynamicScale;
        }
        
        /// <summary>
        /// Show or hide the crosshair
        /// </summary>
        public void SetCrosshairVisible(bool visible)
        {
            showCrosshair = visible;
            if (crosshairInstance != null)
            {
                crosshairInstance.SetActive(visible);
            }
        }
        
        /// <summary>
        /// Set the crosshair color
        /// </summary>
        public void SetCrosshairColor(Color color)
        {
            crosshairColor = color;
            if (crosshairInstance != null)
            {
                Renderer renderer = crosshairInstance.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = color;
                }
            }
        }
    }
} 