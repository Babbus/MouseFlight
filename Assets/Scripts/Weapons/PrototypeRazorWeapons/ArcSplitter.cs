using UnityEngine;
using DomeClash.Core;
using DomeClash.Ships;

namespace DomeClash.Weapons.PrototypeRazorWeapons
{
    /// <summary>
    /// Arc Splitter - Prototype Razor Primary
    /// Twin-barreled energy railgun with 3-round burst fire
    /// Arcade: Fast, responsive, satisfying sound/visual feedback
    /// Realistic: Heat buildup, energy consumption, twin barrels
    /// Sci-Fi: Energy projectiles, electric arcs, shield damage bonus
    /// </summary>
    public class ArcSplitter : PulseLaser
    {
        [Header("Arc Splitter Prototype")]
        [SerializeField] private int burstCount = 3;
        [SerializeField] private float burstDelay = 0.1f; // Time between burst shots
        [SerializeField] private float shieldDamageMultiplier = 1.5f; // Bonus vs shields
        [SerializeField] protected Transform[] twinBarrels; // Left and right barrels
        [SerializeField] private ParticleSystem electricArcEffect;
        [SerializeField] private AudioClip[] burstSounds; // Different sound per burst shot
        [SerializeField] private Color energyColor = Color.cyan;
        
        private int currentBurstShot = 0;
        private bool isBursting = false;
        private float nextBurstTime = 0f;
        private int currentBarrel = 0;

        protected override void Awake()
        {
            base.Awake();
            
            // Razor Arc Splitter Stats (Prototype focused)
            weaponName = "Arc Splitter [PROTOTYPE]";
            weaponType = WeaponType.PulseLaser;
            
            // Combat stats - balanced for arcade feel
            stats.damage = 45f; // Medium damage per shot, high DPS in burst
            stats.fireRate = 4f; // 4 bursts per second when not overheated
            stats.range = 400f; // Good range for railgun
            stats.damageType = DamageType.Energy;
            
            // Physical properties - light and agile
            stats.mass = 280f; // Medium weight (from GDD)
            stats.recoil = 3f; // Light recoil per shot
            
            // Heat management - key for arcade/realistic balance
            stats.heatGeneration = 12f; // Heat per shot (36 per burst)
            stats.maxHeat = 100f;
            stats.heatDissipation = 20f; // Fast cooling for arcade feel
            stats.overheatCooldown = 1.5f; // Quick recovery
            
            // Energy consumption
            stats.energyConsumption = 8f; // Per shot (24 per burst)
            
            // Combat properties
            stats.criticalChance = 0.12f; // Higher crit chance for precision weapon
            stats.criticalMultiplier = 1.8f;
            
            // Setup twin barrels
            if (twinBarrels == null || twinBarrels.Length == 0)
            {
                twinBarrels = new Transform[] { firePoint };
            }
            
            // Set laser visual properties
            SetLaserColor(energyColor);
            SetLaserWidth(0.05f, 0.02f); // Thin energy beam
        }

        protected override void HandleInput()
        {
            // Handle burst firing
            if (isBursting)
            {
                if (Time.time >= nextBurstTime)
                {
                    FireBurstShot();
                }
                return;
            }
            
            // Start new burst on input
            if (Input.GetMouseButtonDown(0) && CanFire())
            {
                StartBurst();
            }
        }

        private void StartBurst()
        {
            isBursting = true;
            currentBurstShot = 0;
            FireBurstShot();
        }

        private void FireBurstShot()
        {
            if (!CanFire())
            {
                EndBurst();
                return;
            }
            
            // Fire from current barrel
            Transform currentBarrelTransform = twinBarrels[currentBarrel];
            firePoint = currentBarrelTransform;
            
            // Perform hitscan with shield damage bonus
            PerformEnhancedHitscan();
            
            // Play burst effects
            PlayBurstEffects();
            
            // Update heat and energy
            currentHeat += stats.heatGeneration;
            if (ownerShip != null)
            {
                OnEnergyConsumed?.Invoke(stats.energyConsumption);
            }
            
            // Check for overheating
            if (currentHeat >= stats.maxHeat && !isOverheated)
            {
                isOverheated = true;
                overheatRecoveryTime = stats.overheatCooldown;
                OnWeaponOverheated?.Invoke();
                Debug.Log($"{weaponName} overheated during burst!");
                EndBurst();
                return;
            }
            
            // Switch barrel for next shot
            currentBarrel = (currentBarrel + 1) % twinBarrels.Length;
            currentBurstShot++;
            
            // Check if burst is complete
            if (currentBurstShot >= burstCount)
            {
                EndBurst();
            }
            else
            {
                nextBurstTime = Time.time + burstDelay;
            }
            
            OnHeatChanged?.Invoke(currentHeat);
        }

        private void PerformEnhancedHitscan()
        {
            Vector3 startPos = firePoint.position;
            Vector3 direction = firePoint.forward;
            
            // Perform raycast
            RaycastHit hit;
            Vector3 endPos;
            bool hitSomething = Physics.Raycast(startPos, direction, out hit, stats.range, hitLayers);
            
            if (hitSomething)
            {
                endPos = hit.point;
                
                // Apply damage with shield bonus
                                    PrototypeShip targetShip = hit.collider.GetComponent<PrototypeShip>();
                if (targetShip != null && targetShip != ownerShip)
                {
                    ApplyEnhancedDamage(targetShip, stats.damage);
                }
                
                // Create hit effect
                CreateHitEffect(hit.point, hit.normal);
            }
            else
            {
                endPos = startPos + direction * stats.range;
            }
            
            // Show laser beam with electric arcs
            ShowEnhancedLaserBeam(startPos, endPos);
        }

        private void ApplyEnhancedDamage(PrototypeShip target, float baseDamage)
        {
            DamageSystem targetDamageSystem = target.GetComponent<DamageSystem>();
            if (targetDamageSystem != null)
            {
                float finalDamage = baseDamage;
                
                // Bonus damage against shields (sci-fi element)
                if (targetDamageSystem.HasShield)
                {
                    finalDamage *= shieldDamageMultiplier;
                    Debug.Log($"Arc Splitter shield bonus! {finalDamage} damage");
                }
                
                targetDamageSystem.TakeDamage(finalDamage, DamageType.Energy, ownerShip);
            }
        }

        private void ShowEnhancedLaserBeam(Vector3 startPos, Vector3 endPos)
        {
            // Show main laser beam
            ShowLaserBeam(startPos, endPos);
            
            // Add electric arc effect (sci-fi flair)
            if (electricArcEffect != null)
            {
                electricArcEffect.transform.position = startPos;
                electricArcEffect.transform.LookAt(endPos);
                electricArcEffect.Play();
            }
        }

        private void PlayBurstEffects()
        {
            // Play burst sound with variation
            if (burstSounds != null && burstSounds.Length > 0 && audioSource != null)
            {
                int soundIndex = currentBurstShot % burstSounds.Length;
                audioSource.pitch = 1f + (currentBurstShot * 0.1f); // Pitch increases per shot
                audioSource.PlayOneShot(burstSounds[soundIndex]);
            }
            
            // Visual feedback
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
            
            // Light effects
            if (muzzleLight != null)
            {
                muzzleLight.enabled = true;
                muzzleLight.color = energyColor;
                muzzleLight.intensity = 1f + currentBurstShot * 0.3f; // Intensity builds up
            }
        }

        private void EndBurst()
        {
            isBursting = false;
            currentBurstShot = 0;
            lastFireTime = Time.time; // Set for fire rate cooldown
            currentBarrel = 0; // Reset to first barrel
        }

        protected override bool CanFire()
        {
            if (isBursting) return true; // Allow burst continuation
            if (Time.time - lastFireTime < 1f / stats.fireRate) return false;
            if (isOverheated) return false;
            
            return true;
        }

        // Public methods for tweaking
        public void SetBurstCount(int count)
        {
            burstCount = Mathf.Max(1, count);
        }

        public void SetBurstDelay(float delay)
        {
            burstDelay = Mathf.Max(0.05f, delay);
        }

        public void SetShieldDamageMultiplier(float multiplier)
        {
            shieldDamageMultiplier = Mathf.Max(1f, multiplier);
        }

        public void SetEnergyColor(Color color)
        {
            energyColor = color;
            SetLaserColor(color);
            
            if (muzzleLight != null)
                muzzleLight.color = color;
        }

        // Public properties for setup
        public Transform[] TwinBarrels
        {
            get => twinBarrels;
            set => twinBarrels = value;
        }

        // Debug info
        public bool IsBursting() => isBursting;
        public int GetCurrentBurstShot() => currentBurstShot;
        public float GetShieldDamageMultiplier() => shieldDamageMultiplier;
    }
} 