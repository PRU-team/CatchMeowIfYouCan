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
        [SerializeField] private bool keepPlayerInView = true;
        [SerializeField] private float playerBoundaryX = 8f;
        
        [Header("Player Management")]
        [SerializeField] private Transform player;
        [SerializeField] private float resetPlayerX = 0f;
        
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
            
            if (keepPlayerInView)
            {
                ManagePlayerBoundary();
            }
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
        /// Quản lý player trong giới hạn màn hình
        /// </summary>
        private void ManagePlayerBoundary()
        {
            if (player == null) return;
            
            float playerX = player.position.x;
            
            // Nếu player đi quá xa về phía phải, reset về vị trí trung tâm
            if (playerX > playerBoundaryX)
            {
                Vector3 newPos = player.position;
                newPos.x = resetPlayerX;
                player.position = newPos;
                
                Debug.Log("Player reset to X: " + resetPlayerX + " (was at X: " + playerX + ")");
            }
            
            // Nếu player đi quá xa về phía trái, cũng reset
            if (playerX < -playerBoundaryX)
            {
                Vector3 newPos = player.position;
                newPos.x = resetPlayerX;
                player.position = newPos;
                
                Debug.Log("Player reset to X: " + resetPlayerX + " (was at X: " + playerX + ")");
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