using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CatchMeowIfYouCan.Obstacles
{
    /// <summary>
    /// Manages obstacle spawning, difficulty scaling, and obstacle lifecycle
    /// Controls obstacle patterns, timing, and interaction with game systems
    /// </summary>
    public class ObstacleManager : MonoBehaviour
    {
        [Header("Spawning Settings")]
        [SerializeField] private bool enableSpawning = true;
        [SerializeField] private float baseSpawnInterval = 3f;
        [SerializeField] private float minSpawnInterval = 0.8f;
        [SerializeField] private float spawnIntervalReduction = 0.1f;
        [SerializeField] private float spawnPositionX = 15f;
        [SerializeField] private int maxActiveObstacles = 10;
        
        [Header("Lane Configuration")]
        [SerializeField] private Transform[] spawnLanes;
        [SerializeField] private float[] laneYPositions = { -1f, 0f, 1f }; // Bottom, Middle, Top lanes
        [SerializeField] private bool allowMultipleLaneSpawning = true;
        [SerializeField] private float multipleLaneSpawnChance = 0.3f;
        
        [Header("Obstacle Prefabs")]
        [SerializeField] private ObstaclePrefabConfig[] obstaclePrefabs;
        [SerializeField] private GameObject defaultCarPrefab;
        [SerializeField] private GameObject defaultTrashBinPrefab;
        
        [Header("Difficulty Scaling")]
        [SerializeField] private DifficultyConfig[] difficultyLevels;
        [SerializeField] private float difficultyScaleTime = 30f; // Time to increase difficulty
        [SerializeField] private int maxDifficultyLevel = 5;
        [SerializeField] private AnimationCurve difficultyProgressionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Pattern System")]
        [SerializeField] private bool enablePatterns = true;
        [SerializeField] private ObstaclePattern[] obstaclePatterns;
        [SerializeField] private float patternSpawnChance = 0.4f;
        [SerializeField] private float patternCooldown = 10f;
        
        [Header("Special Events")]
        [SerializeField] private bool enableSpecialEvents = true;
        [SerializeField] private SpecialEvent[] specialEvents;
        [SerializeField] private float specialEventChance = 0.1f;
        [SerializeField] private float specialEventCooldown = 20f;
        
        [Header("Object Pooling")]
        [SerializeField] private bool useObjectPooling = true;
        [SerializeField] private int poolSizePerType = 5;
        [SerializeField] private Transform poolParent;
        
        [Header("Performance")]
        [SerializeField] private float cleanupInterval = 2f;
        [SerializeField] private float obstacleLifetime = 30f;
        [SerializeField] private float offScreenBoundary = -20f;
        
        // Runtime state
        private float currentSpawnInterval;
        private float nextSpawnTime;
        private int currentDifficultyLevel = 0;
        private float gameStartTime;
        private float lastPatternSpawnTime;
        private float lastSpecialEventTime;
        private bool isSpawning = false;
        
        // Active obstacles tracking
        private List<BaseObstacle> activeObstacles = new List<BaseObstacle>();
        private Queue<BaseObstacle> obstaclePool = new Queue<BaseObstacle>();
        private Dictionary<System.Type, Queue<BaseObstacle>> typedPools = new Dictionary<System.Type, Queue<BaseObstacle>>();
        
        // Pattern state
        private bool isExecutingPattern = false;
        private Coroutine currentPatternCoroutine;
        
        // Game references
        private Core.GameManager gameManager;
        private Core.AudioManager audioManager;
        private Player.CatController catController;
        private Environment.StreetGenerator streetGenerator;
        
        // Events
        public System.Action<BaseObstacle> OnObstacleSpawned;
        public System.Action<BaseObstacle> OnObstacleDestroyed;
        public System.Action<int> OnDifficultyChanged;
        public System.Action<ObstaclePattern> OnPatternStarted;
        public System.Action<SpecialEvent> OnSpecialEventTriggered;
        
        // Statistics
        [System.Serializable]
        public class ObstacleStats
        {
            public int totalSpawned = 0;
            public int totalDestroyed = 0;
            public int carsSpawned = 0;
            public int trashBinsSpawned = 0;
            public int patternsExecuted = 0;
            public int specialEventsTriggered = 0;
            public float averageSpawnInterval = 0f;
        }
        
        public ObstacleStats Stats { get; private set; } = new ObstacleStats();
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeManager();
        }
        
        private void Start()
        {
            StartManager();
        }
        
        private void Update()
        {
            if (!enableSpawning || !isSpawning) return;
            
            UpdateDifficulty();
            HandleSpawning();
            UpdateObstacles();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeManager()
        {
            // Get game references
            gameManager = Core.GameManager.Instance;
            audioManager = FindObjectOfType<Core.AudioManager>();
            catController = FindObjectOfType<Player.CatController>();
            streetGenerator = FindObjectOfType<Environment.StreetGenerator>();
            
            // Setup spawn lanes if not assigned
            if (spawnLanes == null || spawnLanes.Length == 0)
            {
                SetupDefaultSpawnLanes();
            }
            
            // Initialize object pools
            if (useObjectPooling)
            {
                InitializeObjectPools();
            }
            
            // Initialize difficulty
            currentSpawnInterval = baseSpawnInterval;
            gameStartTime = Time.time;
            
            // Setup cleanup coroutine
            StartCoroutine(CleanupCoroutine());
        }
        
        private void StartManager()
        {
            if (gameManager != null)
            {
                // Subscribe to game events
                gameManager.OnGameStateChanged += HandleGameStateChanged;
                gameManager.OnGameSpeedChanged += HandleGameSpeedChanged;
            }
            
            // Start spawning
            isSpawning = true;
            nextSpawnTime = Time.time + currentSpawnInterval;
        }
        
        private void SetupDefaultSpawnLanes()
        {
            spawnLanes = new Transform[laneYPositions.Length];
            
            for (int i = 0; i < laneYPositions.Length; i++)
            {
                GameObject lane = new GameObject($"SpawnLane_{i}");
                lane.transform.SetParent(transform);
                lane.transform.position = new Vector3(spawnPositionX, laneYPositions[i], 0);
                spawnLanes[i] = lane.transform;
            }
        }
        
        private void InitializeObjectPools()
        {
            if (poolParent == null)
            {
                GameObject poolContainer = new GameObject("ObstaclePool");
                poolContainer.transform.SetParent(transform);
                poolParent = poolContainer.transform;
            }
            
            // Initialize pools for each obstacle type
            foreach (var config in obstaclePrefabs)
            {
                if (config.prefab != null)
                {
                    var obstacleType = config.prefab.GetComponent<BaseObstacle>()?.GetType();
                    if (obstacleType != null && !typedPools.ContainsKey(obstacleType))
                    {
                        typedPools[obstacleType] = new Queue<BaseObstacle>();
                        
                        // Pre-populate pool
                        for (int i = 0; i < poolSizePerType; i++)
                        {
                            CreatePooledObstacle(config.prefab, obstacleType);
                        }
                    }
                }
            }
        }
        
        private void CreatePooledObstacle(GameObject prefab, System.Type obstacleType)
        {
            GameObject obj = Instantiate(prefab, poolParent);
            BaseObstacle obstacle = obj.GetComponent<BaseObstacle>();
            
            if (obstacle != null)
            {
                obstacle.SetActive(false);
                obstacle.OnObstacleDestroyed += ReturnToPool;
                typedPools[obstacleType].Enqueue(obstacle);
            }
        }
        
        #endregion
        
        #region Spawning System
        
        private void HandleSpawning()
        {
            if (Time.time >= nextSpawnTime && activeObstacles.Count < maxActiveObstacles)
            {
                SpawnObstacle();
                ScheduleNextSpawn();
            }
        }
        
        private void SpawnObstacle()
        {
            // Check for pattern spawning
            if (enablePatterns && ShouldSpawnPattern())
            {
                SpawnPattern();
                return;
            }
            
            // Check for special events
            if (enableSpecialEvents && ShouldTriggerSpecialEvent())
            {
                TriggerSpecialEvent();
                return;
            }
            
            // Regular obstacle spawning
            SpawnRegularObstacle();
        }
        
        private void SpawnRegularObstacle()
        {
            // Select obstacle type based on difficulty
            ObstaclePrefabConfig selectedConfig = SelectObstacleType();
            if (selectedConfig.prefab == null) return;
            
            // Select spawn lane(s)
            List<int> selectedLanes = SelectSpawnLanes();
            
            foreach (int laneIndex in selectedLanes)
            {
                if (laneIndex >= 0 && laneIndex < spawnLanes.Length)
                {
                    BaseObstacle obstacle = CreateObstacle(selectedConfig, laneIndex);
                    if (obstacle != null)
                    {
                        FinalizeObstacleSpawn(obstacle);
                        Stats.totalSpawned++;
                        
                        // Update type-specific stats
                        if (obstacle is CarObstacle) Stats.carsSpawned++;
                        else if (obstacle is TrashBinObstacle) Stats.trashBinsSpawned++;
                    }
                }
            }
        }
        
        private ObstaclePrefabConfig SelectObstacleType()
        {
            if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
            {
                // Fallback to default prefabs
                return new ObstaclePrefabConfig
                {
                    prefab = defaultCarPrefab,
                    spawnWeight = 1f,
                    minDifficultyLevel = 0
                };
            }
            
            // Filter by difficulty level
            List<ObstaclePrefabConfig> availableConfigs = new List<ObstaclePrefabConfig>();
            float totalWeight = 0f;
            
            foreach (var config in obstaclePrefabs)
            {
                if (config.minDifficultyLevel <= currentDifficultyLevel)
                {
                    availableConfigs.Add(config);
                    totalWeight += config.spawnWeight;
                }
            }
            
            if (availableConfigs.Count == 0)
            {
                return obstaclePrefabs[0]; // Fallback
            }
            
            // Weighted random selection
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (var config in availableConfigs)
            {
                currentWeight += config.spawnWeight;
                if (randomValue <= currentWeight)
                {
                    return config;
                }
            }
            
            return availableConfigs[availableConfigs.Count - 1];
        }
        
        private List<int> SelectSpawnLanes()
        {
            List<int> selectedLanes = new List<int>();
            
            if (!allowMultipleLaneSpawning || Random.Range(0f, 1f) > multipleLaneSpawnChance)
            {
                // Single lane spawning
                selectedLanes.Add(Random.Range(0, spawnLanes.Length));
            }
            else
            {
                // Multiple lane spawning
                int numLanes = Random.Range(2, Mathf.Min(4, spawnLanes.Length + 1));
                HashSet<int> usedLanes = new HashSet<int>();
                
                for (int i = 0; i < numLanes && usedLanes.Count < spawnLanes.Length; i++)
                {
                    int lane;
                    do
                    {
                        lane = Random.Range(0, spawnLanes.Length);
                    } while (usedLanes.Contains(lane));
                    
                    usedLanes.Add(lane);
                    selectedLanes.Add(lane);
                }
            }
            
            return selectedLanes;
        }
        
        private BaseObstacle CreateObstacle(ObstaclePrefabConfig config, int laneIndex)
        {
            BaseObstacle obstacle = null;
            
            if (useObjectPooling)
            {
                obstacle = GetFromPool(config.prefab);
            }
            
            if (obstacle == null)
            {
                // Create new obstacle
                Vector3 spawnPosition = spawnLanes[laneIndex].position;
                GameObject obstacleObj = Instantiate(config.prefab, spawnPosition, Quaternion.identity);
                obstacle = obstacleObj.GetComponent<BaseObstacle>();
            }
            else
            {
                // Reset pooled obstacle
                Vector3 spawnPosition = spawnLanes[laneIndex].position;
                obstacle.transform.position = spawnPosition;
                obstacle.transform.rotation = Quaternion.identity;
                obstacle.ResetObstacle();
                obstacle.SetActive(true);
            }
            
            if (obstacle != null)
            {
                // Apply difficulty modifiers
                ApplyDifficultyModifiers(obstacle);
                
                // Subscribe to events
                obstacle.OnObstacleDestroyed += HandleObstacleDestroyed;
                obstacle.OnObstacleHit += HandleObstacleHit;
            }
            
            return obstacle;
        }
        
        private void FinalizeObstacleSpawn(BaseObstacle obstacle)
        {
            activeObstacles.Add(obstacle);
            OnObstacleSpawned?.Invoke(obstacle);
        }
        
        private void ScheduleNextSpawn()
        {
            nextSpawnTime = Time.time + currentSpawnInterval;
            Stats.averageSpawnInterval = (Stats.averageSpawnInterval * (Stats.totalSpawned - 1) + currentSpawnInterval) / Stats.totalSpawned;
        }
        
        #endregion
        
        #region Pattern System
        
        private bool ShouldSpawnPattern()
        {
            if (isExecutingPattern) return false;
            if (Time.time - lastPatternSpawnTime < patternCooldown) return false;
            if (obstaclePatterns == null || obstaclePatterns.Length == 0) return false;
            
            return Random.Range(0f, 1f) < patternSpawnChance;
        }
        
        private void SpawnPattern()
        {
            // Select random pattern appropriate for current difficulty
            List<ObstaclePattern> availablePatterns = new List<ObstaclePattern>();
            
            foreach (var pattern in obstaclePatterns)
            {
                if (pattern.minDifficultyLevel <= currentDifficultyLevel)
                {
                    availablePatterns.Add(pattern);
                }
            }
            
            if (availablePatterns.Count == 0) return;
            
            ObstaclePattern selectedPattern = availablePatterns[Random.Range(0, availablePatterns.Count)];
            ExecutePattern(selectedPattern);
        }
        
        private void ExecutePattern(ObstaclePattern pattern)
        {
            if (isExecutingPattern) return;
            
            isExecutingPattern = true;
            lastPatternSpawnTime = Time.time;
            Stats.patternsExecuted++;
            
            currentPatternCoroutine = StartCoroutine(PatternCoroutine(pattern));
            OnPatternStarted?.Invoke(pattern);
        }
        
        private IEnumerator PatternCoroutine(ObstaclePattern pattern)
        {
            for (int i = 0; i < pattern.obstacleSequence.Length; i++)
            {
                var patternObstacle = pattern.obstacleSequence[i];
                
                if (patternObstacle.prefab != null)
                {
                    // Spawn obstacle at specified lane
                    int laneIndex = Mathf.Clamp(patternObstacle.laneIndex, 0, spawnLanes.Length - 1);
                    
                    ObstaclePrefabConfig tempConfig = new ObstaclePrefabConfig
                    {
                        prefab = patternObstacle.prefab,
                        spawnWeight = 1f,
                        minDifficultyLevel = 0
                    };
                    
                    BaseObstacle obstacle = CreateObstacle(tempConfig, laneIndex);
                    if (obstacle != null)
                    {
                        FinalizeObstacleSpawn(obstacle);
                        Stats.totalSpawned++;
                    }
                }
                
                // Wait for next obstacle in pattern
                if (i < pattern.obstacleSequence.Length - 1)
                {
                    yield return new WaitForSeconds(pattern.timeBetweenObstacles);
                }
            }
            
            isExecutingPattern = false;
        }
        
        #endregion
        
        #region Special Events System
        
        private bool ShouldTriggerSpecialEvent()
        {
            if (Time.time - lastSpecialEventTime < specialEventCooldown) return false;
            if (specialEvents == null || specialEvents.Length == 0) return false;
            
            return Random.Range(0f, 1f) < specialEventChance;
        }
        
        private void TriggerSpecialEvent()
        {
            // Select random special event
            SpecialEvent selectedEvent = specialEvents[Random.Range(0, specialEvents.Length)];
            ExecuteSpecialEvent(selectedEvent);
        }
        
        private void ExecuteSpecialEvent(SpecialEvent specialEvent)
        {
            lastSpecialEventTime = Time.time;
            Stats.specialEventsTriggered++;
            
            StartCoroutine(SpecialEventCoroutine(specialEvent));
            OnSpecialEventTriggered?.Invoke(specialEvent);
        }
        
        private IEnumerator SpecialEventCoroutine(SpecialEvent specialEvent)
        {
            Debug.Log($"Special Event: {specialEvent.eventName}");
            
            switch (specialEvent.eventType)
            {
                case SpecialEventType.TrafficJam:
                    yield return StartCoroutine(TrafficJamEvent(specialEvent));
                    break;
                    
                case SpecialEventType.ClearPath:
                    yield return StartCoroutine(ClearPathEvent(specialEvent));
                    break;
                    
                case SpecialEventType.ObstacleRain:
                    yield return StartCoroutine(ObstacleRainEvent(specialEvent));
                    break;
                    
                case SpecialEventType.SafeZone:
                    yield return StartCoroutine(SafeZoneEvent(specialEvent));
                    break;
            }
        }
        
        private IEnumerator TrafficJamEvent(SpecialEvent specialEvent)
        {
            // Spawn multiple cars in quick succession
            float originalInterval = currentSpawnInterval;
            currentSpawnInterval = 0.5f;
            
            for (int i = 0; i < 5; i++)
            {
                SpawnRegularObstacle();
                yield return new WaitForSeconds(currentSpawnInterval);
            }
            
            currentSpawnInterval = originalInterval;
        }
        
        private IEnumerator ClearPathEvent(SpecialEvent specialEvent)
        {
            // No obstacles spawn for a period
            float pauseDuration = specialEvent.duration;
            enableSpawning = false;
            
            yield return new WaitForSeconds(pauseDuration);
            
            enableSpawning = true;
        }
        
        private IEnumerator ObstacleRainEvent(SpecialEvent specialEvent)
        {
            // Rapid spawning of obstacles
            float originalInterval = currentSpawnInterval;
            currentSpawnInterval = 0.3f;
            
            float eventEndTime = Time.time + specialEvent.duration;
            
            while (Time.time < eventEndTime)
            {
                SpawnRegularObstacle();
                yield return new WaitForSeconds(currentSpawnInterval);
            }
            
            currentSpawnInterval = originalInterval;
        }
        
        private IEnumerator SafeZoneEvent(SpecialEvent specialEvent)
        {
            // Destroy all current obstacles and pause spawning
            foreach (var obstacle in activeObstacles.ToArray())
            {
                if (obstacle != null)
                {
                    obstacle.ForceDestroy();
                }
            }
            
            enableSpawning = false;
            yield return new WaitForSeconds(specialEvent.duration);
            enableSpawning = true;
        }
        
        #endregion
        
        #region Difficulty System
        
        private void UpdateDifficulty()
        {
            float gameTime = Time.time - gameStartTime;
            int newDifficultyLevel = Mathf.FloorToInt(gameTime / difficultyScaleTime);
            newDifficultyLevel = Mathf.Clamp(newDifficultyLevel, 0, maxDifficultyLevel);
            
            if (newDifficultyLevel != currentDifficultyLevel)
            {
                currentDifficultyLevel = newDifficultyLevel;
                ApplyDifficultyLevel();
                OnDifficultyChanged?.Invoke(currentDifficultyLevel);
            }
            
            // Gradually reduce spawn interval
            float difficultyProgress = difficultyProgressionCurve.Evaluate((float)currentDifficultyLevel / maxDifficultyLevel);
            currentSpawnInterval = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, difficultyProgress);
        }
        
        private void ApplyDifficultyLevel()
        {
            if (difficultyLevels != null && currentDifficultyLevel < difficultyLevels.Length)
            {
                var difficulty = difficultyLevels[currentDifficultyLevel];
                
                // Apply difficulty settings
                multipleLaneSpawnChance = difficulty.multipleLaneChance;
                patternSpawnChance = difficulty.patternSpawnChance;
                specialEventChance = difficulty.specialEventChance;
            }
        }
        
        private void ApplyDifficultyModifiers(BaseObstacle obstacle)
        {
            if (difficultyLevels != null && currentDifficultyLevel < difficultyLevels.Length)
            {
                var difficulty = difficultyLevels[currentDifficultyLevel];
                
                // Apply speed multiplier if obstacle supports it
                // TODO: Add speed modification interface to BaseObstacle
                // obstacle.SetSpeedMultiplier(difficulty.speedMultiplier);
            }
        }
        
        #endregion
        
        #region Object Pooling
        
        private BaseObstacle GetFromPool(GameObject prefab)
        {
            if (!useObjectPooling) return null;
            
            var obstacleComponent = prefab.GetComponent<BaseObstacle>();
            if (obstacleComponent == null) return null;
            
            var obstacleType = obstacleComponent.GetType();
            
            if (typedPools.ContainsKey(obstacleType) && typedPools[obstacleType].Count > 0)
            {
                return typedPools[obstacleType].Dequeue();
            }
            
            return null;
        }
        
        private void ReturnToPool(BaseObstacle obstacle)
        {
            if (!useObjectPooling || obstacle == null) return;
            
            var obstacleType = obstacle.GetType();
            
            if (typedPools.ContainsKey(obstacleType))
            {
                obstacle.SetActive(false);
                obstacle.transform.SetParent(poolParent);
                typedPools[obstacleType].Enqueue(obstacle);
            }
        }
        
        #endregion
        
        #region Obstacle Management
        
        private void UpdateObstacles()
        {
            // Remove destroyed or off-screen obstacles
            for (int i = activeObstacles.Count - 1; i >= 0; i--)
            {
                if (activeObstacles[i] == null || 
                    activeObstacles[i].IsDestroyed || 
                    activeObstacles[i].transform.position.x < offScreenBoundary)
                {
                    if (activeObstacles[i] != null && !activeObstacles[i].IsDestroyed)
                    {
                        activeObstacles[i].ForceDestroy();
                    }
                    activeObstacles.RemoveAt(i);
                }
            }
        }
        
        private IEnumerator CleanupCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(cleanupInterval);
                
                // Force cleanup of very old obstacles
                float currentTime = Time.time;
                for (int i = activeObstacles.Count - 1; i >= 0; i--)
                {
                    var obstacle = activeObstacles[i];
                    if (obstacle != null)
                    {
                        float obstacleAge = currentTime - obstacle.GetInfo().timeAlive;
                        if (obstacleAge > obstacleLifetime)
                        {
                            obstacle.ForceDestroy();
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleGameStateChanged(Core.GameManager.GameState newState)
        {
            switch (newState)
            {
                case Core.GameManager.GameState.Playing:
                    isSpawning = true;
                    break;
                    
                case Core.GameManager.GameState.Paused:
                case Core.GameManager.GameState.GameOver:
                    isSpawning = false;
                    break;
                    
                case Core.GameManager.GameState.MainMenu:
                    ResetManager();
                    break;
            }
        }
        
        private void HandleGameSpeedChanged(float newSpeed)
        {
            // Adjust spawn interval based on game speed
            currentSpawnInterval = baseSpawnInterval / newSpeed;
            currentSpawnInterval = Mathf.Max(currentSpawnInterval, minSpawnInterval);
        }
        
        private void HandleObstacleDestroyed(BaseObstacle obstacle)
        {
            if (obstacle != null)
            {
                Stats.totalDestroyed++;
                OnObstacleDestroyed?.Invoke(obstacle);
                
                // Return to pool if using object pooling
                if (useObjectPooling)
                {
                    ReturnToPool(obstacle);
                }
            }
        }
        
        private void HandleObstacleHit(BaseObstacle obstacle, Collider2D playerCollider)
        {
            // Handle obstacle hit logic
            Debug.Log($"Obstacle hit: {obstacle.Type}");
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Start spawning obstacles
        /// </summary>
        public void StartSpawning()
        {
            enableSpawning = true;
            isSpawning = true;
            nextSpawnTime = Time.time + currentSpawnInterval;
        }
        
        /// <summary>
        /// Stop spawning obstacles
        /// </summary>
        public void StopSpawning()
        {
            enableSpawning = false;
            isSpawning = false;
        }
        
        /// <summary>
        /// Clear all active obstacles
        /// </summary>
        public void ClearAllObstacles()
        {
            foreach (var obstacle in activeObstacles.ToArray())
            {
                if (obstacle != null)
                {
                    obstacle.ForceDestroy();
                }
            }
            activeObstacles.Clear();
        }
        
        /// <summary>
        /// Force spawn specific obstacle type
        /// </summary>
        public BaseObstacle ForceSpawnObstacle(GameObject prefab, int laneIndex = -1)
        {
            if (prefab == null) return null;
            
            if (laneIndex < 0)
            {
                laneIndex = Random.Range(0, spawnLanes.Length);
            }
            
            laneIndex = Mathf.Clamp(laneIndex, 0, spawnLanes.Length - 1);
            
            ObstaclePrefabConfig config = new ObstaclePrefabConfig
            {
                prefab = prefab,
                spawnWeight = 1f,
                minDifficultyLevel = 0
            };
            
            BaseObstacle obstacle = CreateObstacle(config, laneIndex);
            if (obstacle != null)
            {
                FinalizeObstacleSpawn(obstacle);
                Stats.totalSpawned++;
            }
            
            return obstacle;
        }
        
        /// <summary>
        /// Set difficulty level manually
        /// </summary>
        public void SetDifficultyLevel(int level)
        {
            currentDifficultyLevel = Mathf.Clamp(level, 0, maxDifficultyLevel);
            ApplyDifficultyLevel();
            OnDifficultyChanged?.Invoke(currentDifficultyLevel);
        }
        
        /// <summary>
        /// Get current obstacle count
        /// </summary>
        public int GetActiveObstacleCount()
        {
            return activeObstacles.Count;
        }
        
        /// <summary>
        /// Get obstacles in specific lane
        /// </summary>
        public List<BaseObstacle> GetObstaclesInLane(int laneIndex)
        {
            List<BaseObstacle> laneObstacles = new List<BaseObstacle>();
            
            if (laneIndex < 0 || laneIndex >= laneYPositions.Length) return laneObstacles;
            
            float laneY = laneYPositions[laneIndex];
            float laneTolerance = 0.5f;
            
            foreach (var obstacle in activeObstacles)
            {
                if (obstacle != null && Mathf.Abs(obstacle.transform.position.y - laneY) < laneTolerance)
                {
                    laneObstacles.Add(obstacle);
                }
            }
            
            return laneObstacles;
        }
        
        /// <summary>
        /// Reset manager to initial state
        /// </summary>
        public void ResetManager()
        {
            // Stop all spawning
            StopSpawning();
            
            // Stop pattern execution
            if (currentPatternCoroutine != null)
            {
                StopCoroutine(currentPatternCoroutine);
                currentPatternCoroutine = null;
            }
            isExecutingPattern = false;
            
            // Clear all obstacles
            ClearAllObstacles();
            
            // Reset state
            currentDifficultyLevel = 0;
            currentSpawnInterval = baseSpawnInterval;
            gameStartTime = Time.time;
            lastPatternSpawnTime = -patternCooldown;
            lastSpecialEventTime = -specialEventCooldown;
            
            // Reset stats
            Stats = new ObstacleStats();
        }
        
        #endregion
        
        #region Data Structures
        
        [System.Serializable]
        public class ObstaclePrefabConfig
        {
            public GameObject prefab;
            public float spawnWeight = 1f;
            public int minDifficultyLevel = 0;
            public bool canSpawnInPatterns = true;
        }
        
        [System.Serializable]
        public class DifficultyConfig
        {
            public string levelName;
            public float speedMultiplier = 1f;
            public float multipleLaneChance = 0.3f;
            public float patternSpawnChance = 0.4f;
            public float specialEventChance = 0.1f;
            public int maxSimultaneousObstacles = 10;
        }
        
        [System.Serializable]
        public class ObstaclePattern
        {
            public string patternName;
            public int minDifficultyLevel = 0;
            public float timeBetweenObstacles = 0.5f;
            public PatternObstacle[] obstacleSequence;
        }
        
        [System.Serializable]
        public class PatternObstacle
        {
            public GameObject prefab;
            public int laneIndex;
        }
        
        [System.Serializable]
        public class SpecialEvent
        {
            public string eventName;
            public SpecialEventType eventType;
            public float duration = 5f;
            public int minDifficultyLevel = 0;
        }
        
        public enum SpecialEventType
        {
            TrafficJam,     // Lots of cars spawn quickly
            ClearPath,      // No obstacles for a period
            ObstacleRain,   // Rapid obstacle spawning
            SafeZone        // All obstacles cleared, no spawning
        }
        
        #endregion
        
        #region Debug
        
        public string GetDebugInfo()
        {
            return $"Obstacle Manager Debug Info:\n" +
                   $"Spawning Enabled: {enableSpawning}\n" +
                   $"Is Spawning: {isSpawning}\n" +
                   $"Active Obstacles: {activeObstacles.Count}/{maxActiveObstacles}\n" +
                   $"Current Difficulty: {currentDifficultyLevel}/{maxDifficultyLevel}\n" +
                   $"Spawn Interval: {currentSpawnInterval:F2}s\n" +
                   $"Next Spawn: {nextSpawnTime - Time.time:F1}s\n" +
                   $"Pattern Executing: {isExecutingPattern}\n" +
                   $"Total Spawned: {Stats.totalSpawned}\n" +
                   $"Total Destroyed: {Stats.totalDestroyed}\n" +
                   $"Cars Spawned: {Stats.carsSpawned}\n" +
                   $"Trash Bins Spawned: {Stats.trashBinsSpawned}\n" +
                   $"Patterns Executed: {Stats.patternsExecuted}\n" +
                   $"Special Events: {Stats.specialEventsTriggered}";
        }
        
        private void OnDrawGizmos()
        {
            // Draw spawn lanes
            if (spawnLanes != null)
            {
                Gizmos.color = Color.cyan;
                foreach (var lane in spawnLanes)
                {
                    if (lane != null)
                    {
                        Gizmos.DrawWireSphere(lane.position, 0.5f);
                        Gizmos.DrawLine(lane.position, lane.position + Vector3.left * 5f);
                    }
                }
            }
            
            // Draw off-screen boundary
            Gizmos.color = Color.red;
            Vector3 boundaryStart = new Vector3(offScreenBoundary, -5f, 0);
            Vector3 boundaryEnd = new Vector3(offScreenBoundary, 5f, 0);
            Gizmos.DrawLine(boundaryStart, boundaryEnd);
        }
        
        #endregion
    }
}