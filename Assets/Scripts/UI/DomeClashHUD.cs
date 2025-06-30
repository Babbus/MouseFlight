using UnityEngine;
using UnityEngine.UI;
using DomeClash.Core;
using DomeClash.Ships;

namespace DomeClash.UI
{
    public class DomeClashHUD : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private MouseFlightController flightController;
        [SerializeField] private PrototypeShip playerShip;
        
        [Header("HUD Elements")]
        [SerializeField] private Image shieldBar;
        [SerializeField] private Image armorBar;
        [SerializeField] private Image energyBar;
        
        [Header("Crosshair")]
        [SerializeField] private RectTransform crosshair;
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
                playerShip = FindFirstObjectByType<PrototypeShip>();
                
            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        private void Update()
        {
            UpdateCrosshair();
            UpdateRadar();
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
                PrototypeShip ship = col.GetComponent<PrototypeShip>();
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

        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }

        public void UpdateMatchTime(float time)
        {
            if (matchTimeText != null)
            {
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                matchTimeText.text = $"{minutes:00}:{seconds:00}";
            }
        }
    }
} 