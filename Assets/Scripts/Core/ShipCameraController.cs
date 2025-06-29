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
        [SerializeField] private float height = 12f;
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private float rotationFollowStrength = 0.5f;
        
        [Header("Camera Components")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Camera cam;
        
        // Cached values for performance
        private Vector3 currentVelocity;
        private Vector3 currentRotationVelocity;
        private float lastShipYaw;
        private float lastShipPitch;
        private float smoothTime;
        private bool isInitialized = false;
        
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
            
            // Calculate yaw and pitch differences separately (ignore roll completely)
            float yawDifference = CalculateAngleDifference(shipYaw, lastShipYaw);
            float pitchDifference = CalculateAngleDifference(shipPitch, lastShipPitch);
            
            // Create base camera rotation that follows ship's yaw and pitch ONLY
            Quaternion baseRotation = Quaternion.AngleAxis(shipYaw, Vector3.up) * Quaternion.AngleAxis(shipPitch, Vector3.right);
            
            // Add additional rotation based on turning direction (yaw and pitch only)
            Quaternion turnRotation = Quaternion.identity;
            if (Mathf.Abs(yawDifference) > 0.1f || Mathf.Abs(pitchDifference) > 0.1f)
            {
                float additionalYawRotation = yawDifference * rotationFollowStrength;
                float additionalPitchRotation = pitchDifference * rotationFollowStrength;
                
                turnRotation = Quaternion.AngleAxis(additionalYawRotation, Vector3.up) * 
                              Quaternion.AngleAxis(additionalPitchRotation, Vector3.right);
            }
            
            // Combine base rotation with turn rotation
            Quaternion targetRotation = baseRotation * turnRotation;
            
            // Smooth rotation
            Vector3 currentEuler = cameraRig.rotation.eulerAngles;
            Vector3 targetEuler = targetRotation.eulerAngles;
            
            // Smooth each axis separately to avoid gimbal lock issues
            float newX = Mathf.SmoothDampAngle(currentEuler.x, targetEuler.x, ref currentRotationVelocity.x, smoothTime);
            float newY = Mathf.SmoothDampAngle(currentEuler.y, targetEuler.y, ref currentRotationVelocity.y, smoothTime);
            float newZ = Mathf.SmoothDampAngle(currentEuler.z, targetEuler.z, ref currentRotationVelocity.z, smoothTime);
            
            cameraRig.rotation = Quaternion.Euler(newX, newY, newZ);
            
            // Update tracking variables (yaw and pitch only)
            lastShipYaw = shipYaw;
            lastShipPitch = shipPitch;
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