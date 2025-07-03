using UnityEngine;
using System;
using DomeClash.Ships;

namespace DomeClash.Core
{
    public enum DamageType
    {
        Kinetic,    // Physical projectiles, bullets, impacts - Effective against Armor, Core
        Energy,     // Lasers, beams, pulse weapons - Effective against Shields
        EMP,        // Disruptive, disables systems - Effective against Shields, Modules, Radar, Engines
        Thermal,    // Heat-based (AoE, overheat) - Effective against Armor, Weapons, Engine
        Explosive   // Missiles, cluster munitions - Moderate across all layers
    }

    [System.Serializable]
    public class DamageResistance
    {
        [Header("Damage Type Resistances (0-1)")]
        [Range(0f, 1f)] public float kinetic = 0f;
        [Range(0f, 1f)] public float energy = 0f;
        [Range(0f, 1f)] public float emp = 0f;
        [Range(0f, 1f)] public float thermal = 0f;
        [Range(0f, 1f)] public float explosive = 0f;

        public float GetResistance(DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Kinetic => kinetic,
                DamageType.Energy => energy,
                DamageType.EMP => emp,
                DamageType.Thermal => thermal,
                DamageType.Explosive => explosive,
                _ => 0f
            };
        }
    }

    [System.Serializable]
    public class DefenseLayer
    {
        [Header("Layer Properties")]
        public float maxHP = 100f;
        public float currentHP = 100f;
        public DamageResistance resistance;
        public bool canRegenerate = false;
        public float regenRate = 0f;
        public float regenDelay = 3f;
        
        [Header("State")]
        public bool isActive = true;
        public float lastDamageTime = 0f;

        public bool IsDepleted => currentHP <= 0f;
        public float HPPercent => maxHP > 0f ? currentHP / maxHP : 0f;

        public void TakeDamage(float damage, DamageType damageType)
        {
            if (!isActive || IsDepleted) return;

            float resistance = this.resistance.GetResistance(damageType);
            float finalDamage = damage * (1f - resistance);
            
            currentHP = Mathf.Max(0f, currentHP - finalDamage);
            lastDamageTime = Time.time;
        }

        public void Regenerate()
        {
            if (!canRegenerate || !isActive || currentHP >= maxHP) return;
            if (Time.time - lastDamageTime < regenDelay) return;

            currentHP = Mathf.Min(maxHP, currentHP + regenRate * Time.deltaTime);
        }

        public void Repair(float amount)
        {
            if (!isActive) return;
            currentHP = Mathf.Min(maxHP, currentHP + amount);
        }

        public void Reset()
        {
            currentHP = maxHP;
            lastDamageTime = 0f;
            isActive = true;
        }
    }

    public class DamageSystem : MonoBehaviour
    {
        [Header("Defense Layers")]
        [SerializeField] private DefenseLayer shield;
        [SerializeField] private DefenseLayer armor;
        [SerializeField] private DefenseLayer core;

        [Header("Critical Hit System")]
        [SerializeField] private bool enableCriticalHits = true;
        [SerializeField] private float criticalHitChance = 0.1f;
        [SerializeField] private float criticalHitMultiplier = 2f;

        // Events
        public event Action<float, DamageType> OnDamageTaken;
        public event Action<DefenseLayer> OnLayerDepleted;
        public event Action OnShipDestroyed;
        public event Action<string> OnCriticalFailure;

        // Properties
        public DefenseLayer Shield => shield;
        public DefenseLayer Armor => armor;
        public DefenseLayer Core => core;
        
        public bool IsAlive => !core.IsDepleted;
        public float TotalHP => shield.currentHP + armor.currentHP + core.currentHP;
        public float TotalMaxHP => shield.maxHP + armor.maxHP + core.maxHP;
        public float TotalHPPercent => TotalMaxHP > 0f ? TotalHP / TotalMaxHP : 0f;

        private void Awake()
        {
            InitializeLayers();
        }

        private void Update()
        {
            // Handle regeneration
            shield.Regenerate();
            armor.Regenerate();
            // Core typically doesn't regenerate in combat
        }

        private void InitializeLayers()
        {
            // Initialize shield (typically regenerates)
            if (shield == null)
            {
                shield = new DefenseLayer
                {
                    maxHP = 100f,
                    currentHP = 100f,
                    canRegenerate = true,
                    regenRate = 10f,
                    regenDelay = 3f,
                    resistance = new DamageResistance { energy = 0.2f }
                };
            }

            // Initialize armor (typically doesn't regenerate)
            if (armor == null)
            {
                armor = new DefenseLayer
                {
                    maxHP = 150f,
                    currentHP = 150f,
                    canRegenerate = false,
                    resistance = new DamageResistance { kinetic = 0.15f, explosive = 0.1f }
                };
            }

            // Initialize core (critical systems)
            if (core == null)
            {
                core = new DefenseLayer
                {
                    maxHP = 75f,
                    currentHP = 75f,
                    canRegenerate = false,
                    resistance = new DamageResistance()
                };
            }

            shield.Reset();
            armor.Reset();
            core.Reset();
        }

        public void TakeDamage(float damage, DamageType damageType, PrototypeShip attacker = null)
        {
            if (!IsAlive) return;

            float originalDamage = damage;
            
            // Apply critical hit chance
            if (enableCriticalHits && UnityEngine.Random.value < criticalHitChance)
            {
                damage *= criticalHitMultiplier;
                Debug.Log($"Critical Hit! {damage} damage dealt!");
            }

            // Apply damage through layers: Shield -> Armor -> Core
            float remainingDamage = damage;

            // Shield layer
            if (!shield.IsDepleted && remainingDamage > 0f)
            {
                float shieldDamage = Mathf.Min(remainingDamage, shield.currentHP);
                shield.TakeDamage(shieldDamage, damageType);
                remainingDamage -= shieldDamage;

                if (shield.IsDepleted)
                {
                    OnLayerDepleted?.Invoke(shield);
                    Debug.Log("Shield depleted!");
                }
            }

            // Armor layer
            if (!armor.IsDepleted && remainingDamage > 0f)
            {
                float armorDamage = Mathf.Min(remainingDamage, armor.currentHP);
                armor.TakeDamage(armorDamage, damageType);
                remainingDamage -= armorDamage;

                if (armor.IsDepleted)
                {
                    OnLayerDepleted?.Invoke(armor);
                    Debug.Log("Armor depleted!");
                }
            }

            // Core layer
            if (remainingDamage > 0f)
            {
                core.TakeDamage(remainingDamage, damageType);
                
                if (core.IsDepleted)
                {
                    OnLayerDepleted?.Invoke(core);
                    OnShipDestroyed?.Invoke();
                    Debug.Log("Ship destroyed!");
                }
                else
                {
                    // Check for critical system failures
                    CheckCriticalFailures();
                }
            }

            // Fire damage taken event
            OnDamageTaken?.Invoke(originalDamage, damageType);
        }

        private void CheckCriticalFailures()
        {
            if (core.HPPercent < 0.25f)
            {
                // High chance of critical failures when core is low
                if (UnityEngine.Random.value < 0.15f)
                {
                    TriggerCriticalFailure();
                }
            }
        }

        private void TriggerCriticalFailure()
        {
            string[] failures = {
                "Engine stall",
                "Weapon misfire", 
                "Radar blackout",
                "Control flicker"
            };

            string failure = failures[UnityEngine.Random.Range(0, failures.Length)];
            OnCriticalFailure?.Invoke(failure);
            Debug.Log($"Critical failure: {failure}");
        }

        public void RepairShield(float amount)
        {
            shield.Repair(amount);
        }

        public void RepairArmor(float amount)
        {
            armor.Repair(amount);
        }

        public void RepairCore(float amount)
        {
            core.Repair(amount);
        }

        public void FullRepair()
        {
            shield.Reset();
            armor.Reset();
            core.Reset();
        }

        // Utility methods
        public bool HasShield => !shield.IsDepleted;
        public bool HasArmor => !armor.IsDepleted;
        public float GetLayerPercent(int layer)
        {
            return layer switch
            {
                0 => shield.HPPercent,
                1 => armor.HPPercent,
                2 => core.HPPercent,
                _ => 0f
            };
        }
    }
} 