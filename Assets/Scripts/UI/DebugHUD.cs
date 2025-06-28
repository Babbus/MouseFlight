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
        public DomeClashFlightController flightController;
        public PrototypeShip playerShip;

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
                flightController = FindFirstObjectByType<DomeClashFlightController>();
            
            if (playerShip == null)
                playerShip = FindFirstObjectByType<PrototypeShip>();
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

            // Background panel
            GUI.Box(new Rect(10, 10, 400, 600), "");

            float yOffset = 25;
            float lineHeight = 18;

            // Header
            GUI.Label(new Rect(20, yOffset, 380, 20), "MOUSEFLIGHT DEBUG HUD", headerStyle);
            yOffset += 30;

            // Flight Controller Data
            if (flightController != null)
            {
                GUI.Label(new Rect(20, yOffset, 380, 20), "FLIGHT CONTROLLER", headerStyle);
                yOffset += lineHeight;

                GUI.Label(new Rect(30, yOffset, 350, 15), $"Mouse Aim: {VectorToString(flightController.MouseAimPos)}", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Boresight: {VectorToString(flightController.BoresightPos)}", hudStyle);
                yOffset += lineHeight;
                yOffset += 10;
            }

            // Ship Input Data
            if (playerShip != null)
            {
                GUI.Label(new Rect(20, yOffset, 380, 20), "INPUT SYSTEM", headerStyle);
                yOffset += lineHeight;

                GUI.Label(new Rect(30, yOffset, 350, 15), $"Pitch Input: {playerShip.GetPitchInput():F3} (TRANSFORM)", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Yaw Input: {playerShip.GetYawInput():F3} (TRANSFORM)", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Roll Input: {playerShip.GetRollInput():F3} (TRANSFORM)", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Strafe Input: {playerShip.GetStrafeInput():F3}", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Throttle: {playerShip.GetThrottle():F3}", hudStyle);
                yOffset += lineHeight;
                yOffset += 10;
            }

            // Transform-Based Movement Data
            if (playerShip != null)
            {
                GUI.Label(new Rect(20, yOffset, 380, 20), "MOVEMENT (NO PHYSICS)", headerStyle);
                yOffset += lineHeight;

                float speed = playerShip.GetCurrentSpeed();
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Current Speed: {speed:F1} m/s", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Flight Speed: {playerShip.GetFlightSpeed():F1} m/s", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Turn Speed: {playerShip.GetTurnSpeed():F1} deg/s", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"System: TRANSFORM-BASED", hudStyle);
                yOffset += lineHeight;
                yOffset += 10;
            }

            // Ship Stats
            if (playerShip != null && playerShip.stats != null)
            {
                GUI.Label(new Rect(20, yOffset, 380, 20), "SHIP STATUS", headerStyle);
                yOffset += lineHeight;

                var stats = playerShip.stats;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Health: {stats.health:F0}/{stats.maxHealth:F0}", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Energy: {stats.energy:F0}/{stats.maxEnergy:F0}", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Shields: {stats.shields:F0}/{stats.maxShields:F0}", hudStyle);
                yOffset += lineHeight;
                yOffset += 10;
            }

            // Position & Rotation
            if (playerShip != null)
            {
                GUI.Label(new Rect(20, yOffset, 380, 20), "TRANSFORM", headerStyle);
                yOffset += lineHeight;

                GUI.Label(new Rect(30, yOffset, 350, 15), $"Position: {VectorToString(playerShip.transform.position)}", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Rotation: {VectorToString(playerShip.transform.eulerAngles)}", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Forward: {VectorToString(playerShip.transform.forward)}", hudStyle);
                yOffset += lineHeight;
                yOffset += 10;
            }

            // Performance Settings
            if (playerShip != null)
            {
                GUI.Label(new Rect(20, yOffset, 380, 20), "PERFORMANCE", headerStyle);
                yOffset += lineHeight;

                GUI.Label(new Rect(30, yOffset, 350, 15), $"Flight Speed: {playerShip.GetFlightSpeed():F0}", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"Turn Speed: {playerShip.GetTurnSpeed():F0} deg/s", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(30, yOffset, 350, 15), $"FPS: {(1f / Time.deltaTime):F0}", hudStyle);
                yOffset += lineHeight;
            }

            // Controls
            yOffset += 10;
            GUI.Label(new Rect(20, yOffset, 380, 20), "CONTROLS", headerStyle);
            yOffset += lineHeight;
            GUI.Label(new Rect(30, yOffset, 350, 15), $"{toggleHUDKey}: Toggle HUD", hudStyle);
            yOffset += lineHeight;
            GUI.Label(new Rect(30, yOffset, 350, 15), $"{toggleLoggingKey}: Toggle Logging", hudStyle);
            yOffset += lineHeight;
            GUI.Label(new Rect(30, yOffset, 350, 15), "C: Free Look", hudStyle);
            yOffset += lineHeight;
            GUI.Label(new Rect(30, yOffset, 350, 15), "A/D: Strafe, Double-tap: Dodge", hudStyle);
            yOffset += lineHeight;
            GUI.Label(new Rect(30, yOffset, 350, 15), "Arrow Keys: Manual Override", hudStyle);
        }

        private void LogFlightData()
        {
            if (playerShip != null)
            {
                float speed = playerShip.GetCurrentSpeed();
                Debug.Log($"[MouseFlight Transform] Speed: {speed:F1} | Pitch: {playerShip.GetPitchInput():F2} | Yaw: {playerShip.GetYawInput():F2} | Roll: {playerShip.GetRollInput():F2}");
            }
        }

        private string VectorToString(Vector3 vector)
        {
            return $"({vector.x:F1}, {vector.y:F1}, {vector.z:F1})";
        }

        // Public methods for external access
        public void SetFlightController(DomeClashFlightController controller)
        {
            flightController = controller;
        }

        public void SetPlayerShip(PrototypeShip ship)
        {
            playerShip = ship;
        }
    }
} 