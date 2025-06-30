using UnityEngine;
using DomeClash.Ships;

namespace DomeClash.Core
{
    /// <summary>
    /// Optimized 3rd Person Camera Controller
    /// Follows the ship from behind with smooth movement
    /// </summary>
    public class ShipCameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        
        [Header("Camera Settings")]
        [SerializeField] private float distance = 30f;
        [SerializeField] private float height = 4f;
        [SerializeField] private float smoothSpeed = 1f;
        [SerializeField] private float rotationFollowStrength = 20f;
        [SerializeField] private float lookAheadSmoothMultiplier = 2f; // Slower smoothing for look-ahead
        
        [Header("Camera Components")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Camera cam;
        
        // Cached values for performance
        private Vector3 currentVelocity;
        private Vector3 currentRotationVelocity;
        private Vector3 currentLookAheadVelocity; // Separate velocity for look-ahead
        private float lastShipYaw;
        private float lastShipPitch;
        private float smoothTime;
        private float lookAheadSmoothTime;
        private bool isInitialized = false;
        private Quaternion currentLookAheadRotation; // Track accumulated look-ahead rotation
        
        // Angular velocity tracking for look-ahead (eliminates feedback loop)
        private float shipYawVelocity;
        private float shipPitchVelocity;
        private float lastYawVelocity;
        private float lastPitchVelocity;
        
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
            
            // Find camera rig if not assigned
            if (cameraRig == null)
            {
                cameraRig = transform.Find("CameraRig");
                if (cameraRig == null)
                {
                    GameObject rig = new GameObject("CameraRig");
                    rig.transform.SetParent(transform);
                    rig.transform.localPosition = Vector3.zero;
                    cameraRig = rig.transform;
                }
            }
            
            // Find camera if not assigned
            if (cam == null)
            {
                cam = Camera.main;
                if (cam != null)
                {
                    cam.transform.SetParent(cameraRig);
                    cam.transform.localPosition = Vector3.zero;
                    cam.transform.localRotation = Quaternion.identity;
                }
            }
            
            // Initialize tracking variables
            if (target != null)
            {
                lastShipYaw = target.eulerAngles.y;
                lastShipPitch = target.eulerAngles.x;
                isInitialized = true;
            }
            
            // Cache smooth time calculation
            smoothTime = 1f / smoothSpeed;
            lookAheadSmoothTime = smoothTime * lookAheadSmoothMultiplier;
        }
        
        private void LateUpdate()
        {
            if (!isInitialized || target == null) return;
            
            UpdateCameraPosition();
            UpdateCameraRotation();
        }
        
        private void UpdateCameraPosition()
        {
            // Calculate target position behind the ship
            Vector3 targetPosition = target.position - (target.forward * distance) + (Vector3.up * height);
            
            // Smooth movement
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        }
        
        private void UpdateCameraRotation()
        {
            if (cameraRig == null) return;
            
            // Get ship's current rotation (yaw and pitch only)
            Vector3 shipEuler = target.eulerAngles;
            float shipYaw = shipEuler.y;
            float shipPitch = shipEuler.x;
            
            // Convert ship pitch to -180 to +180 range
            if (shipPitch > 180f) shipPitch -= 360f;
            
            // Calculate angular velocity (degrees per second) - this eliminates feedback loop
            float deltaTime = Time.deltaTime;
            float yawVelocity = CalculateAngleDifference(shipYaw, lastShipYaw) / deltaTime;
            float pitchVelocity = CalculateAngleDifference(shipPitch, lastShipPitch) / deltaTime;
            
            // Smooth the angular velocity to reduce noise
            shipYawVelocity = Mathf.Lerp(lastYawVelocity, yawVelocity, 0.8f);
            shipPitchVelocity = Mathf.Lerp(lastPitchVelocity, pitchVelocity, 0.8f);
            
            // Create base camera rotation that follows ship's yaw and pitch ONLY
            Quaternion baseRotation = Quaternion.AngleAxis(shipYaw, Vector3.up) * Quaternion.AngleAxis(shipPitch, Vector3.right);
            
            // Calculate look-ahead based on angular velocity (no feedback loop!)
            if (Mathf.Abs(shipYawVelocity) > 1f || Mathf.Abs(shipPitchVelocity) > 1f)
            {
                // Convert velocity to rotation offset (velocity * time * strength)
                float additionalYawRotation = shipYawVelocity * deltaTime * rotationFollowStrength;
                float additionalPitchRotation = shipPitchVelocity * deltaTime * rotationFollowStrength;
                
                // Create target look-ahead rotation
                Quaternion targetLookAhead = Quaternion.AngleAxis(additionalYawRotation, Vector3.up) * 
                                           Quaternion.AngleAxis(additionalPitchRotation, Vector3.right);
                
                // Smooth the look-ahead rotation from current accumulated rotation
                Vector3 currentLookAheadEuler = currentLookAheadRotation.eulerAngles;
                Vector3 targetLookAheadEuler = targetLookAhead.eulerAngles;
                
                float smoothX = Mathf.SmoothDampAngle(currentLookAheadEuler.x, targetLookAheadEuler.x, ref currentLookAheadVelocity.x, lookAheadSmoothTime);
                float smoothY = Mathf.SmoothDampAngle(currentLookAheadEuler.y, targetLookAheadEuler.y, ref currentLookAheadVelocity.y, lookAheadSmoothTime);
                float smoothZ = Mathf.SmoothDampAngle(currentLookAheadEuler.z, targetLookAheadEuler.z, ref currentLookAheadVelocity.z, lookAheadSmoothTime);
                
                currentLookAheadRotation = Quaternion.Euler(smoothX, smoothY, smoothZ);
            }
            else
            {
                // Gradually return to identity when not turning
                Vector3 currentLookAheadEuler = currentLookAheadRotation.eulerAngles;
                float smoothX = Mathf.SmoothDampAngle(currentLookAheadEuler.x, 0f, ref currentLookAheadVelocity.x, lookAheadSmoothTime);
                float smoothY = Mathf.SmoothDampAngle(currentLookAheadEuler.y, 0f, ref currentLookAheadVelocity.y, lookAheadSmoothTime);
                float smoothZ = Mathf.SmoothDampAngle(currentLookAheadEuler.z, 0f, ref currentLookAheadVelocity.z, lookAheadSmoothTime);
                
                currentLookAheadRotation = Quaternion.Euler(smoothX, smoothY, smoothZ);
            }
            
            // Combine base rotation with smoothed look-ahead rotation
            Quaternion targetRotation = baseRotation * currentLookAheadRotation;
            
            // Smooth the final camera rotation
            Vector3 currentEuler = cameraRig.rotation.eulerAngles;
            Vector3 targetEuler = targetRotation.eulerAngles;
            
            // Smooth each axis separately to avoid gimbal lock issues
            float newX = Mathf.SmoothDampAngle(currentEuler.x, targetEuler.x, ref currentRotationVelocity.x, smoothTime);
            float newY = Mathf.SmoothDampAngle(currentEuler.y, targetEuler.y, ref currentRotationVelocity.y, smoothTime);
            float newZ = Mathf.SmoothDampAngle(currentEuler.z, targetEuler.z, ref currentRotationVelocity.z, smoothTime);
            
            cameraRig.rotation = Quaternion.Euler(newX, newY, newZ);
            
            // Update tracking variables
            lastShipYaw = shipYaw;
            lastShipPitch = shipPitch;
            lastYawVelocity = shipYawVelocity;
            lastPitchVelocity = shipPitchVelocity;
        }
        
        private float CalculateAngleDifference(float current, float last)
        {
            float diff = current - last;
            if (diff > 180f) diff -= 360f;
            if (diff < -180f) diff += 360f;
            return diff;
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
    }
} 