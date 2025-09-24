using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using CatchMeowIfYouCan.Core;

namespace CatchMeowIfYouCan.UI
{
    /// <summary>
    /// Game Over UI controller
    /// Handles game over screen display, score presentation, and restart options
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button shareScoreButton;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private Button leaderboardButton;
        
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI newHighScoreText;
        [SerializeField] private TextMeshProUGUI coinsEarnedText;
        [SerializeField] private TextMeshProUGUI totalCoinsText;
        
        [Header("Statistics Display")]
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI collectiblesText;
        [SerializeField] private TextMeshProUGUI powerUpsUsedText;
        [SerializeField] private TextMeshProUGUI timePlayedText;
        
        [Header("Visual Elements")]
        [SerializeField] private Image gameOverTitle;
        [SerializeField] private Image scoreBackground;
        [SerializeField] private Image characterImage;
        [SerializeField] private ParticleSystem celebrationParticles;
        [SerializeField] private ParticleSystem sadParticles;
        
        [Header("Animation Settings")]
        [SerializeField] private bool enableAnimations = true;
        [SerializeField] private float animationDuration = 0.8f;
        [SerializeField] private AnimationCurve slideInCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
        [SerializeField] private AnimationCurve scoreCountCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float scoreCountDuration = 2f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip gameOverSound;
        [SerializeField] private AudioClip newHighScoreSound;
        [SerializeField] private AudioClip scoreCountSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private float audioVolume = 0.8f;
        
        [Header("New High Score Effects")]
        [SerializeField] private GameObject newHighScorePanel;
        [SerializeField] private Animator newHighScoreAnimator;
        [SerializeField] private Color newHighScoreColor = new Color(1f, 0.84f, 0f); // Gold color
        [SerializeField] private float highScoreFlashDuration = 0.5f;
        
        // Component references
        private AudioManager audioManager;
        private ScoreManager scoreManager;
        private CanvasGroup canvasGroup;
        
        // Animation state
        private bool isAnimating = false;
        private int currentScore;
        private int displayedScore;
        private bool isNewHighScore;
        
        // Original positions for animations
        private Vector3 originalTitlePosition;
        private Vector3 originalScorePosition;
        private Vector3[] originalButtonPositions;
        
        // Events
        public System.Action OnRestartButtonClicked;
        public System.Action OnMainMenuButtonClicked;
        public System.Action OnShareScoreClicked;
        public System.Action OnWatchAdClicked;
        public System.Action OnLeaderboardClicked;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            SetupGameOverUI();
        }
        
        private void OnEnable()
        {
            if (enableAnimations)
            {
                StartGameOverAnimations();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            // Get component references
            audioManager = FindObjectOfType<AudioManager>();
            scoreManager = FindObjectOfType<ScoreManager>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // Store original positions for animations
            StoreOriginalPositions();
            
            // Setup button events
            SetupButtonEvents();
        }
        
        private void StoreOriginalPositions()
        {
            if (gameOverTitle != null)
                originalTitlePosition = gameOverTitle.transform.localPosition;
            
            if (scoreBackground != null)
                originalScorePosition = scoreBackground.transform.localPosition;
            
            Button[] buttons = { restartButton, mainMenuButton, shareScoreButton, watchAdButton, leaderboardButton };
            originalButtonPositions = new Vector3[buttons.Length];
            
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    originalButtonPositions[i] = buttons[i].transform.localPosition;
                }
            }
        }
        
        private void SetupButtonEvents()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(() => OnButtonClicked(OnRestartButtonClicked));
            }
            
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(() => OnButtonClicked(OnMainMenuButtonClicked));
            }
            
            if (shareScoreButton != null)
            {
                shareScoreButton.onClick.AddListener(() => OnButtonClicked(OnShareScoreClicked));
            }
            
            if (watchAdButton != null)
            {
                watchAdButton.onClick.AddListener(() => OnButtonClicked(OnWatchAdClicked));
            }
            
            if (leaderboardButton != null)
            {
                leaderboardButton.onClick.AddListener(() => OnButtonClicked(OnLeaderboardClicked));
            }
        }
        
        private void SetupGameOverUI()
        {
            // Initially hide elements for animation
            if (enableAnimations)
            {
                HideElementsForAnimation();
            }
        }
        
        private void HideElementsForAnimation()
        {
            canvasGroup.alpha = 0f;
            
            if (gameOverTitle != null)
                gameOverTitle.transform.localPosition = originalTitlePosition + Vector3.up * 300f;
            
            if (scoreBackground != null)
                scoreBackground.transform.localPosition = originalScorePosition + Vector3.down * 400f;
            
            Button[] buttons = { restartButton, mainMenuButton, shareScoreButton, watchAdButton, leaderboardButton };
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].transform.localScale = Vector3.zero;
                }
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Show the game over screen with score and statistics
        /// </summary>
        public void ShowGameOver(int finalScore, bool newHighScore = false)
        {
            currentScore = finalScore;
            isNewHighScore = newHighScore;
            
            UpdateScoreDisplay();
            UpdateStatistics();
            
            PlayGameOverSound();
            
            if (enableAnimations)
            {
                StartCoroutine(PlayGameOverAnimation());
            }
            
            // Show appropriate particles
            if (newHighScore && celebrationParticles != null)
            {
                celebrationParticles.Play();
            }
            else if (sadParticles != null)
            {
                sadParticles.Play();
            }
        }
        
        #endregion
        
        #region Score and Statistics
        
        private void UpdateScoreDisplay()
        {
            if (finalScoreText != null)
            {
                finalScoreText.text = "0";
                displayedScore = 0;
            }
            
            if (highScoreText != null && scoreManager != null)
            {
                // TODO: Implement GetHighScore method in ScoreManager
                int highScore = 0; // scoreManager.GetHighScore();
                highScoreText.text = $"Best: {highScore:N0}";
            }
            
            if (newHighScoreText != null)
            {
                newHighScoreText.gameObject.SetActive(isNewHighScore);
            }
            
            if (coinsEarnedText != null)
            {
                int coinsEarned = CalculateCoinsEarned(currentScore);
                coinsEarnedText.text = $"+{coinsEarned}";
            }
            
            if (totalCoinsText != null)
            {
                // TODO: Get total coins from save system
                totalCoinsText.text = "Total: 0";
            }
        }
        
        private void UpdateStatistics()
        {
            // TODO: Get actual game statistics
            if (distanceText != null)
            {
                distanceText.text = "Distance: 0m";
            }
            
            if (collectiblesText != null)
            {
                collectiblesText.text = "Gems: 0";
            }
            
            if (powerUpsUsedText != null)
            {
                powerUpsUsedText.text = "Power-ups: 0";
            }
            
            if (timePlayedText != null)
            {
                timePlayedText.text = "Time: 0:00";
            }
        }
        
        private int CalculateCoinsEarned(int score)
        {
            // Simple formula: 1 coin per 100 points
            return Mathf.FloorToInt(score / 100f);
        }
        
        #endregion
        
        #region Animations
        
        private void StartGameOverAnimations()
        {
            if (!enableAnimations) return;
            
            StartCoroutine(PlayGameOverAnimation());
        }
        
        private IEnumerator PlayGameOverAnimation()
        {
            isAnimating = true;
            
            // Fade in the screen
            yield return StartCoroutine(FadeInScreen());
            
            // Animate title sliding in from top
            yield return StartCoroutine(AnimateTitle());
            
            // Animate score panel sliding in from bottom
            yield return StartCoroutine(AnimateScorePanel());
            
            // Count up the score
            yield return StartCoroutine(AnimateScoreCount());
            
            // Show new high score effects if applicable
            if (isNewHighScore)
            {
                yield return StartCoroutine(ShowNewHighScoreEffects());
            }
            
            // Animate buttons appearing
            yield return StartCoroutine(AnimateButtons());
            
            isAnimating = false;
        }
        
        private IEnumerator FadeInScreen()
        {
            float elapsed = 0f;
            while (elapsed < animationDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / (animationDuration * 0.5f);
                
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
        
        private IEnumerator AnimateTitle()
        {
            if (gameOverTitle == null) yield break;
            
            Vector3 startPos = originalTitlePosition + Vector3.up * 300f;
            Vector3 targetPos = originalTitlePosition;
            
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / animationDuration;
                float curvedProgress = slideInCurve.Evaluate(progress);
                
                gameOverTitle.transform.localPosition = Vector3.Lerp(startPos, targetPos, curvedProgress);
                yield return null;
            }
            
            gameOverTitle.transform.localPosition = targetPos;
        }
        
        private IEnumerator AnimateScorePanel()
        {
            if (scoreBackground == null) yield break;
            
            Vector3 startPos = originalScorePosition + Vector3.down * 400f;
            Vector3 targetPos = originalScorePosition;
            
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / animationDuration;
                float curvedProgress = slideInCurve.Evaluate(progress);
                
                scoreBackground.transform.localPosition = Vector3.Lerp(startPos, targetPos, curvedProgress);
                yield return null;
            }
            
            scoreBackground.transform.localPosition = targetPos;
        }
        
        private IEnumerator AnimateScoreCount()
        {
            if (finalScoreText == null) yield break;
            
            PlayScoreCountSound();
            
            float elapsed = 0f;
            while (elapsed < scoreCountDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / scoreCountDuration;
                float curvedProgress = scoreCountCurve.Evaluate(progress);
                
                displayedScore = Mathf.RoundToInt(Mathf.Lerp(0f, currentScore, curvedProgress));
                finalScoreText.text = displayedScore.ToString("N0");
                
                yield return null;
            }
            
            finalScoreText.text = currentScore.ToString("N0");
        }
        
        private IEnumerator ShowNewHighScoreEffects()
        {
            if (newHighScorePanel != null)
            {
                newHighScorePanel.SetActive(true);
            }
            
            if (newHighScoreAnimator != null)
            {
                newHighScoreAnimator.SetTrigger("ShowHighScore");
            }
            
            PlayNewHighScoreSound();
            
            // Flash the high score text
            if (highScoreText != null)
            {
                Color originalColor = highScoreText.color;
                float elapsed = 0f;
                
                while (elapsed < highScoreFlashDuration * 3f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float intensity = Mathf.PingPong(elapsed * 4f, 1f);
                    highScoreText.color = Color.Lerp(originalColor, newHighScoreColor, intensity);
                    yield return null;
                }
                
                highScoreText.color = newHighScoreColor;
            }
            
            yield return new WaitForSecondsRealtime(1f);
        }
        
        private IEnumerator AnimateButtons()
        {
            Button[] buttons = { restartButton, mainMenuButton, shareScoreButton, watchAdButton, leaderboardButton };
            
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    StartCoroutine(AnimateButtonAppear(buttons[i]));
                    yield return new WaitForSecondsRealtime(0.1f);
                }
            }
        }
        
        private IEnumerator AnimateButtonAppear(Button button)
        {
            if (button == null) yield break;
            
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float curvedProgress = slideInCurve.Evaluate(progress);
                
                button.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, curvedProgress);
                yield return null;
            }
            
            button.transform.localScale = Vector3.one;
        }
        
        #endregion
        
        #region Button Events
        
        private void OnButtonClicked(System.Action buttonAction)
        {
            if (isAnimating) return;
            
            PlayButtonSound();
            buttonAction?.Invoke();
        }
        
        #endregion
        
        #region Audio
        
        private void PlayGameOverSound()
        {
            if (gameOverSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(gameOverSound, audioVolume);
            }
        }
        
        private void PlayNewHighScoreSound()
        {
            if (newHighScoreSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(newHighScoreSound, audioVolume);
            }
        }
        
        private void PlayScoreCountSound()
        {
            if (scoreCountSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(scoreCountSound, audioVolume * 0.6f);
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
                // TODO: Implement PlayButtonSound method in AudioManager
                // audioManager.PlayButtonSound();
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Set visibility of optional buttons based on platform/features
        /// </summary>
        public void SetButtonVisibility(bool showShare = true, bool showAd = true, bool showLeaderboard = true)
        {
            if (shareScoreButton != null)
                shareScoreButton.gameObject.SetActive(showShare);
            
            if (watchAdButton != null)
                watchAdButton.gameObject.SetActive(showAd);
            
            if (leaderboardButton != null)
                leaderboardButton.gameObject.SetActive(showLeaderboard);
        }
        
        /// <summary>
        /// Update the game statistics display with actual values
        /// </summary>
        public void UpdateGameStatistics(float distance, int collectibles, int powerUpsUsed, float timeElapsed)
        {
            if (distanceText != null)
            {
                distanceText.text = $"Distance: {distance:F0}m";
            }
            
            if (collectiblesText != null)
            {
                collectiblesText.text = $"Gems: {collectibles}";
            }
            
            if (powerUpsUsedText != null)
            {
                powerUpsUsedText.text = $"Power-ups: {powerUpsUsed}";
            }
            
            if (timePlayedText != null)
            {
                int minutes = Mathf.FloorToInt(timeElapsed / 60f);
                int seconds = Mathf.FloorToInt(timeElapsed % 60f);
                timePlayedText.text = $"Time: {minutes}:{seconds:D2}";
            }
        }
        
        /// <summary>
        /// Enable or disable all buttons
        /// </summary>
        public void SetButtonsInteractable(bool interactable)
        {
            Button[] buttons = { restartButton, mainMenuButton, shareScoreButton, watchAdButton, leaderboardButton };
            
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.interactable = interactable;
                }
            }
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDisable()
        {
            // Stop all particles
            if (celebrationParticles != null)
                celebrationParticles.Stop();
            
            if (sadParticles != null)
                sadParticles.Stop();
            
            // Reset animation state
            isAnimating = false;
        }
        
        #endregion
    }
}