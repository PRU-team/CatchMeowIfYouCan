using UnityEngine;

namespace CatchMeowIfYouCan.Environment
{
    /// <summary>
    /// Quản lý background cố định cho endless runner
    /// Background sẽ đứng yên, camera không follow player
    /// </summary>
    public class FixedBackgroundManager : MonoBehaviour
    {
        [Header("Background Settings")]
        [SerializeField] private Transform backgroundParent;
        [SerializeField] private bool lockBackground = true;
        [SerializeField] private Vector3 backgroundPosition = Vector3.zero;
        
        [Header("Camera Settings")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private bool lockCamera = true;
        [SerializeField] private Vector3 fixedCameraPosition = new Vector3(0, 0, -10);
        [SerializeField] private bool keepPlayerInView = false; // TẮT để mèo có thể trôi ra ngoài màn hình để trigger catcher
        [SerializeField] private float playerBoundaryX = 30f; // Tăng giới hạn rất lớn để không cản trở catcher system
        
        [Header("Player Management")]
        [SerializeField] private Transform player;
        [SerializeField] private float resetPlayerX = 0f;
        [SerializeField] private bool usePhysicsBoundaries = true; // Sử dụng physics thay vì hard reset
        [SerializeField] private float boundaryForceMultiplier = 15f; // Tăng lực cho survival mode
        [SerializeField] private float maxBoundaryDrag = 10f; // Tăng drag tối đa cho survival
        [SerializeField] private float normalDrag = 2f; // Drag bình thường phù hợp với survival
        [SerializeField] private float leftBoundaryAssist = 0.5f; // Hỗ trợ khi trôi về trái quá xa
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        // Components
        private CatchMeowIfYouCan.Player.CatController catController;
        
        // State
        private Vector3 originalBackgroundPos;
        private Vector3 originalCameraPos;
        private bool isInitialized = false;
        
        private void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// Khởi tạo fixed background system
        /// </summary>
        private void Initialize()
        {
            // Tìm camera nếu chưa assign
            if (gameCamera == null)
            {
                gameCamera = Camera.main;
                if (gameCamera == null)
                {
                    gameCamera = FindFirstObjectByType<Camera>();
                }
            }
            
            // Tìm player nếu chưa assign
            if (player == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                    catController = playerObj.GetComponent<CatchMeowIfYouCan.Player.CatController>();
                }
            }
            
            // Tìm background parent nếu chưa assign
            if (backgroundParent == null)
            {
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.ToLower().Contains("background") || obj.name.ToLower().Contains("bg"))
                    {
                        backgroundParent = obj.transform;
                        Debug.Log("Auto-found background: " + obj.name);
                        break;
                    }
                }
            }
            
            // Lưu vị trí gốc
            if (backgroundParent != null)
            {
                originalBackgroundPos = backgroundParent.position;
            }
            
            if (gameCamera != null)
            {
                originalCameraPos = gameCamera.transform.position;
            }
            
            // Apply fixed settings
            ApplyFixedSettings();
            
            isInitialized = true;
            Debug.Log("FixedBackgroundManager initialized successfully!");
        }
        
        /// <summary>
        /// Áp dụng các settings để fix background và camera
        /// </summary>
        private void ApplyFixedSettings()
        {
            // Fix background position
            if (lockBackground && backgroundParent != null)
            {
                backgroundParent.position = backgroundPosition;
                Debug.Log("Background locked to position: " + backgroundPosition);
            }
            
            // Fix camera position  
            if (lockCamera && gameCamera != null)
            {
                gameCamera.transform.position = fixedCameraPosition;
                Debug.Log("Camera locked to position: " + fixedCameraPosition);
                
                // Tắt camera follow nếu có EndlessRunManager
                var endlessRunManager = FindFirstObjectByType<EndlessRunManager>();
                if (endlessRunManager != null)
                {
                    endlessRunManager.SetCameraFollow(false);
                    Debug.Log("Disabled camera follow in EndlessRunManager");
                }
            }
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            EnforceFixedPositions();
            
            // DISABLED: Tắt hoàn toàn boundary management để mèo trôi tự do đến catcher
            // if (keepPlayerInView)
            // {
            //     ManagePlayerBoundary();
            // }
        }
        
        /// <summary>
        /// Đảm bảo background và camera giữ nguyên vị trí cố định
        /// </summary>
        private void EnforceFixedPositions()
        {
            // Giữ background cố định
            if (lockBackground && backgroundParent != null)
            {
                if (Vector3.Distance(backgroundParent.position, backgroundPosition) > 0.1f)
                {
                    backgroundParent.position = backgroundPosition;
                }
            }
            
            // Giữ camera cố định
            if (lockCamera && gameCamera != null)
            {
                if (Vector3.Distance(gameCamera.transform.position, fixedCameraPosition) > 0.1f)
                {
                    gameCamera.transform.position = fixedCameraPosition;
                }
            }
        }
        
        /// <summary>
        /// Quản lý player trong giới hạn màn hình với physics thay vì hard reset
        /// DISABLED - để mèo có thể trôi ra ngoài màn hình để trigger catcher system
        /// </summary>
        private void ManagePlayerBoundary()
        {
            if (player == null || !keepPlayerInView) return; // TẮT boundary management để mèo trôi tự do
            
            float playerX = player.position.x;
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            
            // Kiểm tra input của player để tránh can thiệp vào control
            bool playerMovingLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            bool playerMovingRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            
            if (usePhysicsBoundaries)
            {
                // Sử dụng physics để hạn chế thay vì hard reset
                if (playerX > playerBoundaryX)
                {
                    // CHỈ áp dụng boundary force nếu player KHÔNG đang cố gắng đi về trái
                    if (!playerMovingLeft && playerRb != null)
                    {
                        float pushForce = (playerX - playerBoundaryX) * boundaryForceMultiplier;
                        Vector2 currentVel = playerRb.linearVelocity;
                        
                        // Chỉ áp dụng force về trái, không can thiệp vào Y velocity
                        float targetVelX = Mathf.Min(currentVel.x, -pushForce);
                        playerRb.linearVelocity = new Vector2(targetVelX, currentVel.y);
                        
                        // Tăng drag nhẹ để giảm tốc độ về phải
                        playerRb.linearDamping = Mathf.Lerp(playerRb.linearDamping, maxBoundaryDrag * 0.5f, Time.deltaTime * 2f);
                    }
                    
                    if (enableDebugLogs)
                        Debug.Log($"Player beyond right boundary ({playerX:F2} > {playerBoundaryX}), playerMovingLeft: {playerMovingLeft}");
                }
                else if (playerX < -playerBoundaryX)
                {
                    // Player trôi quá xa về trái - cung cấp assist force để giúp quay lại
                    if (!playerMovingRight && playerRb != null)
                    {
                        float assistForce = (-playerBoundaryX - playerX) * boundaryForceMultiplier * leftBoundaryAssist;
                        Vector2 currentVel = playerRb.linearVelocity;
                        
                        // Gentle assist force để không làm game quá dễ
                        float targetVelX = Mathf.Max(currentVel.x, assistForce);
                        playerRb.linearVelocity = new Vector2(targetVelX, currentVel.y);
                        
                        // Tăng drag để player biết đang ở vùng nguy hiểm
                        playerRb.linearDamping = Mathf.Lerp(playerRb.linearDamping, maxBoundaryDrag * 0.7f, Time.deltaTime * 2f);
                    }
                    
                    if (enableDebugLogs)
                        Debug.Log($"Player drifted too far left ({playerX:F2} < {-playerBoundaryX}), providing gentle assist");
                }
                else
                {
                    // Trong giới hạn bình thường, giảm drag về mức bình thường
                    if (playerRb != null)
                    {
                        playerRb.linearDamping = Mathf.Lerp(playerRb.linearDamping, normalDrag, Time.deltaTime * 3f);
                    }
                }
            }
            else
            {
                // Fallback: Hard reset (old behavior) - chỉ khi player không đang control
                if (playerX > playerBoundaryX && !playerMovingLeft)
                {
                    Vector3 newPos = player.position;
                    newPos.x = resetPlayerX;
                    player.position = newPos;
                    
                    if (playerRb != null)
                    {
                        playerRb.linearVelocity = new Vector2(0, playerRb.linearVelocity.y);
                    }
                    
                    Debug.Log("Player reset to X: " + resetPlayerX + " (was at X: " + playerX + ")");
                }
                
                if (playerX < -playerBoundaryX && !playerMovingRight)
                {
                    Vector3 newPos = player.position;
                    newPos.x = resetPlayerX;
                    player.position = newPos;
                    
                    if (playerRb != null)
                    {
                        playerRb.linearVelocity = new Vector2(0, playerRb.linearVelocity.y);
                    }
                    
                    Debug.Log("Player reset to X: " + resetPlayerX + " (was at X: " + playerX + ")");
                }
            }
        }
        
        /// <summary>
        /// Bật/tắt background cố định
        /// </summary>
        public void SetBackgroundLocked(bool locked)
        {
            lockBackground = locked;
            
            if (!locked && backgroundParent != null)
            {
                backgroundParent.position = originalBackgroundPos;
            }
            else
            {
                ApplyFixedSettings();
            }
        }
        
        /// <summary>
        /// Bật/tắt camera cố định
        /// </summary>
        public void SetCameraLocked(bool locked)
        {
            lockCamera = locked;
            
            if (!locked && gameCamera != null)
            {
                gameCamera.transform.position = originalCameraPos;
                
                // Bật lại camera follow
                var endlessRunManager = FindFirstObjectByType<EndlessRunManager>();
                if (endlessRunManager != null)
                {
                    endlessRunManager.SetCameraFollow(true);
                }
            }
            else
            {
                ApplyFixedSettings();
            }
        }
        
        /// <summary>
        /// Đặt vị trí cố định cho background
        /// </summary>
        public void SetBackgroundPosition(Vector3 position)
        {
            backgroundPosition = position;
            if (lockBackground && backgroundParent != null)
            {
                backgroundParent.position = position;
            }
        }
        
        /// <summary>
        /// Đặt vị trí cố định cho camera
        /// </summary>
        public void SetCameraPosition(Vector3 position)
        {
            fixedCameraPosition = position;
            if (lockCamera && gameCamera != null)
            {
                gameCamera.transform.position = position;
            }
        }
        
        /// <summary>
        /// Reset toàn bộ system về trạng thái ban đầu
        /// </summary>
        public void ResetToOriginal()
        {
            if (backgroundParent != null)
            {
                backgroundParent.position = originalBackgroundPos;
            }
            
            if (gameCamera != null)
            {
                gameCamera.transform.position = originalCameraPos;
            }
            
            if (player != null)
            {
                Vector3 playerPos = player.position;
                playerPos.x = resetPlayerX;
                player.position = playerPos;
            }
            
            Debug.Log("FixedBackgroundManager reset to original state");
        }
        
        /// <summary>
        /// Toggle giữa physics boundaries và hard reset
        /// </summary>
        [ContextMenu("Toggle Physics Boundaries")]
        public void TogglePhysicsBoundaries()
        {
            usePhysicsBoundaries = !usePhysicsBoundaries;
            Debug.Log($"Physics Boundaries: {(usePhysicsBoundaries ? "ENABLED" : "DISABLED")}");
            
            // Reset player drag về bình thường khi thay đổi mode
            if (player != null)
            {
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.linearDamping = normalDrag;
                }
            }
        }
        
        /// <summary>
        /// Debug player boundary status
        /// </summary>
        [ContextMenu("Debug Player Boundary")]
        public void DebugPlayerBoundary()
        {
            if (player == null)
            {
                Debug.LogError("No player assigned!");
                return;
            }
            
            Debug.Log("=== PLAYER BOUNDARY DEBUG ===");
            Debug.Log($"Player Position: {player.position}");
            Debug.Log($"Boundary X: ±{playerBoundaryX}");
            Debug.Log($"Reset Position X: {resetPlayerX}");
            Debug.Log($"Use Physics Boundaries: {usePhysicsBoundaries}");
            
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Debug.Log($"Player Velocity: {playerRb.linearVelocity}");
                Debug.Log($"Player Drag: {playerRb.linearDamping}");
                Debug.Log($"Normal Drag: {normalDrag}, Max Boundary Drag: {maxBoundaryDrag}");
            }
            
            float playerX = player.position.x;
            if (playerX > playerBoundaryX)
            {
                Debug.LogWarning($"Player is beyond RIGHT boundary ({playerX:F2} > {playerBoundaryX})");
            }
            else if (playerX < -playerBoundaryX)
            {
                Debug.LogWarning($"Player is beyond LEFT boundary ({playerX:F2} < {-playerBoundaryX})");
            }
            else
            {
                Debug.Log("Player is within boundaries ✓");
            }
        }
        
        /// <summary>
        /// Test physics boundary force
        /// </summary>
        [ContextMenu("Test Boundary Force")]
        public void TestBoundaryForce()
        {
            if (player == null)
            {
                Debug.LogError("No player assigned!");
                return;
            }
            
            Debug.Log("=== TESTING BOUNDARY FORCE ===");
            
            // Đặt player ở vị trí xa để test boundary force
            Vector3 testPos = player.position;
            testPos.x = playerBoundaryX + 2f; // Vượt boundary 2 units
            player.position = testPos;
            
            Debug.Log($"Moved player to test position: {testPos}");
            Debug.Log("Boundary force should be applied automatically in next Update cycle");
        }
        
        /// <summary>
        /// Test survival mode compatibility
        /// </summary>
        [ContextMenu("Test Survival Mode Compatibility")]
        public void TestSurvivalModeCompatibility()
        {
            var catController = player?.GetComponent<CatchMeowIfYouCan.Player.CatController>();
            if (catController == null)
            {
                Debug.LogError("No CatController found on player!");
                return;
            }
            
            Debug.Log("=== SURVIVAL MODE COMPATIBILITY TEST ===");
            Debug.Log($"Current boundary settings optimized for backward drift survival mode:");
            Debug.Log($"  - Boundary Force Multiplier: {boundaryForceMultiplier}");
            Debug.Log($"  - Max Boundary Drag: {maxBoundaryDrag}");
            Debug.Log($"  - Normal Drag: {normalDrag}");
            Debug.Log($"  - Left Boundary Assist: {leftBoundaryAssist}");
            Debug.Log("");
            Debug.Log("Backward Drift Survival Mechanics:");
            Debug.Log("  - Cat naturally drifts backward (leftward) constantly");
            Debug.Log("  - Player must hold D to move right and fight drift");
            Debug.Log("  - No input = backward drift force causes leftward movement");
            Debug.Log("  - Left boundary provides gentle assist to prevent falling off");
            Debug.Log("  - Right boundary prevents going too far ahead");
            Debug.Log("  - High friction when not pressing D creates tension");
        }
        
        /// <summary>
        /// Hiển thị debug info
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            
            // Vẽ giới hạn player boundary
            Gizmos.color = Color.yellow;
            Vector3 leftBoundary = new Vector3(-playerBoundaryX, fixedCameraPosition.y, 0);
            Vector3 rightBoundary = new Vector3(playerBoundaryX, fixedCameraPosition.y, 0);
            
            Gizmos.DrawLine(leftBoundary + Vector3.up * 5, leftBoundary + Vector3.down * 5);
            Gizmos.DrawLine(rightBoundary + Vector3.up * 5, rightBoundary + Vector3.down * 5);
            
            // Vẽ reset position
            Gizmos.color = Color.green;
            Vector3 resetPos = new Vector3(resetPlayerX, fixedCameraPosition.y, 0);
            Gizmos.DrawWireCube(resetPos, Vector3.one);
            
            // Vẽ camera position
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(fixedCameraPosition, new Vector3(2, 2, 1));
        }
    }
}