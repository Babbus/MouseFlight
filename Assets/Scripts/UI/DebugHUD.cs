using UnityEngine;
using DomeClash.Core;
using DomeClash.Ships;

namespace DomeClash.UI
{
    /// <summary>
    /// MouseFlight Transform-Based System Debug HUD
    /// Real-time flight data, input states, transform info
    /// NO PHYSICS - Direct transform control
    /// </summary>
    public class DebugHUD : MonoBehaviour
    {
        [Header("References")]
        public MouseFlightController flightController;
        public PrototypeShip playerShip;
        public ShipFlightController flightMovement;

        [Header("HUD Settings")]
        public bool showHUD = true;
        public bool enableConsoleLogging = false;
        public KeyCode toggleHUDKey = KeyCode.F1;
        public KeyCode toggleLoggingKey = KeyCode.F2;

        private GUIStyle hudStyle;
        private GUIStyle headerStyle;
        private bool isInitialized = false;

        private void Awake()
        {
            // Auto-find references if not assigned
            if (flightController == null)
                flightController = FindFirstObjectByType<MouseFlightController>();
            
            if (playerShip == null)
                playerShip = FindFirstObjectByType<PrototypeShip>();
                
            if (flightMovement == null)
                flightMovement = FindFirstObjectByType<ShipFlightController>();
        }

        private void Start()
        {
            InitializeGUIStyles();
        }

        private void Update()
        {
            // Toggle HUD visibility
            if (Input.GetKeyDown(toggleHUDKey))
            {
                showHUD = !showHUD;
                Debug.Log($"Debug HUD: {(showHUD ? "ON" : "OFF")}");
            }

            // Toggle console logging
            if (Input.GetKeyDown(toggleLoggingKey))
            {
                enableConsoleLogging = !enableConsoleLogging;
                Debug.Log($"Console Logging: {(enableConsoleLogging ? "ON" : "OFF")}");
            }

            // Log data if enabled
            if (enableConsoleLogging && playerShip != null)
            {
                LogFlightData();
            }
        }

        private void InitializeGUIStyles()
        {
            hudStyle = new GUIStyle();
            hudStyle.normal.textColor = Color.white;
            hudStyle.fontSize = 14;
            hudStyle.fontStyle = FontStyle.Normal;

            headerStyle = new GUIStyle();
            headerStyle.normal.textColor = Color.yellow;
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;

            isInitialized = true;
        }

        private void OnGUI()
        {
            if (!showHUD || !isInitialized) return;

            // Background panel - smaller and cleaner
            GUI.Box(new Rect(10, 10, 350, 400), "");

            float yOffset = 25;
            float lineHeight = 18;

            // Header
            GUI.Label(new Rect(20, yOffset, 330, 20), "FLIGHT DEBUG HUD", headerStyle);
            yOffset += 30;

            // ESSENTIAL FLIGHT DATA
            if (flightMovement != null)
            {
                // Speed Section
                float actualSpeed = flightMovement.GetActualSpeed();
                float thrustSpeed = flightMovement.CurrentSpeed;
                
                GUI.color = Color.cyan;
                GUI.Label(new Rect(20, yOffset, 330, 20), "SPEED", headerStyle);
                GUI.color = Color.white;
                yOffset += lineHeight;
                
                GUI.Label(new Rect(30, yOffset, 300, 15), $"Actual: {actualSpeed:F0} m/s", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 300, 15), $"Thrust: {thrustSpeed:F0} m/s", hudStyle);
                yOffset += lineHeight;
                yOffset += 10;

                // Stall Section
                bool isStalled = flightMovement.IsStalled();
                float controlMultiplier = flightMovement.GetStallControlMultiplier();
                
                GUI.color = isStalled ? Color.red : Color.green;
                GUI.Label(new Rect(20, yOffset, 330, 20), "STALL SYSTEM", headerStyle);
                GUI.color = Color.white;
                yOffset += lineHeight;
                
                GUI.Label(new Rect(30, yOffset, 300, 15), $"State: {(isStalled ? "STALLED" : "NORMAL")}", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 300, 15), $"Threshold: {flightMovement.GetDynamicStallThreshold():F0} m/s", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 300, 15), $"Control: {(controlMultiplier * 100):F0}%", hudStyle);
                yOffset += lineHeight;
                yOffset += 10;
            }

            // INPUT DATA
            if (playerShip != null)
            {
                GUI.Label(new Rect(20, yOffset, 330, 20), "INPUTS", headerStyle);
                yOffset += lineHeight;

                GUI.Label(new Rect(30, yOffset, 300, 15), $"Pitch: {playerShip.GetPitchInput():F2} | Yaw: {playerShip.GetYawInput():F2}", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 300, 15), $"Strafe: {playerShip.GetStrafeInput():F2} | Throttle: {playerShip.GetThrottle():F2}", hudStyle);
                yOffset += lineHeight;
                yOffset += 10;
            }

            // POSITION & ROTATION
            if (playerShip != null)
            {
                GUI.Label(new Rect(20, yOffset, 330, 20), "TRANSFORM", headerStyle);
                yOffset += lineHeight;

                Vector3 pos = playerShip.transform.position;
                GUI.Label(new Rect(30, yOffset, 300, 15), $"Pos: ({pos.x:F0}, {pos.y:F0}, {pos.z:F0})", hudStyle);
                yOffset += lineHeight;
                
                Vector3 rot = playerShip.transform.eulerAngles;
                GUI.Label(new Rect(30, yOffset, 300, 15), $"Rot: ({rot.x:F0}°, {rot.y:F0}°, {rot.z:F0}°)", hudStyle);
                yOffset += lineHeight;
                yOffset += 10;
            }

            // BANKING
            if (flightMovement != null)
            {
                GUI.Label(new Rect(20, yOffset, 330, 20), "BANKING", headerStyle);
                yOffset += lineHeight;

                GUI.Label(new Rect(30, yOffset, 300, 15), $"Bank: {flightMovement.GetCurrentBankAngle():F0}°", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 300, 15), $"Pitch: {flightMovement.GetCurrentPitch():F0}° | Yaw: {flightMovement.GetCurrentYaw():F0}°", hudStyle);
                yOffset += lineHeight;
                yOffset += 10;
            }

            // CONTROLS
            GUI.Label(new Rect(20, yOffset, 330, 20), "CONTROLS", headerStyle);
            yOffset += lineHeight;
            GUI.Label(new Rect(30, yOffset, 300, 15), $"{toggleHUDKey}: Toggle HUD", hudStyle);
            yOffset += lineHeight;
            GUI.Label(new Rect(30, yOffset, 300, 15), "Mouse: Aim | A/D: Strafe | W/S: Throttle", hudStyle);
            yOffset += lineHeight;
            GUI.Label(new Rect(30, yOffset, 300, 15), $"FPS: {(1f / Time.deltaTime):F0}", hudStyle);
        }

        private void LogFlightData()
        {
            // Console logging disabled by default to prevent spam
            // Enable in inspector if needed for debugging
            /*
            if (playerShip != null)
            {
                float speed = playerShip.GetCurrentSpeed();
                Debug.Log($"[MouseFlight Transform] Speed: {speed:F1} | Pitch: {playerShip.GetPitchInput():F2} | Yaw: {playerShip.GetYawInput():F2} | Roll: {playerShip.GetRollInput():F2}");
            }
            */
        }

        private string VectorToString(Vector3 vector)
        {
            return $"({vector.x:F1}, {vector.y:F1}, {vector.z:F1})";
        }

        // Public methods for external access
        public void SetFlightController(MouseFlightController controller)
        {
            flightController = controller;
        }

        public void SetPlayerShip(PrototypeShip ship)
        {
            playerShip = ship;
        }
        
        public void SetFlightMovement(ShipFlightController movement)
        {
            flightMovement = movement;
        }
    }
} 