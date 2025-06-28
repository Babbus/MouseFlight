using UnityEngine;

namespace DomeClash.Core
{
    /// <summary>
    /// Simple Camera Controller - Stable ship following
    /// No complex mouse flight system, just clean 3rd person camera
    /// </summary>
    public class SimpleCameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        
        [Header("Camera Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 8, -25);
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float rotationSpeed = 3f;
        [SerializeField] private bool lookAtTarget = true;
        
        [Header("Manual Control")]
        [SerializeField] private bool allowMouseLook = false;
        [SerializeField] private float mouseSensitivity = 2f;
        
        private Vector3 currentVelocity;
        private float mouseX = 0f;
        private float mouseY = 0f;
        
        private void Start()
        {
            // Auto-find target if not assigned
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                    Debug.Log($"SimpleCameraController: Auto-found target: {target.name}");
                }
            }
            
            // Set initial position
            if (target != null)
            {
                transform.position = target.position + offset;
                if (lookAtTarget)
                    transform.LookAt(target);
            }
        }
        
        private void LateUpdate()
        {
            if (target == null) return;
            
            // Handle mouse input for manual control
            if (allowMouseLook && Input.GetMouseButton(1)) // Right mouse button
            {
                mouseX += Input.GetAxis("Mouse X") * mouseSensitivity;
                mouseY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                mouseY = Mathf.Clamp(mouseY, -60f, 60f);
            }
            
            // Calculate desired position
            Vector3 desiredPosition;
            
            if (allowMouseLook && Input.GetMouseButton(1))
            {
                // Manual mouse control
                Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0);
                desiredPosition = target.position + rotation * offset;
            }
            else
            {
                // Auto follow behind target
                Vector3 targetForward = target.forward;
                Vector3 targetPosition = target.position;
                desiredPosition = targetPosition - targetForward * Mathf.Abs(offset.z) + Vector3.up * offset.y;
            }
            
            // Smooth movement
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, 
                ref currentVelocity, 1f / followSpeed, Mathf.Infinity, Time.deltaTime);
            
            // Look at target
            if (lookAtTarget)
            {
                Vector3 direction = target.position - transform.position;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                    rotationSpeed * Time.deltaTime);
            }
        }
        
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
        
        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }
        
        // Enable/disable the camera controller
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }
    }
} 