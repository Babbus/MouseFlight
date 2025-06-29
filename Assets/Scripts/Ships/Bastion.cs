using UnityEngine;
using DomeClash.Weapons;
using DomeClash.Core;

namespace DomeClash.Ships
{
    /// <summary>
    /// Bastion - Heavy Tank with Siege Mode
    /// Uses ShipFlightController for all movement (modern modular system)
    /// Special abilities: Siege Mode (damage reduction, energy consumption)
    /// </summary>
    public class Bastion : ShipClass
    {
        [Header("Modular Flight System")]
        [Tooltip("Flight movement component reference")]
        public ShipFlightController flightMovement;

        [Header("Bastion-Specific Abilities")]
        [SerializeField] private float siegeModeDamageReduction = 0.5f;
        [SerializeField] private float siegeModeEnergyCost = 5f;
        [SerializeField] private bool isInSiegeMode = false;
        
        [Header("Weapons")]
        [SerializeField] private WeaponSystem primaryWeapon;
        [SerializeField] private WeaponSystem secondaryWeapon;

        protected override void InitializeShip()
        {
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

            // Transform-based system - NO RIGIDBODY NEEDED!
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                Debug.Log($"{name}: Rigidbody kinematic yapıldı - fizik devre dışı");
            }

            // Auto-find or create ShipFlightController
            if (flightMovement == null)
            {
                flightMovement = GetComponent<ShipFlightController>();
                if (flightMovement == null)
                {
                    flightMovement = gameObject.AddComponent<ShipFlightController>();
                    Debug.Log($"{name}: ShipFlightController added automatically");
                }
            }

            // Auto-assign Bastion flight profile
            if (flightMovement != null && flightMovement.GetFlightProfile() == null)
            {
                FlightProfile bastionProfile = FlightProfile.CreateBastionProfile();
                flightMovement.SetFlightProfile(bastionProfile);
                Debug.Log($"{name}: Auto-assigned Bastion flight profile");
            }

            Debug.Log($"Bastion initialized with Modular Flight System");
        }

        protected override void Update()
        {
            base.Update();
            HandleSiegeMode();
        }

        // Flight system delegation - movement handled by ShipFlightController

        // Input set functions - delegate to ShipFlightController
        public override void SetPitchInput(float value)
        {
            if (flightMovement != null)
                flightMovement.SetPitchInput(value);
        }

        public override void SetYawInput(float value)
        {
            if (flightMovement != null)
                flightMovement.SetYawInput(value);
        }

        public override void SetRollInput(float value)
        {
            if (flightMovement != null)
                flightMovement.SetRollInput(value);
        }

        public override void SetStrafeInput(float value)
        {
            if (flightMovement != null)
                flightMovement.SetStrafeInput(value);
        }

        // Getter methods for DebugHUD - delegate to ShipFlightController
        public float GetPitchInput() => flightMovement?.GetPitchInput() ?? 0f;
        public float GetYawInput() => flightMovement?.GetYawInput() ?? 0f;
        public float GetRollInput() => flightMovement?.GetRollInput() ?? 0f;
        public float GetStrafeInput() => flightMovement?.GetStrafeInput() ?? 0f;
        public float GetThrottle() => flightMovement?.Throttle ?? 0f;
        public new float GetCurrentSpeed() => flightMovement?.CurrentSpeed ?? 0f;
        public float GetFlightSpeed() => flightMovement?.GetFlightProfile()?.flightSpeed ?? 0f;
        public float GetTurnSpeed() => flightMovement?.GetFlightProfile()?.turnSpeed ?? 0f;

        // Banking system getters
        public float GetCurrentBankAngle() => flightMovement?.GetCurrentBankAngle() ?? 0f;
        public float GetCurrentPitch() => flightMovement?.GetCurrentPitch() ?? 0f;
        public float GetCurrentYaw() => flightMovement?.GetCurrentYaw() ?? 0f;

        // Throttle control methods - delegate to ShipFlightController
        public void SetThrottle(float newThrottle)
        {
            if (flightMovement != null)
                flightMovement.SetThrottle(newThrottle);
        }

        public void IncreaseThrottle(float amount = 0.1f)
        {
            if (flightMovement != null)
                flightMovement.IncreaseThrottle(amount);
        }

        public void DecreaseThrottle(float amount = 0.1f)
        {
            if (flightMovement != null)
                flightMovement.DecreaseThrottle(amount);
        }

        // Bastion-specific ability methods
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
            // Transform-based system: Siege mode effects handled by ShipFlightController
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

        // Ability getters
        public bool IsInSiegeMode() => isInSiegeMode;
        public float GetSiegeModeDamageReduction() => isInSiegeMode ? siegeModeDamageReduction : 0f;
    }
} 