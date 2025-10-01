using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.Core
{
    /// <summary>
    /// Main game manager that controls the overall game flow and state
    /// Handles game states, progression, and coordination between all systems
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private float gameStartDelay = 2f;
        [SerializeField] private float gameOverDelay = 3f;
        [SerializeField] private bool autoStart = true;
        
        [Header("Game Progression")]
        [SerializeField] private float speedIncreaseInterval = 30f; // Increase speed every 30 seconds
        [SerializeField] private float speedIncreaseAmount = 0.5f;
        [SerializeField] private float maxGameSpeed = 3f;
        [SerializeField] private float difficultyIncreaseRate = 0.1f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        
        // Game State
        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public float GameTime { get; private set; } = 0f;
        public float CurrentGameSpeed { get; private set; } = 1f;
        public int Lives { get; private set; } = 3;
        
        // Singleton instance
        public static GameManager Instance { get; private set; }
        
        // Component references
        private ScoreManager scoreManager;
        private AudioManager audioManager;
        private CatchMeowIfYouCan.Player.CatController player;
        private CatchMeowIfYouCan.Enemies.CatcherManager catcherManager;
        
        // Progression tracking
        private float lastSpeedIncreaseTime;
        private float pausedTime;
        private bool isPaused = false;
        
        // Events for UI and other systems
        public System.Action<GameState> OnGameStateChanged;
        public System.Action<float> OnGameSpeedChanged;
        public System.Action<int> OnLivesChanged;
        public System.Action OnGameOver;
        public System.Action OnGameStart;
        public System.Action OnGamePaused;
        public System.Action OnGameResumed;
        
        public enum GameState
        {
            MainMenu,
            Starting,
            Playing,
            Paused,
            GameOver,
            Restarting
        }
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            FindComponents();
            SetupEventListeners();
            
            if (autoStart)
            {
                StartCoroutine(DelayedStart());
            }
        }
        
        private void Update()
        {
            HandleInput();
            UpdateGameTime();
            UpdateGameProgression();
            
            if (showDebugInfo)
            {
                DrawDebugInfo();
            }
        }
        
        #region Initialization
        
        private void InitializeGame()
        {
            // Set target frame rate
            Application.targetFrameRate = 60;
            
            // Initialize game values
            CurrentGameSpeed = 1f;
            Lives = 3;
            GameTime = 0f;
            
            Debug.Log("GameManager initialized");
        }
        
        private void FindComponents()
        {
            // Find core managers
            scoreManager = FindFirstObjectByType<ScoreManager>();
            audioManager = FindFirstObjectByType<AudioManager>();
            
            // Find game objects
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<CatchMeowIfYouCan.Player.CatController>();
            }
            
            // Find CatcherManager
            catcherManager = FindFirstObjectByType<CatchMeowIfYouCan.Enemies.CatcherManager>();
        }
        
        private void SetupEventListeners()
        {
            // Listen to player events
            if (player != null)
            {
                player.OnDeath += HandlePlayerDeath;
                player.OnCoinCollected += HandleCoinCollected;
                player.OnPowerUpCollected += HandlePowerUpCollected;
            }
            
            // Listen to catcher events
            if (catcherManager != null)
            {
                catcherManager.OnAnyCatCaught += HandlePlayerCaught;
            }
        }
        
        #endregion
        
        #region Game State Management
        
        private void ChangeGameState(GameState newState)
        {
            if (CurrentState == newState) return;
            
            GameState previousState = CurrentState;
            CurrentState = newState;
            
            Debug.Log($"Game state changed: {previousState} -> {newState}");
            
            HandleStateTransition(previousState, newState);
            OnGameStateChanged?.Invoke(newState);
        }
        
        private void HandleStateTransition(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.Starting:
                    StartCoroutine(StartGameSequence());
                    break;
                    
                case GameState.Playing:
                    ResumeGame();
                    break;
                    
                case GameState.Paused:
                    PauseGame();
                    break;
                    
                case GameState.GameOver:
                    StartCoroutine(GameOverSequence());
                    break;
                    
                case GameState.Restarting:
                    StartCoroutine(RestartSequence());
                    break;
            }
        }
        
        #endregion
        
        #region Game Flow Control
        
        private IEnumerator DelayedStart()
        {
            yield return new WaitForSeconds(1f);
            StartGame();
        }
        
        public void StartGame()
        {
            if (CurrentState != GameState.MainMenu && CurrentState != GameState.GameOver) return;
            
            ChangeGameState(GameState.Starting);
        }
        
        private IEnumerator StartGameSequence()
        {
            // Reset game values
            GameTime = 0f;
            CurrentGameSpeed = 1f;
            lastSpeedIncreaseTime = 0f;
            
            // Initialize player and catcher
            if (player != null)
            {
                player.ResetPlayer();
            }
            
            if (catcherManager != null)
            {
                catcherManager.ResetAllCatchers();
            }
            
            // Play start sound
            if (audioManager != null)
            {
                audioManager.PlayGameStartSound();
            }
            
            // Wait for start delay
            yield return new WaitForSeconds(gameStartDelay);
            
            // Start the game
            ChangeGameState(GameState.Playing);
            OnGameStart?.Invoke();
        }
        
        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            
            isPaused = true;
            Time.timeScale = 0f;
            pausedTime = Time.unscaledTime;
            
            if (audioManager != null)
            {
                audioManager.PauseMusic();
            }
            
            OnGamePaused?.Invoke();
        }
        
        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused && !isPaused) return;
            
            isPaused = false;
            Time.timeScale = 1f;
            
            if (audioManager != null)
            {
                audioManager.ResumeMusic();
            }
            
            OnGameResumed?.Invoke();
        }
        
        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
            {
                ChangeGameState(GameState.Paused);
            }
            else if (CurrentState == GameState.Paused)
            {
                ChangeGameState(GameState.Playing);
            }
        }
        
        private IEnumerator GameOverSequence()
        {
            // Stop all game objects
            if (player != null)
            {
                // Player doesn't have SetActive, just stop movement
                // This will be handled by the player's IsAlive state
            }
            
            if (catcherManager != null)
            {
                catcherManager.ResetAllCatchers();
            }
            
            // Play game over sound
            if (audioManager != null)
            {
                audioManager.PlayGameOverSound();
            }
            
            // Wait before showing game over UI
            yield return new WaitForSeconds(gameOverDelay);
            
            OnGameOver?.Invoke();
        }
        
        public void RestartGame()
        {
            ChangeGameState(GameState.Restarting);
        }
        
        private IEnumerator RestartSequence()
        {
            // Reset score
            if (scoreManager != null)
            {
                scoreManager.ResetScore();
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // Restart the game
            ChangeGameState(GameState.Starting);
        }
        
        public void QuitToMainMenu()
        {
            Time.timeScale = 1f;
            ChangeGameState(GameState.MainMenu);
            
            if (audioManager != null)
            {
                audioManager.PlayMenuMusic();
            }
        }
        
        #endregion
        
        #region Game Progression
        
        private void UpdateGameTime()
        {
            if (CurrentState == GameState.Playing && !isPaused)
            {
                GameTime += Time.deltaTime;
            }
        }
        
        private void UpdateGameProgression()
        {
            if (CurrentState != GameState.Playing) return;
            
            // Increase game speed over time
            if (GameTime - lastSpeedIncreaseTime >= speedIncreaseInterval)
            {
                IncreaseGameSpeed();
                lastSpeedIncreaseTime = GameTime;
            }
        }
        
        private void IncreaseGameSpeed()
        {
            if (CurrentGameSpeed < maxGameSpeed)
            {
                CurrentGameSpeed += speedIncreaseAmount;
                OnGameSpeedChanged?.Invoke(CurrentGameSpeed);
                
                // Speed increase handled by individual systems
                // CatcherManager will adapt to game speed automatically
                
                Debug.Log($"Game speed increased to: {CurrentGameSpeed:F1}");
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandlePlayerDeath()
        {
            Lives--;
            OnLivesChanged?.Invoke(Lives);
            
            if (Lives <= 0)
            {
                ChangeGameState(GameState.GameOver);
            }
            else
            {
                // Player has lives left, restart from checkpoint
                StartCoroutine(RespawnPlayer());
            }
        }
        
        private void HandlePlayerCaught(CatchMeowIfYouCan.Enemies.CatcherController catcher)
        {
            // Same as player death
            HandlePlayerDeath();
        }
        
        private void HandleCoinCollected(int coinValue)
        {
            if (scoreManager != null)
            {
                scoreManager.AddCoins(coinValue);
            }
        }
        
        private void HandlePowerUpCollected(string powerUpType)
        {
            if (scoreManager != null)
            {
                scoreManager.AddScore(50); // Bonus points for power-up
            }
            
            if (audioManager != null)
            {
                audioManager.PlayPowerUpSound();
            }
        }
        
        private IEnumerator RespawnPlayer()
        {
            yield return new WaitForSeconds(2f);
            
            if (player != null)
            {
                player.ResetPlayer();
            }
            
            if (catcherManager != null)
            {
                catcherManager.ResetAllCatchers();
            }
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                TogglePause();
            }
            
            // Debug keys
            if (showDebugInfo)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    RestartGame();
                }
                
                if (Input.GetKeyDown(KeyCode.G))
                {
                    ChangeGameState(GameState.GameOver);
                }
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Check if game is currently active (playing)
        /// </summary>
        public bool IsGameActive()
        {
            return CurrentState == GameState.Playing && !isPaused;
        }
        
        /// <summary>
        /// Get formatted game time string
        /// </summary>
        public string GetFormattedGameTime()
        {
            int minutes = Mathf.FloorToInt(GameTime / 60f);
            int seconds = Mathf.FloorToInt(GameTime % 60f);
            return $"{minutes:00}:{seconds:00}";
        }
        
        /// <summary>
        /// Set number of lives
        /// </summary>
        public void SetLives(int lives)
        {
            Lives = lives;
            OnLivesChanged?.Invoke(Lives);
        }
        
        /// <summary>
        /// Add extra life
        /// </summary>
        public void AddLife()
        {
            Lives++;
            OnLivesChanged?.Invoke(Lives);
        }
        
        /// <summary>
        /// Return to main menu (for UI integration)
        /// </summary>
        public void ReturnToMainMenu()
        {
            // Reset game variables
            GameTime = 0f;
            CurrentGameSpeed = 1f;
            Lives = 3;
            isPaused = false;
            
            // Change to main menu state
            ChangeGameState(GameState.MainMenu);
        }
        
        #endregion
        
        #region Debug
        
        private void DrawDebugInfo()
        {
            // This would be better implemented with Unity's UI system
            // For now, just log important info
        }
        
        public string GetDebugInfo()
        {
            return $"State: {CurrentState}\n" +
                   $"Time: {GetFormattedGameTime()}\n" +
                   $"Speed: {CurrentGameSpeed:F2}\n" +
                   $"Lives: {Lives}\n" +
                   $"Paused: {isPaused}";
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Cleanup events
            if (player != null)
            {
                player.OnDeath -= HandlePlayerDeath;
                player.OnCoinCollected -= HandleCoinCollected;
                player.OnPowerUpCollected -= HandlePowerUpCollected;
            }
            
            if (catcherManager != null)
            {
                catcherManager.OnAnyCatCaught -= HandlePlayerCaught;
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && CurrentState == GameState.Playing)
            {
                ChangeGameState(GameState.Paused);
            }
        }
    }
}