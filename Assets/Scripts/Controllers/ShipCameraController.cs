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
        [SerializeField] private float smoothSpeed = 5f; // Increased for smoother following
        
        [Header("Stall Transition Settings")]
        [SerializeField] private float stallTransitionSpeed = 2f;
        
        [Header("Component References")]
        [SerializeField] private Camera cam;
        
        // Private cached references & state
        private ShipFlightController flightController;
        private bool isInitialized = false;
        private float stallBlendFactor = 0f;

        private void Start()
        {
            InitializeCamera();
        }
        
        private void InitializeCamera()
        {
            if (target == null)
            {
                Debug.LogError("Camera target is not assigned!", this);
                return;
            }

            flightController = target.GetComponent<ShipFlightController>();
            if (flightController == null)
            {
                Debug.LogError("FlightController not found on the target!", this);
                return;
            }
            
            if (cam == null)
            {
                cam = GetComponentInChildren<Camera>();
                if (cam == null)
                {
                   Debug.LogError("No camera found as a child of this rig!", this);
                   return;
                }
            }
            
            // Initial positioning
            transform.position = target.position - (target.forward * distance) + (Vector3.up * height);
            transform.LookAt(target.position, Vector3.up);
            
            isInitialized = true;
        }
        
        private void LateUpdate()
        {
            if (!isInitialized) return;
            
            // --- POSITION ---
            // The camera rig is now independent. It needs to follow the target's position smoothly.
            Vector3 targetPosition = target.position - (target.forward * distance) + (target.up * height);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

            // --- ROTATION ---
            bool isCurrentlyStalled = flightController.IsStalled();
            stallBlendFactor = Mathf.MoveTowards(stallBlendFactor, isCurrentlyStalled ? 1f : 0f, stallTransitionSpeed * Time.deltaTime);

            // Determine the look-at point based on stall state
            Vector3 lookAtNormal = target.position + (target.forward * 100f);
            Vector3 lookAtStall;

            Vector3 velocity = flightController.CurrentVelocity;
            if (isCurrentlyStalled && velocity.sqrMagnitude > 0.1f)
            {
                // In stall, look where the ship is GOING
                lookAtStall = target.position + velocity.normalized * 100f;
            }
            else
            {
                // If not stalled or not moving, look where the ship is FACING
                lookAtStall = lookAtNormal;
            }

            // Blend between the two look-at points
            Vector3 finalLookAt = Vector3.Lerp(lookAtNormal, lookAtStall, stallBlendFactor);
            
            // Calculate the final rotation with gimbal lock avoidance
            Vector3 direction = finalLookAt - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                // Check if we're looking almost straight up or down
                if (Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.99f)
                {
                    // Use ship's right vector as a stable fallback to prevent spinning
                    transform.rotation = Quaternion.LookRotation(direction, target.right);
                }
                else
                {
                    // Otherwise, always keep the camera level with the world
                    transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                }
            }
        }
    }
} 