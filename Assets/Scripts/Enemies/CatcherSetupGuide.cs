using UnityEngine;

namespace CatchMeowIfYouCan.Enemies
{
    /// <summary>
    /// Setup guide và helper cho Catcher System
    /// Hướng dẫn setup và test catcher mechanics
    /// </summary>
    public class CatcherSetupGuide : MonoBehaviour
    {
        [Header("Setup Guide")]
        [TextArea(3, 10)]
        [SerializeField] private string setupInstructions = @"
CATCHER SYSTEM SETUP GUIDE:

1. CREATE CATCHER GAMEOBJECT:
   - Tạo Empty GameObject cho Catcher
   - Thêm CatcherController component
   - Thêm visual (Sprite, Model, etc.)
   - Set vị trí Active Position (nơi catcher xuất hiện)

2. CONFIGURE CATCHER SETTINGS:
   - Trigger Boundary: Left/Right/Top/Bottom
   - Boundary Trigger Distance: 2f (khoảng cách từ boundary)
   - Chase Speed: 6f, Rise Speed: 8f, Retreat Speed: 4f
   - Touch Detection Radius: 1.5f
   - Escape Distance: 4f (mèo thoát khi xa hơn này)

3. SETUP CATCHER MANAGER:
   - Tạo Empty GameObject 'CatcherManager'
   - Thêm CatcherManager component
   - Enable Auto Find Catchers hoặc assign manual

4. TESTING:
   - Play game và di chuyển mèo gần boundary
   - Catcher sẽ xuất hiện và chase
   - Test escape và death scenarios
        ";
        
        [Header("Quick Setup")]
        [SerializeField] private GameObject catcherPrefab;
        [SerializeField] private bool createCatcherManager = true;
        [SerializeField] private CatcherController.BoundaryDirection defaultBoundary = CatcherController.BoundaryDirection.Left;
        
        [Header("Visual Debug")]
        [SerializeField] private bool showBoundaryLines = true;
        [SerializeField] private Color boundaryColor = Color.cyan;
        
        [ContextMenu("Quick Setup Catcher System")]
        public void QuickSetupCatcherSystem()
        {
            Debug.Log("=== QUICK SETUP CATCHER SYSTEM ===");
            
            // 1. Create CatcherManager if needed
            if (createCatcherManager)
            {
                CreateCatcherManager();
            }
            
            // 2. Create sample catcher if prefab is assigned
            if (catcherPrefab != null)
            {
                CreateSampleCatcher();
            }
            else
            {
                Debug.LogWarning("No catcher prefab assigned! Create one manually or assign prefab.");
            }
            
            Debug.Log("Catcher system setup complete! Check Console for next steps.");
            LogNextSteps();
        }
        
        private void CreateCatcherManager()
        {
            CatcherManager existingManager = FindFirstObjectByType<CatcherManager>();
            if (existingManager != null)
            {
                Debug.Log("CatcherManager already exists: " + existingManager.name);
                return;
            }
            
            GameObject managerObj = new GameObject("CatcherManager");
            CatcherManager manager = managerObj.AddComponent<CatcherManager>();
            
            Debug.Log("✓ Created CatcherManager: " + managerObj.name);
        }
        
        private void CreateSampleCatcher()
        {
            GameObject catcherObj = Instantiate(catcherPrefab);
            catcherObj.name = $"Catcher_{defaultBoundary}";
            
            CatcherController catcher = catcherObj.GetComponent<CatcherController>();
            if (catcher == null)
            {
                catcher = catcherObj.AddComponent<CatcherController>();
            }
            
            // Configure for the specified boundary
            ConfigureCatcherForBoundary(catcher, defaultBoundary);
            
            Debug.Log($"✓ Created sample catcher: {catcherObj.name} for {defaultBoundary} boundary");
        }
        
        private void ConfigureCatcherForBoundary(CatcherController catcher, CatcherController.BoundaryDirection boundary)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            
            if (cam != null)
            {
                Vector3 cameraPos = cam.transform.position;
                float cameraHeight = 2f * cam.orthographicSize;
                float cameraWidth = cameraHeight * cam.aspect;
                
                Vector3 activePosition = Vector3.zero;
                
                switch (boundary)
                {
                    case CatcherController.BoundaryDirection.Left:
                        activePosition = new Vector3(cameraPos.x - (cameraWidth * 0.3f), cameraPos.y, 0);
                        break;
                    case CatcherController.BoundaryDirection.Right:
                        activePosition = new Vector3(cameraPos.x + (cameraWidth * 0.3f), cameraPos.y, 0);
                        break;
                    case CatcherController.BoundaryDirection.Bottom:
                        activePosition = new Vector3(cameraPos.x, cameraPos.y - (cameraHeight * 0.3f), 0);
                        break;
                    case CatcherController.BoundaryDirection.Top:
                        activePosition = new Vector3(cameraPos.x, cameraPos.y + (cameraHeight * 0.3f), 0);
                        break;
                }
                
                // Set active position (this would need reflection or public fields)
                catcher.transform.position = activePosition;
                Debug.Log($"Positioned catcher at: {activePosition} for {boundary} boundary");
            }
        }
        
        private void LogNextSteps()
        {
            Debug.Log("=== NEXT STEPS ===");
            Debug.Log("1. Assign visual sprite/model to your catcher GameObject");
            Debug.Log("2. Adjust CatcherController settings in Inspector:");
            Debug.Log("   - Trigger Boundary Direction");
            Debug.Log("   - Boundary Trigger Distance");
            Debug.Log("   - Movement speeds (Rise, Chase, Retreat)");
            Debug.Log("   - Touch Detection Radius & Escape Distance");
            Debug.Log("3. Test by playing game and moving cat near boundaries");
            Debug.Log("4. Use context menu Debug options to test manually");
            Debug.Log("5. Fine-tune settings based on gameplay feel");
        }
        
        [ContextMenu("Test Boundary Detection")]
        public void TestBoundaryDetection()
        {
            Debug.Log("=== TESTING BOUNDARY DETECTION ===");
            
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            
            if (cam == null)
            {
                Debug.LogError("No camera found for boundary testing!");
                return;
            }
            
            Vector3 cameraPos = cam.transform.position;
            float cameraHeight = 2f * cam.orthographicSize;
            float cameraWidth = cameraHeight * cam.aspect;
            
            Debug.Log($"Camera Position: {cameraPos}");
            Debug.Log($"Camera Bounds: Width={cameraWidth:F2}, Height={cameraHeight:F2}");
            
            // Calculate all boundaries
            float leftBoundary = cameraPos.x - (cameraWidth * 0.5f);
            float rightBoundary = cameraPos.x + (cameraWidth * 0.5f);
            float topBoundary = cameraPos.y + (cameraHeight * 0.5f);
            float bottomBoundary = cameraPos.y - (cameraHeight * 0.5f);
            
            Debug.Log($"Boundaries: Left={leftBoundary:F2}, Right={rightBoundary:F2}, Top={topBoundary:F2}, Bottom={bottomBoundary:F2}");
            
            // Find cat position
            GameObject cat = GameObject.FindWithTag("Player");
            if (cat != null)
            {
                Vector3 catPos = cat.transform.position;
                Debug.Log($"Cat Position: {catPos}");
                
                // Check distances to each boundary
                float distToLeft = catPos.x - leftBoundary;
                float distToRight = rightBoundary - catPos.x;
                float distToTop = topBoundary - catPos.y;
                float distToBottom = catPos.y - bottomBoundary;
                
                Debug.Log($"Cat distances - Left: {distToLeft:F2}, Right: {distToRight:F2}, Top: {distToTop:F2}, Bottom: {distToBottom:F2}");
                
                // Check trigger conditions
                float triggerDistance = 2f;
                bool nearLeft = distToLeft <= triggerDistance;
                bool nearRight = distToRight <= triggerDistance;
                bool nearTop = distToTop <= triggerDistance;
                bool nearBottom = distToBottom <= triggerDistance;
                
                Debug.Log($"Near boundaries (trigger={triggerDistance}) - Left: {nearLeft}, Right: {nearRight}, Top: {nearTop}, Bottom: {nearBottom}");
            }
            else
            {
                Debug.LogWarning("No cat found! Make sure Player GameObject has 'Player' tag.");
            }
        }
        
        [ContextMenu("Validate Catcher Setup")]
        public void ValidateCatcherSetup()
        {
            Debug.Log("=== VALIDATING CATCHER SETUP ===");
            
            // Check for CatcherManager
            CatcherManager manager = FindFirstObjectByType<CatcherManager>();
            if (manager != null)
            {
                Debug.Log("✓ CatcherManager found: " + manager.name);
            }
            else
            {
                Debug.LogWarning("✗ No CatcherManager found! Create one with Quick Setup.");
            }
            
            // Check for Catchers
            CatcherController[] catchers = FindObjectsByType<CatcherController>(FindObjectsSortMode.None);
            Debug.Log($"Found {catchers.Length} CatcherController(s):");
            
            for (int i = 0; i < catchers.Length; i++)
            {
                CatcherController catcher = catchers[i];
                Debug.Log($"  [{i}] {catcher.name} - Boundary: {catcher.GetCurrentState()}");
                
                // Check if catcher has visual
                Renderer renderer = catcher.GetComponent<Renderer>();
                SpriteRenderer spriteRenderer = catcher.GetComponent<SpriteRenderer>();
                
                if (renderer == null && spriteRenderer == null)
                {
                    Debug.LogWarning($"    ⚠ {catcher.name} has no visual component (Renderer/SpriteRenderer)");
                }
                else
                {
                    Debug.Log($"    ✓ {catcher.name} has visual component");
                }
            }
            
            // Check for Player/Cat
            GameObject cat = GameObject.FindWithTag("Player");
            if (cat != null)
            {
                Debug.Log("✓ Player/Cat found: " + cat.name);
                
                CatchMeowIfYouCan.Player.CatController catController = cat.GetComponent<CatchMeowIfYouCan.Player.CatController>();
                if (catController != null)
                {
                    Debug.Log("✓ CatController found on player");
                }
                else
                {
                    Debug.LogWarning("✗ No CatController found on player!");
                }
            }
            else
            {
                Debug.LogError("✗ No Player found! Make sure cat GameObject has 'Player' tag.");
            }
            
            Debug.Log("Validation complete. Check warnings above.");
        }
        
        private void OnDrawGizmos()
        {
            if (!showBoundaryLines) return;
            
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            if (cam == null) return;
            
            Vector3 cameraPos = cam.transform.position;
            float cameraHeight = 2f * cam.orthographicSize;
            float cameraWidth = cameraHeight * cam.aspect;
            
            Gizmos.color = boundaryColor;
            
            // Draw camera bounds
            Vector3 topLeft = new Vector3(cameraPos.x - (cameraWidth * 0.5f), cameraPos.y + (cameraHeight * 0.5f), 0);
            Vector3 topRight = new Vector3(cameraPos.x + (cameraWidth * 0.5f), cameraPos.y + (cameraHeight * 0.5f), 0);
            Vector3 bottomLeft = new Vector3(cameraPos.x - (cameraWidth * 0.5f), cameraPos.y - (cameraHeight * 0.5f), 0);
            Vector3 bottomRight = new Vector3(cameraPos.x + (cameraWidth * 0.5f), cameraPos.y - (cameraHeight * 0.5f), 0);
            
            // Draw boundary lines
            Gizmos.DrawLine(topLeft, topRight);       // Top
            Gizmos.DrawLine(bottomLeft, bottomRight); // Bottom
            Gizmos.DrawLine(topLeft, bottomLeft);     // Left
            Gizmos.DrawLine(topRight, bottomRight);   // Right
            
            // Draw trigger zones (slightly inset)
            float triggerDistance = 2f;
            Gizmos.color = Color.yellow;
            
            // Left trigger zone
            Vector3 leftTriggerTop = new Vector3(topLeft.x + triggerDistance, topLeft.y, 0);
            Vector3 leftTriggerBottom = new Vector3(bottomLeft.x + triggerDistance, bottomLeft.y, 0);
            Gizmos.DrawLine(leftTriggerTop, leftTriggerBottom);
            
            // Right trigger zone
            Vector3 rightTriggerTop = new Vector3(topRight.x - triggerDistance, topRight.y, 0);
            Vector3 rightTriggerBottom = new Vector3(bottomRight.x - triggerDistance, bottomRight.y, 0);
            Gizmos.DrawLine(rightTriggerTop, rightTriggerBottom);
        }
    }
}