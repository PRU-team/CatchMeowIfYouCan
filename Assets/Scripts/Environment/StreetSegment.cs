using UnityEngine;
using System.Collections.Generic;

namespace CatchMeowIfYouCan.Environment
{
    /// <summary>
    /// Represents a single street segment with obstacles, collectibles, and decorations
    /// Handles segment initialization, obstacle spawning, and cleanup
    /// </summary>
    public class StreetSegment : MonoBehaviour
    {
        [Header("Segment Properties")]
        [SerializeField] private float segmentLength = 20f;
        [SerializeField] private SegmentDifficulty difficulty = SegmentDifficulty.Easy;
        [SerializeField] private SegmentType segmentType = SegmentType.Normal;
        
        [Header("Lane Configuration")]
        [SerializeField] private float[] lanePositions = { -2f, 0f, 2f };
        [SerializeField] private Transform[] laneTransforms;
        
        [Header("Obstacle Spawning")]
        [SerializeField] private GameObject[] obstaclePrefabs;
        [SerializeField] private float[] obstacleWeights = { 0.4f, 0.3f, 0.2f, 0.1f };
        [SerializeField] private int minObstacles = 1;
        [SerializeField] private int maxObstacles = 3;
        [SerializeField] private float obstacleSpacing = 5f;
        
        [Header("Collectible Spawning")]
        [SerializeField] private GameObject[] collectiblePrefabs;
        [SerializeField] private float collectibleChance = 0.6f;
        [SerializeField] private int maxCollectibles = 5;
        [SerializeField] private float collectibleHeight = 1f;
        
        [Header("Decorations")]
        [SerializeField] private GameObject[] decorationPrefabs;
        [SerializeField] private Transform[] decorationSpawnPoints;
        [SerializeField] private float decorationChance = 0.3f;
        
        [Header("Special Features")]
        [SerializeField] private bool canHavePowerUps = true;
        [SerializeField] private GameObject[] powerUpPrefabs;
        [SerializeField] private float powerUpChance = 0.1f;
        
        // Runtime data
        private List<GameObject> spawnedObjects = new List<GameObject>();
        private float currentDifficulty = 0f;
        private int segmentIndex = 0;
        private bool isInitialized = false;
        
        // Lane occupation tracking
        private bool[] laneOccupied;
        
        public enum SegmentDifficulty
        {
            Easy,
            Medium,
            Hard,
            Extreme
        }
        
        public enum SegmentType
        {
            Normal,
            Bonus,
            Challenge,
            Safe
        }
        
        // Properties
        public float SegmentLength => segmentLength;
        public SegmentDifficulty Difficulty => difficulty;
        public SegmentType Type => segmentType;
        public bool IsInitialized => isInitialized;
        
        private void Awake()
        {
            InitializeLaneTracking();
            SetupLaneTransforms();
        }
        
        #region Initialization
        
        private void InitializeLaneTracking()
        {
            laneOccupied = new bool[lanePositions.Length];
        }
        
        private void SetupLaneTransforms()
        {
            // Create lane transforms if not assigned
            if (laneTransforms == null || laneTransforms.Length != lanePositions.Length)
            {
                laneTransforms = new Transform[lanePositions.Length];
                
                for (int i = 0; i < lanePositions.Length; i++)
                {
                    GameObject laneObj = new GameObject($"Lane_{i}");
                    laneObj.transform.SetParent(transform);
                    laneObj.transform.localPosition = new Vector3(lanePositions[i], 0f, 0f);
                    laneTransforms[i] = laneObj.transform;
                }
            }
        }
        
        /// <summary>
        /// Initialize segment with difficulty and index
        /// </summary>
        public void InitializeSegment(float difficultyLevel, int index)
        {
            currentDifficulty = difficultyLevel;
            segmentIndex = index;
            
            ClearSegment();
            GenerateSegmentContent();
            
            isInitialized = true;
        }
        
        /// <summary>
        /// Copy properties from a template segment
        /// </summary>
        public void CopyFromTemplate(StreetSegment template)
        {
            if (template == null) return;
            
            difficulty = template.difficulty;
            segmentType = template.segmentType;
            canHavePowerUps = template.canHavePowerUps;
            
            // Copy arrays if needed
            if (template.obstaclePrefabs != null)
            {
                obstaclePrefabs = template.obstaclePrefabs;
            }
            
            if (template.collectiblePrefabs != null)
            {
                collectiblePrefabs = template.collectiblePrefabs;
            }
        }
        
        #endregion
        
        #region Content Generation
        
        private void GenerateSegmentContent()
        {
            ResetLaneOccupation();
            
            switch (segmentType)
            {
                case SegmentType.Normal:
                    GenerateNormalSegment();
                    break;
                    
                case SegmentType.Bonus:
                    GenerateBonusSegment();
                    break;
                    
                case SegmentType.Challenge:
                    GenerateChallengeSegment();
                    break;
                    
                case SegmentType.Safe:
                    GenerateSafeSegment();
                    break;
            }
            
            GenerateDecorations();
        }
        
        private void GenerateNormalSegment()
        {
            // Spawn obstacles based on difficulty
            int obstacleCount = GetObstacleCountForDifficulty();
            SpawnObstacles(obstacleCount);
            
            // Spawn collectibles
            if (Random.value < collectibleChance)
            {
                SpawnCollectibles();
            }
            
            // Spawn power-ups
            if (canHavePowerUps && Random.value < powerUpChance * (1f + currentDifficulty))
            {
                SpawnPowerUp();
            }
        }
        
        private void GenerateBonusSegment()
        {
            // Bonus segments have more collectibles, fewer obstacles
            SpawnObstacles(Mathf.Max(0, minObstacles - 1));
            
            // Guaranteed collectibles
            SpawnCollectibles();
            
            // Higher chance for power-ups
            if (canHavePowerUps && Random.value < powerUpChance * 3f)
            {
                SpawnPowerUp();
            }
        }
        
        private void GenerateChallengeSegment()
        {
            // Challenge segments have maximum obstacles
            int obstacleCount = Mathf.RoundToInt(maxObstacles * (0.8f + currentDifficulty * 0.4f));
            SpawnObstacles(obstacleCount);
            
            // Still spawn some collectibles as reward
            if (Random.value < collectibleChance * 0.5f)
            {
                SpawnCollectibles();
            }
        }
        
        private void GenerateSafeSegment()
        {
            // Safe segments have no obstacles, only collectibles
            SpawnCollectibles();
            
            if (canHavePowerUps && Random.value < powerUpChance * 2f)
            {
                SpawnPowerUp();
            }
        }
        
        #endregion
        
        #region Obstacle Spawning
        
        private int GetObstacleCountForDifficulty()
        {
            float difficultyFactor = currentDifficulty;
            
            switch (difficulty)
            {
                case SegmentDifficulty.Easy:
                    return Mathf.RoundToInt(Mathf.Lerp(minObstacles, minObstacles + 1, difficultyFactor));
                    
                case SegmentDifficulty.Medium:
                    return Mathf.RoundToInt(Mathf.Lerp(minObstacles + 1, maxObstacles - 1, difficultyFactor));
                    
                case SegmentDifficulty.Hard:
                    return Mathf.RoundToInt(Mathf.Lerp(maxObstacles - 1, maxObstacles, difficultyFactor));
                    
                case SegmentDifficulty.Extreme:
                    return maxObstacles;
                    
                default:
                    return minObstacles;
            }
        }
        
        private void SpawnObstacles(int count)
        {
            if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;
            
            for (int i = 0; i < count; i++)
            {
                // Find available lane
                int laneIndex = GetRandomAvailableLane();
                if (laneIndex == -1) break; // No available lanes
                
                // Choose obstacle prefab
                GameObject obstaclePrefab = ChooseObstaclePrefab();
                if (obstaclePrefab == null) continue;
                
                // Calculate position within segment
                float xOffset = Random.Range(2f, segmentLength - 2f);
                Vector3 spawnPosition = laneTransforms[laneIndex].position + Vector3.right * xOffset;
                
                // Spawn obstacle
                GameObject obstacle = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity, transform);
                spawnedObjects.Add(obstacle);
                
                // Mark lane as occupied
                laneOccupied[laneIndex] = true;
            }
        }
        
        private GameObject ChooseObstaclePrefab()
        {
            if (obstaclePrefabs.Length == 1)
            {
                return obstaclePrefabs[0];
            }
            
            // Weighted random selection
            float totalWeight = 0f;
            for (int i = 0; i < obstacleWeights.Length && i < obstaclePrefabs.Length; i++)
            {
                totalWeight += obstacleWeights[i];
            }
            
            float randomValue = Random.value * totalWeight;
            float currentWeight = 0f;
            
            for (int i = 0; i < obstacleWeights.Length && i < obstaclePrefabs.Length; i++)
            {
                currentWeight += obstacleWeights[i];
                if (randomValue <= currentWeight)
                {
                    return obstaclePrefabs[i];
                }
            }
            
            return obstaclePrefabs[obstaclePrefabs.Length - 1];
        }
        
        #endregion
        
        #region Collectible Spawning
        
        private void SpawnCollectibles()
        {
            if (collectiblePrefabs == null || collectiblePrefabs.Length == 0) return;
            
            int collectibleCount = Random.Range(1, maxCollectibles + 1);
            
            for (int i = 0; i < collectibleCount; i++)
            {
                // Choose random position within segment
                float xOffset = Random.Range(1f, segmentLength - 1f);
                float yOffset = collectibleHeight;
                
                // Choose random lane (collectibles can be in occupied lanes)
                int laneIndex = Random.Range(0, lanePositions.Length);
                
                Vector3 spawnPosition = laneTransforms[laneIndex].position + 
                                      new Vector3(xOffset, yOffset, 0f);
                
                // Choose collectible prefab
                GameObject collectiblePrefab = collectiblePrefabs[Random.Range(0, collectiblePrefabs.Length)];
                
                // Spawn collectible
                GameObject collectible = Instantiate(collectiblePrefab, spawnPosition, Quaternion.identity, transform);
                spawnedObjects.Add(collectible);
            }
        }
        
        private void SpawnPowerUp()
        {
            if (powerUpPrefabs == null || powerUpPrefabs.Length == 0) return;
            
            // Find an unoccupied lane for power-up
            int laneIndex = GetRandomAvailableLane();
            if (laneIndex == -1)
            {
                laneIndex = Random.Range(0, lanePositions.Length); // Force spawn if no available lanes
            }
            
            // Position in middle of segment
            float xOffset = segmentLength * 0.5f;
            Vector3 spawnPosition = laneTransforms[laneIndex].position + 
                                  new Vector3(xOffset, collectibleHeight, 0f);
            
            // Choose power-up prefab
            GameObject powerUpPrefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
            
            // Spawn power-up
            GameObject powerUp = Instantiate(powerUpPrefab, spawnPosition, Quaternion.identity, transform);
            spawnedObjects.Add(powerUp);
        }
        
        #endregion
        
        #region Decoration Spawning
        
        private void GenerateDecorations()
        {
            if (decorationPrefabs == null || decorationPrefabs.Length == 0) return;
            if (decorationSpawnPoints == null || decorationSpawnPoints.Length == 0) return;
            
            foreach (Transform spawnPoint in decorationSpawnPoints)
            {
                if (Random.value < decorationChance)
                {
                    GameObject decorationPrefab = decorationPrefabs[Random.Range(0, decorationPrefabs.Length)];
                    GameObject decoration = Instantiate(decorationPrefab, spawnPoint.position, spawnPoint.rotation, transform);
                    spawnedObjects.Add(decoration);
                }
            }
        }
        
        #endregion
        
        #region Lane Management
        
        private void ResetLaneOccupation()
        {
            for (int i = 0; i < laneOccupied.Length; i++)
            {
                laneOccupied[i] = false;
            }
        }
        
        private int GetRandomAvailableLane()
        {
            List<int> availableLanes = new List<int>();
            
            for (int i = 0; i < laneOccupied.Length; i++)
            {
                if (!laneOccupied[i])
                {
                    availableLanes.Add(i);
                }
            }
            
            if (availableLanes.Count == 0)
            {
                return -1; // No available lanes
            }
            
            return availableLanes[Random.Range(0, availableLanes.Count)];
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Clear all spawned objects in this segment
        /// </summary>
        public void ClearSegment()
        {
            foreach (GameObject obj in spawnedObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            
            spawnedObjects.Clear();
            ResetLaneOccupation();
        }
        
        /// <summary>
        /// Reset segment to initial state
        /// </summary>
        public void ResetSegment()
        {
            ClearSegment();
            isInitialized = false;
            currentDifficulty = 0f;
            segmentIndex = 0;
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get the position of a specific lane
        /// </summary>
        public Vector3 GetLanePosition(int laneIndex)
        {
            if (laneIndex >= 0 && laneIndex < laneTransforms.Length)
            {
                return laneTransforms[laneIndex].position;
            }
            
            return transform.position;
        }
        
        /// <summary>
        /// Check if a lane is occupied by obstacles
        /// </summary>
        public bool IsLaneOccupied(int laneIndex)
        {
            if (laneIndex >= 0 && laneIndex < laneOccupied.Length)
            {
                return laneOccupied[laneIndex];
            }
            
            return false;
        }
        
        /// <summary>
        /// Get segment statistics
        /// </summary>
        public SegmentStats GetSegmentStats()
        {
            return new SegmentStats
            {
                spawnedObjectCount = spawnedObjects.Count,
                difficulty = currentDifficulty,
                segmentIndex = segmentIndex,
                segmentType = segmentType,
                occupiedLanes = GetOccupiedLaneCount()
            };
        }
        
        private int GetOccupiedLaneCount()
        {
            int count = 0;
            foreach (bool occupied in laneOccupied)
            {
                if (occupied) count++;
            }
            return count;
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            // Draw segment bounds
            Gizmos.color = Color.white;
            Vector3 center = transform.position + Vector3.right * segmentLength * 0.5f;
            Vector3 size = new Vector3(segmentLength, 0.1f, 1f);
            Gizmos.DrawWireCube(center, size);
            
            // Draw lane positions
            if (laneTransforms != null)
            {
                Gizmos.color = Color.blue;
                foreach (Transform laneTransform in laneTransforms)
                {
                    if (laneTransform != null)
                    {
                        Vector3 laneStart = laneTransform.position;
                        Vector3 laneEnd = laneStart + Vector3.right * segmentLength;
                        Gizmos.DrawLine(laneStart, laneEnd);
                    }
                }
            }
            
            // Draw difficulty color
            if (isInitialized)
            {
                Gizmos.color = Color.Lerp(Color.green, Color.red, currentDifficulty);
                Gizmos.DrawWireCube(center + Vector3.up * 0.5f, Vector3.one * 0.5f);
            }
        }
        
        public string GetDebugInfo()
        {
            var stats = GetSegmentStats();
            return $"Segment #{stats.segmentIndex}\n" +
                   $"Type: {stats.segmentType}\n" +
                   $"Difficulty: {stats.difficulty:F2}\n" +
                   $"Objects: {stats.spawnedObjectCount}\n" +
                   $"Occupied Lanes: {stats.occupiedLanes}/{lanePositions.Length}";
        }
        
        #endregion
        
        [System.Serializable]
        public struct SegmentStats
        {
            public int spawnedObjectCount;
            public float difficulty;
            public int segmentIndex;
            public SegmentType segmentType;
            public int occupiedLanes;
        }
    }
}