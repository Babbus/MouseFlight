using UnityEngine;
using DomeClash.Weapons;
using DomeClash.Core;

namespace DomeClash.Ships
{
    public class Razor : ShipClass
    {
        [Header("Razor-Specific")]
        [SerializeField] private float cloakDuration = 3f;
        [SerializeField] private float cloakEnergyCost = 30f;
        [SerializeField] private bool isCloaked = false;
        [SerializeField] private float cloakTimer = 0f;
        
        [Header("Stealth")]
        [SerializeField] private float stealthDetectionRange = 50f;
        [SerializeField] private bool isStealthActive = false;
        
        [Header("Weapons")]
        [SerializeField] private WeaponSystem primaryWeapon;
        [SerializeField] private WeaponSystem secondaryWeapon;

        protected override void Awake()
        {
            base.Awake();
            
            // Set Razor-specific stats
            shipType = ShipType.Razor;
            shipName = "Razor";
            
            // Configure stats for light scout role
            stats.maxSpeed = 108f;
            stats.acceleration = 11.7f;
            stats.turnRate = 38f;
            stats.strafeSpeed = 35f;
            stats.boostDuration = 2.4f;
            
            stats.shieldCapacity = 60f;
            stats.armorRating = 30f;
            stats.energyCapacity = 100f;
            
            stats.mass = 220f;  // Reference only - no physics
        }

        protected override void Update()
        {
            base.Update();
            HandleCloak();
            HandleStealth();
        }

        private void HandleCloak()
        {
            // Toggle cloak with Space key
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleCloak();
            }
            
            // Update cloak timer
            if (isCloaked)
            {
                cloakTimer -= Time.deltaTime;
                
                if (cloakTimer <= 0f)
                {
                    DeactivateCloak();
                }
            }
        }

        private void ToggleCloak()
        {
            if (isCloaked)
            {
                DeactivateCloak();
            }
            else
            {
                ActivateCloak();
            }
        }

        private void ActivateCloak()
        {
            if (ConsumeEnergy(cloakEnergyCost))
            {
                isCloaked = true;
                cloakTimer = cloakDuration;
                Debug.Log("Razor: Cloak Activated");
                
                // Visual feedback
                SetCloakVisibility(false);
            }
        }

        private void DeactivateCloak()
        {
            isCloaked = false;
            cloakTimer = 0f;
            Debug.Log("Razor: Cloak Deactivated");
            
            // Visual feedback
            SetCloakVisibility(true);
        }

        private void SetCloakVisibility(bool visible)
        {
            // Simple visibility toggle - in full implementation would use shaders
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Color color = renderer.material.color;
                color.a = visible ? 1f : 0.3f;
                renderer.material.color = color;
            }
        }

        private void HandleStealth()
        {
            // Razor has passive stealth when not firing
            bool shouldBeStealth = !isCloaked && !IsFiring();
            
            if (shouldBeStealth != isStealthActive)
            {
                isStealthActive = shouldBeStealth;
                Debug.Log("Razor: Stealth " + (isStealthActive ? "Active" : "Inactive"));
            }
        }

        private bool IsFiring()
        {
            // Check if weapons are firing
            if (primaryWeapon != null && Input.GetMouseButton(0))
                return true;
            if (secondaryWeapon != null && Input.GetMouseButton(1))
                return true;
            
            return false;
        }

        public override void TakeDamage(float damage, DamageType damageType)
        {
            float modifiedDamage = damage;
            
            // Apply cloak damage reduction
            if (isCloaked)
            {
                modifiedDamage *= 0.3f; // 70% damage reduction while cloaked
            }
            
            // Apply class-specific resistances
            switch (damageType)
            {
                case DamageType.Kinetic:
                    modifiedDamage *= 1.1f; // 10% kinetic vulnerability
                    break;
                case DamageType.Energy:
                    modifiedDamage *= 0.9f; // 10% energy resistance
                    break;
                case DamageType.Explosive:
                    modifiedDamage *= 1.3f; // 30% explosive vulnerability
                    break;
            }
            
            base.TakeDamage(modifiedDamage, damageType);
        }

        // Transform-based system doesn't need stall handling

        public bool IsCloaked()
        {
            return isCloaked;
        }

        public bool IsStealthActive()
        {
            return isStealthActive;
        }

        public float GetCloakTimeRemaining()
        {
            return cloakTimer;
        }

        public float GetStealthDetectionRange()
        {
            return stealthDetectionRange;
        }
    }
} 