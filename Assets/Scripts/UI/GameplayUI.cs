using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using CatchMeowIfYouCan.Core;

namespace CatchMeowIfYouCan.UI
{
    // TODO: Replace with actual PowerUp implementation
    public class BasePowerUp
    {
        public enum PowerUpType { Speed, Magnet, Shield, Score, Time, Freeze }
        public PowerUpType Type { get; set; }
        public float Duration { get; set; }
        public bool IsActive { get; set; } = true;
        public Transform transform { get; set; }
    }
    
    /// <summary>
    /// Gameplay UI (HUD) controller
    /// Handles in-game UI elements like score, lives, power-ups, and other gameplay indicators
    /// </summary>
    public class GameplayUI : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI multiplierText;
        [SerializeField] private Slider scoreSlider;
        [SerializeField] private Image scoreSliderFill;
        [SerializeField] private Gradient scoreSliderGradient;
        
        [Header("Lives Display")]
        [SerializeField] private Image[] lifeIcons;
        [SerializeField] private Color fullLifeColor = Color.red;
        [SerializeField] private Color emptyLifeColor = Color.gray;
        [SerializeField] private Animator livesAnimator;
        
        [Header("Power-Up Display")]
        [SerializeField] private GameObject powerUpContainer;
        [SerializeField] private PowerUpIndicator[] powerUpSlots;
        [SerializeField] private GameObject powerUpSlotPrefab;
        [SerializeField] private int maxPowerUpSlots = 3;
        
        [Header("Game Info")]
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Slider levelProgressSlider;
        
        [Header("Control Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button muteButton;
        [SerializeField] private Image muteButtonIcon;
        [SerializeField] private Sprite soundOnIcon;
        [SerializeField] private Sprite soundOffIcon;
        
        [Header("Special Effects")]
        [SerializeField] private GameObject comboIndicator;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private Animator comboAnimator;
        [SerializeField] private ParticleSystem scoreParticles;
        [SerializeField] private ParticleSystem collectParticles;
        
        [Header("Warning Indicators")]
        [SerializeField] private GameObject lowHealthWarning;
        [SerializeField] private GameObject obstacleWarning;
        [SerializeField] private Image healthWarningOverlay;
        [SerializeField] private float warningFlashSpeed = 2f;
        
        [Header("Animation Settings")]
        [SerializeField] private bool enableAnimations = true;
        [SerializeField] private AnimationCurve scorePunchCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(1f, 1f));
        [SerializeField] private float scorePunchDuration = 0.3f;
        [SerializeField] private float scorePunchScale = 1.2f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip scoreIncreaseSound;
        [SerializeField] private AudioClip comboSound;
        [SerializeField] private AudioClip lifePickupSound;
        [SerializeField] private AudioClip lifeLostSound;
        [SerializeField] private float uiAudioVolume = 0.7f;
        
        // Component references
        private GameManager gameManager;
        private ScoreManager scoreManager;
        // TODO: Implement PowerUpManager class
        // private PowerUpManager powerUpManager;
        private AudioManager audioManager;
        
        // Current game state
        private int currentScore;
        private int currentLives;
        private int currentCoins;
        private float currentDistance;
        private int currentLevel;
        private int currentCombo;
        private float currentMultiplier;
        
        // Animation state
        private bool isAnimatingScore = false;
        private Vector3 originalScoreScale;
        private List<PowerUpIndicator> activePowerUpIndicators = new List<PowerUpIndicator>();
        
        // Events
        public System.Action OnPauseButtonClicked;
        public System.Action OnSettingsButtonClicked;
        public System.Action OnMuteToggled;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            SetupGameplayUI();
        }
        
        private void Update()
        {
            UpdateGameplayUI();
            UpdateWarningEffects();
        }
        
        private void OnEnable()
        {
            SubscribeToEvents();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            // Get component references
            gameManager = FindObjectOfType<GameManager>();
            scoreManager = FindObjectOfType<ScoreManager>();
            // TODO: Implement PowerUpManager
            // powerUpManager = FindObjectOfType<PowerUpManager>();
            audioManager = FindObjectOfType<AudioManager>();
            
            // Store original scales for animations
            if (scoreText != null)
            {
                originalScoreScale = scoreText.transform.localScale;
            }
            
            // Setup button events
            SetupButtonEvents();
            
            // Initialize power-up slots
            InitializePowerUpSlots();
        }
        
        private void SetupButtonEvents()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(() => OnPauseButtonClicked?.Invoke());
            }
            
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() => OnSettingsButtonClicked?.Invoke());
            }
            
            if (muteButton != null)
            {
                muteButton.onClick.AddListener(ToggleMute);
            }
        }
        
        private void InitializePowerUpSlots()
        {
            if (powerUpSlots == null || powerUpSlots.Length == 0)
            {
                // Create power-up slots dynamically if not assigned
                CreatePowerUpSlots();
            }
            
            // Initialize all power-up slots
            foreach (var slot in powerUpSlots)
            {
                if (slot != null)
                {
                    slot.Initialize();
                    slot.SetActive(false);
                }
            }
        }
        
        private void CreatePowerUpSlots()
        {
            if (powerUpContainer == null || powerUpSlotPrefab == null) return;
            
            powerUpSlots = new PowerUpIndicator[maxPowerUpSlots];
            
            for (int i = 0; i < maxPowerUpSlots; i++)
            {
                GameObject slotObj = Instantiate(powerUpSlotPrefab, powerUpContainer.transform);
                PowerUpIndicator indicator = slotObj.GetComponent<PowerUpIndicator>();
                
                if (indicator != null)
                {
                    powerUpSlots[i] = indicator;
                }
            }
        }
        
        private void SetupGameplayUI()
        {
            // Initialize displays
            UpdateScoreDisplay(0);
            UpdateLivesDisplay(3); // Default lives
            UpdateCoinsDisplay(0);
            UpdateDistanceDisplay(0f);
            UpdateLevelDisplay(1, 0f);
            UpdateMultiplierDisplay(1f);
            
            // Hide special indicators initially
            if (comboIndicator != null)
                comboIndicator.SetActive(false);
            
            if (lowHealthWarning != null)
                lowHealthWarning.SetActive(false);
            
            if (obstacleWarning != null)
                obstacleWarning.SetActive(false);
        }
        
        private void SubscribeToEvents()
        {
            // Subscribe to game events
            if (scoreManager != null)
            {
                // scoreManager.OnScoreChanged += OnScoreChanged;
                // scoreManager.OnMultiplierChanged += OnMultiplierChanged;
                // scoreManager.OnComboChanged += OnComboChanged;
            }
            
            // TODO: Setup PowerUpManager events
            // if (powerUpManager != null)
            // {
            //     powerUpManager.OnPowerUpActivated += OnPowerUpActivated;
            //     powerUpManager.OnPowerUpExpired += OnPowerUpExpired;
            //     powerUpManager.OnPowerUpCollected += OnPowerUpCollected;
            // }
            
            if (gameManager != null)
            {
                // gameManager.OnLivesChanged += OnLivesChanged;
                // gameManager.OnCoinsChanged += OnCoinsChanged;
                // gameManager.OnLevelChanged += OnLevelChanged;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            // Unsubscribe from events to prevent memory leaks
            if (scoreManager != null)
            {
                // scoreManager.OnScoreChanged -= OnScoreChanged;
                // scoreManager.OnMultiplierChanged -= OnMultiplierChanged;
                // scoreManager.OnComboChanged -= OnComboChanged;
            }
            
            // TODO: Cleanup PowerUpManager events
            // if (powerUpManager != null)
            // {
            //     powerUpManager.OnPowerUpActivated -= OnPowerUpActivated;
            //     powerUpManager.OnPowerUpExpired -= OnPowerUpExpired;
            //     powerUpManager.OnPowerUpCollected -= OnPowerUpCollected;
            // }
            
            if (gameManager != null)
            {
                // gameManager.OnLivesChanged -= OnLivesChanged;
                // gameManager.OnCoinsChanged -= OnCoinsChanged;
                // gameManager.OnLevelChanged -= OnLevelChanged;
            }
        }
        
        #endregion
        
        #region UI Updates
        
        public void UpdateUI()
        {
            // Update all UI elements with current game state
            if (scoreManager != null)
            {
                // TODO: Implement GetCurrentScore and GetScoreMultiplier in ScoreManager
                // UpdateScoreDisplay(scoreManager.GetCurrentScore());
                // UpdateMultiplierDisplay(scoreManager.GetScoreMultiplier());
            }
            
            if (gameManager != null)
            {
                // UpdateLivesDisplay(gameManager.GetCurrentLives());
                // UpdateCoinsDisplay(gameManager.GetCurrentCoins());
                // UpdateDistanceDisplay(gameManager.GetDistanceTraveled());
                // UpdateLevelDisplay(gameManager.GetCurrentLevel(), gameManager.GetLevelProgress());
            }
        }
        
        private void UpdateGameplayUI()
        {
            // Called every frame to update dynamic elements
            UpdateWarningIndicators();
        }
        
        #endregion
        
        #region Display Updates
        
        private void UpdateScoreDisplay(int newScore)
        {
            if (currentScore != newScore)
            {
                currentScore = newScore;
                
                if (scoreText != null)
                {
                    scoreText.text = currentScore.ToString("N0");
                    
                    if (enableAnimations && !isAnimatingScore)
                    {
                        StartCoroutine(AnimateScorePunch());
                    }
                }
                
                if (scoreSlider != null)
                {
                    // Update score progress slider (for level progression)
                    float progress = (currentScore % 1000) / 1000f;
                    scoreSlider.value = progress;
                    
                    if (scoreSliderFill != null)
                    {
                        scoreSliderFill.color = scoreSliderGradient.Evaluate(progress);
                    }
                }
                
                PlayScoreSound();
                PlayScoreParticles();
            }
        }
        
        private void UpdateLivesDisplay(int newLives)
        {
            if (currentLives != newLives)
            {
                bool lostLife = newLives < currentLives;
                currentLives = newLives;
                
                if (lifeIcons != null)
                {
                    for (int i = 0; i < lifeIcons.Length; i++)
                    {
                        if (lifeIcons[i] != null)
                        {
                            lifeIcons[i].color = i < currentLives ? fullLifeColor : emptyLifeColor;
                        }
                    }
                }
                
                if (livesAnimator != null)
                {
                    if (lostLife)
                    {
                        livesAnimator.SetTrigger("LifeLost");
                        PlayLifeLostSound();
                    }
                    else
                    {
                        livesAnimator.SetTrigger("LifeGained");
                        PlayLifePickupSound();
                    }
                }
            }
        }
        
        private void UpdateCoinsDisplay(int newCoins)
        {
            if (currentCoins != newCoins)
            {
                currentCoins = newCoins;
                
                if (coinsText != null)
                {
                    coinsText.text = currentCoins.ToString();
                }
            }
        }
        
        private void UpdateDistanceDisplay(float newDistance)
        {
            if (Mathf.Abs(currentDistance - newDistance) > 0.1f)
            {
                currentDistance = newDistance;
                
                if (distanceText != null)
                {
                    distanceText.text = $"{currentDistance:F0}m";
                }
            }
        }
        
        private void UpdateLevelDisplay(int newLevel, float progress)
        {
            if (currentLevel != newLevel)
            {
                currentLevel = newLevel;
                
                if (levelText != null)
                {
                    levelText.text = $"Level {currentLevel}";
                }
            }
            
            if (levelProgressSlider != null)
            {
                levelProgressSlider.value = progress;
            }
        }
        
        private void UpdateMultiplierDisplay(float newMultiplier)
        {
            if (Mathf.Abs(currentMultiplier - newMultiplier) > 0.01f)
            {
                currentMultiplier = newMultiplier;
                
                if (multiplierText != null)
                {
                    multiplierText.text = $"x{currentMultiplier:F1}";
                    multiplierText.gameObject.SetActive(currentMultiplier > 1f);
                }
            }
        }
        
        private void UpdateComboDisplay(int newCombo)
        {
            if (currentCombo != newCombo)
            {
                currentCombo = newCombo;
                
                if (comboText != null)
                {
                    comboText.text = $"Combo x{currentCombo}";
                }
                
                if (comboIndicator != null)
                {
                    comboIndicator.SetActive(currentCombo > 1);
                }
                
                if (comboAnimator != null && currentCombo > 1)
                {
                    comboAnimator.SetTrigger("ComboUpdate");
                }
                
                if (currentCombo > 1)
                {
                    PlayComboSound();
                }
            }
        }
        
        #endregion
        
        #region Power-Up Management
        
        private void OnPowerUpActivated(BasePowerUp powerUp)
        {
            ShowPowerUpIndicator(powerUp);
        }
        
        private void OnPowerUpExpired(BasePowerUp powerUp)
        {
            HidePowerUpIndicator(powerUp);
        }
        
        private void OnPowerUpCollected(BasePowerUp powerUp)
        {
            // Play collection effects
            if (collectParticles != null)
            {
                // TODO: Fix powerUp transform access
                // collectParticles.transform.position = Camera.main.WorldToScreenPoint(powerUp.transform.position);
                collectParticles.Play();
            }
        }
        
        private void ShowPowerUpIndicator(BasePowerUp powerUp)
        {
            // Find an available power-up slot
            PowerUpIndicator availableSlot = null;
            
            foreach (var slot in powerUpSlots)
            {
                if (slot != null && !slot.IsActive)
                {
                    availableSlot = slot;
                    break;
                }
            }
            
            if (availableSlot != null)
            {
                availableSlot.ShowPowerUp(powerUp);
                activePowerUpIndicators.Add(availableSlot);
            }
        }
        
        private void HidePowerUpIndicator(BasePowerUp powerUp)
        {
            // Find the indicator showing this power-up
            PowerUpIndicator targetIndicator = null;
            
            foreach (var indicator in activePowerUpIndicators)
            {
                if (indicator.GetPowerUp() == powerUp)
                {
                    targetIndicator = indicator;
                    break;
                }
            }
            
            if (targetIndicator != null)
            {
                targetIndicator.HidePowerUp();
                activePowerUpIndicators.Remove(targetIndicator);
            }
        }
        
        #endregion
        
        #region Warning Systems
        
        private void UpdateWarningIndicators()
        {
            // Update low health warning
            bool showHealthWarning = currentLives <= 1;
            if (lowHealthWarning != null)
            {
                lowHealthWarning.SetActive(showHealthWarning);
            }
            
            if (healthWarningOverlay != null && showHealthWarning)
            {
                float alpha = (Mathf.Sin(Time.time * warningFlashSpeed) + 1f) * 0.1f;
                Color overlayColor = healthWarningOverlay.color;
                overlayColor.a = alpha;
                healthWarningOverlay.color = overlayColor;
            }
        }
        
        private void UpdateWarningEffects()
        {
            // Called every frame for dynamic warning effects
            UpdateWarningIndicators();
        }
        
        public void ShowObstacleWarning(float duration = 2f)
        {
            if (obstacleWarning != null)
            {
                StartCoroutine(ShowObstacleWarningCoroutine(duration));
            }
        }
        
        private IEnumerator ShowObstacleWarningCoroutine(float duration)
        {
            obstacleWarning.SetActive(true);
            yield return new WaitForSeconds(duration);
            obstacleWarning.SetActive(false);
        }
        
        #endregion
        
        #region Animations
        
        private IEnumerator AnimateScorePunch()
        {
            if (scoreText == null) yield break;
            
            isAnimatingScore = true;
            
            float elapsed = 0f;
            Vector3 targetScale = originalScoreScale * scorePunchScale;
            
            // Scale up
            while (elapsed < scorePunchDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / (scorePunchDuration * 0.5f);
                float curvedProgress = scorePunchCurve.Evaluate(progress);
                
                scoreText.transform.localScale = Vector3.Lerp(originalScoreScale, targetScale, curvedProgress);
                yield return null;
            }
            
            // Scale down
            elapsed = 0f;
            while (elapsed < scorePunchDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / (scorePunchDuration * 0.5f);
                
                scoreText.transform.localScale = Vector3.Lerp(targetScale, originalScoreScale, progress);
                yield return null;
            }
            
            scoreText.transform.localScale = originalScoreScale;
            isAnimatingScore = false;
        }
        
        #endregion
        
        #region Button Events
        
        private void ToggleMute()
        {
            if (audioManager != null)
            {
                // TODO: Implement IsMuted and SetMuted in AudioManager
                // bool isMuted = audioManager.IsMuted();
                // audioManager.SetMuted(!isMuted);
                
                // UpdateMuteButtonIcon(!isMuted);
                OnMuteToggled?.Invoke();
            }
        }
        
        private void UpdateMuteButtonIcon(bool isMuted)
        {
            if (muteButtonIcon != null)
            {
                muteButtonIcon.sprite = isMuted ? soundOffIcon : soundOnIcon;
            }
        }
        
        #endregion
        
        #region Audio
        
        private void PlayScoreSound()
        {
            if (scoreIncreaseSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(scoreIncreaseSound, uiAudioVolume);
            }
        }
        
        private void PlayComboSound()
        {
            if (comboSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(comboSound, uiAudioVolume);
            }
        }
        
        private void PlayLifePickupSound()
        {
            if (lifePickupSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(lifePickupSound, uiAudioVolume);
            }
        }
        
        private void PlayLifeLostSound()
        {
            if (lifeLostSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(lifeLostSound, uiAudioVolume);
            }
        }
        
        #endregion
        
        #region Visual Effects
        
        private void PlayScoreParticles()
        {
            if (scoreParticles != null)
            {
                scoreParticles.Play();
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Manually update specific UI elements
        /// </summary>
        public void UpdateScore(int score) => UpdateScoreDisplay(score);
        public void UpdateLives(int lives) => UpdateLivesDisplay(lives);
        public void UpdateCoins(int coins) => UpdateCoinsDisplay(coins);
        public void UpdateDistance(float distance) => UpdateDistanceDisplay(distance);
        public void UpdateLevel(int level, float progress) => UpdateLevelDisplay(level, progress);
        public void UpdateMultiplier(float multiplier) => UpdateMultiplierDisplay(multiplier);
        public void UpdateCombo(int combo) => UpdateComboDisplay(combo);
        
        /// <summary>
        /// Show or hide the HUD
        /// </summary>
        public void SetHUDVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
        
        /// <summary>
        /// Enable or disable HUD interactions
        /// </summary>
        public void SetHUDInteractable(bool interactable)
        {
            if (pauseButton != null)
                pauseButton.interactable = interactable;
            
            if (settingsButton != null)
                settingsButton.interactable = interactable;
            
            if (muteButton != null)
                muteButton.interactable = interactable;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Individual power-up indicator component
    /// </summary>
    [System.Serializable]
    public class PowerUpIndicator : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image timerFill;
        [SerializeField] private TextMeshProUGUI stackCountText;
        [SerializeField] private CanvasGroup canvasGroup;
        
        private BasePowerUp currentPowerUp;
        private bool isActive = false;
        
        public bool IsActive => isActive;
        public BasePowerUp GetPowerUp() => currentPowerUp;
        
        public void Initialize()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            
            SetActive(false);
        }
        
        public void ShowPowerUp(BasePowerUp powerUp)
        {
            currentPowerUp = powerUp;
            isActive = true;
            
            if (iconImage != null)
            {
                // Set power-up icon based on type
                iconImage.sprite = GetPowerUpIcon(powerUp.Type);
            }
            
            if (stackCountText != null)
            {
                // stackCountText.text = powerUp.StackCount > 1 ? powerUp.StackCount.ToString() : "";
                stackCountText.gameObject.SetActive(false); // TODO: Implement stack count
            }
            
            SetActive(true);
            StartCoroutine(UpdateTimer());
        }
        
        public void HidePowerUp()
        {
            currentPowerUp = null;
            isActive = false;
            SetActive(false);
        }
        
        public void SetActive(bool active)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = active ? 1f : 0f;
                canvasGroup.interactable = active;
                canvasGroup.blocksRaycasts = active;
            }
            else
            {
                gameObject.SetActive(active);
            }
        }
        
        private IEnumerator UpdateTimer()
        {
            while (currentPowerUp != null && currentPowerUp.IsActive)
            {
                if (timerFill != null)
                {
                    // float progress = currentPowerUp.GetRemainingDuration() / currentPowerUp.Duration;
                    // timerFill.fillAmount = progress;
                    timerFill.fillAmount = 1f; // TODO: Implement duration tracking
                }
                
                yield return null;
            }
        }
        
        private Sprite GetPowerUpIcon(BasePowerUp.PowerUpType type)
        {
            // TODO: Load appropriate icon sprite based on power-up type
            return null;
        }
    }
}