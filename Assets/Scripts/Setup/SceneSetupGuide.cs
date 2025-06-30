using UnityEngine;
using DomeClash.Core;
using DomeClash.Ships;

namespace DomeClash.Setup
{
    /// <summary>
    /// MouseFlight Sistemi Sahne Setup Rehberi
    /// Bu script sahne düzenlemesi için gerekli adımları açıklar
    /// Unity 6000.1.9f1 Compatible
    /// </summary>
    public class SceneSetupGuide : MonoBehaviour
    {
        [Header("Required Hierarchy Structure")]
        [TextArea(10, 20)]
        public string hierarchyStructure = @"
MOUSEFLIGHT SAHNE YAPISI:

Scene Root:
├── DomeClashFlightRig (Empty GameObject)
│   ├── MouseAim (Empty GameObject)
│   └── CameraRig (Empty GameObject)
│       └── Main Camera (Camera + AudioListener)
│
├── Example1_Grey (Ship Prefab)
│   ├── PrototypeShip (Script)
│   ├── Rigidbody
│   └── Model (3D Model)
│
├── DebugHUD (Empty GameObject)
│   └── DebugHUD (Script)
│
└── Environment
    ├── Directional Light
    ├── Skybox
    └── Ground/Terrain

ÖNEMLI NOKTALAR:
1. DomeClashFlightRig asla başka bir objeye parent edilmemeli!
2. MouseAim ve CameraRig mutlaka FlightRig'in child'ı olmalı
3. Ship'e PrototypeShip script'i eklenmeli
4. MouseFlightController script'i FlightRig'e eklenmeli
";

        [Header("Component References")]
        [SerializeField] private Transform flightRig;
        [SerializeField] private Transform mouseAim;
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform aircraft;
        [SerializeField] private PrototypeShip prototypeShip;
        [SerializeField] private MouseFlightController flightController;

        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupScene = false;

        private void Start()
        {
            if (autoSetupScene)
            {
                SetupScene();
            }
            else
            {
                ValidateScene();
            }
        }

        [ContextMenu("Setup Scene Automatically")]
        public void SetupScene()
        {
            Debug.Log("🎯 MouseFlight Scene Setup başlatılıyor...");

            // 1. Create Flight Rig
            CreateFlightRig();

            // 2. Setup Camera
            SetupCamera();

            // 3. Setup Ship
            SetupShip();

            // 4. Setup Debug HUD
            SetupDebugHUD();

            // 5. Assign References
            AssignReferences();

            Debug.Log("✅ MouseFlight Scene Setup tamamlandı!");
        }

        private void CreateFlightRig()
        {
            // Create main flight rig
            GameObject rigGO = new GameObject("DomeClashFlightRig");
            rigGO.transform.position = Vector3.zero;
            rigGO.transform.rotation = Quaternion.identity;
            flightRig = rigGO.transform;

            // Add MouseFlightController
            flightController = rigGO.AddComponent<MouseFlightController>();

            // Create MouseAim
            GameObject mouseAimGO = new GameObject("MouseAim");
            mouseAimGO.transform.SetParent(flightRig);
            mouseAimGO.transform.localPosition = Vector3.zero;
            mouseAimGO.transform.localRotation = Quaternion.identity;
            mouseAim = mouseAimGO.transform;

            // Create CameraRig
            GameObject cameraRigGO = new GameObject("CameraRig");
            cameraRigGO.transform.SetParent(flightRig);
            cameraRigGO.transform.localPosition = Vector3.zero;
            cameraRigGO.transform.localRotation = Quaternion.identity;
            cameraRig = cameraRigGO.transform;

            Debug.Log("✅ Flight Rig oluşturuldu");
        }

        private void SetupCamera()
        {
            // Find existing camera or create new one
            Camera existingCamera = FindFirstObjectByType<Camera>();
            
            if (existingCamera != null)
            {
                // Move existing camera to camera rig
                existingCamera.transform.SetParent(cameraRig);
                existingCamera.transform.localPosition = new Vector3(0, 9, -30);
                existingCamera.transform.localRotation = Quaternion.identity;
                mainCamera = existingCamera;
            }
            else
            {
                // Create new camera
                GameObject cameraGO = new GameObject("Main Camera");
                cameraGO.transform.SetParent(cameraRig);
                cameraGO.transform.localPosition = new Vector3(0, 9, -30);
                cameraGO.transform.localRotation = Quaternion.identity;
                cameraGO.tag = "MainCamera";

                mainCamera = cameraGO.AddComponent<Camera>();
                cameraGO.AddComponent<AudioListener>();

                // Camera settings
                mainCamera.fieldOfView = 60f;
                mainCamera.nearClipPlane = 1f;
                mainCamera.farClipPlane = 10000f;
            }

            Debug.Log("✅ Camera setup tamamlandı");
        }

        private void SetupShip()
        {
            // Find existing ship or create placeholder
            PrototypeShip existingShip = FindFirstObjectByType<PrototypeShip>();
            
            if (existingShip != null)
            {
                prototypeShip = existingShip;
                aircraft = existingShip.transform;
            }
            else
            {
                // Create placeholder ship
                GameObject shipGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                shipGO.name = "PrototypeShip";
                shipGO.transform.position = Vector3.zero;
                shipGO.transform.rotation = Quaternion.identity;
                shipGO.transform.localScale = new Vector3(2, 2, 8);

                // Add Rigidbody
                Rigidbody rb = shipGO.AddComponent<Rigidbody>();
                rb.mass = 100f;
                rb.useGravity = false;

                // Add PrototypeShip component
                prototypeShip = shipGO.AddComponent<PrototypeShip>();
                aircraft = shipGO.transform;
            }

            Debug.Log("✅ Ship setup tamamlandı");
        }

        private void SetupDebugHUD()
        {
            // Create Debug HUD
            GameObject hudGO = new GameObject("DebugHUD");
            var debugHUD = hudGO.AddComponent<DomeClash.UI.DebugHUD>();
            
            // Auto-assign references
            debugHUD.flightController = flightController;
            debugHUD.playerShip = prototypeShip;

            Debug.Log("✅ Debug HUD oluşturuldu");
        }

        private void AssignReferences()
        {
            if (flightController != null)
            {
                // Assign all references to flight controller
                var controller = flightController;
                
                // Use reflection to set private fields
                var aircraftField = typeof(MouseFlightController).GetField("aircraft", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var mouseAimField = typeof(MouseFlightController).GetField("mouseAim", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var cameraRigField = typeof(MouseFlightController).GetField("cameraRig", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var camField = typeof(MouseFlightController).GetField("cam", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                aircraftField?.SetValue(controller, aircraft);
                mouseAimField?.SetValue(controller, mouseAim);
                cameraRigField?.SetValue(controller, cameraRig);
                camField?.SetValue(controller, mainCamera?.transform);

                Debug.Log("✅ References atandı");
            }
        }

        [ContextMenu("Validate Scene Setup")]
        public void ValidateScene()
        {
            Debug.Log("🔍 MouseFlight Scene Validation başlatılıyor...");

            bool isValid = true;

            // Check Flight Rig
            if (flightRig == null)
            {
                Debug.LogError("❌ DomeClashFlightRig bulunamadı!");
                isValid = false;
            }
            else if (flightRig.parent != null)
            {
                Debug.LogError("❌ FlightRig başka bir objeye parent edilmiş! Parent kaldırılmalı.");
                isValid = false;
            }

            // Check MouseAim
            if (mouseAim == null)
            {
                Debug.LogError("❌ MouseAim bulunamadı!");
                isValid = false;
            }
            else if (mouseAim.parent != flightRig)
            {
                Debug.LogError("❌ MouseAim FlightRig'in child'ı değil!");
                isValid = false;
            }

            // Check CameraRig
            if (cameraRig == null)
            {
                Debug.LogError("❌ CameraRig bulunamadı!");
                isValid = false;
            }
            else if (cameraRig.parent != flightRig)
            {
                Debug.LogError("❌ CameraRig FlightRig'in child'ı değil!");
                isValid = false;
            }

            // Check Camera
            if (mainCamera == null)
            {
                Debug.LogError("❌ Main Camera bulunamadı!");
                isValid = false;
            }
            else if (mainCamera.transform.parent != cameraRig)
            {
                Debug.LogError("❌ Camera CameraRig'in child'ı değil!");
                isValid = false;
            }

            // Check Ship
            if (aircraft == null)
            {
                Debug.LogError("❌ Aircraft bulunamadı!");
                isValid = false;
            }

            if (prototypeShip == null)
            {
                Debug.LogError("❌ PrototypeShip component bulunamadı!");
                isValid = false;
            }

            // Check Flight Controller
            if (flightController == null)
            {
                Debug.LogError("❌ MouseFlightController bulunamadı!");
                isValid = false;
            }

            if (isValid)
            {
                Debug.Log("✅ MouseFlight Scene Setup geçerli!");
            }
            else
            {
                Debug.Log("❌ Scene setup'ında sorunlar var. Yukarıdaki hataları düzeltin.");
            }
        }

        [ContextMenu("Show Setup Instructions")]
        public void ShowSetupInstructions()
        {
            Debug.Log(@"
🎯 MOUSEFLIGHT MANUEL SETUP TALİMATLARI:

1. DomeClashFlightRig oluştur (Empty GameObject)
   - Position: (0, 0, 0)
   - Parent: NONE (çok önemli!)

2. MouseAim oluştur (Empty GameObject)
   - Parent: DomeClashFlightRig
   - Local Position: (0, 0, 0)

3. CameraRig oluştur (Empty GameObject)
   - Parent: DomeClashFlightRig
   - Local Position: (0, 0, 0)

4. Main Camera'yı CameraRig'in child'ı yap
   - Local Position: (0, 9, -30)
   - Tag: MainCamera

5. MouseFlightController script'ini FlightRig'e ekle
   - Aircraft: Ship transform'u ata
   - MouseAim: MouseAim transform'u ata
   - CameraRig: CameraRig transform'u ata
   - Cam: Camera transform'u ata

6. Ship'e PrototypeShip script'i ekle
   - Rigidbody gerekli
   - Mass: 100, UseGravity: false

7. DebugHUD oluştur ve script ekle
   - References'ları ata

✅ Setup tamamlandığında 'Validate Scene Setup' butonunu kullan!
");
        }
    }
} 