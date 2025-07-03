using UnityEngine;

namespace DomeClash.Core
{
    /// <summary>
    /// Mouse Flight Controller - Handles mouse input for ship control and aiming
    /// Provides mouse position and aiming data for weapon systems
    /// </summary>
    public class MouseFlightController : MonoBehaviour
    {
        [Header("Mouse Flight Settings")]
        [SerializeField] public bool systemEnabled = true;
        [SerializeField] private float mouseSensitivity = 1.0f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private float aimRange = 1000f;

        [Header("Current State")]
        [SerializeField] private Vector3 mouseAimPos;
        [SerializeField] private Vector2 mouseInput;
        [SerializeField] private bool isMouseControlActive = true;

        // Component references
        private Transform mouseAim;
        private Transform aircraft;
        private Camera playerCamera;

        // Input tracking
        private Vector2 lastMousePosition;
        private Vector2 mouseMovementDelta;

        public Vector3 MouseAimPos => mouseAimPos;
        public Vector2 MouseInput => mouseInput;
        public bool IsSystemEnabled => systemEnabled;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            // Find camera for mouse positioning
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    playerCamera = FindFirstObjectByType<Camera>();
                }
            }

            // Initialize mouse position
            UpdateMouseAimPosition();
        }

        private void Update()
        {
            if (!systemEnabled) return;

            UpdateMouseInput();
            UpdateMouseAimPosition();
        }

        private void InitializeComponents()
        {
            // Find aircraft (Player tagged GameObject)
            if (aircraft == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    aircraft = playerObj.transform;
                }
            }

            // Create or find MouseAim object
            if (mouseAim == null)
            {
                GameObject mouseAimObj = GameObject.Find("MouseAim");
                if (mouseAimObj == null)
                {
                    mouseAimObj = new GameObject("MouseAim");
                    mouseAimObj.transform.position = Vector3.zero;
                }
                mouseAim = mouseAimObj.transform;
            }
        }

        private void UpdateMouseInput()
        {
            // Get mouse movement
            Vector2 currentMousePosition = Input.mousePosition;
            
            if (lastMousePosition != Vector2.zero)
            {
                mouseMovementDelta = currentMousePosition - lastMousePosition;
            }
            
            lastMousePosition = currentMousePosition;

            // Convert to normalized input
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 mouseOffset = currentMousePosition - screenCenter;
            
            mouseInput.x = (mouseOffset.x / screenCenter.x) * mouseSensitivity;
            mouseInput.y = (mouseOffset.y / screenCenter.y) * mouseSensitivity * (invertY ? -1f : 1f);
            
            // Clamp input
            mouseInput.x = Mathf.Clamp(mouseInput.x, -1f, 1f);
            mouseInput.y = Mathf.Clamp(mouseInput.y, -1f, 1f);
        }

        private void UpdateMouseAimPosition()
        {
            if (playerCamera == null || aircraft == null) 
            {
                // Fallback to simple forward position
                mouseAimPos = transform.position + transform.forward * aimRange;
                return;
            }

            // Cast ray from camera through mouse position into world
            Ray mouseRay = playerCamera.ScreenPointToRay(Input.mousePosition);
            
            // Project the ray to a distance
            mouseAimPos = mouseRay.origin + mouseRay.direction * aimRange;
            
            // Update mouse aim object position
            if (mouseAim != null)
            {
                mouseAim.position = mouseAimPos;
            }
        }

        // Public methods for weapon systems
        public Vector3 GetAimDirection()
        {
            if (aircraft != null)
            {
                return (mouseAimPos - aircraft.position).normalized;
            }
            return transform.forward;
        }

        public Vector3 GetAimDirection(Vector3 fromPosition)
        {
            return (mouseAimPos - fromPosition).normalized;
        }

        public bool IsAiming()
        {
            return systemEnabled && isMouseControlActive;
        }

        // Settings
        public void SetSensitivity(float sensitivity)
        {
            mouseSensitivity = sensitivity;
        }

        public void SetInvertY(bool invert)
        {
            invertY = invert;
        }

        public void SetSystemEnabled(bool enabled)
        {
            systemEnabled = enabled;
        }

        public void SetAimRange(float range)
        {
            aimRange = range;
        }

        // Debug methods
        private void OnDrawGizmos()
        {
            if (!systemEnabled) return;

            // Draw aim line
            if (aircraft != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(aircraft.position, mouseAimPos);
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(mouseAimPos, 2f);
            }
        }
    }
} 