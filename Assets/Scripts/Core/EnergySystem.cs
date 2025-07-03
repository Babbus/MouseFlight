using UnityEngine;
using System;

namespace DomeClash.Core
{
    [System.Serializable]
    public class EnergyStats
    {
        [Header("Energy Pool")]
        public float maxEnergy = 100f;
        public float currentEnergy = 100f;
        public float energyRegenRate = 10f; // Energy per second
        public float energyRegenDelay = 1f; // Delay after consumption before regen starts
        
        [Header("Energy Costs")]
        public float dodgeEnergyCost = 25f;
        public float boostEnergyCost = 15f; // Per second of boost
        public float weaponEnergyMultiplier = 1f; // Multiplier for weapon energy costs
        
        [Header("State")]
        public bool isRegenerating = true;
        public float lastConsumptionTime = 0f;
        
        public float EnergyPercent => maxEnergy > 0f ? currentEnergy / maxEnergy : 0f;
        public bool IsEmpty => currentEnergy <= 0f;
        public bool IsFull => currentEnergy >= maxEnergy;
    }

    public class EnergySystem : MonoBehaviour
    {
        [Header("Energy Configuration")]
        [SerializeField] private EnergyStats energyStats;
        
        // Events
        public event Action<float> OnEnergyChanged;
        public event Action OnEnergyDepleted;
        public event Action OnEnergyFull;
        public event Action<float> OnEnergyConsumed;
        
        // Properties
        public EnergyStats Stats => energyStats;
        public float CurrentEnergy => energyStats.currentEnergy;
        public float MaxEnergy => energyStats.maxEnergy;
        public float EnergyPercent => energyStats.EnergyPercent;
        public bool HasEnergy => !energyStats.IsEmpty;

        private void Awake()
        {
            InitializeEnergy();
        }

        private void Update()
        {
            UpdateEnergyRegeneration();
        }

        private void InitializeEnergy()
        {
            if (energyStats == null)
            {
                energyStats = new EnergyStats();
            }
            
            energyStats.currentEnergy = energyStats.maxEnergy;
            energyStats.lastConsumptionTime = 0f;
            energyStats.isRegenerating = true;
        }

        private void UpdateEnergyRegeneration()
        {
            // Check if we should start regenerating
            if (!energyStats.isRegenerating)
            {
                if (Time.time - energyStats.lastConsumptionTime >= energyStats.energyRegenDelay)
                {
                    energyStats.isRegenerating = true;
                }
            }
            
            // Regenerate energy
            if (energyStats.isRegenerating && !energyStats.IsFull)
            {
                float regenAmount = energyStats.energyRegenRate * Time.deltaTime;
                float oldEnergy = energyStats.currentEnergy;
                
                energyStats.currentEnergy = Mathf.Min(energyStats.maxEnergy, energyStats.currentEnergy + regenAmount);
                
                // Fire events
                OnEnergyChanged?.Invoke(energyStats.currentEnergy);
                
                if (oldEnergy <= 0f && energyStats.currentEnergy > 0f)
                {
                    Debug.Log("Energy restored!");
                }
                
                if (energyStats.IsFull)
                {
                    OnEnergyFull?.Invoke();
                }
            }
        }

        public bool ConsumeEnergy(float amount)
        {
            if (amount <= 0f) return true;
            if (energyStats.currentEnergy < amount) return false;
            
            float oldEnergy = energyStats.currentEnergy;
            energyStats.currentEnergy = Mathf.Max(0f, energyStats.currentEnergy - amount);
            energyStats.lastConsumptionTime = Time.time;
            energyStats.isRegenerating = false;
            
            // Fire events
            OnEnergyChanged?.Invoke(energyStats.currentEnergy);
            OnEnergyConsumed?.Invoke(amount);
            
            if (oldEnergy > 0f && energyStats.currentEnergy <= 0f)
            {
                OnEnergyDepleted?.Invoke();
                Debug.Log("Energy depleted!");
            }
            
            return true;
        }

        public bool HasEnoughEnergy(float amount)
        {
            return energyStats.currentEnergy >= amount;
        }

        public bool ConsumeWeaponEnergy(float baseAmount)
        {
            float adjustedAmount = baseAmount * energyStats.weaponEnergyMultiplier;
            return ConsumeEnergy(adjustedAmount);
        }

        public bool ConsumeDodgeEnergy()
        {
            return ConsumeEnergy(energyStats.dodgeEnergyCost);
        }

        public bool ConsumeBoostEnergy(float deltaTime)
        {
            float boostCost = energyStats.boostEnergyCost * deltaTime;
            return ConsumeEnergy(boostCost);
        }

        public void RestoreEnergy(float amount)
        {
            if (amount <= 0f) return;
            
            float oldEnergy = energyStats.currentEnergy;
            energyStats.currentEnergy = Mathf.Min(energyStats.maxEnergy, energyStats.currentEnergy + amount);
            
            OnEnergyChanged?.Invoke(energyStats.currentEnergy);
            
            if (oldEnergy <= 0f && energyStats.currentEnergy > 0f)
            {
                Debug.Log("Energy restored!");
            }
        }

        public void SetMaxEnergy(float newMax)
        {
            if (newMax <= 0f) return;
            
            float ratio = energyStats.EnergyPercent;
            energyStats.maxEnergy = newMax;
            energyStats.currentEnergy = energyStats.maxEnergy * ratio;
            
            OnEnergyChanged?.Invoke(energyStats.currentEnergy);
        }

        public void FullRestore()
        {
            energyStats.currentEnergy = energyStats.maxEnergy;
            energyStats.isRegenerating = true;
            OnEnergyChanged?.Invoke(energyStats.currentEnergy);
            OnEnergyFull?.Invoke();
        }

        public void SetEnergyRegenRate(float newRate)
        {
            energyStats.energyRegenRate = Mathf.Max(0f, newRate);
        }

        public void SetWeaponEnergyMultiplier(float multiplier)
        {
            energyStats.weaponEnergyMultiplier = Mathf.Max(0f, multiplier);
        }

        // Debug methods
        public void DebugDrainEnergy()
        {
            ConsumeEnergy(energyStats.maxEnergy * 0.5f);
        }

        public void DebugRestoreEnergy()
        {
            FullRestore();
        }
    }
} 