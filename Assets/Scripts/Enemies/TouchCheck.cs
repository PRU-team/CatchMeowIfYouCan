using UnityEngine;

namespace CatchMeowIfYouCan.Enemies
{
    /// <summary>
    /// TouchCheck - Detect collision với mèo cho Catcher
    /// Tương tự như GroundCheck, sử dụng collider để detect chính xác khi catcher chạm mèo
    /// </summary>
    public class TouchCheck : MonoBehaviour
    {
        [Header("Touch Detection Settings")]
        [SerializeField] private LayerMask catLayerMask = -1; // Layer của mèo
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool enableDebugGizmos = true;
        
        [Header("Touch Status")]
        [SerializeField] private bool isTouchingCat = false;
        [SerializeField] private GameObject touchedCat = null;
        [SerializeField] private int catContactCount = 0;
        
        // Events cho CatcherController subscribe
        public System.Action<GameObject> OnCatTouched;
        public System.Action<GameObject> OnCatExited;
        public System.Action<GameObject> OnCatStayTouching;
        
        // Public properties
        public bool IsTouchingCat => isTouchingCat;
        public GameObject TouchedCat => touchedCat;
        public int CatContactCount => catContactCount;
        
        // Internal
        private Collider2D touchCheckCollider;
        private CatcherController parentCatcher;
        
        private void Awake()
        {
            // Get collider component
            touchCheckCollider = GetComponent<Collider2D>();
            if (touchCheckCollider == null)
            {
                Debug.LogError($"[TouchCheck] {gameObject.name}: No Collider2D component found! Touch detection will not work.");
            }
            else
            {
                // Ensure collider is trigger
                touchCheckCollider.isTrigger = true;
                if (enableDebugLogs) Debug.Log($"[TouchCheck] {gameObject.name}: Collider2D found and set as trigger");
            }
            
            // Find parent CatcherController
            parentCatcher = GetComponentInParent<CatcherController>();
            if (parentCatcher == null)
            {
                Debug.LogWarning($"[TouchCheck] {gameObject.name}: No CatcherController found in parent! Events will not be linked.");
            }
            else
            {
                if (enableDebugLogs) Debug.Log($"[TouchCheck] {gameObject.name}: Linked to CatcherController: {parentCatcher.name}");
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check if collided object is a cat
            if (IsCat(other))
            {
                isTouchingCat = true;
                touchedCat = other.gameObject;
                catContactCount++;
                
                if (enableDebugLogs) Debug.Log($"[TouchCheck] 🎯 CAT TOUCHED! {other.name} - Contact #{catContactCount}");
                
                // Trigger events
                OnCatTouched?.Invoke(other.gameObject);
                
                // Notify parent catcher
                if (parentCatcher != null)
                {
                    parentCatcher.OnCatTouchedByTouchCheck(other.gameObject);
                }
            }
        }
        
        private void OnTriggerStay2D(Collider2D other)
        {
            // Continuous touch detection
            if (IsCat(other) && isTouchingCat)
            {
                OnCatStayTouching?.Invoke(other.gameObject);
                
                // Debug mỗi 30 frames (0.5s at 60fps)
                if (enableDebugLogs && Time.frameCount % 30 == 0)
                {
                    Vector3 distance = transform.position - other.transform.position;
                    Debug.Log($"[TouchCheck] 🤝 Cat staying in touch: {other.name}, Distance: {distance.magnitude:F2}");
                }
            }
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            // Check if the exiting object is the cat we were touching
            if (IsCat(other) && other.gameObject == touchedCat)
            {
                if (enableDebugLogs) Debug.Log($"[TouchCheck] 👋 Cat exited touch: {other.name}");
                
                isTouchingCat = false;
                touchedCat = null;
                
                // Trigger events
                OnCatExited?.Invoke(other.gameObject);
                
                // Notify parent catcher
                if (parentCatcher != null)
                {
                    parentCatcher.OnCatExitedTouchCheck(other.gameObject);
                }
            }
        }
        
        /// <summary>
        /// Check if collider belongs to a cat
        /// </summary>
        private bool IsCat(Collider2D collider)
        {
            // Check layer mask
            bool isInLayerMask = (catLayerMask.value & (1 << collider.gameObject.layer)) != 0;
            
            // Check tag (backup method)
            bool hasPlayerTag = collider.CompareTag("Player");
            
            // Check for CatController component (most reliable)
            bool hasCatController = collider.GetComponent<CatchMeowIfYouCan.Player.CatController>() != null;
            
            bool isCat = isInLayerMask || hasPlayerTag || hasCatController;
            
            if (enableDebugLogs && isCat && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[TouchCheck] Cat detected: {collider.name} - Layer: {isInLayerMask}, Tag: {hasPlayerTag}, Component: {hasCatController}");
            }
            
            return isCat;
        }
        
        /// <summary>
        /// Manual reset touch status
        /// </summary>
        public void ResetTouchStatus()
        {
            isTouchingCat = false;
            touchedCat = null;
            catContactCount = 0;
            if (enableDebugLogs) Debug.Log($"[TouchCheck] Touch status reset");
        }
        
        /// <summary>
        /// Force check for cats in trigger area
        /// </summary>
        public void ForceCheckForCats()
        {
            if (touchCheckCollider == null) return;
            
            Collider2D[] overlapping = Physics2D.OverlapBoxAll(
                touchCheckCollider.bounds.center,
                touchCheckCollider.bounds.size,
                0f,
                catLayerMask
            );
            
            Debug.Log($"[TouchCheck] Force check found {overlapping.Length} potential cats in area");
            
            foreach (var collider in overlapping)
            {
                if (IsCat(collider))
                {
                    Debug.Log($"[TouchCheck] Found cat in area: {collider.name}");
                    if (!isTouchingCat)
                    {
                        OnTriggerEnter2D(collider);
                    }
                }
            }
        }
        
        /// <summary>
        /// Debug methods
        /// </summary>
        [ContextMenu("Debug Touch Status")]
        public void DebugTouchStatus()
        {
            Debug.Log("=== TOUCH CHECK DEBUG ===");
            Debug.Log($"Is Touching Cat: {isTouchingCat}");
            Debug.Log($"Touched Cat: {(touchedCat != null ? touchedCat.name : "NULL")}");
            Debug.Log($"Contact Count: {catContactCount}");
            Debug.Log($"Cat Layer Mask: {catLayerMask}");
            Debug.Log($"Collider: {(touchCheckCollider != null ? "OK" : "NULL")}");
            Debug.Log($"Parent Catcher: {(parentCatcher != null ? parentCatcher.name : "NULL")}");
        }
        
        [ContextMenu("Force Check For Cats")]
        public void DebugForceCheck()
        {
            ForceCheckForCats();
        }
        
        [ContextMenu("Reset Touch Status")]
        public void DebugResetTouch()
        {
            ResetTouchStatus();
        }
        
        /// <summary>
        /// Gizmos for visualization
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!enableDebugGizmos) return;
            
            // Draw touch area
            Gizmos.color = isTouchingCat ? Color.green : Color.yellow;
            
            if (touchCheckCollider != null)
            {
                Gizmos.DrawWireCube(touchCheckCollider.bounds.center, touchCheckCollider.bounds.size);
                
                if (isTouchingCat)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(transform.position, 0.2f);
                }
            }
            else
            {
                // Fallback visualization
                Gizmos.DrawWireSphere(transform.position, 1f);
            }
            
            // Draw line to touched cat
            if (isTouchingCat && touchedCat != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, touchedCat.transform.position);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!enableDebugGizmos) return;
            
            // More detailed gizmos when selected
            Gizmos.color = Color.cyan;
            if (touchCheckCollider != null)
            {
                Gizmos.DrawCube(touchCheckCollider.bounds.center, touchCheckCollider.bounds.size);
            }
        }
    }
}