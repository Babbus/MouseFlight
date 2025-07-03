using UnityEngine;
using DomeClash.Weapons.PrototypeRazorWeapons;

namespace DomeClash.Weapons
{
    /// <summary>
    /// Creates a simple missile prefab for testing Hunter Seeker
    /// </summary>
    public class SimpleMissilePrefabCreator : MonoBehaviour
    {
        [Header("Missile Prefab Creator")]
        [SerializeField] private bool createMissilePrefab = false;
        [SerializeField] private bool assignToHunterSeeker = false;
        
        private GameObject createdMissilePrefab;

        void Update()
        {
            if (createMissilePrefab)
            {
                createMissilePrefab = false;
                CreateSimpleMissilePrefab();
            }
            
            if (assignToHunterSeeker)
            {
                assignToHunterSeeker = false;
                AssignToHunterSeekers();
            }
        }

        private void CreateSimpleMissilePrefab()
        {
            // Create a simple missile GameObject
            GameObject missile = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            missile.name = "HunterMissile";
            
            // Scale it to look like a missile
            missile.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
            
            // Make it red for visibility
            Renderer renderer = missile.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material missileMat = new Material(Shader.Find("Standard"));
                missileMat.color = Color.red;
                renderer.material = missileMat;
            }
            
            // Add Rigidbody
            Rigidbody rb = missile.GetComponent<Rigidbody>();
            if (rb == null)
                rb = missile.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = 0.1f;
            
            // Make collider a trigger
            Collider col = missile.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
            
            // Add HunterMissile component
            HunterMissile hunterMissile = missile.AddComponent<HunterMissile>();
            
            createdMissilePrefab = missile;
            
            Debug.Log("✓ Simple missile prefab created! Now assign it to Hunter Seekers.");
        }

        private void AssignToHunterSeekers()
        {
            if (createdMissilePrefab == null)
            {
                Debug.LogError("No missile prefab created! Create one first.");
                return;
            }
            
            // Find all Hunter Seeker weapons in scene
            HunterSeeker[] hunterSeekers = FindObjectsByType<HunterSeeker>(FindObjectsSortMode.None);
            
            foreach (HunterSeeker hunterSeeker in hunterSeekers)
            {
                // Use reflection to set the private hunterMissilePrefab field
                var field = typeof(HunterSeeker).GetField("hunterMissilePrefab", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    field.SetValue(hunterSeeker, createdMissilePrefab);
                    Debug.Log($"✓ Missile prefab assigned to {hunterSeeker.name}");
                }
            }
            
            Debug.Log($"✓ Missile prefab assigned to {hunterSeekers.Length} Hunter Seeker(s)");
        }
    }
} 