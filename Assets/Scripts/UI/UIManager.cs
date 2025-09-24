using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using CatchMeowIfYouCan.Core;

namespace CatchMeowIfYouCan.UI
{
    /// <summary>
    /// Central UI manager that handles all UI screens and transitions
    /// Manages main menu, gameplay HUD, pause menu, game over screen, and settings
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI Screens")]
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject gameplayScreen;
        [SerializeField] private GameObject pauseMenuScreen;
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private GameObject settingsScreen;
        [SerializeField] private GameObject loadingScreen;
        
        [Header("UI Components")]
        [SerializeField] private MainMenuUI mainMenuUI;
        [SerializeField] private GameplayUI gameplayUI;
        [SerializeField] private PauseMenuUI pauseMenuUI;
        [SerializeField] private GameOverUI gameOverUI;
        [SerializeField] private SettingsUI settingsUI;
        
        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 0.3f;
        [SerializeField] private bool useTransitionEffects = true;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Audio Settings")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip screenTransitionSound;
        [SerializeField] private float uiAudioVolume = 0.8f;
        
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugMode = false;
        [SerializeField] private bool logUITransitions = true;
        
        // Current state
        private UIScreen currentScreen = UIScreen.MainMenu;
        private UIScreen previousScreen = UIScreen.None;
        private bool isTransitioning = false;
        private bool isPaused = false;
        
        // Core managers
        private GameManager gameManager;
        private AudioManager audioManager;
        private ScoreManager scoreManager;
        
        // UI state stack for returning to previous screens
        private Stack<UIScreen> screenStack = new Stack<UIScreen>();
        
        // Events
        public System.Action<UIScreen> OnScreenChanged;
        public System.Action OnGamePaused;
        public System.Action OnGameResumed;
        
        // Properties
        public UIScreen CurrentScreen => currentScreen;
        public bool IsTransitioning => isTransitioning;
        public bool IsPaused => isPaused;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeUIManager();
        }
        
        private void Start()
        {
            SetupInitialUI();
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeUIManager()
        {
            // Find core managers
            gameManager = FindObjectOfType<GameManager>();
            audioManager = FindObjectOfType<AudioManager>();
            scoreManager = FindObjectOfType<ScoreManager>();
            
            // Auto-find UI components if not assigned
            FindUIComponents();
            
            // Setup event listeners
            SetupEventListeners();
            
            if (logUITransitions)
            {
                Debug.Log("UIManager initialized successfully");
            }
        }
        
        private void FindUIComponents()
        {
            // Auto-find UI components if not manually assigned
            if (mainMenuUI == null)
                mainMenuUI = FindObjectOfType<MainMenuUI>();
            if (gameplayUI == null)
                gameplayUI = FindObjectOfType<GameplayUI>();
            if (pauseMenuUI == null)
                pauseMenuUI = FindObjectOfType<PauseMenuUI>();
            if (gameOverUI == null)
                gameOverUI = FindObjectOfType<GameOverUI>();
            if (settingsUI == null)
                settingsUI = FindObjectOfType<SettingsUI>();
        }
        
        private void SetupEventListeners()
        {
            // Subscribe to game manager events
            if (gameManager != null)
            {
                // gameManager.OnGameStateChanged += OnGameStateChanged;
                // gameManager.OnGameStarted += OnGameStarted;
                // gameManager.OnGameEnded += OnGameEnded;
            }
            
            // Setup UI component events
            SetupUIComponentEvents();
        }
        
        private void SetupUIComponentEvents()
        {
            // Main Menu Events
            if (mainMenuUI != null)
            {
                mainMenuUI.OnPlayButtonClicked += StartGame;
                mainMenuUI.OnSettingsButtonClicked += ShowSettings;
                mainMenuUI.OnQuitButtonClicked += QuitGame;
            }
            
            // Gameplay Events
            if (gameplayUI != null)
            {
                gameplayUI.OnPauseButtonClicked += PauseGame;
            }
            
            // Pause Menu Events
            if (pauseMenuUI != null)
            {
                pauseMenuUI.OnResumeButtonClicked += ResumeGame;
                pauseMenuUI.OnRestartButtonClicked += RestartGame;
                pauseMenuUI.OnMainMenuButtonClicked += ReturnToMainMenu;
                pauseMenuUI.OnSettingsButtonClicked += ShowSettingsFromPause;
            }
            
            // Game Over Events
            if (gameOverUI != null)
            {
                gameOverUI.OnRestartButtonClicked += RestartGame;
                gameOverUI.OnMainMenuButtonClicked += ReturnToMainMenu;
                gameOverUI.OnShareScoreClicked += ShareScore;
            }
            
            // Settings Events
            if (settingsUI != null)
            {
                settingsUI.OnBackButtonClicked += ReturnToPreviousScreen;
                settingsUI.OnSettingsChanged += ApplySettings;
            }
        }
        
        private void SetupInitialUI()
        {
            // Show main menu by default
            ShowScreen(UIScreen.MainMenu, false);
        }
        
        #endregion
        
        #region Screen Management
        
        public void ShowScreen(UIScreen screen, bool useTransition = true)
        {
            if (isTransitioning) return;
            
            if (currentScreen == screen) return;
            
            previousScreen = currentScreen;
            
            if (useTransition && useTransitionEffects)
            {
                StartCoroutine(TransitionToScreen(screen));
            }
            else
            {
                SetScreenActive(screen, true);
                SetScreenActive(currentScreen, false);
                currentScreen = screen;
                OnScreenChanged?.Invoke(screen);
            }
            
            PlayTransitionSound();
            
            if (logUITransitions)
            {
                Debug.Log($"Switched from {previousScreen} to {screen}");
            }
        }
        
        private IEnumerator TransitionToScreen(UIScreen targetScreen)
        {
            isTransitioning = true;
            
            // Fade out current screen
            yield return StartCoroutine(FadeScreen(currentScreen, false));
            
            // Switch screens
            SetScreenActive(currentScreen, false);
            SetScreenActive(targetScreen, true);
            currentScreen = targetScreen;
            
            // Fade in new screen
            yield return StartCoroutine(FadeScreen(targetScreen, true));
            
            isTransitioning = false;
            OnScreenChanged?.Invoke(targetScreen);
        }
        
        private IEnumerator FadeScreen(UIScreen screen, bool fadeIn)
        {
            GameObject screenObject = GetScreenObject(screen);
            if (screenObject == null) yield break;
            
            CanvasGroup canvasGroup = screenObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = screenObject.AddComponent<CanvasGroup>();
            }
            
            float startAlpha = fadeIn ? 0f : 1f;
            float targetAlpha = fadeIn ? 1f : 0f;
            float elapsed = 0f;
            
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / transitionDuration;
                float curvedProgress = transitionCurve.Evaluate(progress);
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curvedProgress);
                yield return null;
            }
            
            canvasGroup.alpha = targetAlpha;
        }
        
        private void SetScreenActive(UIScreen screen, bool active)
        {
            GameObject screenObject = GetScreenObject(screen);
            if (screenObject != null)
            {
                screenObject.SetActive(active);
            }
        }
        
        private GameObject GetScreenObject(UIScreen screen)
        {
            return screen switch
            {
                UIScreen.MainMenu => mainMenuScreen,
                UIScreen.Gameplay => gameplayScreen,
                UIScreen.PauseMenu => pauseMenuScreen,
                UIScreen.GameOver => gameOverScreen,
                UIScreen.Settings => settingsScreen,
                UIScreen.Loading => loadingScreen,
                _ => null
            };
        }
        
        public void PushScreen(UIScreen screen)
        {
            screenStack.Push(currentScreen);
            ShowScreen(screen);
        }
        
        public void PopScreen()
        {
            if (screenStack.Count > 0)
            {
                UIScreen previousScreen = screenStack.Pop();
                ShowScreen(previousScreen);
            }
        }
        
        #endregion
        
        #region Game Flow Management
        
        public void StartGame()
        {
            PlayButtonSound();
            ShowScreen(UIScreen.Loading);
            
            // Start game after a brief loading screen
            StartCoroutine(StartGameCoroutine());
        }
        
        private IEnumerator StartGameCoroutine()
        {
            yield return new WaitForSeconds(1f); // Brief loading time
            
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
            
            ShowScreen(UIScreen.Gameplay);
        }
        
        public void PauseGame()
        {
            if (isPaused) return;
            
            isPaused = true;
            Time.timeScale = 0f;
            
            PlayButtonSound();
            PushScreen(UIScreen.PauseMenu);
            
            OnGamePaused?.Invoke();
            
            if (logUITransitions)
            {
                Debug.Log("Game paused");
            }
        }
        
        public void ResumeGame()
        {
            if (!isPaused) return;
            
            isPaused = false;
            Time.timeScale = 1f;
            
            PlayButtonSound();
            PopScreen(); // Return to gameplay
            
            OnGameResumed?.Invoke();
            
            if (logUITransitions)
            {
                Debug.Log("Game resumed");
            }
        }
        
        public void RestartGame()
        {
            PlayButtonSound();
            
            // Reset time scale
            Time.timeScale = 1f;
            isPaused = false;
            
            // Clear screen stack
            screenStack.Clear();
            
            if (gameManager != null)
            {
                gameManager.RestartGame();
            }
            
            ShowScreen(UIScreen.Gameplay);
        }
        
        public void EndGame(int finalScore, bool isNewHighScore = false)
        {
            if (gameOverUI != null)
            {
                gameOverUI.ShowGameOver(finalScore, isNewHighScore);
            }
            
            ShowScreen(UIScreen.GameOver);
        }
        
        public void ReturnToMainMenu()
        {
            PlayButtonSound();
            
            // Reset time scale
            Time.timeScale = 1f;
            isPaused = false;
            
            // Clear screen stack
            screenStack.Clear();
            
            if (gameManager != null)
            {
                // TODO: Implement ReturnToMainMenu method in GameManager
                // gameManager.ReturnToMainMenu();
            }
            
            ShowScreen(UIScreen.MainMenu);
        }
        
        public void QuitGame()
        {
            PlayButtonSound();
            
            if (logUITransitions)
            {
                Debug.Log("Quitting game");
            }
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        #endregion
        
        #region Settings Management
        
        public void ShowSettings()
        {
            PlayButtonSound();
            PushScreen(UIScreen.Settings);
        }
        
        public void ShowSettingsFromPause()
        {
            PlayButtonSound();
            PushScreen(UIScreen.Settings);
        }
        
        public void ReturnToPreviousScreen()
        {
            PlayButtonSound();
            PopScreen();
        }
        
        public void ApplySettings()
        {
            if (logUITransitions)
            {
                Debug.Log("Settings applied");
            }
        }
        
        #endregion
        
        #region Social Features
        
        public void ShareScore()
        {
            PlayButtonSound();
            
            if (scoreManager != null)
            {
                // TODO: Implement GetCurrentScore method in ScoreManager
                int currentScore = 0; // scoreManager.GetCurrentScore();
                string shareText = $"I just scored {currentScore} points in Catch Meow If You Can! Can you beat my score?";
                
                // Implement platform-specific sharing
                ShareScoreOnPlatform(shareText);
            }
        }
        
        private void ShareScoreOnPlatform(string shareText)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
                AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
                AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");
                intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intentObject.Call<AndroidJavaObject>("setType", "text/plain");
                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), shareText);
                AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                currentActivity.Call("startActivity", intentObject);
            #elif UNITY_IOS && !UNITY_EDITOR
                // iOS sharing implementation would go here
                Debug.Log($"Share on iOS: {shareText}");
            #else
                Debug.Log($"Share: {shareText}");
            #endif
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleInput()
        {
            // Handle escape/back button
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleBackButton();
            }
            
            // Handle pause during gameplay
            if (Input.GetKeyDown(KeyCode.P) && currentScreen == UIScreen.Gameplay)
            {
                if (isPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }
        
        private void HandleBackButton()
        {
            switch (currentScreen)
            {
                case UIScreen.MainMenu:
                    QuitGame();
                    break;
                case UIScreen.Gameplay:
                    PauseGame();
                    break;
                case UIScreen.PauseMenu:
                    ResumeGame();
                    break;
                case UIScreen.GameOver:
                    ReturnToMainMenu();
                    break;
                case UIScreen.Settings:
                    ReturnToPreviousScreen();
                    break;
            }
        }
        
        #endregion
        
        #region Audio
        
        private void PlayButtonSound()
        {
            if (buttonClickSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(buttonClickSound, uiAudioVolume);
            }
        }
        
        private void PlayTransitionSound()
        {
            if (screenTransitionSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(screenTransitionSound, uiAudioVolume * 0.8f);
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Update gameplay UI elements (score, lives, etc.)
        /// </summary>
        public void UpdateGameplayUI()
        {
            if (gameplayUI != null && currentScreen == UIScreen.Gameplay)
            {
                gameplayUI.UpdateUI();
            }
        }
        
        /// <summary>
        /// Show loading screen with custom message
        /// </summary>
        public void ShowLoading(string message = "Loading...")
        {
            // Implementation for loading screen
            ShowScreen(UIScreen.Loading);
        }
        
        /// <summary>
        /// Hide loading screen
        /// </summary>
        public void HideLoading()
        {
            if (currentScreen == UIScreen.Loading)
            {
                ShowScreen(previousScreen);
            }
        }
        
        /// <summary>
        /// Check if a specific screen is currently active
        /// </summary>
        public bool IsScreenActive(UIScreen screen)
        {
            return currentScreen == screen;
        }
        
        /// <summary>
        /// Get UI component reference
        /// </summary>
        public T GetUIComponent<T>() where T : MonoBehaviour
        {
            if (typeof(T) == typeof(MainMenuUI)) return mainMenuUI as T;
            if (typeof(T) == typeof(GameplayUI)) return gameplayUI as T;
            if (typeof(T) == typeof(PauseMenuUI)) return pauseMenuUI as T;
            if (typeof(T) == typeof(GameOverUI)) return gameOverUI as T;
            if (typeof(T) == typeof(SettingsUI)) return settingsUI as T;
            
            return null;
        }
        
        #endregion
        
        #region Debug
        
        public string GetDebugInfo()
        {
            return $"UIManager Debug Info:\n" +
                   $"Current Screen: {currentScreen}\n" +
                   $"Previous Screen: {previousScreen}\n" +
                   $"Is Transitioning: {isTransitioning}\n" +
                   $"Is Paused: {isPaused}\n" +
                   $"Screen Stack Count: {screenStack.Count}\n" +
                   $"Time Scale: {Time.timeScale}";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Enum defining all possible UI screens
    /// </summary>
    public enum UIScreen
    {
        None,
        MainMenu,
        Gameplay,
        PauseMenu,
        GameOver,
        Settings,
        Loading
    }
}