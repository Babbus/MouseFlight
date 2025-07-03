using UnityEngine;
using DomeClash.Core;
using DomeClash.Ships;
using DomeClash.Weapons.PrototypeRazorWeapons;

namespace DomeClash.Setup
{
    /// <summary>
    /// Unity Inspector setup guide for Razor Prototype weapons
    /// This script helps configure all components correctly
    /// </summary>
    public class PrototypeSetupGuide : MonoBehaviour
    {
        [Header("=== SETUP GUIDE ===")]
        [Header("1. Create Empty GameObjects for Fire Points")]
        [Tooltip("Position these where lasers should fire from (usually weapon tips)")]
        public Transform[] primaryFirePoints = new Transform[2]; // Left and Right barrels
        
        [Tooltip("Position these where missiles should launch from")]
        public Transform[] missileFirePoints = new Transform[3]; // Multiple missile pods
        
        [Header("2. Weapon Prefabs (Optional - for visual missiles)")]
        [Tooltip("Drag a simple projectile prefab here (sphere/cylinder works)")]
        public GameObject hunterMissilePrefab;
        
        [Header("3. Effects (Optional - for visual feedback)")]
        [Tooltip("Particle systems for muzzle flash, electric arcs, etc.")]
        public ParticleSystem muzzleFlash;
        public ParticleSystem electricArcEffect;
        public ParticleSystem missilePodFlash;
        
        [Header("4. Audio (Optional - for sound effects)")]
        public AudioClip[] arcSplitterSounds = new AudioClip[3]; // Different sounds per burst
        public AudioClip[] missileLaunchSounds = new AudioClip[2];
        
        [Header("=== AUTO SETUP BUTTONS ===")]
        [Header("Click these buttons in Play Mode to test")]
        public bool setupFirePoints = false;
        public bool setupWeaponComponents = false;
        public bool testWeapons = false;
        public bool createFirePoints = false;
        public bool setupWeapons = false;
        
        private ArcSplitter arcSplitter;
        private HunterSeeker hunterSeeker;

        [Header("Ship Prefab Setup")]
        [SerializeField] private bool setupShipHierarchy = false;
        [SerializeField] private Transform shipModel; // Your ship 3D model

        void Start()
        {
            FindWeaponComponents();
        }

        void Update()
        {
            if (setupShipHierarchy)
            {
                setupShipHierarchy = false;
                SetupCompleteShipPrefab();
            }
            
            if (createFirePoints)
            {
                createFirePoints = false;
                CreateFirePoints();
            }
            
            if (setupWeapons)
            {
                setupWeapons = false;
                SetupWeaponSystems();
            }
            
            if (setupFirePoints)
            {
                setupFirePoints = false;
                CreateFirePoints();
            }
            
            if (setupWeaponComponents)
            {
                setupWeaponComponents = false;
                SetupWeaponComponents();
            }
            
            if (testWeapons && (arcSplitter != null || hunterSeeker != null))
            {
                ShowTestInstructions();
                testWeapons = false;
            }
        }

        private void CreateFirePoints()
        {
            Debug.Log("=== CREATING FIRE POINTS ===");
            
            // Create primary fire points (for Arc Splitter)
            if (primaryFirePoints == null || primaryFirePoints.Length != 2)
            {
                primaryFirePoints = new Transform[2];
            }
            
            for (int i = 0; i < 2; i++)
            {
                if (primaryFirePoints[i] == null)
                {
                    GameObject firePoint = new GameObject($"PrimaryFirePoint_{i + 1}");
                    firePoint.transform.parent = transform;
                    firePoint.transform.localPosition = new Vector3(i == 0 ? -0.5f : 0.5f, 0, 1f); // Left/Right
                    primaryFirePoints[i] = firePoint.transform;
                    
                    Debug.Log($"Created {firePoint.name} at position {firePoint.transform.localPosition}");
                }
            }
            
            // Create missile fire points (for Hunter Seeker)
            if (missileFirePoints == null || missileFirePoints.Length != 3)
            {
                missileFirePoints = new Transform[3];
            }
            
            for (int i = 0; i < 3; i++)
            {
                if (missileFirePoints[i] == null)
                {
                    GameObject missilePoint = new GameObject($"MissileFirePoint_{i + 1}");
                    missilePoint.transform.parent = transform;
                    missilePoint.transform.localPosition = new Vector3((i - 1) * 0.3f, -0.2f, 0.8f); // Spread out
                    missileFirePoints[i] = missilePoint.transform;
                    
                    Debug.Log($"Created {missilePoint.name} at position {missilePoint.transform.localPosition}");
                }
            }
            
            Debug.Log("Fire points created! Position them manually if needed.");
        }

        private void SetupWeaponComponents()
        {
            Debug.Log("=== SETTING UP WEAPON COMPONENTS ===");
            
            // Add Arc Splitter
            if (arcSplitter == null)
            {
                GameObject arcSplitterObj = new GameObject("ArcSplitter");
                arcSplitterObj.transform.parent = transform;
                arcSplitter = arcSplitterObj.AddComponent<ArcSplitter>();
                
                Debug.Log("Arc Splitter component added!");
            }
            
            // Add Hunter Seeker
            if (hunterSeeker == null)
            {
                GameObject hunterSeekerObj = new GameObject("HunterSeeker");
                hunterSeekerObj.transform.parent = transform;
                hunterSeeker = hunterSeekerObj.AddComponent<HunterSeeker>();
                
                Debug.Log("Hunter Seeker component added!");
            }
            
            // Configure Arc Splitter
            if (arcSplitter != null && primaryFirePoints != null)
            {
                // Set fire points via reflection (since twinBarrels might be private)
                System.Reflection.FieldInfo twinBarrelsField = typeof(ArcSplitter).GetField("twinBarrels", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (twinBarrelsField != null)
                {
                    twinBarrelsField.SetValue(arcSplitter, primaryFirePoints);
                    Debug.Log("Arc Splitter fire points configured!");
                }
                
                // Set visual effects
                if (electricArcEffect != null)
                {
                    System.Reflection.FieldInfo effectField = typeof(ArcSplitter).GetField("electricArcEffect", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (effectField != null)
                        effectField.SetValue(arcSplitter, electricArcEffect);
                }
                
                // Set sounds
                if (arcSplitterSounds != null && arcSplitterSounds.Length > 0)
                {
                    System.Reflection.FieldInfo soundsField = typeof(ArcSplitter).GetField("burstSounds", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (soundsField != null)
                        soundsField.SetValue(arcSplitter, arcSplitterSounds);
                }
            }
            
            // Configure Hunter Seeker
            if (hunterSeeker != null && missileFirePoints != null)
            {
                // Set missile pods
                System.Reflection.FieldInfo missilePods = typeof(HunterSeeker).GetField("missilePods", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (missilePods != null)
                {
                    missilePods.SetValue(hunterSeeker, missileFirePoints);
                    Debug.Log("Hunter Seeker missile pods configured!");
                }
                
                // Set missile prefab
                if (hunterMissilePrefab != null)
                {
                    System.Reflection.FieldInfo prefabField = typeof(HunterSeeker).GetField("hunterMissilePrefab", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (prefabField != null)
                        prefabField.SetValue(hunterSeeker, hunterMissilePrefab);
                }
                
                // Set effects
                if (missilePodFlash != null)
                {
                    System.Reflection.FieldInfo flashField = typeof(HunterSeeker).GetField("podFlash", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (flashField != null)
                        flashField.SetValue(hunterSeeker, missilePodFlash);
                }
                
                // Set sounds
                if (missileLaunchSounds != null && missileLaunchSounds.Length > 0)
                {
                    System.Reflection.FieldInfo soundsField = typeof(HunterSeeker).GetField("launchSounds", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (soundsField != null)
                        soundsField.SetValue(hunterSeeker, missileLaunchSounds);
                }
            }
            
            Debug.Log("Weapon components configured!");
        }

        private void FindWeaponComponents()
        {
            if (arcSplitter == null)
                arcSplitter = GetComponentInChildren<ArcSplitter>();
                
            if (hunterSeeker == null)
                hunterSeeker = GetComponentInChildren<HunterSeeker>();
        }

        private void ShowTestInstructions()
        {
            Debug.Log("=== WEAPON TEST INSTRUCTIONS ===");
            Debug.Log("LEFT MOUSE BUTTON: Fire Arc Splitter (3-round burst)");
            Debug.Log("RIGHT MOUSE BUTTON: Fire Hunter Seeker missiles (requires target lock)");
            Debug.Log("Watch the Console for weapon feedback!");
            Debug.Log("Watch for heat buildup and energy consumption!");
        }

        // Manual test buttons for Inspector
        [Header("=== MANUAL TEST BUTTONS ===")]
        public bool fireArcSplitter = false;
        public bool fireHunterSeeker = false;
        public bool overHeatWeapons = false;
        public bool restoreEnergy = false;

        void LateUpdate()
        {
            // Manual test controls
            if (fireArcSplitter && arcSplitter != null)
            {
                Debug.Log("Manual Arc Splitter test!");
                fireArcSplitter = false;
            }
            
            if (fireHunterSeeker && hunterSeeker != null)
            {
                Debug.Log("Manual Hunter Seeker test!");
                fireHunterSeeker = false;
            }
            
            if (overHeatWeapons)
            {
                if (arcSplitter != null) arcSplitter.ForceOverheat();
                if (hunterSeeker != null) hunterSeeker.ForceOverheat();
                overHeatWeapons = false;
                Debug.Log("Weapons force overheated!");
            }
            
            if (restoreEnergy)
            {
                EnergySystem energy = GetComponent<EnergySystem>();
                if (energy != null) energy.DebugRestoreEnergy();
                restoreEnergy = false;
                Debug.Log("Energy restored!");
            }
        }

        private void SetupCompleteShipPrefab()
        {
            Debug.Log("=== SHIP PREFAB SETUP STARTING ===");
            
            // 1. Create main ship structure
            CreateShipHierarchy();
            
            // 2. Setup fire points
            CreateFirePoints();
            
            // 3. Setup weapon systems
            SetupWeaponSystems();
            
            // 4. Setup ship components
            SetupShipComponents();
            
            Debug.Log("=== SHIP PREFAB SETUP COMPLETE ===");
            Debug.Log("Your ship is ready for combat! Test with mouse controls:");
            Debug.Log("- Left Click: Arc Splitter (Primary)");
            Debug.Log("- Right Click: Hunter Seeker (Secondary)");
        }

        private void CreateShipHierarchy()
        {
            // Create weapon mount points under ship
            Transform weaponMounts = new GameObject("WeaponMounts").transform;
            weaponMounts.SetParent(transform);
            weaponMounts.localPosition = Vector3.zero;
            
            // Primary weapon mount (Arc Splitter)
            Transform primaryMount = new GameObject("PrimaryWeaponMount").transform;
            primaryMount.SetParent(weaponMounts);
            primaryMount.localPosition = Vector3.zero;
            
            // Secondary weapon mount (Hunter Seeker)
            Transform secondaryMount = new GameObject("SecondaryWeaponMount").transform;
            secondaryMount.SetParent(weaponMounts);
            secondaryMount.localPosition = Vector3.zero;
            
            // Effects mount
            Transform effectsMount = new GameObject("EffectsMount").transform;
            effectsMount.SetParent(transform);
            effectsMount.localPosition = Vector3.zero;
            
            Debug.Log("✓ Ship hierarchy created");
        }

        private void SetupWeaponSystems()
        {
            Debug.Log("=== SETTING UP WEAPON SYSTEMS ===");
            
            // This calls the existing SetupWeaponComponents method
            SetupWeaponComponents();
            
            Debug.Log("✓ Weapon systems setup complete");
        }

        private void SetupShipComponents()
        {
            // Add PrototypeRazorShip component
            PrototypeRazorShip razorShip = GetComponent<PrototypeRazorShip>();
            if (razorShip == null)
            {
                razorShip = gameObject.AddComponent<PrototypeRazorShip>();
                Debug.Log("✓ PrototypeRazorShip component added");
            }
            
            // Add Rigidbody for physics
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.mass = 1000f; // Razor ship mass
                rb.linearDamping = 2f;
                rb.angularDamping = 5f;
                Debug.Log("✓ Rigidbody added with Razor specs");
            }
            
            // Add Collider
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
                boxCol.size = new Vector3(2f, 1f, 4f); // Adjust to your ship size
                Debug.Log("✓ BoxCollider added (adjust size as needed)");
            }
            
            // Add DamageSystem
            DomeClash.Core.DamageSystem damageSystem = GetComponent<DomeClash.Core.DamageSystem>();
            if (damageSystem == null)
            {
                damageSystem = gameObject.AddComponent<DomeClash.Core.DamageSystem>();
                Debug.Log("✓ DamageSystem added");
            }
            
            // Add EnergySystem
            DomeClash.Core.EnergySystem energySystem = GetComponent<DomeClash.Core.EnergySystem>();
            if (energySystem == null)
            {
                energySystem = gameObject.AddComponent<DomeClash.Core.EnergySystem>();
                Debug.Log("✓ EnergySystem added");
            }
        }
    }
} 