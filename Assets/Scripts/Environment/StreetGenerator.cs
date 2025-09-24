using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace CatchMeowIfYouCan.Environment
{
    /// <summary>
    /// Generates and manages endless street segments for the runner game
    /// Handles procedural generation, recycling, and difficulty progression
    /// </summary>
    public class StreetGenerator : MonoBehaviour
    {
        [Header("Street Generation")]
        [SerializeField] private StreetSegment[] streetPrefabs;
        [SerializeField] private int initialSegmentCount = 5;
        [SerializeField] private int maxActiveSegments = 10;
        [SerializeField] private float segmentLength = 20f;
        [SerializeField] private float generationDistance = 50f;
        
        [Header("Difficulty Progression")]
        [SerializeField] private float difficultyIncreaseInterval = 30f;
        [SerializeField] private AnimationCurve difficultyProgressionCurve;
        [SerializeField] private float maxDifficulty = 1f;
        
        [Header("Segment Types")]
        [SerializeField] private SegmentType[] segmentTypes;
        [SerializeField] private float[] segmentWeights = { 0.4f, 0.3f, 0.2f, 0.1f }; // Easy, Medium, Hard, Extreme
        
        [Header("Special Segments")]
        [SerializeField] private StreetSegment[] bonusSegments;
        [SerializeField] private float bonusSegmentChance = 0.1f;
        [SerializeField] private int minSegmentsBetweenBonus = 5;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool previewGeneration = false;
        
        // Active segments tracking
        private Queue<StreetSegment> activeSegments = new Queue<StreetSegment>();
        private Queue<StreetSegment> segmentPool = new Queue<StreetSegment>();
        private Transform player;
        
        // Generation state
        private float nextSegmentPosition = 0f;
        private float currentDifficulty = 0f;
        private int segmentsSinceBonus = 0;
        private int totalSegmentsGenerated = 0;
        
        // Performance tracking
        private float lastGenerationCheck = 0f;
        private const float GENERATION_CHECK_INTERVAL = 0.5f;
        
        // Events
        public System.Action<StreetSegment> OnSegmentGenerated;
        public System.Action<StreetSegment> OnSegmentRecycled;
        public System.Action<float> OnDifficultyChanged;
        
        [System.Serializable]
        public struct SegmentType
        {
            public string name;
            public StreetSegment[] prefabs;
            public float minDifficulty;
            public float maxDifficulty;
            public bool canHaveBonus;
        }
        
        private void Awake()
        {
            InitializeSegmentPool();
        }
        
        private void Start()
        {
            FindPlayer();
            GenerateInitialStreet();
            
            // Subscribe to game events
            if (CatchMeowIfYouCan.Core.GameManager.Instance != null)
            {
                CatchMeowIfYouCan.Core.GameManager.Instance.OnGameSpeedChanged += OnGameSpeedChanged;
            }
        }
        
        private void Update()
        {
            if (ShouldUpdate())
            {
                UpdateDifficulty();
                CheckGenerationTrigger();
                CleanupOldSegments();
            }
        }
        
        #region Initialization
        
        private void InitializeSegmentPool()
        {
            // Create initial pool of segments
            int poolSize = maxActiveSegments * 2;
            
            for (int i = 0; i < poolSize; i++)
            {
                if (streetPrefabs.Length > 0)
                {
                    StreetSegment prefab = streetPrefabs[Random.Range(0, streetPrefabs.Length)];
                    StreetSegment segment = Instantiate(prefab, transform);
                    segment.gameObject.SetActive(false);
                    segmentPool.Enqueue(segment);
                }
            }
            
            Debug.Log($"StreetGenerator: Initialized pool with {segmentPool.Count} segments");
        }
        
        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogError("StreetGenerator: Player not found! Make sure player has 'Player' tag.");
            }
        }
        
        #endregion
        
        #region Street Generation
        
        private void GenerateInitialStreet()
        {
            for (int i = 0; i < initialSegmentCount; i++)
            {
                GenerateNextSegment();
            }
        }
        
        private void GenerateNextSegment()
        {
            StreetSegment segmentToUse = GetNextSegmentPrefab();
            StreetSegment newSegment = GetSegmentFromPool(segmentToUse);
            
            if (newSegment != null)
            {
                // Position the segment
                Vector3 position = new Vector3(nextSegmentPosition, 0f, 0f);
                newSegment.transform.position = position;
                newSegment.gameObject.SetActive(true);
                
                // Initialize the segment
                newSegment.InitializeSegment(currentDifficulty, totalSegmentsGenerated);
                
                // Add to active segments
                activeSegments.Enqueue(newSegment);
                
                // Update position for next segment
                nextSegmentPosition += segmentLength;
                totalSegmentsGenerated++;
                segmentsSinceBonus++;
                
                OnSegmentGenerated?.Invoke(newSegment);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Generated segment #{totalSegmentsGenerated} at {position.x}, difficulty: {currentDifficulty:F2}");
                }
            }
        }
        
        private StreetSegment GetNextSegmentPrefab()
        {
            // Check for bonus segment first
            if (ShouldGenerateBonusSegment())
            {
                segmentsSinceBonus = 0;
                return bonusSegments[Random.Range(0, bonusSegments.Length)];
            }
            
            // Choose segment type based on difficulty and weights
            SegmentType chosenType = ChooseSegmentType();
            
            if (chosenType.prefabs.Length > 0)
            {
                return chosenType.prefabs[Random.Range(0, chosenType.prefabs.Length)];
            }
            
            // Fallback to basic street prefabs
            return streetPrefabs[Random.Range(0, streetPrefabs.Length)];
        }
        
        private bool ShouldGenerateBonusSegment()
        {
            return bonusSegments.Length > 0 && 
                   segmentsSinceBonus >= minSegmentsBetweenBonus &&
                   Random.value < bonusSegmentChance;
        }
        
        private SegmentType ChooseSegmentType()
        {
            // Filter segment types by current difficulty
            List<SegmentType> availableTypes = new List<SegmentType>();
            List<float> weights = new List<float>();
            
            for (int i = 0; i < segmentTypes.Length; i++)
            {
                SegmentType type = segmentTypes[i];
                if (currentDifficulty >= type.minDifficulty && currentDifficulty <= type.maxDifficulty)
                {
                    availableTypes.Add(type);
                    weights.Add(i < segmentWeights.Length ? segmentWeights[i] : 0.1f);
                }
            }
            
            if (availableTypes.Count == 0)
            {
                return segmentTypes[0]; // Fallback to first type
            }
            
            // Weighted random selection
            return availableTypes[GetWeightedRandomIndex(weights.ToArray())];
        }
        
        private int GetWeightedRandomIndex(float[] weights)
        {
            float totalWeight = 0f;
            foreach (float weight in weights)
            {
                totalWeight += weight;
            }
            
            float randomValue = Random.value * totalWeight;
            float currentWeight = 0f;
            
            for (int i = 0; i < weights.Length; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return i;
                }
            }
            
            return weights.Length - 1;
        }
        
        #endregion
        
        #region Segment Management
        
        private StreetSegment GetSegmentFromPool(StreetSegment prefab)
        {
            if (segmentPool.Count > 0)
            {
                StreetSegment pooledSegment = segmentPool.Dequeue();
                // Copy properties from prefab if needed
                pooledSegment.CopyFromTemplate(prefab);
                return pooledSegment;
            }
            
            // Pool is empty, create new segment
            return Instantiate(prefab, transform);
        }
        
        private void ReturnSegmentToPool(StreetSegment segment)
        {
            segment.gameObject.SetActive(false);
            segment.ResetSegment();
            segmentPool.Enqueue(segment);
            OnSegmentRecycled?.Invoke(segment);
        }
        
        private void CleanupOldSegments()
        {
            if (player == null || activeSegments.Count == 0) return;
            
            // Remove segments that are too far behind the player
            while (activeSegments.Count > 0)
            {
                StreetSegment oldestSegment = activeSegments.Peek();
                float segmentEndPosition = oldestSegment.transform.position.x + segmentLength;
                
                if (segmentEndPosition < player.position.x - segmentLength)
                {
                    StreetSegment segmentToRemove = activeSegments.Dequeue();
                    ReturnSegmentToPool(segmentToRemove);
                }
                else
                {
                    break; // No more segments to remove
                }
            }
        }
        
        #endregion
        
        #region Difficulty Management
        
        private void UpdateDifficulty()
        {
            if (CatchMeowIfYouCan.Core.GameManager.Instance == null) return;
            
            float gameTime = CatchMeowIfYouCan.Core.GameManager.Instance.GameTime;
            float difficultyProgress = gameTime / difficultyIncreaseInterval;
            
            float newDifficulty;
            if (difficultyProgressionCurve != null && difficultyProgressionCurve.keys.Length > 0)
            {
                newDifficulty = difficultyProgressionCurve.Evaluate(difficultyProgress) * maxDifficulty;
            }
            else
            {
                newDifficulty = Mathf.Clamp01(difficultyProgress) * maxDifficulty;
            }
            
            if (Mathf.Abs(newDifficulty - currentDifficulty) > 0.01f)
            {
                currentDifficulty = newDifficulty;
                OnDifficultyChanged?.Invoke(currentDifficulty);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Difficulty updated to: {currentDifficulty:F2}");
                }
            }
        }
        
        #endregion
        
        #region Trigger Checks
        
        private bool ShouldUpdate()
        {
            if (Time.time - lastGenerationCheck >= GENERATION_CHECK_INTERVAL)
            {
                lastGenerationCheck = Time.time;
                return true;
            }
            return false;
        }
        
        private void CheckGenerationTrigger()
        {
            if (player == null) return;
            
            // Generate new segments when player gets close to the end
            float furthestSegmentPosition = nextSegmentPosition - segmentLength;
            float distanceToEnd = furthestSegmentPosition - player.position.x;
            
            if (distanceToEnd < generationDistance)
            {
                GenerateNextSegment();
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnGameSpeedChanged(float newSpeed)
        {
            // Adjust generation distance based on game speed
            generationDistance = Mathf.Max(50f, 50f * newSpeed);
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Force generate a specific number of segments
        /// </summary>
        public void ForceGenerateSegments(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GenerateNextSegment();
            }
        }
        
        /// <summary>
        /// Clear all active segments and regenerate
        /// </summary>
        public void RegenerateStreet()
        {
            // Return all active segments to pool
            while (activeSegments.Count > 0)
            {
                StreetSegment segment = activeSegments.Dequeue();
                ReturnSegmentToPool(segment);
            }
            
            // Reset generation position
            nextSegmentPosition = player != null ? player.position.x : 0f;
            totalSegmentsGenerated = 0;
            segmentsSinceBonus = 0;
            
            // Generate new initial street
            GenerateInitialStreet();
        }
        
        /// <summary>
        /// Get current street generation statistics
        /// </summary>
        public StreetStats GetStreetStats()
        {
            return new StreetStats
            {
                activeSegmentCount = activeSegments.Count,
                pooledSegmentCount = segmentPool.Count,
                totalSegmentsGenerated = totalSegmentsGenerated,
                currentDifficulty = currentDifficulty,
                nextSegmentPosition = nextSegmentPosition
            };
        }
        
        /// <summary>
        /// Set difficulty manually (for testing)
        /// </summary>
        public void SetDifficulty(float difficulty)
        {
            currentDifficulty = Mathf.Clamp01(difficulty);
            OnDifficultyChanged?.Invoke(currentDifficulty);
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            if (!previewGeneration || player == null) return;
            
            // Draw generation trigger distance
            Gizmos.color = Color.yellow;
            Vector3 triggerPos = new Vector3(player.position.x + generationDistance, 0f, 0f);
            Gizmos.DrawWireCube(triggerPos, Vector3.one * 2f);
            
            // Draw active segments
            Gizmos.color = Color.green;
            foreach (StreetSegment segment in activeSegments)
            {
                if (segment != null)
                {
                    Vector3 segmentPos = segment.transform.position;
                    Gizmos.DrawWireCube(segmentPos + Vector3.right * segmentLength * 0.5f, 
                                      new Vector3(segmentLength, 1f, 1f));
                }
            }
            
            // Draw next segment position
            Gizmos.color = Color.red;
            Vector3 nextPos = new Vector3(nextSegmentPosition, 0f, 0f);
            Gizmos.DrawWireCube(nextPos, Vector3.one);
        }
        
        public string GetDebugInfo()
        {
            var stats = GetStreetStats();
            return $"Active Segments: {stats.activeSegmentCount}\n" +
                   $"Pooled Segments: {stats.pooledSegmentCount}\n" +
                   $"Total Generated: {stats.totalSegmentsGenerated}\n" +
                   $"Current Difficulty: {stats.currentDifficulty:F2}\n" +
                   $"Next Position: {stats.nextSegmentPosition:F1}";
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Cleanup events
            if (CatchMeowIfYouCan.Core.GameManager.Instance != null)
            {
                CatchMeowIfYouCan.Core.GameManager.Instance.OnGameSpeedChanged -= OnGameSpeedChanged;
            }
        }
        
        [System.Serializable]
        public struct StreetStats
        {
            public int activeSegmentCount;
            public int pooledSegmentCount;
            public int totalSegmentsGenerated;
            public float currentDifficulty;
            public float nextSegmentPosition;
        }
    }
}