using UnityEngine;
using UnityEngine.UI;
using DomeClash.Core;
using DomeClash.Weapons;

namespace DomeClash.UI
{
    public class DomeClashHUD : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private MouseFlightController flightController;
        [SerializeField] private ShipClass playerShip;
        [SerializeField] private WeaponSystem primaryWeapon;
        [SerializeField] private WeaponSystem secondaryWeapon;
        
        [Header("HUD Elements")]
        [SerializeField] private Image shieldBar;
        [SerializeField] private Image armorBar;
        [SerializeField] private Image energyBar;
        [SerializeField] private Image heatBar;
        [SerializeField] private Image lockProgressBar;
        
        [Header("Crosshair")]
        [SerializeField] private RectTransform crosshair;
        [SerializeField] private RectTransform lockIndicator;
        [SerializeField] private RectTransform mouseAimIndicator;
        
        [Header("Radar")]
        [SerializeField] private RectTransform radarDisplay;
        [SerializeField] private float radarRange = 200f;
        [SerializeField] private float radarSize = 200f;
        
        [Header("Score")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text matchTimeText;
        
        private Camera playerCamera;

        private void Awake()
        {
            // Find components if not assigned
            if (flightController == null)
                flightController = FindFirstObjectByType<MouseFlightController>();
                
            if (playerShip == null)
                playerShip = FindFirstObjectByType<ShipClass>();
                
            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        private void Start()
        {
            // Subscribe to events
            if (playerShip != null)
            {
                playerShip.OnShieldChanged += UpdateShieldBar;
                playerShip.OnArmorChanged += UpdateArmorBar;
                playerShip.OnEnergyChanged += UpdateEnergyBar;
            }
            
            if (primaryWeapon != null)
            {
                primaryWeapon.OnHeatChanged += UpdateHeatBar;
                primaryWeapon.OnTargetLocked += OnTargetLocked;
                primaryWeapon.OnTargetLost += OnTargetLost;
            }
        }

        private void Update()
        {
            UpdateCrosshair();
            UpdateRadar();
            UpdateLockIndicator();
        }

        private void UpdateCrosshair()
        {
            if (crosshair == null || playerCamera == null) return;

            // Update crosshair position based on mouse aim
            Vector3 mouseAimPos = flightController?.MouseAimPos ?? Vector3.zero;
            Vector3 screenPos = playerCamera.WorldToScreenPoint(mouseAimPos);
            
            if (screenPos.z > 0)
            {
                crosshair.position = screenPos;
                crosshair.gameObject.SetActive(true);
            }
            else
            {
                crosshair.gameObject.SetActive(false);
            }
        }

        private void UpdateRadar()
        {
            if (radarDisplay == null || playerShip == null) return;

            // Simple radar implementation
            // In a full implementation, this would show nearby ships
            Collider[] nearbyColliders = Physics.OverlapSphere(playerShip.transform.position, radarRange);
            
            // Clear existing radar blips
            foreach (Transform child in radarDisplay)
            {
                Destroy(child.gameObject);
            }
            
            // Create radar blips for nearby ships
            foreach (Collider col in nearbyColliders)
            {
                ShipClass ship = col.GetComponent<ShipClass>();
                if (ship != null && ship != playerShip)
                {
                    CreateRadarBlip(ship.transform);
                }
            }
        }

        private void CreateRadarBlip(Transform target)
        {
            if (radarDisplay == null) return;

            // Calculate relative position
            Vector3 relativePos = target.position - playerShip.transform.position;
            Vector2 radarPos = new Vector2(relativePos.x, relativePos.z) / radarRange * (radarSize * 0.5f);
            
            // Create blip
            GameObject blip = new GameObject("RadarBlip");
            blip.transform.SetParent(radarDisplay);
            
            Image blipImage = blip.AddComponent<Image>();
            blipImage.color = Color.red;
            blipImage.rectTransform.sizeDelta = new Vector2(4, 4);
            blipImage.rectTransform.anchoredPosition = radarPos;
        }

        private void UpdateLockIndicator()
        {
            if (lockIndicator == null || primaryWeapon == null) return;

            Transform lockedTarget = primaryWeapon.GetLockedTarget();
            if (lockedTarget != null && playerCamera != null)
            {
                Vector3 screenPos = playerCamera.WorldToScreenPoint(lockedTarget.position);
                
                if (screenPos.z > 0)
                {
                    lockIndicator.position = screenPos;
                    lockIndicator.gameObject.SetActive(true);
                    
                    // Update lock progress
                    if (lockProgressBar != null)
                    {
                        lockProgressBar.fillAmount = primaryWeapon.GetLockProgress();
                    }
                }
                else
                {
                    lockIndicator.gameObject.SetActive(false);
                }
            }
            else
            {
                lockIndicator.gameObject.SetActive(false);
                if (lockProgressBar != null)
                {
                    lockProgressBar.fillAmount = 0f;
                }
            }
        }

        private void UpdateShieldBar(float currentShield)
        {
            if (shieldBar != null && playerShip != null)
            {
                shieldBar.fillAmount = playerShip.GetShieldPercent();
                
                // Change color based on shield level
                if (playerShip.GetShieldPercent() < 0.25f)
                {
                    shieldBar.color = Color.red;
                }
                else if (playerShip.GetShieldPercent() < 0.5f)
                {
                    shieldBar.color = Color.yellow;
                }
                else
                {
                    shieldBar.color = Color.blue;
                }
            }
        }

        private void UpdateArmorBar(float currentArmor)
        {
            if (armorBar != null && playerShip != null)
            {
                armorBar.fillAmount = playerShip.GetArmorPercent();
                
                // Change color based on armor level
                if (playerShip.GetArmorPercent() < 0.25f)
                {
                    armorBar.color = Color.red;
                }
                else if (playerShip.GetArmorPercent() < 0.5f)
                {
                    armorBar.color = Color.yellow;
                }
                else
                {
                    armorBar.color = Color.gray;
                }
            }
        }

        private void UpdateEnergyBar(float currentEnergy)
        {
            if (energyBar != null && playerShip != null)
            {
                energyBar.fillAmount = playerShip.GetEnergyPercent();
                
                // Change color based on energy level
                if (playerShip.GetEnergyPercent() < 0.25f)
                {
                    energyBar.color = Color.red;
                }
                else if (playerShip.GetEnergyPercent() < 0.5f)
                {
                    energyBar.color = Color.yellow;
                }
                else
                {
                    energyBar.color = Color.green;
                }
            }
        }

        private void UpdateHeatBar(float currentHeat)
        {
            if (heatBar != null && primaryWeapon != null)
            {
                heatBar.fillAmount = primaryWeapon.GetHeatPercent();
                
                // Change color based on heat level
                if (primaryWeapon.IsOverheated())
                {
                    heatBar.color = Color.red;
                }
                else if (primaryWeapon.GetHeatPercent() > 0.7f)
                {
                    heatBar.color = Color.yellow;
                }
                else
                {
                    heatBar.color = Color.orange;
                }
            }
        }

        private void OnTargetLocked(Transform target)
        {
            // Play lock-on sound or effect
            Debug.Log("Target locked: " + target.name);
        }

        private void OnTargetLost()
        {
            // Play lock-lost sound or effect
            Debug.Log("Target lost");
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = "Score: " + score.ToString();
            }
        }

        public void UpdateMatchTime(float time)
        {
            if (matchTimeText != null)
            {
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                matchTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (playerShip != null)
            {
                playerShip.OnShieldChanged -= UpdateShieldBar;
                playerShip.OnArmorChanged -= UpdateArmorBar;
                playerShip.OnEnergyChanged -= UpdateEnergyBar;
            }
            
            if (primaryWeapon != null)
            {
                primaryWeapon.OnHeatChanged -= UpdateHeatBar;
                primaryWeapon.OnTargetLocked -= OnTargetLocked;
                primaryWeapon.OnTargetLost -= OnTargetLost;
            }
        }
    }
} 