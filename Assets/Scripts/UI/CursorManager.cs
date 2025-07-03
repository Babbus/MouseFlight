using UnityEngine;

namespace DomeClash.UI
{
    /// <summary>
    /// Manages custom cursor functionality for the game
    /// </summary>
    public class CursorManager : MonoBehaviour
    {
        [Header("Cursor Settings")]
        [SerializeField] private Texture2D customCursor;
        [SerializeField] private Vector2 hotspot = Vector2.zero;
        [SerializeField] private CursorMode cursorMode = CursorMode.Auto;
        
        [Header("Cursor Visibility")]
        [SerializeField] private bool hideCursorInGame = false;
        [SerializeField] private bool lockCursorInGame = false;
        
        [Header("Advanced Settings")]
        [SerializeField] private bool enableCursorStates = false;
        
        [System.Serializable]
        public class CursorState
        {
            public string name;
            public Texture2D cursor;
            public Vector2 hotspot;
        }
        
        [SerializeField] private CursorState[] cursorStates;
        [SerializeField] private CursorState defaultCursor;

        private void Start()
        {
            InitializeCursor();
        }

        private void InitializeCursor()
        {
            // Set custom cursor if provided
            if (customCursor != null)
            {
                Cursor.SetCursor(customCursor, hotspot, cursorMode);
                Debug.Log($"Custom cursor set: {customCursor.name}");
            }
            
            // Handle cursor visibility and locking
            if (hideCursorInGame)
            {
                Cursor.visible = false;
                Debug.Log("Cursor hidden in game");
            }
            
            if (lockCursorInGame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Debug.Log("Cursor locked in game");
            }
        }

        /// <summary>
        /// Set a specific cursor state by name
        /// </summary>
        public void SetCursor(string cursorName)
        {
            if (!enableCursorStates || cursorStates == null) return;
            
            CursorState state = System.Array.Find(cursorStates, s => s.name == cursorName);
            if (state != null && state.cursor != null)
            {
                Cursor.SetCursor(state.cursor, state.hotspot, cursorMode);
                Debug.Log($"Cursor changed to: {cursorName}");
            }
            else
            {
                Debug.LogWarning($"Cursor state '{cursorName}' not found!");
            }
        }

        /// <summary>
        /// Reset to default cursor
        /// </summary>
        public void ResetToDefault()
        {
            if (defaultCursor != null && defaultCursor.cursor != null)
            {
                Cursor.SetCursor(defaultCursor.cursor, defaultCursor.hotspot, cursorMode);
                Debug.Log("Cursor reset to default");
            }
            else if (customCursor != null)
            {
                Cursor.SetCursor(customCursor, hotspot, cursorMode);
                Debug.Log("Cursor reset to custom cursor");
            }
        }

        /// <summary>
        /// Show/hide cursor
        /// </summary>
        public void SetCursorVisible(bool visible)
        {
            Cursor.visible = visible;
            Debug.Log($"Cursor visibility set to: {visible}");
        }

        /// <summary>
        /// Lock/unlock cursor
        /// </summary>
        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Debug.Log($"Cursor lock set to: {locked}");
        }

        /// <summary>
        /// Set cursor to system default
        /// </summary>
        public void SetSystemCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Debug.Log("Cursor set to system default");
        }

        // Public getters for external access
        public bool IsCursorVisible => Cursor.visible;
        public bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;
        public Texture2D CurrentCursor => customCursor;
    }
} 