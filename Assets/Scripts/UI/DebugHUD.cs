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
            if (playerShip == null)
                playerShip = FindFirstObjectByType<PrototypeShip>();
                
            if (flightMovement == null)
                flightMovement = FindFirstObjectByType<ShipFlightController>();
        }

        private void Start()
        {
            InitializeGUIStyles();
            
            // Debug logging to help identify issues
            Debug.Log($"DebugHUD Start - showHUD: {showHUD}, isInitialized: {isInitialized}");
            Debug.Log($"DebugHUD Start - playerShip: {(playerShip != null ? "Found" : "NULL")}");
            Debug.Log($"DebugHUD Start - flightMovement: {(flightMovement != null ? "Found" : "NULL")}");
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
            
            // Force show HUD if F1 is pressed (debug)
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showHUD = true;
                Debug.Log("Debug HUD forced ON via F1");
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
            // Always show a minimal debug HUD to help troubleshoot
            GUI.color = Color.red;
            GUI.Label(new Rect(10, Screen.height - 60, 400, 20), $"DebugHUD Status - showHUD: {showHUD}, isInitialized: {isInitialized}");
            GUI.Label(new Rect(10, Screen.height - 40, 400, 20), $"Components - PS: {(playerShip != null ? "OK" : "NULL")}, FM: {(flightMovement != null ? "OK" : "NULL")}");
            GUI.color = Color.white;
            
            if (!showHUD || !isInitialized) 
            {
                GUI.color = Color.yellow;
                GUI.Label(new Rect(10, Screen.height - 20, 400, 20), "Press F1 to force show HUD");
                GUI.color = Color.white;
                return;
            }

            // Background panel - smaller for essential data
            GUI.Box(new Rect(10, 10, 320, 180), "");

            float yOffset = 25;
            float lineHeight = 18;

            // Header
            GUI.Label(new Rect(20, yOffset, 280, 20), "FLIGHT DEBUG HUD", headerStyle);
            yOffset += 30;

            // ESSENTIAL FLIGHT DATA
            if (flightMovement != null)
            {
                float actualSpeed = flightMovement.GetActualSpeed();
                float throttlePercent = flightMovement.Throttle * 100f;
                float pitch = flightMovement.GetCurrentPitch();
                float yaw = flightMovement.GetCurrentYaw();
                float bank = flightMovement.GetCurrentBankAngle();

                GUI.Label(new Rect(20, yOffset, 280, 20), $"Speed: {actualSpeed:F1} m/s", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(20, yOffset, 280, 20), $"Throttle: {throttlePercent:F0}%", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(20, yOffset, 280, 20), $"Pitch: {pitch:F1}°", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(20, yOffset, 280, 20), $"Yaw: {yaw:F1}°", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(20, yOffset, 280, 20), $"Roll: {bank:F1}°", hudStyle);
                yOffset += lineHeight;
            }

            // POSITION & ALTITUDE
            if (playerShip != null)
            {
                Vector3 pos = playerShip.transform.position;
                GUI.Label(new Rect(20, yOffset, 280, 20), $"Altitude: {pos.y:F1} m", hudStyle);
                yOffset += lineHeight;
            }

            // FPS
            GUI.Label(new Rect(20, yOffset, 280, 20), $"FPS: {(1f / Time.deltaTime):F0}", hudStyle);

            // Stall information
            if (flightMovement != null)
            {
                float stallThreshold = flightMovement.GetDynamicStallThreshold();
                bool isStalled = flightMovement.IsStalled();
                float controlMultiplier = flightMovement.GetStallControlMultiplier();
                GUI.Label(new Rect(20, yOffset, 280, 20), $"Stall Threshold: {stallThreshold:F1} m/s", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(20, yOffset, 280, 20), $"Stall State: {(isStalled ? "STALLED" : "NORMAL")}", hudStyle);
                yOffset += lineHeight;
                GUI.Label(new Rect(20, yOffset, 280, 20), $"Stall Control: {(controlMultiplier * 100f):F0}%", hudStyle);
                yOffset += lineHeight;
            }
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