using UnityEngine;
using DomeClash.Weapons;
using DomeClash.Core;

namespace DomeClash.Ships
{
    /// <summary>
    /// Razor - Light Interceptor with Cloak Ability
    /// Uses ShipFlightController for all movement (modern modular system)
    /// Special abilities: Cloak, Stealth
    /// </summary>
    public class Razor : ShipClass
    {
        [Header("Modular Flight System")]
        [Tooltip("Flight movement component reference")]
        public ShipFlightController flightMovement;

        [Header("Razor-Specific Abilities")]
        [SerializeField] private float cloakDuration = 3f;
        [SerializeField] private float cloakEnergyCost = 30f;
        [SerializeField] private bool isCloaked = false;
        [SerializeField] private float cloakTimer = 0f;
        
        [Header("Stealth System")]
        [SerializeField] private float stealthDetectionRange = 50f;
        [SerializeField] private bool isStealthActive = false;
        
        [Header("Weapons")]
        [SerializeField] private WeaponSystem primaryWeapon;
        [SerializeField] private WeaponSystem secondaryWeapon;

        protected override void InitializeShip()
        {
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

            // Auto-assign Razor flight profile
            if (flightMovement != null && flightMovement.GetFlightProfile() == null)
            {
                FlightProfile razorProfile = FlightProfile.CreateRazorProfile();
                flightMovement.SetFlightProfile(razorProfile);
                Debug.Log($"{name}: Auto-assigned Razor flight profile");
            }

            Debug.Log($"Razor initialized with Modular Flight System");
        }

        protected override void Update()
        {
            base.Update();
            HandleCloak();
            HandleStealth();
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

        // Razor-specific ability methods
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

        // Ability getters
        public bool IsCloaked() => isCloaked;
        public bool IsStealthActive() => isStealthActive;
        public float GetCloakTimeRemaining() => cloakTimer;
        public float GetStealthDetectionRange() => stealthDetectionRange;
    }
} 