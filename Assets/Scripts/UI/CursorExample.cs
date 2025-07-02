using UnityEngine;

namespace DomeClash.UI
{
    /// <summary>
    /// Example script showing how to create a simple cursor texture programmatically
    /// This is useful for testing cursor functionality without needing external image files
    /// </summary>
    public class CursorExample : MonoBehaviour
    {
        [Header("Example Cursor Settings")]
        [SerializeField] private bool createExampleCursor = false;
        [SerializeField] private Color cursorColor = Color.white;
        [SerializeField] private int cursorSize = 32;
        
        private void Start()
        {
            if (createExampleCursor)
            {
                CreateExampleCursor();
            }
        }
        
        /// <summary>
        /// Creates a simple crosshair cursor texture for testing
        /// </summary>
        private void CreateExampleCursor()
        {
            Texture2D cursorTexture = new Texture2D(cursorSize, cursorSize, TextureFormat.RGBA32, false);
            
            // Clear the texture
            Color[] pixels = new Color[cursorSize * cursorSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear; // Transparent background
            }
            
            // Draw a simple crosshair
            int center = cursorSize / 2;
            int thickness = 2;
            
            // Horizontal line
            for (int x = center - 8; x <= center + 8; x++)
            {
                for (int y = center - thickness; y <= center + thickness; y++)
                {
                    if (x >= 0 && x < cursorSize && y >= 0 && y < cursorSize)
                    {
                        pixels[y * cursorSize + x] = cursorColor;
                    }
                }
            }
            
            // Vertical line
            for (int x = center - thickness; x <= center + thickness; x++)
            {
                for (int y = center - 8; y <= center + 8; y++)
                {
                    if (x >= 0 && x < cursorSize && y >= 0 && y < cursorSize)
                    {
                        pixels[y * cursorSize + x] = cursorColor;
                    }
                }
            }
            
            // Center dot
            for (int x = center - 1; x <= center + 1; x++)
            {
                for (int y = center - 1; y <= center + 1; y++)
                {
                    if (x >= 0 && x < cursorSize && y >= 0 && y < cursorSize)
                    {
                        pixels[y * cursorSize + x] = cursorColor;
                    }
                }
            }
            
            cursorTexture.SetPixels(pixels);
            cursorTexture.Apply();
            
            // Set the cursor
            Vector2 hotspot = new Vector2(center, center); // Center of the crosshair
            Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
            
            Debug.Log($"Example cursor created and set! Size: {cursorSize}x{cursorSize}, Color: {cursorColor}");
        }
        
        /// <summary>
        /// Creates a simple arrow cursor
        /// </summary>
        public void CreateArrowCursor()
        {
            Texture2D cursorTexture = new Texture2D(cursorSize, cursorSize, TextureFormat.RGBA32, false);
            
            // Clear the texture
            Color[] pixels = new Color[cursorSize * cursorSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            
            // Draw a simple arrow shape
            int center = cursorSize / 2;
            
            // Arrow shaft
            for (int x = center - 1; x <= center + 1; x++)
            {
                for (int y = center - 8; y <= center + 4; y++)
                {
                    if (x >= 0 && x < cursorSize && y >= 0 && y < cursorSize)
                    {
                        pixels[y * cursorSize + x] = cursorColor;
                    }
                }
            }
            
            // Arrow head
            for (int x = center - 4; x <= center + 4; x++)
            {
                for (int y = center + 4; y <= center + 8; y++)
                {
                    if (x >= 0 && x < cursorSize && y >= 0 && y < cursorSize)
                    {
                        int distanceFromCenter = Mathf.Abs(x - center);
                        int maxDistance = 8 - y + center;
                        if (distanceFromCenter <= maxDistance)
                        {
                            pixels[y * cursorSize + x] = cursorColor;
                        }
                    }
                }
            }
            
            cursorTexture.SetPixels(pixels);
            cursorTexture.Apply();
            
            // Set the cursor with hotspot at the arrow tip
            Vector2 hotspot = new Vector2(center, center + 8);
            Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
            
            Debug.Log("Arrow cursor created and set!");
        }
        
        /// <summary>
        /// Reset to system cursor
        /// </summary>
        public void ResetToSystemCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Debug.Log("Cursor reset to system default");
        }
    }
} 