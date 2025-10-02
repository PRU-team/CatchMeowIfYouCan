using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace CatchMeowIfYouCan.Environment
{
    /// <summary>
    /// Side-Scrolling Building Spawner - Spawn buildings ở ngoài camera và gắn vào ground
    /// Buildings sẽ di chuyển cùng với ground thông qua GroundMover
    /// </summary>
    public class BuildingSpawner : MonoBehaviour
    {
        [Header("Building Assets")]
        [SerializeField] private GameObject[] buildingPrefabs;
        [SerializeField] private Transform buildingParent;
        
        [Header("Spawn Settings")]
        [SerializeField] private float spawnChance = 0.7f;
        [SerializeField] private bool enableSpawning = true;
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private int maxBuildingsPerGround = 3;
        [SerializeField] private float minBuildingSpacing = 2f;
        [SerializeField] private float maxBuildingSpacing = 5f;
        
        [Header("Spawn Position")]
        [SerializeField] private float spawnDistanceFromCamera = 20f; // Khoảng cách spawn từ camera
        [SerializeField] private float despawnDistanceFromCamera = 25f; // Khoảng cách xóa buildings
        [SerializeField] private LayerMask groundLayerMask = 1;
        [SerializeField] private string groundTag = "Ground";
        
        [Header("Building Visual Settings")]
        [SerializeField] private bool randomScale = true;
        [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.5f);
        [SerializeField] private bool randomRotation = false;
        [SerializeField] private Vector2 rotationRange = new Vector2(-10f, 10f);
        [SerializeField] private bool snapToGroundSurface = true;
        [SerializeField] private float groundOffset = 0.1f;
        
        [Header("Building Layer")]
        [SerializeField] private int buildingLayer = 8;
        [SerializeField] private bool applyLayerToChildren = true;
        
        [Header("References")]
        [SerializeField] private Camera gameCamera;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color spawnZoneColor = Color.green;
        [SerializeField] private Color despawnZoneColor = Color.red;
        
        // Private variables
        private Dictionary<GameObject, List<GameObject>> groundBuildingsMap = new Dictionary<GameObject, List<GameObject>>();
        private Queue<GameObject> buildingPool = new Queue<GameObject>();
        private HashSet<GameObject> processedGrounds = new HashSet<GameObject>();
        private Coroutine spawnCoroutine;
        private bool isInitialized = false;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            CheckForNewGrounds();
            CleanupOldBuildings();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos || gameCamera == null) return;
            
            DrawSpawnAndDespawnZones();
        }
        
        #endregion
        
        #region Initialization
        
        private void Initialize()
        {
            if (enableDebugLogs) Debug.Log("[BuildingSpawner] Initialize Start");
            
            // Tìm camera nếu chưa có
            if (gameCamera == null)
            {
                gameCamera = Camera.main;
                if (gameCamera == null)
                {
                    gameCamera = FindFirstObjectByType<Camera>();
                }
            }
            
            if (gameCamera == null)
            {
                Debug.LogError("[BuildingSpawner] No camera found! Cannot determine spawn positions.");
                return;
            }
            
            // Tạo building parent nếu chưa có
            if (buildingParent == null)
            {
                GameObject parentObj = new GameObject("Buildings_SideScroll");
                buildingParent = parentObj.transform;
                buildingParent.SetParent(transform);
            }
            
            // Validate building prefabs
            if (buildingPrefabs == null || buildingPrefabs.Length == 0)
            {
                Debug.LogError("[BuildingSpawner] No building prefabs assigned!");
                enableSpawning = false;
                return;
            }
            
            // Tạo building pool
            CreateBuildingPool();
            
            // Bắt đầu spawning system
            if (enableSpawning)
            {
                StartSpawning();
            }
            
            isInitialized = true;
            
            if (enableDebugLogs) 
            {
                Debug.Log($"[BuildingSpawner] Initialize Complete. Camera: {gameCamera.name}, Prefabs: {buildingPrefabs.Length}");
            }
        }
        
        private void CreateBuildingPool()
        {
            int poolSize = 20;
            
            for (int i = 0; i < poolSize; i++)
            {
                GameObject randomPrefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
                GameObject building = Instantiate(randomPrefab, buildingParent);
                
                SetupBuilding(building);
                building.SetActive(false);
                
                buildingPool.Enqueue(building);
            }
            
            if (enableDebugLogs) Debug.Log($"[BuildingSpawner] Created building pool with {poolSize} buildings");
        }
        
        private void SetupBuilding(GameObject building)
        {
            // Set layer
            building.layer = buildingLayer;
            if (applyLayerToChildren)
            {
                SetLayerRecursively(building, buildingLayer);
            }
        }
        
        #endregion
        
        #region Ground Detection and Spawning
        
        public void StartSpawning()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }
            
            spawnCoroutine = StartCoroutine(GroundMonitoringLoop());
            if (enableDebugLogs) Debug.Log("[BuildingSpawner] Started ground monitoring loop");
        }
        
        public void StopSpawning()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            
            if (enableDebugLogs) Debug.Log("[BuildingSpawner] Stopped spawning");
        }
        
        private IEnumerator GroundMonitoringLoop()
        {
            while (enableSpawning)
            {
                CheckForNewGrounds();
                yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
            }
        }
        
        private void CheckForNewGrounds()
        {
            // Tìm tất cả ground objects trong spawn zone
            Vector3 cameraPos = gameCamera.transform.position;
            float spawnX = cameraPos.x + spawnDistanceFromCamera;
            
            // Tìm grounds bằng tag
            GameObject[] allGrounds = GameObject.FindGameObjectsWithTag(groundTag);
            
            foreach (GameObject ground in allGrounds)
            {
                if (ground == null || !ground.activeInHierarchy) continue;
                
                // Kiểm tra nếu ground ở trong spawn zone và chưa được xử lý
                float groundX = ground.transform.position.x;
                bool inSpawnZone = groundX >= spawnX - 5f && groundX <= spawnX + 5f;
                
                if (inSpawnZone && !processedGrounds.Contains(ground))
                {
                    // Kiểm tra spawn chance
                    if (Random.value <= spawnChance)
                    {
                        SpawnBuildingsOnGround(ground);
                    }
                    
                    processedGrounds.Add(ground);
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[BuildingSpawner] Processed ground: {ground.name} at X={groundX}");
                    }
                }
            }
            
            // Cleanup processed grounds set (remove null or destroyed grounds)
            processedGrounds.RemoveWhere(g => g == null);
        }
        
        private void SpawnBuildingsOnGround(GameObject ground)
        {
            if (enableDebugLogs) Debug.Log($"[BuildingSpawner] Spawning buildings on ground: {ground.name}");
            
            // Lấy ground bounds để tính vị trí spawn
            Bounds groundBounds = GetGroundBounds(ground);
            
            // Số lượng buildings cần spawn
            int buildingCount = Random.Range(1, maxBuildingsPerGround + 1);
            List<GameObject> spawnedBuildings = new List<GameObject>();
            
            for (int i = 0; i < buildingCount; i++)
            {
                Vector3 spawnPos = CalculateBuildingPosition(ground, groundBounds, i, buildingCount);
                GameObject building = SpawnSingleBuilding(spawnPos, ground);
                
                if (building != null)
                {
                    spawnedBuildings.Add(building);
                    
                    if (enableDebugLogs) 
                    {
                        Debug.Log($"[BuildingSpawner] Spawned building {i+1}/{buildingCount}: {building.name} at {spawnPos}");
                    }
                }
            }
            
            // Lưu mapping giữa ground và buildings
            if (spawnedBuildings.Count > 0)
            {
                groundBuildingsMap[ground] = spawnedBuildings;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[BuildingSpawner] Added {spawnedBuildings.Count} buildings to ground {ground.name}");
                }
            }
        }
        
        private GameObject SpawnSingleBuilding(Vector3 position, GameObject ground)
        {
            GameObject building = GetPooledBuilding();
            if (building == null) return null;
            
            // Set initial position
            building.transform.position = position;
            
            // ===== CRITICAL: GẮN BUILDING VÀO GROUND =====
            // Điều này đảm bảo building sẽ di chuyển cùng với ground
            building.transform.SetParent(ground.transform, true);
            
            // Snap to ground surface nếu enabled
            if (snapToGroundSurface)
            {
                SnapBuildingToGroundSurface(building, ground);
            }
            
            // Apply random modifications
            ApplyRandomModifications(building);
            
            // Activate building
            building.SetActive(true);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[BuildingSpawner] ✓ Building {building.name} attached to ground {ground.name}");
            }
            
            return building;
        }
        
        private Vector3 CalculateBuildingPosition(GameObject ground, Bounds groundBounds, int buildingIndex, int totalBuildings)
        {
            // Tính vị trí trên ground surface
            float groundSurfaceY = groundBounds.max.y;
            
            // Tính vị trí X dọc theo ground
            float xOffset = 0f;
            if (totalBuildings > 1)
            {
                float spacing = Random.Range(minBuildingSpacing, maxBuildingSpacing);
                xOffset = (buildingIndex - (totalBuildings - 1) * 0.5f) * spacing;
            }
            else
            {
                // Random position cho single building
                float maxOffset = groundBounds.size.x * 0.3f;
                xOffset = Random.Range(-maxOffset, maxOffset);
            }
            
            // Local position relative to ground
            Vector3 localPosition = new Vector3(xOffset, groundOffset, 0f);
            
            // Convert to world position
            Vector3 worldPosition = ground.transform.TransformPoint(localPosition);
            worldPosition.y = groundSurfaceY + groundOffset;
            
            return worldPosition;
        }
        
        private void SnapBuildingToGroundSurface(GameObject building, GameObject ground)
        {
            Bounds groundBounds = GetGroundBounds(ground);
            Bounds buildingBounds = GetBuildingBounds(building);
            
            float groundTop = groundBounds.max.y;
            float buildingHeight = buildingBounds.size.y;
            
            // Position building so its bottom sits on ground surface
            Vector3 currentPos = building.transform.position;
            float newY = groundTop + (buildingHeight * 0.5f) + groundOffset;
            
            building.transform.position = new Vector3(currentPos.x, newY, currentPos.z);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[BuildingSpawner] Snapped {building.name} to ground surface at Y={newY}");
            }
        }
        
        #endregion
        
        #region Cleanup System
        
        private void CleanupOldBuildings()
        {
            Vector3 cameraPos = gameCamera.transform.position;
            float despawnX = cameraPos.x - despawnDistanceFromCamera;
            
            List<GameObject> groundsToRemove = new List<GameObject>();
            
            foreach (var kvp in groundBuildingsMap)
            {
                GameObject ground = kvp.Key;
                List<GameObject> buildings = kvp.Value;
                
                // Kiểm tra nếu ground đã bị destroy hoặc đi quá xa
                if (ground == null || ground.transform.position.x < despawnX)
                {
                    // Return buildings to pool
                    foreach (GameObject building in buildings)
                    {
                        if (building != null)
                        {
                            ReturnBuildingToPool(building);
                        }
                    }
                    
                    groundsToRemove.Add(ground);
                    
                    if (enableDebugLogs)
                    {
                        string groundName = ground != null ? ground.name : "NULL";
                        Debug.Log($"[BuildingSpawner] Cleaned up buildings from ground: {groundName}");
                    }
                }
            }
            
            // Remove grounds from map
            foreach (GameObject ground in groundsToRemove)
            {
                groundBuildingsMap.Remove(ground);
                processedGrounds.Remove(ground);
            }
        }
        
        #endregion
        
        #region Pool Management
        
        private GameObject GetPooledBuilding()
        {
            if (buildingPool.Count > 0)
            {
                return buildingPool.Dequeue();
            }
            
            // Nếu pool empty, tạo building mới
            if (buildingPrefabs.Length > 0)
            {
                GameObject randomPrefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
                GameObject newBuilding = Instantiate(randomPrefab, buildingParent);
                SetupBuilding(newBuilding);
                
                if (enableDebugLogs) Debug.Log("[BuildingSpawner] Created new building (pool empty)");
                return newBuilding;
            }
            
            return null;
        }
        
        public void ReturnBuildingToPool(GameObject building)
        {
            if (building == null) return;
            
            // Detach from ground
            building.transform.SetParent(buildingParent);
            building.SetActive(false);
            buildingPool.Enqueue(building);
        }
        
        #endregion
        
        #region Utility Methods
        
        private void ApplyRandomModifications(GameObject building)
        {
            if (randomScale)
            {
                float scale = Random.Range(scaleRange.x, scaleRange.y);
                building.transform.localScale = Vector3.one * scale;
            }
            
            if (randomRotation)
            {
                float rotY = Random.Range(rotationRange.x, rotationRange.y);
                building.transform.rotation = Quaternion.Euler(0, rotY, 0);
            }
        }
        
        private Bounds GetGroundBounds(GameObject ground)
        {
            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds;
            }
            
            Collider collider = ground.GetComponent<Collider>();
            if (collider != null)
            {
                return collider.bounds;
            }
            
            Collider2D collider2D = ground.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                return collider2D.bounds;
            }
            
            // Default ground size
            return new Bounds(ground.transform.position, new Vector3(10f, 1f, 1f));
        }
        
        private Bounds GetBuildingBounds(GameObject building)
        {
            Renderer renderer = building.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds;
            }
            
            Collider collider = building.GetComponent<Collider>();
            if (collider != null)
            {
                return collider.bounds;
            }
            
            Collider2D collider2D = building.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                return collider2D.bounds;
            }
            
            // Default building size
            return new Bounds(building.transform.position, new Vector3(2f, 4f, 2f));
        }
        
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
        
        #endregion
        
        #region Debug and Gizmos
        
        private void DrawSpawnAndDespawnZones()
        {
            if (gameCamera == null) return;
            
            Vector3 cameraPos = gameCamera.transform.position;
            float cameraHeight = gameCamera.orthographicSize * 2f;
            
            // Spawn zone (bên phải camera)
            Gizmos.color = spawnZoneColor;
            Vector3 spawnPos = new Vector3(cameraPos.x + spawnDistanceFromCamera, cameraPos.y, cameraPos.z);
            Gizmos.DrawLine(spawnPos + Vector3.up * cameraHeight * 0.5f, spawnPos - Vector3.up * cameraHeight * 0.5f);
            Gizmos.DrawWireCube(spawnPos, new Vector3(2f, cameraHeight, 1f));
            
            // Despawn zone (bên trái camera)
            Gizmos.color = despawnZoneColor;
            Vector3 despawnPos = new Vector3(cameraPos.x - despawnDistanceFromCamera, cameraPos.y, cameraPos.z);
            Gizmos.DrawLine(despawnPos + Vector3.up * cameraHeight * 0.5f, despawnPos - Vector3.up * cameraHeight * 0.5f);
            Gizmos.DrawWireCube(despawnPos, new Vector3(2f, cameraHeight, 1f));
            
            // Draw buildings on grounds
            Gizmos.color = Color.cyan;
            foreach (var kvp in groundBuildingsMap)
            {
                if (kvp.Key != null)
                {
                    Vector3 groundPos = kvp.Key.transform.position;
                    Gizmos.DrawWireSphere(groundPos, 1f);
                    
                    foreach (GameObject building in kvp.Value)
                    {
                        if (building != null)
                        {
                            Gizmos.DrawWireCube(building.transform.position, GetBuildingBounds(building).size * 0.5f);
                        }
                    }
                }
            }
        }
        
        [ContextMenu("Force Check for New Grounds")]
        public void ForceCheckNewGrounds()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[BuildingSpawner] Not initialized yet!");
                return;
            }
            
            CheckForNewGrounds();
            Debug.Log("[BuildingSpawner] Force checked for new grounds");
        }
        
        [ContextMenu("Clear All Buildings")]
        public void ClearAllBuildings()
        {
            foreach (var kvp in groundBuildingsMap)
            {
                foreach (GameObject building in kvp.Value)
                {
                    if (building != null)
                    {
                        ReturnBuildingToPool(building);
                    }
                }
            }
            
            groundBuildingsMap.Clear();
            processedGrounds.Clear();
            Debug.Log("[BuildingSpawner] Cleared all buildings and reset processed grounds");
        }
        
        [ContextMenu("Debug Spawner State")]
        public void DebugSpawnerState()
        {
            Debug.Log("=== BUILDING SPAWNER STATE ===");
            Debug.Log($"Initialized: {isInitialized}");
            Debug.Log($"Enable Spawning: {enableSpawning}");
            Debug.Log($"Grounds with Buildings: {groundBuildingsMap.Count}");
            Debug.Log($"Processed Grounds: {processedGrounds.Count}");
            Debug.Log($"Pool Size: {buildingPool.Count}");
            Debug.Log($"Camera: {(gameCamera != null ? gameCamera.name : "NULL")}");
            Debug.Log($"Building Prefabs: {(buildingPrefabs != null ? buildingPrefabs.Length : 0)}");
            
            int totalBuildings = 0;
            foreach (var kvp in groundBuildingsMap)
            {
                totalBuildings += kvp.Value.Count;
                Debug.Log($"  Ground {kvp.Key.name}: {kvp.Value.Count} buildings");
            }
            Debug.Log($"Total Active Buildings: {totalBuildings}");
        }
        
        [ContextMenu("Debug Ground Detection")]
        public void DebugGroundDetection()
        {
            GameObject[] allGrounds = GameObject.FindGameObjectsWithTag(groundTag);
            Debug.Log($"=== GROUND DETECTION DEBUG ===");
            Debug.Log($"Found {allGrounds.Length} grounds with tag '{groundTag}':");
            
            Vector3 cameraPos = gameCamera.transform.position;
            float spawnX = cameraPos.x + spawnDistanceFromCamera;
            
            foreach (GameObject ground in allGrounds)
            {
                if (ground != null)
                {
                    float groundX = ground.transform.position.x;
                    bool inSpawnZone = groundX >= spawnX - 5f && groundX <= spawnX + 5f;
                    bool processed = processedGrounds.Contains(ground);
                    
                    Debug.Log($"  - {ground.name}: X={groundX:F1}, InSpawnZone={inSpawnZone}, Processed={processed}");
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        public void SetSpawnChance(float chance)
        {
            spawnChance = Mathf.Clamp01(chance);
        }
        
        public void SetSpawnInterval(float interval)
        {
            spawnInterval = Mathf.Max(0.1f, interval);
        }
        
        public int GetActiveBuildingCount()
        {
            int count = 0;
            foreach (var kvp in groundBuildingsMap)
            {
                count += kvp.Value.Count;
            }
            return count;
        }
        
        public int GetProcessedGroundCount()
        {
            return processedGrounds.Count;
        }
        
        public int GetPoolSize()
        {
            return buildingPool.Count;
        }
        
        #endregion
    }
}