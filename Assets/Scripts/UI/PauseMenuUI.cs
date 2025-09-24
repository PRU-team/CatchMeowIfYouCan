using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using CatchMeowIfYouCan.Core;

namespace CatchMeowIfYouCan.UI
{
    /// <summary>
    /// Pause menu UI controller
    /// Handles pause menu interactions, resume, restart, settings, and quit options
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;
        
        [Header("Display Elements")]
        [SerializeField] private TextMeshProUGUI pauseTitle;
        [SerializeField] private TextMeshProUGUI currentScoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI timePlayedText;
        [SerializeField] private Image backgroundOverlay;
        
        [Header("Quick Stats")]
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI collectiblesText;
        [SerializeField] private TextMeshProUGUI powerUpsText;
        [SerializeField] private TextMeshProUGUI livesText;
        
        [Header("Animation Settings")]
        [SerializeField] private bool enableAnimations = true;
        [SerializeField] private float animationDuration = 0.5f;
                [SerializeField] private AnimationCurve punchCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(1f, 1f));
        [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Visual Effects")]
        [SerializeField] private float backgroundBlurStrength = 0.5f;
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.7f);
        [SerializeField] private bool enableBackgroundBlur = true;
        
        [Header("Audio")]
        [SerializeField] private AudioClip pauseSound;
        [SerializeField] private AudioClip resumeSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private float audioVolume = 0.8f;
        
        [Header("Button Effects")]
        [SerializeField] private float buttonHoverScale = 1.05f;
        [SerializeField] private Color buttonHoverColor = Color.white;
        [SerializeField] private Color buttonNormalColor = Color.gray;
        
        // Component references
        private AudioManager audioManager;
        private ScoreManager scoreManager;
        private GameManager gameManager;
        private CanvasGroup canvasGroup;
        
        // Animation state
        private bool isAnimating = false;
        private Vector3[] originalButtonPositions;
        private Vector3[] originalButtonScales;
        private Vector3 originalTitlePosition;
        private Vector3 originalStatsPosition;
        
        // Game state when paused
        private float pauseStartTime;
        private int scoreWhenPaused;
        private bool wasAudioPaused;
        
        // Events
        public System.Action OnResumeButtonClicked;
        public System.Action OnRestartButtonClicked;
        public System.Action OnSettingsButtonClicked;
        public System.Action OnMainMenuButtonClicked;
        public System.Action OnQuitButtonClicked;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            SetupPauseMenu();
        }
        
        private void Update()
        {
            HandleInput();
            UpdateTimeDisplay();
        }
        
        private void OnEnable()
        {
            OnPauseMenuOpened();
        }
        
        private void OnDisable()
        {
            OnPauseMenuClosed();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            // Get component references
            audioManager = FindObjectOfType<AudioManager>();
            scoreManager = FindObjectOfType<ScoreManager>();
            gameManager = FindObjectOfType<GameManager>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // Store original positions and scales
            StoreOriginalTransforms();
            
            // Setup button events
            SetupButtonEvents();
        }
        
        private void StoreOriginalTransforms()
        {
            // Store button transforms
            Button[] buttons = { resumeButton, restartButton, settingsButton, mainMenuButton, quitButton };
            originalButtonPositions = new Vector3[buttons.Length];
            originalButtonScales = new Vector3[buttons.Length];
            
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    originalButtonPositions[i] = buttons[i].transform.localPosition;
                    originalButtonScales[i] = buttons[i].transform.localScale;
                }
            }
            
            // Store other element positions
            if (pauseTitle != null)
                originalTitlePosition = pauseTitle.transform.localPosition;
            
            if (statsPanel != null)
                originalStatsPosition = statsPanel.transform.localPosition;
        }
        
        private void SetupButtonEvents()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(() => OnButtonClicked(OnResumeButtonClicked));
                SetupButtonHoverEffects(resumeButton);
            }
            
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(() => OnButtonClicked(OnRestartButtonClicked));
                SetupButtonHoverEffects(restartButton);
            }
            
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() => OnButtonClicked(OnSettingsButtonClicked));
                SetupButtonHoverEffects(settingsButton);
            }
            
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(() => OnButtonClicked(OnMainMenuButtonClicked));
                SetupButtonHoverEffects(mainMenuButton);
            }
            
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(() => OnButtonClicked(OnQuitButtonClicked));
                SetupButtonHoverEffects(quitButton);
            }
        }
        
        private void SetupButtonHoverEffects(Button button)
        {
            var eventTrigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }
            
            // Pointer Enter
            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => OnButtonHover(button, true));
            eventTrigger.triggers.Add(pointerEnter);
            
            // Pointer Exit
            var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => OnButtonHover(button, false));
            eventTrigger.triggers.Add(pointerExit);
        }
        
        private void SetupPauseMenu()
        {
            // Setup background overlay
            if (backgroundOverlay != null)
            {
                backgroundOverlay.color = overlayColor;
            }
            
            // Initialize with hidden state for animations
            if (enableAnimations)
            {
                HideElementsForAnimation();
            }
        }
        
        private void HideElementsForAnimation()
        {
            canvasGroup.alpha = 0f;
            
            // Hide title
            if (pauseTitle != null)
                pauseTitle.transform.localPosition = originalTitlePosition + Vector3.up * 200f;
            
            // Hide stats panel
            if (statsPanel != null)
                statsPanel.transform.localPosition = originalStatsPosition + Vector3.down * 300f;
            
            // Hide buttons
            Button[] buttons = { resumeButton, restartButton, settingsButton, mainMenuButton, quitButton };
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].transform.localScale = Vector3.zero;
                }
            }
        }
        
        #endregion
        
        #region Pause Menu Events
        
        private void OnPauseMenuOpened()
        {
            pauseStartTime = Time.unscaledTime;
            
            // Store current game state
            if (scoreManager != null)
            {
                // TODO: Implement GetCurrentScore in ScoreManager
                // scoreWhenPaused = scoreManager.GetCurrentScore();
            }
            
            // Update displays
            UpdatePauseMenuDisplay();
            
            // Play pause sound
            PlayPauseSound();
            
            // Start animations
            if (enableAnimations)
            {
                StartCoroutine(PlayPauseMenuAnimation());
            }
            
            // Pause background audio if needed
            if (audioManager != null)
            {
                // TODO: Implement IsMusicPaused in AudioManager
                // wasAudioPaused = audioManager.IsMusicPaused();
                if (!wasAudioPaused)
                {
                    audioManager.PauseMusic();
                }
            }
        }
        
        private void OnPauseMenuClosed()
        {
            // Resume background audio if it wasn't paused before
            if (audioManager != null && !wasAudioPaused)
            {
                audioManager.ResumeMusic();
            }
        }
        
        #endregion
        
        #region Display Updates
        
        private void UpdatePauseMenuDisplay()
        {
            // Update score displays
            if (currentScoreText != null && scoreManager != null)
            {
                // TODO: Implement GetCurrentScore in ScoreManager
                // currentScoreText.text = $"Score: {scoreManager.GetCurrentScore():N0}";
            }
            
            if (highScoreText != null && scoreManager != null)
            {
                // TODO: Implement GetHighScore in ScoreManager
                // highScoreText.text = $"Best: {scoreManager.GetHighScore():N0}";
            }
            
            // Update game statistics
            UpdateGameStatistics();
        }
        
        private void UpdateGameStatistics()
        {
            if (gameManager == null) return;
            
            // TODO: Get actual statistics from game manager
            if (distanceText != null)
            {
                distanceText.text = "Distance: 0m";
            }
            
            if (collectiblesText != null)
            {
                collectiblesText.text = "Gems: 0";
            }
            
            if (powerUpsText != null)
            {
                powerUpsText.text = "Power-ups: 0";
            }
            
            if (livesText != null)
            {
                livesText.text = "Lives: 3";
            }
        }
        
        private void UpdateTimeDisplay()
        {
            if (timePlayedText != null)
            {
                float timePlayed = Time.unscaledTime - pauseStartTime;
                int minutes = Mathf.FloorToInt(timePlayed / 60f);
                int seconds = Mathf.FloorToInt(timePlayed % 60f);
                timePlayedText.text = $"Time: {minutes}:{seconds:D2}";
            }
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleInput()
        {
            // Handle escape key to resume
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ResumeGame();
            }
            
            // Handle spacebar to resume
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ResumeGame();
            }
        }
        
        #endregion
        
        #region Button Events
        
        private void OnButtonClicked(System.Action buttonAction)
        {
            if (isAnimating) return;
            
            PlayButtonSound();
            buttonAction?.Invoke();
        }
        
        private void OnButtonHover(Button button, bool isHovering)
        {
            if (isAnimating) return;
            
            float targetScale = isHovering ? buttonHoverScale : 1f;
            Color targetColor = isHovering ? buttonHoverColor : buttonNormalColor;
            
            StartCoroutine(AnimateButtonScale(button, targetScale));
            AnimateButtonColor(button, targetColor);
        }
        
        private void ResumeGame()
        {
            OnResumeButtonClicked?.Invoke();
        }
        
        #endregion
        
        #region Animations
        
        private IEnumerator PlayPauseMenuAnimation()
        {
            isAnimating = true;
            
            // Fade in background
            yield return StartCoroutine(FadeInBackground());
            
            // Animate title sliding in
            yield return StartCoroutine(AnimateTitle());
            
            // Animate stats panel sliding in
            yield return StartCoroutine(AnimateStatsPanel());
            
            // Animate buttons appearing
            yield return StartCoroutine(AnimateButtons());
            
            isAnimating = false;
        }
        
        private IEnumerator FadeInBackground()
        {
            float elapsed = 0f;
            float duration = animationDuration * 0.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float curvedProgress = fadeInCurve.Evaluate(progress);
                
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, curvedProgress);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
        
        private IEnumerator AnimateTitle()
        {
            if (pauseTitle == null) yield break;
            
            Vector3 startPos = originalTitlePosition + Vector3.up * 200f;
            Vector3 targetPos = originalTitlePosition;
            
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / animationDuration;
                float curvedProgress = punchCurve.Evaluate(progress);
                
                pauseTitle.transform.localPosition = Vector3.Lerp(startPos, targetPos, curvedProgress);
                yield return null;
            }
            
            pauseTitle.transform.localPosition = targetPos;
        }
        
        private IEnumerator AnimateStatsPanel()
        {
            if (statsPanel == null) yield break;
            
            Vector3 startPos = originalStatsPosition + Vector3.down * 300f;
            Vector3 targetPos = originalStatsPosition;
            
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / animationDuration;
                float curvedProgress = punchCurve.Evaluate(progress);
                
                statsPanel.transform.localPosition = Vector3.Lerp(startPos, targetPos, curvedProgress);
                yield return null;
            }
            
            statsPanel.transform.localPosition = targetPos;
        }
        
        private IEnumerator AnimateButtons()
        {
            Button[] buttons = { resumeButton, restartButton, settingsButton, mainMenuButton, quitButton };
            
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    StartCoroutine(AnimateButtonAppear(buttons[i], i));
                    yield return new WaitForSecondsRealtime(0.1f);
                }
            }
        }
        
        private IEnumerator AnimateButtonAppear(Button button, int index)
        {
            if (button == null) yield break;
            
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float curvedProgress = punchCurve.Evaluate(progress);
                
                button.transform.localScale = Vector3.Lerp(Vector3.zero, originalButtonScales[index], curvedProgress);
                yield return null;
            }
            
            button.transform.localScale = originalButtonScales[index];
        }
        
        private IEnumerator AnimateButtonScale(Button button, float targetScale)
        {
            if (button == null) yield break;
            
            Vector3 startScale = button.transform.localScale;
            Vector3 endScale = Vector3.one * targetScale;
            
            float elapsed = 0f;
            float duration = 0.2f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                
                button.transform.localScale = Vector3.Lerp(startScale, endScale, progress);
                yield return null;
            }
            
            button.transform.localScale = endScale;
        }
        
        private void AnimateButtonColor(Button button, Color targetColor)
        {
            if (button != null)
            {
                var colors = button.colors;
                colors.normalColor = targetColor;
                button.colors = colors;
            }
        }
        
        #endregion
        
        #region Audio
        
        private void PlayPauseSound()
        {
            if (pauseSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(pauseSound, audioVolume);
            }
        }
        
        private void PlayResumeSound()
        {
            if (resumeSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(resumeSound, audioVolume);
            }
        }
        
        private void PlayButtonSound()
        {
            if (buttonClickSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(buttonClickSound, audioVolume);
            }
            else if (audioManager != null)
            {
                // TODO: Implement PlayButtonSound in AudioManager
                // audioManager.PlayButtonSound();
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Manually refresh the pause menu display
        /// </summary>
        public void RefreshPauseMenu()
        {
            UpdatePauseMenuDisplay();
        }
        
        /// <summary>
        /// Set button visibility based on game state
        /// </summary>
        public void SetButtonVisibility(bool showRestart = true, bool showSettings = true, bool showQuit = true)
        {
            if (restartButton != null)
                restartButton.gameObject.SetActive(showRestart);
            
            if (settingsButton != null)
                settingsButton.gameObject.SetActive(showSettings);
            
            if (quitButton != null)
                quitButton.gameObject.SetActive(showQuit);
        }
        
        /// <summary>
        /// Enable or disable all buttons
        /// </summary>
        public void SetButtonsInteractable(bool interactable)
        {
            Button[] buttons = { resumeButton, restartButton, settingsButton, mainMenuButton, quitButton };
            
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.interactable = interactable;
                }
            }
        }
        
        /// <summary>
        /// Update the statistics display with current game values
        /// </summary>
        public void UpdateStatistics(float distance, int collectibles, int powerUps, int lives)
        {
            if (distanceText != null)
                distanceText.text = $"Distance: {distance:F0}m";
            
            if (collectiblesText != null)
                collectiblesText.text = $"Gems: {collectibles}";
            
            if (powerUpsText != null)
                powerUpsText.text = $"Power-ups: {powerUps}";
            
            if (livesText != null)
                livesText.text = $"Lives: {lives}";
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            // Clean up any remaining coroutines
            StopAllCoroutines();
        }
        
        #endregion
    }
}