using UnityEngine;
using DomeClash.Core;
using DomeClash.Ships;
using DomeClash.Weapons.PrototypeRazorWeapons;

namespace DomeClash.Weapons
{
    /// <summary>
    /// Visual Ship Prefab Creator for Unity Inspector
    /// Drag your ship model, click setup, and you're ready to fight!
    /// </summary>
    public class ShipPrefabCreator : MonoBehaviour
    {
        [Header("🚀 SHIP PREFAB CREATOR")]
        [Space(10)]
        
        [Header("1. Drag Your Ship Model Here")]
        [SerializeField] private GameObject shipModel;
        [SerializeField] private string shipName = "My Razor Ship";
        
        [Header("2. Weapon Positioning")]
        [SerializeField] private Vector3 leftBarrelOffset = new Vector3(-0.5f, 0f, 1f);
        [SerializeField] private Vector3 rightBarrelOffset = new Vector3(0.5f, 0f, 1f);
        [SerializeField] private Vector3[] missilePodOffsets = new Vector3[]
        {
            new Vector3(-1f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f)
        };
        
        [Header("3. Ship Stats")]
        [SerializeField] private float shipMass = 1000f;
        [SerializeField] private Vector3 colliderSize = new Vector3(2f, 1f, 4f);
        
        [Header("4. Setup Controls")]
        [Space(10)]
        [SerializeField] private bool previewWeaponPoints = false;
        [SerializeField] private bool createCompletePrefab = false;
        
        private Transform[] primaryFirePoints;
        private Transform[] secondaryFirePoints;

        void Update()
        {
            if (previewWeaponPoints)
            {
                previewWeaponPoints = false;
                PreviewWeaponPoints();
            }
            
            if (createCompletePrefab)
            {
                createCompletePrefab = false;
                CreateCompletePrefab();
            }
        }

        private void PreviewWeaponPoints()
        {
            Debug.Log("=== WEAPON POINTS PREVIEW ===");
            
            // Show where weapons will be placed
            DrawWeaponGizmos();
            
            Debug.Log($"✓ Left Barrel: {transform.position + leftBarrelOffset}");
            Debug.Log($"✓ Right Barrel: {transform.position + rightBarrelOffset}");
            Debug.Log($"✓ Missile Pods: {missilePodOffsets.Length} positions");
        }

        private void CreateCompletePrefab()
        {
            Debug.Log("=== CREATING COMPLETE SHIP PREFAB ===");
            
            // 1. Setup ship model
            SetupShipModel();
            
            // 2. Create weapon mount structure
            CreateWeaponHierarchy();
            
            // 3. Add all ship components
            AddShipComponents();
            
            // 4. Setup weapon systems
            SetupWeaponSystems();
            
            // 5. Configure final settings
            FinalizeSetup();
            
            Debug.Log("=== SHIP PREFAB COMPLETE! ===");
            Debug.Log("🎮 CONTROLS:");
            Debug.Log("   Left Mouse: Arc Splitter (Primary)");
            Debug.Log("   Right Mouse: Hunter Seeker (Secondary)");
            Debug.Log("💡 TIP: Save as prefab for reuse!");
        }

        private void SetupShipModel()
        {
            if (shipModel != null && shipModel != gameObject)
            {
                // Make ship model a child if it's not already
                if (shipModel.transform.parent != transform)
                {
                    shipModel.transform.SetParent(transform);
                    shipModel.transform.localPosition = Vector3.zero;
                    shipModel.transform.localRotation = Quaternion.identity;
                    Debug.Log("✓ Ship model positioned");
                }
            }
            
            gameObject.name = shipName;
        }

        private void CreateWeaponHierarchy()
        {
            // Create main weapon container
            Transform weaponMounts = transform.Find("WeaponMounts");
            if (weaponMounts == null)
            {
                weaponMounts = new GameObject("WeaponMounts").transform;
                weaponMounts.SetParent(transform);
                weaponMounts.localPosition = Vector3.zero;
            }
            
            // Create primary weapon fire points (twin barrels)
            CreatePrimaryFirePoints(weaponMounts);
            
            // Create secondary weapon fire points (missile pods)
            CreateSecondaryFirePoints(weaponMounts);
            
            Debug.Log("✓ Weapon hierarchy created");
        }

        private void CreatePrimaryFirePoints(Transform parent)
        {
            Transform primaryMount = parent.Find("PrimaryWeapon");
            if (primaryMount == null)
            {
                primaryMount = new GameObject("PrimaryWeapon").transform;
                primaryMount.SetParent(parent);
                primaryMount.localPosition = Vector3.zero;
            }
            
            // Left barrel
            Transform leftBarrel = primaryMount.Find("LeftBarrel");
            if (leftBarrel == null)
            {
                leftBarrel = new GameObject("LeftBarrel").transform;
                leftBarrel.SetParent(primaryMount);
                leftBarrel.localPosition = leftBarrelOffset;
                leftBarrel.localRotation = Quaternion.identity;
            }
            
            // Right barrel
            Transform rightBarrel = primaryMount.Find("RightBarrel");
            if (rightBarrel == null)
            {
                rightBarrel = new GameObject("RightBarrel").transform;
                rightBarrel.SetParent(primaryMount);
                rightBarrel.localPosition = rightBarrelOffset;
                rightBarrel.localRotation = Quaternion.identity;
            }
            
            primaryFirePoints = new Transform[] { leftBarrel, rightBarrel };
            Debug.Log("✓ Primary fire points created (Twin Barrels)");
        }

        private void CreateSecondaryFirePoints(Transform parent)
        {
            Transform secondaryMount = parent.Find("SecondaryWeapon");
            if (secondaryMount == null)
            {
                secondaryMount = new GameObject("SecondaryWeapon").transform;
                secondaryMount.SetParent(parent);
                secondaryMount.localPosition = Vector3.zero;
            }
            
            secondaryFirePoints = new Transform[missilePodOffsets.Length];
            
            for (int i = 0; i < missilePodOffsets.Length; i++)
            {
                string podName = $"MissilePod_{i + 1}";
                Transform pod = secondaryMount.Find(podName);
                
                if (pod == null)
                {
                    pod = new GameObject(podName).transform;
                    pod.SetParent(secondaryMount);
                    pod.localPosition = missilePodOffsets[i];
                    pod.localRotation = Quaternion.identity;
                }
                
                secondaryFirePoints[i] = pod;
            }
            
            Debug.Log($"✓ Secondary fire points created ({missilePodOffsets.Length} Missile Pods)");
        }

        private void AddShipComponents()
        {
            // Add Rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.mass = shipMass;
            rb.linearDamping = 2f;
            rb.angularDamping = 5f;
            rb.useGravity = false;
            
            // Add Collider
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
                boxCol.size = colliderSize;
            }
            
            // Add PrototypeShip
            PrototypeShip prototypeShip = GetComponent<PrototypeShip>();
            if (prototypeShip == null)
            {
                prototypeShip = gameObject.AddComponent<PrototypeShip>();
            }
            
            // Add DamageSystem
            DamageSystem damageSystem = GetComponent<DamageSystem>();
            if (damageSystem == null)
            {
                damageSystem = gameObject.AddComponent<DamageSystem>();
            }
            
            // Add EnergySystem
            EnergySystem energySystem = GetComponent<EnergySystem>();
            if (energySystem == null)
            {
                energySystem = gameObject.AddComponent<EnergySystem>();
            }
            
            Debug.Log("✓ Ship components added");
        }

        private void SetupWeaponSystems()
        {
            // Add PrototypeRazorShip
            PrototypeRazorShip razorShip = GetComponent<PrototypeRazorShip>();
            if (razorShip == null)
            {
                razorShip = gameObject.AddComponent<PrototypeRazorShip>();
            }
            
            // Create weapon GameObjects and add components
            CreateArcSplitterWeapon();
            CreateHunterSeekerWeapon();
            
            Debug.Log("✓ Weapon systems created");
        }

        private void CreateArcSplitterWeapon()
        {
            Transform weaponMounts = transform.Find("WeaponMounts");
            Transform primaryMount = weaponMounts.Find("PrimaryWeapon");
            
            GameObject arcSplitterObj = primaryMount.Find("ArcSplitter")?.gameObject;
            if (arcSplitterObj == null)
            {
                arcSplitterObj = new GameObject("ArcSplitter");
                arcSplitterObj.transform.SetParent(primaryMount);
                arcSplitterObj.transform.localPosition = Vector3.zero;
            }
            
            ArcSplitter arcSplitter = arcSplitterObj.GetComponent<ArcSplitter>();
            if (arcSplitter == null)
            {
                arcSplitter = arcSplitterObj.AddComponent<ArcSplitter>();
            }
            
            // Setup fire points
            arcSplitter.TwinBarrels = primaryFirePoints;
            
            Debug.Log("✓ Arc Splitter weapon created");
        }

        private void CreateHunterSeekerWeapon()
        {
            Transform weaponMounts = transform.Find("WeaponMounts");
            Transform secondaryMount = weaponMounts.Find("SecondaryWeapon");
            
            GameObject hunterSeekerObj = secondaryMount.Find("HunterSeeker")?.gameObject;
            if (hunterSeekerObj == null)
            {
                hunterSeekerObj = new GameObject("HunterSeeker");
                hunterSeekerObj.transform.SetParent(secondaryMount);
                hunterSeekerObj.transform.localPosition = Vector3.zero;
            }
            
            HunterSeeker hunterSeeker = hunterSeekerObj.GetComponent<HunterSeeker>();
            if (hunterSeeker == null)
            {
                hunterSeeker = hunterSeekerObj.AddComponent<HunterSeeker>();
            }
            
            // Setup missile pods
            hunterSeeker.MissilePods = secondaryFirePoints;
            
            Debug.Log("✓ Hunter Seeker weapon created");
        }

        private void FinalizeSetup()
        {
            // Set layer for ship
            gameObject.layer = LayerMask.NameToLayer("Default");
            
            // Add tag if needed
            if (gameObject.tag == "Untagged")
            {
                gameObject.tag = "Player"; // or create "Ship" tag
            }
            
            Debug.Log("✓ Final setup complete");
        }

        private void DrawWeaponGizmos()
        {
            // This will show weapon positions in Scene view
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + leftBarrelOffset, 0.1f);
            Gizmos.DrawWireSphere(transform.position + rightBarrelOffset, 0.1f);
            
            Gizmos.color = Color.blue;
            for (int i = 0; i < missilePodOffsets.Length; i++)
            {
                Gizmos.DrawWireCube(transform.position + missilePodOffsets[i], Vector3.one * 0.2f);
            }
        }

        void OnDrawGizmosSelected()
        {
            DrawWeaponGizmos();
        }

        // Public methods for fine-tuning
        public void SetBarrelOffsets(Vector3 left, Vector3 right)
        {
            leftBarrelOffset = left;
            rightBarrelOffset = right;
        }

        public void SetMissilePodOffsets(Vector3[] offsets)
        {
            missilePodOffsets = offsets;
        }
    }
} 