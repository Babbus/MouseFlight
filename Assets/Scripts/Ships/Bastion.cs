using UnityEngine;
using DomeClash.Weapons;
using DomeClash.Core;

namespace DomeClash.Ships
{
    public class Bastion : ShipClass
    {
        [Header("Bastion-Specific")]
        [SerializeField] private float siegeModeDamageReduction = 0.5f;
        [SerializeField] private float siegeModeEnergyCost = 5f;
        [SerializeField] private bool isInSiegeMode = false;
        
        [Header("Weapons")]
        [SerializeField] private WeaponSystem primaryWeapon;
        [SerializeField] private WeaponSystem secondaryWeapon;

        protected override void Awake()
        {
            base.Awake();
            
            // Set Bastion-specific stats
            shipType = ShipType.Bastion;
            shipName = "Bastion";
            
            // Configure stats for heavy tank role
            stats.maxSpeed = 78f;
            stats.acceleration = 6.2f;
            stats.turnRate = 24f;
            stats.strafeSpeed = 15f;
            stats.boostDuration = 3.4f;
            
            stats.shieldCapacity = 150f;
            stats.armorRating = 100f;
            stats.energyCapacity = 120f;
            
            stats.mass = 490f;  // Reference only - no physics
        }

        protected override void Update()
        {
            base.Update();
            HandleSiegeMode();
        }

        private void HandleSiegeMode()
        {
            // Toggle siege mode with Space key
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleSiegeMode();
            }
            
            // Apply siege mode effects
            if (isInSiegeMode)
            {
                ApplySiegeModeEffects();
            }
        }

        private void ToggleSiegeMode()
        {
            if (isInSiegeMode)
            {
                // Exit siege mode
                isInSiegeMode = false;
                Debug.Log("Bastion: Exiting Siege Mode");
            }
            else
            {
                // Enter siege mode if we have enough energy
                if (ConsumeEnergy(siegeModeEnergyCost))
                {
                    isInSiegeMode = true;
                    Debug.Log("Bastion: Entering Siege Mode");
                }
            }
        }

        private void ApplySiegeModeEffects()
        {
            // Transform-based system: Siege mode effects handled by PrototypeShip
            // Speed reduction will be applied in the movement system
            
            // Consume energy over time
            if (Time.frameCount % 60 == 0) // Every 60 frames (1 second at 60fps)
            {
                ConsumeEnergy(1f); // 1 energy per second
            }
        }

        public override void TakeDamage(float damage, DamageType damageType)
        {
            float modifiedDamage = damage;
            
            // Apply siege mode damage reduction
            if (isInSiegeMode)
            {
                modifiedDamage *= (1f - siegeModeDamageReduction);
            }
            
            // Apply class-specific resistances
            switch (damageType)
            {
                case DamageType.Kinetic:
                    modifiedDamage *= 0.8f; // 20% kinetic resistance
                    break;
                case DamageType.Energy:
                    modifiedDamage *= 1.2f; // 20% energy vulnerability
                    break;
                case DamageType.Explosive:
                    modifiedDamage *= 0.9f; // 10% explosive resistance
                    break;
            }
            
            base.TakeDamage(modifiedDamage, damageType);
        }

        // Transform-based system doesn't need stall handling

        public bool IsInSiegeMode()
        {
            return isInSiegeMode;
        }

        public float GetSiegeModeDamageReduction()
        {
            return isInSiegeMode ? siegeModeDamageReduction : 0f;
        }
    }
} 