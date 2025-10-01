using CatchMeowIfYouCan.Player;
using UnityEngine;

namespace CatchMeowIfYouCan.Environment
{
    /// <summary>
    /// Script setup hướng dẫn cách cài đặt endless running system
    /// HƯỚNG DẪN SỬ DỤNG:
    /// 
    /// 1. SETUP PLAYER:
    ///    - Gắn CatController script vào Player GameObject
    ///    - Đảm bảo Player có tag "Player"
    ///    - Player cần có Rigidbody2D và Collider2D
    ///    - Tạo empty GameObject con cho GroundCheck
    ///    - Trong CatController, bật autoRun = true và đặt autoRunSpeed = 5f
    /// 
    /// 2. SETUP GROUND:
    ///    - Tạo Ground Prefab từ ground asset hiện tại
    ///    - Đảm bảo Ground có layer phù hợp với groundLayerMask trong CatController
    ///    - Ground prefab không cần Rigidbody2D (chỉ cần Collider2D)
    /// 
    /// 3. SETUP GROUND SPAWNER:
    ///    - Tạo empty GameObject tên "GroundSpawner"
    ///    - Gắn GroundSpawner script vào object này
    ///    - Assign Ground Prefab vào groundPrefab field
    ///    - Assign Player vào player field
    ///    - Đặt groundWidth = độ rộng thực tế của ground asset
    ///    - Điều chỉnh spawnDistance và destroyDistance nếu cần
    /// 
    /// 4. SETUP ENDLESS RUN MANAGER:
    ///    - Tạo empty GameObject tên "EndlessRunManager"
    ///    - Gắn EndlessRunManager script vào object này
    ///    - Assign GroundSpawner, Player, và Main Camera
    ///    - Điều chỉnh cameraOffset để camera follow phù hợp
    /// 
    /// 5. KIỂM TRA LAYER MASK:
    ///    - Đảm bảo Ground objects có layer phù hợp
    ///    - Trong CatController, groundLayerMask phải match với layer của ground
    ///    - Có thể dùng layer "Default" (value = 1) hoặc tạo layer riêng
    /// 
    /// 6. TEST GAME:
    ///    - Player sẽ tự động chạy về phía phải
    ///    - Ground sẽ tự động spawn phía trước và cleanup phía sau
    ///    - Camera sẽ follow player
    ///    - Tốc độ sẽ tăng dần theo thời gian
    /// 
    /// TROUBLESHOOTING:
    /// - Nếu ground biến mất: Kiểm tra spawnDistance và destroyDistance
    /// - Nếu player không auto run: Kiểm tra autoRun = true trong CatController
    /// - Nếu ground không spawn: Kiểm tra ground prefab và player reference
    /// - Nếu camera không follow: Kiểm tra camera assignment và followPlayer = true
    /// 
    /// FIXED BACKGROUND SETUP (Tùy chọn):
    /// 7. SETUP FIXED BACKGROUND (Để background đứng yên):
    ///    - Tạo empty GameObject tên "FixedBackgroundManager"
    ///    - Gắn FixedBackgroundManager script vào object này
    ///    - Assign Background Parent (GameObject chứa background elements)
    ///    - Assign Player và Main Camera
    ///    - Đặt lockBackground = true và lockCamera = true
    ///    - Điều chỉnh playerBoundaryX để giới hạn player trong màn hình
    ///    - Background sẽ đứng yên, camera cố định, ground di chuyển tạo hiệu ứng
    /// </summary>
    public class EndlessRunSetupGuide : MonoBehaviour
    {
        [Header("KIỂM TRA SETUP")]
        [SerializeField] private bool checkSetup = true;
        
        [Header("References to Check")]
        [SerializeField] private GameObject player;
        [SerializeField] private GameObject groundPrefab;
        [SerializeField] private GroundSpawner groundSpawner;
        [SerializeField] private EndlessRunManager endlessRunManager;
        [SerializeField] private FixedBackgroundManager fixedBackgroundManager;
        [SerializeField] private Camera mainCamera;
        
        private void Start()
        {
            if (checkSetup)
            {
                CheckSetup();
            }
        }
        
        private void CheckSetup()
        {
            Debug.Log("=== ENDLESS RUN SETUP CHECK ===");
            
            // Check Player
            if (player == null)
            {
                player = GameObject.FindWithTag("Player");
            }
            
            if (player != null)
            {
                Debug.Log("✓ Player found: " + player.name);
                
                var catController = player.GetComponent<CatController>();
                if (catController != null)
                {
                    Debug.Log("✓ CatController found on player");
                }
                else
                {
                    Debug.LogError("✗ CatController NOT found on player!");
                }
                
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Debug.Log("✓ Rigidbody2D found on player");
                }
                else
                {
                    Debug.LogError("✗ Rigidbody2D NOT found on player!");
                }
            }
            else
            {
                Debug.LogError("✗ Player with tag 'Player' NOT found!");
            }
            
            // Check Ground Spawner
            if (groundSpawner == null)
            {
                groundSpawner = FindFirstObjectByType<GroundSpawner>();
            }
            
            if (groundSpawner != null)
            {
                Debug.Log("✓ GroundSpawner found: " + groundSpawner.name);
            }
            else
            {
                Debug.LogError("✗ GroundSpawner NOT found!");
            }
            
            // Check Endless Run Manager
            if (endlessRunManager == null)
            {
                endlessRunManager = FindFirstObjectByType<EndlessRunManager>();
            }
            
            if (endlessRunManager != null)
            {
                Debug.Log("✓ EndlessRunManager found: " + endlessRunManager.name);
            }
            else
            {
                Debug.LogWarning("⚠ EndlessRunManager not found (optional)");
            }
            
            // Check Fixed Background Manager
            if (fixedBackgroundManager == null)
            {
                fixedBackgroundManager = FindFirstObjectByType<FixedBackgroundManager>();
            }
            
            if (fixedBackgroundManager != null)
            {
                Debug.Log("✓ FixedBackgroundManager found: " + fixedBackgroundManager.name + " (Background will be stationary)");
            }
            else
            {
                Debug.LogWarning("⚠ FixedBackgroundManager not found (background will follow camera)");
            }
            
            // Check Camera
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            if (mainCamera != null)
            {
                Debug.Log("✓ Main Camera found: " + mainCamera.name);
            }
            else
            {
                Debug.LogError("✗ Main Camera NOT found!");
            }
            
            Debug.Log("=== SETUP CHECK COMPLETE ===");
        }
        
        [ContextMenu("Force Check Setup")]
        public void ForceCheckSetup()
        {
            CheckSetup();
        }
        
        [ContextMenu("Auto Setup References")]
        public void AutoSetupReferences()
        {
            player = GameObject.FindWithTag("Player");
            groundSpawner = FindFirstObjectByType<GroundSpawner>();
            endlessRunManager = FindFirstObjectByType<EndlessRunManager>();
            fixedBackgroundManager = FindFirstObjectByType<FixedBackgroundManager>();
            mainCamera = Camera.main;
            
            Debug.Log("Auto setup complete! Check the assigned references.");
        }
    }
}