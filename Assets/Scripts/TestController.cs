using UnityEngine;
using DomeClash.Ships;
using DomeClash.Core;

namespace DomeClash.Testing
{
    /// <summary>
    /// Flight-integrated weapon test controller
    /// Uses your existing MouseFlightController and ShipFlightController
    /// </summary>
    public class TestController : MonoBehaviour
    {
        [Header("Flight System Integration")]
        [SerializeField] private bool enableFlightSystem = true;
        [SerializeField] private MouseFlightController mouseFlightController;
        [SerializeField] private ShipFlightController shipFlightController;
        
        private PrototypeRazorShip razorShip;

        void Start()
        {
            razorShip = GetComponent<PrototypeRazorShip>();
            
            // Auto-find flight controllers if not assigned
            if (mouseFlightController == null)
                mouseFlightController = FindFirstObjectByType<MouseFlightController>();
            if (shipFlightController == null)
                shipFlightController = GetComponent<ShipFlightController>();
                
            // Setup flight system
            SetupFlightSystem();
            
            Debug.Log("=== FLIGHT-INTEGRATED WEAPON TEST ===");
            Debug.Log("FLIGHT CONTROLS:");
            Debug.Log("Mouse: Pitch/Yaw/Banking");
            Debug.Log("A/D: Strafe Left/Right (Double-tap for dodge)");
            Debug.Log("Left Shift: BOOST (hold to boost)");
            Debug.Log("S: SLOW (hold to slow down)");
            Debug.Log("WEAPON CONTROLS:");
            Debug.Log("Left Click: Arc Splitter (3-round burst)");
            Debug.Log("Right Click: Hunter Seeker (6-missile volley)");
            Debug.Log("DEBUG:");
            Debug.Log("I: Ship Status | T: Toggle Flight System");
            Debug.Log("=====================================");
        }

        void Update()
        {
            HandleDebugInput();
            ShowDebugInfo();
        }

        private void SetupFlightSystem()
        {
            if (!enableFlightSystem) return;
            
            // Ensure ship has required components
            if (shipFlightController == null)
            {
                shipFlightController = gameObject.AddComponent<ShipFlightController>();
                Debug.Log("✓ ShipFlightController added");
            }
            
            // Setup MouseFlightController if not found
            if (mouseFlightController == null)
            {
                GameObject mouseFlightObj = new GameObject("MouseFlightController");
                mouseFlightController = mouseFlightObj.AddComponent<MouseFlightController>();
                Debug.Log("✓ MouseFlightController created");
            }
            
            // Tag ship as Player for flight system
            if (gameObject.tag != "Player")
            {
                gameObject.tag = "Player";
                Debug.Log("✓ Ship tagged as Player");
            }
        }

        private void HandleDebugInput()
        {
            // Toggle flight system
            if (Input.GetKeyDown(KeyCode.T))
            {
                enableFlightSystem = !enableFlightSystem;
                
                if (mouseFlightController != null)
                    mouseFlightController.systemEnabled = enableFlightSystem;
                    
                Debug.Log($"Flight System: {(enableFlightSystem ? "ENABLED" : "DISABLED")}");
            }
        }

        private void ShowDebugInfo()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                if (razorShip != null)
                {
                    var primary = razorShip.GetPrimaryWeapon();
                    var secondary = razorShip.GetSecondaryWeapon();
                    var energy = razorShip.GetEnergySystem();
                    
                    Debug.Log("=== SHIP STATUS ===");
                    
                    // Flight System Status
                    Debug.Log($"Flight System: {(enableFlightSystem ? "ENABLED" : "DISABLED")}");
                    if (shipFlightController != null)
                    {
                        Debug.Log($"Speed: {shipFlightController.CurrentSpeed:F1} | Boost: {shipFlightController.IsBoosting} | Slow: {shipFlightController.IsSlowing}");
                        Debug.Log($"Banking: {shipFlightController.GetCurrentBankAngle():F1}° | Stalled: {shipFlightController.IsStalled()}");
                    }
                    
                    // Weapon System Status
                    Debug.Log($"Primary Weapon: {(primary != null ? "OK" : "MISSING")}");
                    Debug.Log($"Secondary Weapon: {(secondary != null ? "OK" : "MISSING")}");
                    Debug.Log($"Energy System: {(energy != null ? energy.CurrentEnergy + "/" + energy.MaxEnergy : "MISSING")}");
                    
                    if (primary != null)
                    {
                        Debug.Log($"Primary Heat: {primary.GetCurrentHeat():F1}/{primary.GetMaxHeat():F1}");
                        Debug.Log($"Primary Overheated: {primary.IsOverheated()}");
                    }
                    
                    if (secondary != null)
                    {
                        Debug.Log($"Secondary Heat: {secondary.GetCurrentHeat():F1}/{secondary.GetMaxHeat():F1}");
                        Debug.Log($"Secondary Overheated: {secondary.IsOverheated()}");
                    }
                }
            }
        }
    }
} 