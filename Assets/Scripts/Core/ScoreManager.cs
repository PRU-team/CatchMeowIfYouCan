using UnityEngine;

namespace CatchMeowIfYouCan.Core
{
    /// <summary>
    /// Manages the game scoring system, coins, and high scores
    /// Handles score calculations, multipliers, and persistent data
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        [Header("Score Settings")]
        [SerializeField] private int scorePerSecond = 10;
        [SerializeField] private int coinValue = 10;
        [SerializeField] private int distanceScoreMultiplier = 1;
        
        [Header("Multiplier Settings")]
        [SerializeField] private float baseMultiplier = 1f;
        [SerializeField] private float maxMultiplier = 5f;
        [SerializeField] private float multiplierIncreaseRate = 0.1f;
        [SerializeField] private float multiplierDecayRate = 0.05f;
        [SerializeField] private float multiplierDecayDelay = 2f;
        
        [Header("Combo System")]
        [SerializeField] private int comboThreshold = 5;
        [SerializeField] private float comboTimeWindow = 3f;
        [SerializeField] private float comboBonusMultiplier = 1.5f;
        
        [Header("High Score")]
        [SerializeField] private string highScoreKey = "HighScore";
        [SerializeField] private string totalCoinsKey = "TotalCoins";
        [SerializeField] private string gamesPlayedKey = "GamesPlayed";
        
        // Current game stats
        public int CurrentScore { get; private set; } = 0;
        public int CurrentCoins { get; private set; } = 0;
        public float CurrentMultiplier { get; private set; } = 1f;
        public int CurrentCombo { get; private set; } = 0;
        public float DistanceTraveled { get; private set; } = 0f;
        
        // Persistent stats
        public int HighScore { get; private set; } = 0;
        public int TotalCoins { get; private set; } = 0;
        public int GamesPlayed { get; private set; } = 0;
        
        // Score tracking
        private float lastScoreTime;
        private float lastMultiplierUpdate;
        private float lastComboTime;
        private int consecutiveCollections = 0;
        private bool isComboActive = false;
        
        // Events
        public System.Action<int> OnScoreChanged;
        public System.Action<int> OnCoinsChanged;
        public System.Action<float> OnMultiplierChanged;
        public System.Action<int> OnComboChanged;
        public System.Action<bool> OnNewHighScore;
        public System.Action<int> OnDistanceChanged;
        
        private void Awake()
        {
            LoadPersistentData();
        }
        
        private void Start()
        {
            ResetCurrentGameStats();
        }
        
        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameActive())
            {
                UpdateDistanceScore();
                UpdateMultiplier();
                UpdateCombo();
            }
        }
        
        #region Score Management
        
        /// <summary>
        /// Add points to the current score
        /// </summary>
        public void AddScore(int points)
        {
            if (points <= 0) return;
            
            int finalPoints = Mathf.RoundToInt(points * CurrentMultiplier);
            CurrentScore += finalPoints;
            
            OnScoreChanged?.Invoke(CurrentScore);
            
            // Check for high score
            if (CurrentScore > HighScore)
            {
                bool wasNewHighScore = HighScore == 0; // First time
                HighScore = CurrentScore;
                SaveHighScore();
                
                if (!wasNewHighScore)
                {
                    OnNewHighScore?.Invoke(true);
                }
            }
        }
        
        /// <summary>
        /// Add coins to the current collection
        /// </summary>
        public void AddCoins(int coins)
        {
            if (coins <= 0) return;
            
            CurrentCoins += coins;
            TotalCoins += coins;
            
            OnCoinsChanged?.Invoke(CurrentCoins);
            
            // Increase multiplier for coin collection
            IncreaseMultiplier();
            
            // Update combo
            UpdateComboOnCollection();
            
            // Add score bonus for coins
            AddScore(coins * 2); // Coins give 2x their value in score
            
            SaveTotalCoins();
        }
        
        /// <summary>
        /// Reset current game statistics
        /// </summary>
        public void ResetScore()
        {
            ResetCurrentGameStats();
            IncrementGamesPlayed();
        }
        
        private void ResetCurrentGameStats()
        {
            CurrentScore = 0;
            CurrentCoins = 0;
            CurrentMultiplier = baseMultiplier;
            CurrentCombo = 0;
            DistanceTraveled = 0f;
            consecutiveCollections = 0;
            isComboActive = false;
            lastScoreTime = 0f;
            lastMultiplierUpdate = 0f;
            lastComboTime = 0f;
            
            // Notify UI
            OnScoreChanged?.Invoke(CurrentScore);
            OnCoinsChanged?.Invoke(CurrentCoins);
            OnMultiplierChanged?.Invoke(CurrentMultiplier);
            OnComboChanged?.Invoke(CurrentCombo);
            OnDistanceChanged?.Invoke(0);
        }
        
        #endregion
        
        #region Distance and Time Scoring
        
        private void UpdateDistanceScore()
        {
            if (GameManager.Instance == null) return;
            
            float gameTime = GameManager.Instance.GameTime;
            
            // Add distance-based score every second
            if (gameTime - lastScoreTime >= 1f)
            {
                AddScore(scorePerSecond);
                DistanceTraveled += GameManager.Instance.CurrentGameSpeed * 10f; // Approximate distance
                
                OnDistanceChanged?.Invoke(Mathf.RoundToInt(DistanceTraveled));
                lastScoreTime = gameTime;
            }
        }
        
        #endregion
        
        #region Multiplier System
        
        private void IncreaseMultiplier()
        {
            CurrentMultiplier = Mathf.Min(maxMultiplier, CurrentMultiplier + multiplierIncreaseRate);
            lastMultiplierUpdate = Time.time;
            OnMultiplierChanged?.Invoke(CurrentMultiplier);
        }
        
        private void UpdateMultiplier()
        {
            // Decay multiplier over time if no recent collections
            if (Time.time - lastMultiplierUpdate > multiplierDecayDelay)
            {
                if (CurrentMultiplier > baseMultiplier)
                {
                    CurrentMultiplier = Mathf.Max(baseMultiplier, 
                        CurrentMultiplier - multiplierDecayRate * Time.deltaTime);
                    OnMultiplierChanged?.Invoke(CurrentMultiplier);
                }
            }
        }
        
        #endregion
        
        #region Combo System
        
        private void UpdateComboOnCollection()
        {
            consecutiveCollections++;
            lastComboTime = Time.time;
            
            if (consecutiveCollections >= comboThreshold)
            {
                if (!isComboActive)
                {
                    isComboActive = true;
                    CurrentMultiplier *= comboBonusMultiplier;
                    OnMultiplierChanged?.Invoke(CurrentMultiplier);
                }
                
                CurrentCombo = consecutiveCollections;
                OnComboChanged?.Invoke(CurrentCombo);
            }
        }
        
        private void UpdateCombo()
        {
            // Reset combo if too much time has passed without collection
            if (isComboActive && Time.time - lastComboTime > comboTimeWindow)
            {
                ResetCombo();
            }
        }
        
        private void ResetCombo()
        {
            if (isComboActive)
            {
                CurrentMultiplier /= comboBonusMultiplier;
                OnMultiplierChanged?.Invoke(CurrentMultiplier);
            }
            
            consecutiveCollections = 0;
            CurrentCombo = 0;
            isComboActive = false;
            OnComboChanged?.Invoke(CurrentCombo);
        }
        
        #endregion
        
        #region Persistent Data
        
        private void LoadPersistentData()
        {
            HighScore = PlayerPrefs.GetInt(highScoreKey, 0);
            TotalCoins = PlayerPrefs.GetInt(totalCoinsKey, 0);
            GamesPlayed = PlayerPrefs.GetInt(gamesPlayedKey, 0);
        }
        
        private void SaveHighScore()
        {
            PlayerPrefs.SetInt(highScoreKey, HighScore);
            PlayerPrefs.Save();
        }
        
        private void SaveTotalCoins()
        {
            PlayerPrefs.SetInt(totalCoinsKey, TotalCoins);
            PlayerPrefs.Save();
        }
        
        private void IncrementGamesPlayed()
        {
            GamesPlayed++;
            PlayerPrefs.SetInt(gamesPlayedKey, GamesPlayed);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Clear all persistent data (for testing)
        /// </summary>
        public void ClearAllData()
        {
            PlayerPrefs.DeleteKey(highScoreKey);
            PlayerPrefs.DeleteKey(totalCoinsKey);
            PlayerPrefs.DeleteKey(gamesPlayedKey);
            PlayerPrefs.Save();
            
            LoadPersistentData();
            ResetCurrentGameStats();
        }
        
        #endregion
        
        #region Bonus Systems
        
        /// <summary>
        /// Apply bonus score for special actions
        /// </summary>
        public void AddBonusScore(string bonusType, int basePoints)
        {
            int bonusPoints = basePoints;
            
            switch (bonusType.ToLower())
            {
                case "nearmiss":
                    bonusPoints = Mathf.RoundToInt(basePoints * 1.5f);
                    break;
                    
                case "perfectjump":
                    bonusPoints = Mathf.RoundToInt(basePoints * 2f);
                    break;
                    
                case "powerupchain":
                    bonusPoints = Mathf.RoundToInt(basePoints * 3f);
                    break;
                    
                case "survivalbonus":
                    bonusPoints = Mathf.RoundToInt(basePoints * CurrentMultiplier);
                    break;
            }
            
            AddScore(bonusPoints);
        }
        
        /// <summary>
        /// Calculate end-game bonus
        /// </summary>
        public int CalculateEndGameBonus()
        {
            int bonus = 0;
            
            // Time survival bonus
            if (GameManager.Instance != null)
            {
                bonus += Mathf.RoundToInt(GameManager.Instance.GameTime * 5f);
            }
            
            // Distance bonus
            bonus += Mathf.RoundToInt(DistanceTraveled * 0.1f);
            
            // Coin collection bonus
            bonus += CurrentCoins * 5;
            
            // Combo bonus
            if (CurrentCombo > 0)
            {
                bonus += CurrentCombo * 50;
            }
            
            return bonus;
        }
        
        #endregion
        
        #region Statistics
        
        /// <summary>
        /// Get comprehensive game statistics
        /// </summary>
        public GameStatistics GetGameStatistics()
        {
            return new GameStatistics
            {
                currentScore = CurrentScore,
                currentCoins = CurrentCoins,
                highScore = HighScore,
                totalCoins = TotalCoins,
                gamesPlayed = GamesPlayed,
                distanceTraveled = DistanceTraveled,
                currentMultiplier = CurrentMultiplier,
                currentCombo = CurrentCombo,
                survivalTime = GameManager.Instance?.GameTime ?? 0f
            };
        }
        
        /// <summary>
        /// Calculate average score per game
        /// </summary>
        public float GetAverageScore()
        {
            return GamesPlayed > 0 ? (float)HighScore / GamesPlayed : 0f;
        }
        
        /// <summary>
        /// Get coins per minute rate
        /// </summary>
        public float GetCoinsPerMinute()
        {
            float gameTime = GameManager.Instance?.GameTime ?? 0f;
            return gameTime > 0 ? (CurrentCoins / gameTime) * 60f : 0f;
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get formatted score string
        /// </summary>
        public string GetFormattedScore(int score = -1)
        {
            int scoreToFormat = score == -1 ? CurrentScore : score;
            return scoreToFormat.ToString("N0");
        }
        
        /// <summary>
        /// Get formatted distance string
        /// </summary>
        public string GetFormattedDistance()
        {
            return $"{DistanceTraveled:F0}m";
        }
        
        /// <summary>
        /// Check if player has enough coins for purchase
        /// </summary>
        public bool CanAfford(int cost)
        {
            return TotalCoins >= cost;
        }
        
        /// <summary>
        /// Spend coins (for shop system)
        /// </summary>
        public bool SpendCoins(int cost)
        {
            if (CanAfford(cost))
            {
                TotalCoins -= cost;
                SaveTotalCoins();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Get current score (for UI integration)
        /// </summary>
        public int GetCurrentScore()
        {
            return CurrentScore;
        }
        
        /// <summary>
        /// Get high score (for UI integration)
        /// </summary>
        public int GetHighScore()
        {
            return HighScore;
        }
        
        /// <summary>
        /// Get score multiplier (for UI integration)
        /// </summary>
        public float GetScoreMultiplier()
        {
            return CurrentMultiplier;
        }
        
        /// <summary>
        /// Get current coin count (for UI integration)
        /// </summary>
        public int GetCurrentCoins()
        {
            return CurrentCoins;
        }
        
        /// <summary>
        /// Get total coins (for UI integration)
        /// </summary>
        public int GetTotalCoins()
        {
            return TotalCoins;
        }
        
        #endregion
        
        [System.Serializable]
        public struct GameStatistics
        {
            public int currentScore;
            public int currentCoins;
            public int highScore;
            public int totalCoins;
            public int gamesPlayed;
            public float distanceTraveled;
            public float currentMultiplier;
            public int currentCombo;
            public float survivalTime;
        }
    }
}