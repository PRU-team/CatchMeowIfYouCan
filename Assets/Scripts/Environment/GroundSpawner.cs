using UnityEngine;
using System.Collections.Generic;

namespace CatchMeowIfYouCan.Environment
{
    /// <summary>
    /// Tự động sinh và di chuyển ground để tạo cảm giác endless running
    /// </summary>
    public class GroundSpawner : MonoBehaviour
    {
        [Header("Ground Settings")]
        [SerializeField] private GameObject groundPrefab; // Ground asset của bạn
        [SerializeField] private Transform player; // Reference đến mèo
        [SerializeField] private int maxGroundPieces = 5; // Số lượng ground tối đa
        [SerializeField] private float groundWidth = 10f; // Độ rộng của mỗi piece ground
        
        [Header("Movement Settings")]
        [SerializeField] private float groundSpeed = 5f; // Tốc độ di chuyển ground
        [SerializeField] private bool autoSpeed = true; // Tự động điều chỉnh tốc độ
        [SerializeField] private float speedMultiplier = 1.2f; // Nhân tốc độ theo thời gian
        
        [Header("Spawn Settings")]
        [SerializeField] private float spawnDistance = 30f; // Tăng khoảng cách spawn ground mới
        [SerializeField] private float destroyDistance = -20f; // Khoảng cách hủy ground cũ
        [SerializeField] private Vector3 groundSpawnOffset = Vector3.zero; // Offset khi spawn
        
        // Ground management
        private List<GameObject> activeGrounds = new List<GameObject>();
        private Queue<GameObject> groundPool = new Queue<GameObject>();
        private float nextSpawnX;
        private float currentSpeed;
        
        // Fixed background support
        private float lastSpawnTime = 0f;
        private float spawnInterval = 1f; // Spawn ground mỗi 1 giây trong fixed background mode
        
        // Events
        public System.Action<float> OnSpeedChanged;
        
        private void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// Khởi tạo hệ thống ground spawning
        /// </summary>
        private void Initialize()
        {
            currentSpeed = groundSpeed;
            
            // Tìm player nếu chưa assign
            if (player == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
            }
            
            // Khởi tạo vị trí spawn đầu tiên
            nextSpawnX = player != null ? player.position.x : 0f;
            
            // Tạo ground pool
            CreateGroundPool();
            
            // Spawn ground ban đầu
            SpawnInitialGrounds();
            
            Debug.Log($"GroundSpawner initialized with {maxGroundPieces} ground pieces");
        }
        
        /// <summary>
        /// Tạo pool các ground objects để tái sử dụng
        /// </summary>
        private void CreateGroundPool()
        {
            for (int i = 0; i < maxGroundPieces + 2; i++) // +2 để có dự phòng
            {
                GameObject ground = CreateGroundPiece();
                ground.SetActive(false);
                groundPool.Enqueue(ground);
            }
        }
        
        /// <summary>
        /// Tạo một piece ground
        /// </summary>
        private GameObject CreateGroundPiece()
        {
            if (groundPrefab == null)
            {
                Debug.LogError("Ground prefab is not assigned!");
                return null;
            }
            
            GameObject ground = Instantiate(groundPrefab, transform);
            
            // Thêm GroundMover component
            GroundMover mover = ground.GetComponent<GroundMover>();
            if (mover == null)
            {
                mover = ground.AddComponent<GroundMover>();
            }
            mover.Initialize(this);
            
            return ground;
        }
        
        /// <summary>
        /// Spawn ground ban đầu
        /// </summary>
        private void SpawnInitialGrounds()
        {
            for (int i = 0; i < maxGroundPieces; i++)
            {
                SpawnGroundPiece();
            }
        }
        
        /// <summary>
        /// Spawn một piece ground mới
        /// </summary>
        private void SpawnGroundPiece()
        {
            GameObject ground = GetPooledGround();
            if (ground == null) return;
            
            // Kiểm tra xem có FixedBackgroundManager không để điều chỉnh spawn position
            var fixedBgManager = FindFirstObjectByType<FixedBackgroundManager>();
            bool hasFixedBackground = fixedBgManager != null;
            
            Vector3 spawnPosition;
            
            if (hasFixedBackground)
            {
                // Trong fixed background mode, spawn dựa trên ground piece xa nhất
                float rightmostX = GetRightmostGroundPosition();
                if (rightmostX == float.MinValue)
                {
                    // Không có ground nào, spawn ở vị trí mặc định
                    spawnPosition = new Vector3(0f, 0f, 0f) + groundSpawnOffset;
                }
                else
                {
                    // Spawn tiếp theo ground piece xa nhất
                    spawnPosition = new Vector3(rightmostX + groundWidth, 0f, 0f) + groundSpawnOffset;
                }
            }
            else
            {
                // Logic cũ cho moving camera
                spawnPosition = new Vector3(nextSpawnX, 0f, 0f) + groundSpawnOffset;
                nextSpawnX += groundWidth;
            }
            
            ground.transform.position = spawnPosition;
            ground.SetActive(true);
            
            // Thêm vào danh sách active
            activeGrounds.Add(ground);
            
            Debug.Log($"Spawned ground at X: {spawnPosition.x} (Fixed BG: {hasFixedBackground})");
        }
        
        /// <summary>
        /// Tìm vị trí X của ground piece nằm xa nhất về phía phải
        /// </summary>
        private float GetRightmostGroundPosition()
        {
            float rightmostX = float.MinValue;
            foreach (GameObject ground in activeGrounds)
            {
                if (ground != null && ground.activeInHierarchy)
                {
                    float groundX = ground.transform.position.x;
                    if (groundX > rightmostX)
                    {
                        rightmostX = groundX;
                    }
                }
            }
            return rightmostX;
        }
        
        /// <summary>
        /// Lấy ground từ pool
        /// </summary>
        private GameObject GetPooledGround()
        {
            if (groundPool.Count > 0)
            {
                return groundPool.Dequeue();
            }
            
            // Nếu pool trống, tạo mới
            return CreateGroundPiece();
        }
        
        /// <summary>
        /// Trả ground về pool
        /// </summary>
        public void ReturnToPool(GameObject ground)
        {
            if (activeGrounds.Contains(ground))
            {
                activeGrounds.Remove(ground);
            }
            
            ground.SetActive(false);
            groundPool.Enqueue(ground);
        }
        
        private void Update()
        {
            if (player == null) return;
            
            UpdateSpeed();
            CheckSpawning();
            CleanupOldGrounds();
        }
        
        /// <summary>
        /// Cập nhật tốc độ ground
        /// </summary>
        private void UpdateSpeed()
        {
            if (autoSpeed)
            {
                // Tăng tốc độ theo thời gian
                currentSpeed = groundSpeed * (1f + Time.time * 0.01f * speedMultiplier);
                OnSpeedChanged?.Invoke(currentSpeed);
                
                // Điều chỉnh spawn interval dựa trên tốc độ (càng nhanh càng spawn nhiều)
                spawnInterval = Mathf.Max(0.5f, 2f - (currentSpeed / 10f));
            }
        }
        
        /// <summary>
        /// Kiểm tra và spawn ground mới khi cần
        /// </summary>
        private void CheckSpawning()
        {
            // Kiểm tra xem có FixedBackgroundManager không
            var fixedBgManager = FindFirstObjectByType<FixedBackgroundManager>();
            bool hasFixedBackground = fixedBgManager != null;
            
            if (hasFixedBackground)
            {
                // Với fixed background, spawn dựa trên ground movement
                CheckSpawningForFixedBackground();
            }
            else
            {
                // Logic cũ dựa trên player position
                CheckSpawningForMovingCamera();
            }
        }
        
        /// <summary>
        /// Spawn logic cho fixed background mode
        /// </summary>
        private void CheckSpawningForFixedBackground()
        {
            // Tìm ground piece nằm xa nhất về phía phải
            float rightmostGroundX = GetRightmostGroundPosition();
            
            // Spawn theo timer để đảm bảo liên tục có ground mới
            bool shouldSpawnByTime = Time.time - lastSpawnTime > spawnInterval;
            
            // Spawn nếu không có ground hoặc ground xa nhất không đủ xa, hoặc đã đến lúc spawn theo timer
            if (rightmostGroundX == float.MinValue || rightmostGroundX < spawnDistance || shouldSpawnByTime)
            {
                SpawnGroundPiece();
                lastSpawnTime = Time.time;
            }
            
            // Đảm bảo luôn có ít nhất 4 ground pieces trong fixed background mode
            if (activeGrounds.Count < 4)
            {
                SpawnGroundPiece();
                lastSpawnTime = Time.time;
            }
        }
        
        /// <summary>
        /// Spawn logic cho moving camera mode (logic cũ)
        /// </summary>
        private void CheckSpawningForMovingCamera()
        {
            float playerX = player.position.x;
            
            // Spawn ground mới nếu player đến gần
            while (nextSpawnX - playerX < spawnDistance)
            {
                SpawnGroundPiece();
            }
            
            // Đảm bảo luôn có ít nhất 3 ground pieces phía trước player
            if (activeGrounds.Count < 3)
            {
                SpawnGroundPiece();
            }
        }
        
        /// <summary>
        /// Dọn dẹp ground cũ đã đi qua
        /// </summary>
        private void CleanupOldGrounds()
        {
            // Kiểm tra xem có FixedBackgroundManager không
            var fixedBgManager = FindFirstObjectByType<FixedBackgroundManager>();
            bool hasFixedBackground = fixedBgManager != null;
            
            if (hasFixedBackground)
            {
                CleanupForFixedBackground();
            }
            else
            {
                CleanupForMovingCamera();
            }
        }
        
        /// <summary>
        /// Cleanup logic cho fixed background mode
        /// </summary>
        private void CleanupForFixedBackground()
        {
            // Trong fixed background, dọn dẹp ground ở phía trái màn hình
            float leftBoundary = -20f; // Ground nằm ngoài phía trái này sẽ bị cleanup
            
            for (int i = activeGrounds.Count - 1; i >= 0; i--)
            {
                if (activeGrounds[i] != null)
                {
                    float groundX = activeGrounds[i].transform.position.x;
                    
                    // Chỉ cleanup khi ground đã đi quá xa về phía trái và còn đủ ground
                    if (groundX < leftBoundary && activeGrounds.Count > maxGroundPieces)
                    {
                        Debug.Log($"Cleaning up ground at X: {groundX} (Fixed Background Mode)");
                        ReturnToPool(activeGrounds[i]);
                    }
                }
            }
        }
        
        /// <summary>
        /// Cleanup logic cho moving camera mode (logic cũ)
        /// </summary>
        private void CleanupForMovingCamera()
        {
            float playerX = player.position.x;
            
            for (int i = activeGrounds.Count - 1; i >= 0; i--)
            {
                if (activeGrounds[i] != null)
                {
                    float groundX = activeGrounds[i].transform.position.x;
                    
                    // Chỉ hủy ground khi nó đã đi quá xa phía sau player
                    // Và đảm bảo vẫn còn đủ ground phía trước
                    if (groundX < playerX + destroyDistance && activeGrounds.Count > maxGroundPieces)
                    {
                        Debug.Log($"Cleaning up ground at X: {groundX}, Player at X: {playerX}");
                        ReturnToPool(activeGrounds[i]);
                    }
                }
            }
        }
        
        /// <summary>
        /// Lấy tốc độ hiện tại
        /// </summary>
        public float GetCurrentSpeed()
        {
            return currentSpeed;
        }
        
        /// <summary>
        /// Đặt tốc độ ground
        /// </summary>
        public void SetSpeed(float speed)
        {
            groundSpeed = speed;
            currentSpeed = speed;
        }
        
        /// <summary>
        /// Reset hệ thống ground spawning
        /// </summary>
        public void ResetSpawner()
        {
            // Dọn dẹp tất cả ground active
            for (int i = activeGrounds.Count - 1; i >= 0; i--)
            {
                if (activeGrounds[i] != null)
                {
                    ReturnToPool(activeGrounds[i]);
                }
            }
            
            // Reset vị trí spawn
            nextSpawnX = player != null ? player.position.x : 0f;
            currentSpeed = groundSpeed;
            
            // Spawn lại ground ban đầu
            SpawnInitialGrounds();
        }
        
        /// <summary>
        /// Vẽ gizmos để debug
        /// </summary>
        private void OnDrawGizmos()
        {
            if (player == null) return;
            
            Vector3 playerPos = player.position;
            
            // Vẽ spawn distance
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                new Vector3(playerPos.x + spawnDistance, playerPos.y - 2f, 0),
                new Vector3(playerPos.x + spawnDistance, playerPos.y + 2f, 0)
            );
            
            // Vẽ destroy distance
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                new Vector3(playerPos.x + destroyDistance, playerPos.y - 2f, 0),
                new Vector3(playerPos.x + destroyDistance, playerPos.y + 2f, 0)
            );
            
            // Vẽ next spawn position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(nextSpawnX, 0f, 0f), new Vector3(1f, 1f, 1f));
        }
    }
}