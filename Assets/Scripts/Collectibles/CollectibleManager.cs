using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace CatchMeowIfYouCan.Collectibles
{
    /// <summary>
    /// Manages all collectibles in the game including spawning, pooling, and statistics
    /// Handles collectible distribution, difficulty scaling, and performance optimization
    /// </summary>
    public class CollectibleManager : MonoBehaviour
    {
        [Header("Collectible Prefabs")]
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private GameObject gemPrefab;
        [SerializeField] private Transform collectibleParent;
        
        [Header("Spawn Settings")]
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private float spawnVariance = 0.5f;
        [SerializeField] private int maxActiveCollectibles = 50;
        
        [Header("Distribution Settings")]
        [SerializeField] private CollectibleDistribution distribution;
        
        [Header("Object Pooling")]
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private bool enablePooling = true;
        [SerializeField] private bool expandPoolDynamically = true;
        
        [Header("Difficulty Scaling")]
        [SerializeField] private bool enableDifficultyScaling = true;
        [SerializeField] private AnimationCurve difficultyValueCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f);
        [SerializeField] private AnimationCurve difficultyRarityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Special Events")]
        [SerializeField] private bool enableBonusEvents = true;
        [SerializeField] private float bonusEventInterval = 60f;
        [SerializeField] private float bonusEventDuration = 10f;
        
        // Singleton instance
        public static CollectibleManager Instance { get; private set; }
        
        // Object pools
        private Queue<GameObject> coinPool = new Queue<GameObject>();
        private Queue<GameObject> gemPool = new Queue<GameObject>();
        
        // Active collectibles tracking
        private List<BaseCollectible> activeCollectibles = new List<BaseCollectible>();
        private Dictionary<BaseCollectible.CollectibleType, int> collectibleCounts = new Dictionary<BaseCollectible.CollectibleType, int>();
        
        // Spawn management
        private float nextSpawnTime = 0f;
        private bool isSpawning = true;
        
        // Statistics
        private CollectibleStats stats = new CollectibleStats();
        
        // Game references
        private Core.GameManager gameManager;
        private Environment.StreetGenerator streetGenerator;
        
        // Bonus event state
        private bool isBonusEventActive = false;
        private float lastBonusEventTime = 0f;
        private Coroutine bonusEventCoroutine;
        
        [System.Serializable]
        public class CollectibleDistribution
        {
            [Header("Coin Distribution")]
            [Range(0f, 1f)] public float coinChance = 0.7f;
            [Range(0f, 1f)] public float silverCoinChance = 0.2f;
            [Range(0f, 1f)] public float goldCoinChance = 0.05f;
            
            [Header("Gem Distribution")]
            [Range(0f, 1f)] public float gemChance = 0.3f;
            [Range(0f, 1f)] public float sapphireChance = 0.15f;
            [Range(0f, 1f)] public float emeraldChance = 0.08f;
            [Range(0f, 1f)] public float diamondChance = 0.02f;
        }
        
        public struct CollectibleStats
        {
            public int totalSpawned;
            public int totalCollected;
            public int coinsCollected;
            public int gemsCollected;
            public int totalValue;
            public float collectionRate;
            public Dictionary<BaseCollectible.CollectibleType, int> typeBreakdown;
            
            public void Initialize()
            {
                typeBreakdown = new Dictionary<BaseCollectible.CollectibleType, int>();
                foreach (BaseCollectible.CollectibleType type in System.Enum.GetValues(typeof(BaseCollectible.CollectibleType)))
                {
                    typeBreakdown[type] = 0;
                }
            }
        }
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Initialize stats
            stats.Initialize();
            InitializeCollectibleCounts();
        }
        
        private void Start()
        {
            // Get references
            gameManager = Core.GameManager.Instance;
            streetGenerator = FindObjectOfType<Environment.StreetGenerator>();
            
            // Set up collectible parent
            if (collectibleParent == null)
            {
                collectibleParent = new GameObject("Collectibles").transform;
                collectibleParent.SetParent(transform);
            }
            
            // Initialize object pools
            if (enablePooling)
            {
                InitializeObjectPools();
            }
            
            // Subscribe to game events
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += OnGameStateChanged;
            }
            
            nextSpawnTime = Time.time + spawnInterval;
        }
        
        private void Update()
        {
            if (!isSpawning || gameManager == null) return;
            
            // Spawn collectibles
            if (Time.time >= nextSpawnTime && activeCollectibles.Count < maxActiveCollectibles)
            {
                SpawnCollectible();
                ScheduleNextSpawn();
            }
            
            // Clean up destroyed collectibles
            CleanupDestroyedCollectibles();
            
            // Check for bonus events
            if (enableBonusEvents)
            {
                CheckBonusEvents();
            }
            
            // Update statistics
            UpdateStatistics();
        }
        
        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeCollectibleCounts()
        {
            foreach (BaseCollectible.CollectibleType type in System.Enum.GetValues(typeof(BaseCollectible.CollectibleType)))
            {
                collectibleCounts[type] = 0;
            }
        }
        
        private void InitializeObjectPools()
        {
            // Initialize coin pool
            if (coinPrefab != null)
            {
                for (int i = 0; i < initialPoolSize; i++)
                {
                    GameObject coin = Instantiate(coinPrefab, collectibleParent);
                    coin.SetActive(false);
                    coinPool.Enqueue(coin);
                }
            }
            
            // Initialize gem pool
            if (gemPrefab != null)
            {
                for (int i = 0; i < initialPoolSize / 2; i++) // Fewer gems in pool
                {
                    GameObject gem = Instantiate(gemPrefab, collectibleParent);
                    gem.SetActive(false);
                    gemPool.Enqueue(gem);
                }
            }
        }
        
        #endregion
        
        #region Spawning System
        
        public void SpawnCollectible()
        {
            if (!isSpawning) return;
            
            // Determine collectible type based on distribution
            BaseCollectible.CollectibleType typeToSpawn = DetermineCollectibleType();
            
            // Get spawn position
            Vector3 spawnPosition = GetSpawnPosition();
            if (spawnPosition == Vector3.zero) return; // No valid spawn position
            
            // Spawn the collectible
            GameObject collectibleObj = CreateCollectible(typeToSpawn, spawnPosition);
            
            if (collectibleObj != null)
            {
                BaseCollectible collectible = collectibleObj.GetComponent<BaseCollectible>();
                if (collectible != null)
                {
                    RegisterCollectible(collectible);
                    ApplyDifficultyScaling(collectible);
                    
                    stats.totalSpawned++;
                }
            }
        }
        
        private BaseCollectible.CollectibleType DetermineCollectibleType()
        {
            float random = Random.value;
            float difficultyFactor = GetCurrentDifficultyFactor();
            
            // Apply difficulty scaling to rarity
            float adjustedGemChance = distribution.gemChance * difficultyRarityCurve.Evaluate(difficultyFactor);
            
            if (random < adjustedGemChance)
            {
                return DetermineGemType();
            }
            else
            {
                return BaseCollectible.CollectibleType.Coin;
            }
        }
        
        private BaseCollectible.CollectibleType DetermineGemType()
        {
            // During bonus events, increase rare gem chances
            float multiplier = isBonusEventActive ? 2f : 1f;
            
            float random = Random.value;
            float cumulative = 0f;
            
            cumulative += distribution.diamondChance * multiplier;
            if (random < cumulative) return BaseCollectible.CollectibleType.Gem;
            
            cumulative += distribution.emeraldChance * multiplier;
            if (random < cumulative) return BaseCollectible.CollectibleType.Gem;
            
            cumulative += distribution.sapphireChance * multiplier;
            if (random < cumulative) return BaseCollectible.CollectibleType.Gem;
            
            return BaseCollectible.CollectibleType.Gem;
        }
        
        private Vector3 GetSpawnPosition()
        {
            // Get spawn position from street generator or use default
            if (streetGenerator != null)
            {
                // TODO: Implement GetCollectibleSpawnPosition in StreetGenerator
                // return streetGenerator.GetCollectibleSpawnPosition();
            }
            
            // Fallback: spawn ahead of camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 cameraPos = mainCamera.transform.position;
                float spawnX = cameraPos.x + Random.Range(15f, 25f);
                float spawnY = Random.Range(-1f, 3f);
                return new Vector3(spawnX, spawnY, 0f);
            }
            
            return Vector3.zero;
        }
        
        private GameObject CreateCollectible(BaseCollectible.CollectibleType type, Vector3 position)
        {
            GameObject collectibleObj = null;
            
            if (enablePooling)
            {
                collectibleObj = GetFromPool(type);
            }
            
            // Fallback to instantiation if pooling disabled or pool empty
            if (collectibleObj == null)
            {
                GameObject prefab = GetPrefabForType(type);
                if (prefab != null)
                {
                    collectibleObj = Instantiate(prefab, collectibleParent);
                }
            }
            
            if (collectibleObj != null)
            {
                collectibleObj.transform.position = position;
                collectibleObj.SetActive(true);
                
                // Configure specific collectible type
                ConfigureCollectible(collectibleObj, type);
            }
            
            return collectibleObj;
        }
        
        private void ConfigureCollectible(GameObject collectibleObj, BaseCollectible.CollectibleType type)
        {
            if (type == BaseCollectible.CollectibleType.Coin)
            {
                var coin = collectibleObj.GetComponent<CoinCollectible>();
                if (coin != null)
                {
                    coin.SetCoinType(DetermineCoinType());
                }
            }
            else if (type == BaseCollectible.CollectibleType.Gem)
            {
                var gem = collectibleObj.GetComponent<GemCollectible>();
                if (gem != null)
                {
                    gem.SetGemType(DetermineGemSubType());
                }
            }
        }
        
        private CoinCollectible.CoinType DetermineCoinType()
        {
            float random = Random.value;
            float difficultyFactor = GetCurrentDifficultyFactor();
            
            // Increase chances for better coins with difficulty
            float adjustedGoldChance = distribution.goldCoinChance * (1f + difficultyFactor);
            float adjustedSilverChance = distribution.silverCoinChance * (1f + difficultyFactor * 0.5f);
            
            if (random < adjustedGoldChance)
                return CoinCollectible.CoinType.Gold;
            else if (random < adjustedGoldChance + adjustedSilverChance)
                return CoinCollectible.CoinType.Silver;
            else
                return CoinCollectible.CoinType.Bronze;
        }
        
        private GemCollectible.GemType DetermineGemSubType()
        {
            float random = Random.value;
            float multiplier = isBonusEventActive ? 2f : 1f;
            
            if (random < distribution.diamondChance * multiplier)
                return GemCollectible.GemType.Diamond;
            else if (random < (distribution.diamondChance + distribution.emeraldChance) * multiplier)
                return GemCollectible.GemType.Emerald;
            else if (random < (distribution.diamondChance + distribution.emeraldChance + distribution.sapphireChance) * multiplier)
                return GemCollectible.GemType.Sapphire;
            else
                return GemCollectible.GemType.Ruby;
        }
        
        private void ScheduleNextSpawn()
        {
            float interval = spawnInterval + Random.Range(-spawnVariance, spawnVariance);
            
            // Adjust spawn rate based on game speed
            if (gameManager != null)
            {
                interval /= gameManager.CurrentGameSpeed;
            }
            
            nextSpawnTime = Time.time + interval;
        }
        
        #endregion
        
        #region Object Pooling
        
        private GameObject GetFromPool(BaseCollectible.CollectibleType type)
        {
            Queue<GameObject> pool = null;
            
            switch (type)
            {
                case BaseCollectible.CollectibleType.Coin:
                    pool = coinPool;
                    break;
                case BaseCollectible.CollectibleType.Gem:
                    pool = gemPool;
                    break;
            }
            
            if (pool != null && pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                
                // Reset the collectible
                var collectible = obj.GetComponent<BaseCollectible>();
                if (collectible != null)
                {
                    collectible.ResetCollectible();
                }
                
                return obj;
            }
            
            // Expand pool if enabled and empty
            if (expandPoolDynamically && pool != null)
            {
                return CreateNewPoolObject(type);
            }
            
            return null;
        }
        
        private GameObject CreateNewPoolObject(BaseCollectible.CollectibleType type)
        {
            GameObject prefab = GetPrefabForType(type);
            if (prefab != null)
            {
                return Instantiate(prefab, collectibleParent);
            }
            
            return null;
        }
        
        public void ReturnToPool(BaseCollectible collectible)
        {
            if (!enablePooling || collectible == null) return;
            
            GameObject obj = collectible.gameObject;
            obj.SetActive(false);
            
            // Add back to appropriate pool
            switch (collectible.Type)
            {
                case BaseCollectible.CollectibleType.Coin:
                    coinPool.Enqueue(obj);
                    break;
                case BaseCollectible.CollectibleType.Gem:
                    gemPool.Enqueue(obj);
                    break;
            }
        }
        
        private GameObject GetPrefabForType(BaseCollectible.CollectibleType type)
        {
            switch (type)
            {
                case BaseCollectible.CollectibleType.Coin:
                    return coinPrefab;
                case BaseCollectible.CollectibleType.Gem:
                    return gemPrefab;
                default:
                    return coinPrefab;
            }
        }
        
        #endregion
        
        #region Collectible Management
        
        private void RegisterCollectible(BaseCollectible collectible)
        {
            activeCollectibles.Add(collectible);
            collectibleCounts[collectible.Type]++;
            
            // Subscribe to events
            collectible.OnCollected += OnCollectibleCollected;
            collectible.OnDestroyed += OnCollectibleDestroyed;
        }
        
        private void OnCollectibleCollected(BaseCollectible collectible)
        {
            stats.totalCollected++;
            stats.totalValue += collectible.Value;
            
            if (stats.typeBreakdown.ContainsKey(collectible.Type))
            {
                stats.typeBreakdown[collectible.Type]++;
            }
            
            // Type-specific tracking
            switch (collectible.Type)
            {
                case BaseCollectible.CollectibleType.Coin:
                    stats.coinsCollected++;
                    break;
                case BaseCollectible.CollectibleType.Gem:
                    stats.gemsCollected++;
                    break;
            }
        }
        
        private void OnCollectibleDestroyed(BaseCollectible collectible)
        {
            // Unsubscribe from events
            collectible.OnCollected -= OnCollectibleCollected;
            collectible.OnDestroyed -= OnCollectibleDestroyed;
            
            // Return to pool if pooling is enabled
            if (enablePooling)
            {
                ReturnToPool(collectible);
            }
        }
        
        private void CleanupDestroyedCollectibles()
        {
            activeCollectibles.RemoveAll(c => c == null || c.IsCollected);
            
            // Update counts
            foreach (var type in collectibleCounts.Keys.ToList())
            {
                collectibleCounts[type] = activeCollectibles.Count(c => c.Type == type);
            }
        }
        
        #endregion
        
        #region Difficulty Scaling
        
        private float GetCurrentDifficultyFactor()
        {
            if (!enableDifficultyScaling || gameManager == null) return 0f;
            
            // Base difficulty on game time and speed
            float timeFactor = gameManager.GameTime / 60f; // Normalize to minutes
            float speedFactor = (gameManager.CurrentGameSpeed - 1f) / 2f; // Normalize speed
            
            return Mathf.Clamp01(timeFactor + speedFactor);
        }
        
        private void ApplyDifficultyScaling(BaseCollectible collectible)
        {
            if (!enableDifficultyScaling) return;
            
            float difficultyFactor = GetCurrentDifficultyFactor();
            
            // Increase value based on difficulty
            float valueMultiplier = difficultyValueCurve.Evaluate(difficultyFactor);
            int newValue = Mathf.RoundToInt(collectible.Value * valueMultiplier);
            collectible.SetValue(newValue);
        }
        
        #endregion
        
        #region Bonus Events
        
        private void CheckBonusEvents()
        {
            if (Time.time - lastBonusEventTime >= bonusEventInterval && !isBonusEventActive)
            {
                StartBonusEvent();
            }
        }
        
        private void StartBonusEvent()
        {
            isBonusEventActive = true;
            lastBonusEventTime = Time.time;
            
            bonusEventCoroutine = StartCoroutine(BonusEventCoroutine());
            
            // TODO: Notify UI when UI system is implemented
            // var uiManager = FindObjectOfType<UI.UIManager>();
            // if (uiManager != null)
            // {
            //     uiManager.ShowBonusEventNotification("BONUS TIME!", bonusEventDuration);
            // }
        }
        
        private IEnumerator BonusEventCoroutine()
        {
            yield return new WaitForSeconds(bonusEventDuration);
            
            isBonusEventActive = false;
            bonusEventCoroutine = null;
        }
        
        #endregion
        
        #region Statistics
        
        private void UpdateStatistics()
        {
            if (stats.totalSpawned > 0)
            {
                stats.collectionRate = (float)stats.totalCollected / stats.totalSpawned;
            }
        }
        
        public CollectibleStats GetStatistics()
        {
            return stats;
        }
        
        public void ResetStatistics()
        {
            stats = new CollectibleStats();
            stats.Initialize();
        }
        
        #endregion
        
        #region Game State Management
        
        private void OnGameStateChanged(Core.GameManager.GameState newState)
        {
            switch (newState)
            {
                case Core.GameManager.GameState.Playing:
                    StartSpawning();
                    break;
                    
                case Core.GameManager.GameState.Paused:
                case Core.GameManager.GameState.GameOver:
                case Core.GameManager.GameState.MainMenu:
                    StopSpawning();
                    break;
            }
        }
        
        public void StartSpawning()
        {
            isSpawning = true;
            nextSpawnTime = Time.time + spawnInterval;
        }
        
        public void StopSpawning()
        {
            isSpawning = false;
        }
        
        public void ClearAllCollectibles()
        {
            foreach (var collectible in activeCollectibles.ToList())
            {
                if (collectible != null)
                {
                    collectible.DestroyCollectible();
                }
            }
            
            activeCollectibles.Clear();
            InitializeCollectibleCounts();
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Force spawn a specific type of collectible at position
        /// </summary>
        public BaseCollectible SpawnSpecificCollectible(BaseCollectible.CollectibleType type, Vector3 position)
        {
            GameObject obj = CreateCollectible(type, position);
            return obj?.GetComponent<BaseCollectible>();
        }
        
        /// <summary>
        /// Get all active collectibles of a specific type
        /// </summary>
        public List<BaseCollectible> GetCollectiblesOfType(BaseCollectible.CollectibleType type)
        {
            return activeCollectibles.Where(c => c.Type == type).ToList();
        }
        
        /// <summary>
        /// Get count of active collectibles by type
        /// </summary>
        public int GetCollectibleCount(BaseCollectible.CollectibleType type)
        {
            return collectibleCounts.ContainsKey(type) ? collectibleCounts[type] : 0;
        }
        
        /// <summary>
        /// Set spawn rate multiplier
        /// </summary>
        public void SetSpawnRateMultiplier(float multiplier)
        {
            spawnInterval = spawnInterval / multiplier;
        }
        
        #endregion
        
        #region Debug
        
        public string GetDebugInfo()
        {
            return $"Collectible Manager Debug\n" +
                   $"Active Collectibles: {activeCollectibles.Count}/{maxActiveCollectibles}\n" +
                   $"Coins: {GetCollectibleCount(BaseCollectible.CollectibleType.Coin)}\n" +
                   $"Gems: {GetCollectibleCount(BaseCollectible.CollectibleType.Gem)}\n" +
                   $"Spawning: {isSpawning}\n" +
                   $"Next Spawn: {Mathf.Max(0f, nextSpawnTime - Time.time):F1}s\n" +
                   $"Bonus Event: {isBonusEventActive}\n" +
                   $"Collection Rate: {stats.collectionRate:P1}";
        }
        
        #endregion
    }
}