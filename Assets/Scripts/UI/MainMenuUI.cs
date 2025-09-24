using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using CatchMeowIfYouCan.Core;

namespace CatchMeowIfYouCan.UI
{
    /// <summary>
    /// Main menu UI controller
    /// Handles main menu interactions, animations, and display
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button leaderboardButton;
        
        [Header("Display Elements")]
        [SerializeField] private TextMeshProUGUI gameTitle;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private Image gameLogo;
        [SerializeField] private Image backgroundImage;
        
        [Header("Animation Settings")]
        [SerializeField] private bool enableAnimations = true;
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private AnimationCurve buttonAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float logoFloatSpeed = 1f;
        [SerializeField] private float logoFloatAmount = 10f;
        
        [Header("Button Effects")]
        [SerializeField] private float buttonHoverScale = 1.1f;
        [SerializeField] private float buttonPressScale = 0.95f;
        [SerializeField] private Color buttonHoverColor = Color.white;
        [SerializeField] private Color buttonNormalColor = Color.gray;
        
        [Header("Audio")]
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField] private AudioClip titleMusicLoop;
        [SerializeField] private float musicVolume = 0.6f;
        [SerializeField] private float sfxVolume = 0.8f;
        
        [Header("Background Effects")]
        [SerializeField] private ParticleSystem backgroundParticles;
        [SerializeField] private Animator backgroundAnimator;
        [SerializeField] private bool enableBackgroundAnimation = true;
        
        // Component references
        private AudioManager audioManager;
        private ScoreManager scoreManager;
        private CanvasGroup canvasGroup;
        
        // Animation state
        private bool isAnimating = false;
        private Vector3 originalLogoPosition;
        private Vector3[] originalButtonPositions;
        private Vector3[] originalButtonScales;
        
        // Events
        public System.Action OnPlayButtonClicked;
        public System.Action OnSettingsButtonClicked;
        public System.Action OnQuitButtonClicked;
        public System.Action OnAchievementsButtonClicked;
        public System.Action OnLeaderboardButtonClicked;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            SetupMainMenu();
        }
        
        private void Update()
        {
            if (enableAnimations)
            {
                UpdateAnimations();
            }
        }
        
        private void OnEnable()
        {
            StartMenuAnimations();
        }
        
        private void OnDisable()
        {
            StopMenuAnimations();
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
            
            // Store original positions and scales for animations
            StoreOriginalTransforms();
            
            // Setup button events
            SetupButtonEvents();
        }
        
        private void StoreOriginalTransforms()
        {
            if (gameLogo != null)
            {
                originalLogoPosition = gameLogo.transform.localPosition;
            }
            
            Button[] buttons = { playButton, settingsButton, quitButton, achievementsButton, leaderboardButton };
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
        }
        
        private void SetupButtonEvents()
        {
            // Setup button click events
            if (playButton != null)
            {
                playButton.onClick.AddListener(() => OnButtonClicked(OnPlayButtonClicked));
                SetupButtonHoverEffects(playButton);
            }
            
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() => OnButtonClicked(OnSettingsButtonClicked));
                SetupButtonHoverEffects(settingsButton);
            }
            
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(() => OnButtonClicked(OnQuitButtonClicked));
                SetupButtonHoverEffects(quitButton);
            }
            
            if (achievementsButton != null)
            {
                achievementsButton.onClick.AddListener(() => OnButtonClicked(OnAchievementsButtonClicked));
                SetupButtonHoverEffects(achievementsButton);
            }
            
            if (leaderboardButton != null)
            {
                leaderboardButton.onClick.AddListener(() => OnButtonClicked(OnLeaderboardButtonClicked));
                SetupButtonHoverEffects(leaderboardButton);
            }
        }
        
        private void SetupButtonHoverEffects(Button button)
        {
            // Add hover effects using EventTrigger
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
            
            // Pointer Down
            var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => OnButtonPress(button, true));
            eventTrigger.triggers.Add(pointerDown);
            
            // Pointer Up
            var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => OnButtonPress(button, false));
            eventTrigger.triggers.Add(pointerUp);
        }
        
        private void SetupMainMenu()
        {
            // Update display elements
            UpdateHighScore();
            UpdateVersionText();
            UpdateGameTitle();
            
            // Start background music
            PlayBackgroundMusic();
            
            // Start background effects
            StartBackgroundEffects();
            
            Debug.Log("Main menu setup complete");
        }
        
        #endregion
        
        #region UI Updates
        
        private void UpdateHighScore()
        {
            if (highScoreText != null && scoreManager != null)
            {
                // TODO: Implement GetHighScore method in ScoreManager
                int highScore = 0; // scoreManager.GetHighScore();
                highScoreText.text = $"High Score: {highScore:N0}";
            }
        }
        
        private void UpdateVersionText()
        {
            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
            }
        }
        
        private void UpdateGameTitle()
        {
            if (gameTitle != null)
            {
                gameTitle.text = "Catch Meow If You Can";
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
            
            if (isHovering)
            {
                PlayHoverSound();
                StartCoroutine(AnimateButtonScale(button, buttonHoverScale));
                AnimateButtonColor(button, buttonHoverColor);
            }
            else
            {
                StartCoroutine(AnimateButtonScale(button, 1f));
                AnimateButtonColor(button, buttonNormalColor);
            }
        }
        
        private void OnButtonPress(Button button, bool isPressed)
        {
            if (isAnimating) return;
            
            float targetScale = isPressed ? buttonPressScale : buttonHoverScale;
            StartCoroutine(AnimateButtonScale(button, targetScale));
        }
        
        #endregion
        
        #region Animations
        
        private void StartMenuAnimations()
        {
            if (!enableAnimations) return;
            
            StartCoroutine(PlayIntroAnimation());
        }
        
        private void StopMenuAnimations()
        {
            StopAllCoroutines();
        }
        
        private IEnumerator PlayIntroAnimation()
        {
            isAnimating = true;
            
            // Start with everything invisible
            canvasGroup.alpha = 0f;
            
            // Animate logo first
            if (gameLogo != null)
            {
                yield return StartCoroutine(AnimateLogoIntro());
            }
            
            // Fade in main menu
            yield return StartCoroutine(FadeIn());
            
            // Animate buttons in sequence
            yield return StartCoroutine(AnimateButtonsIntro());
            
            isAnimating = false;
        }
        
        private IEnumerator AnimateLogoIntro()
        {
            if (gameLogo == null) yield break;
            
            Vector3 startPos = originalLogoPosition - Vector3.up * 200f;
            gameLogo.transform.localPosition = startPos;
            
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / animationDuration;
                float curvedProgress = buttonAnimationCurve.Evaluate(progress);
                
                gameLogo.transform.localPosition = Vector3.Lerp(startPos, originalLogoPosition, curvedProgress);
                yield return null;
            }
            
            gameLogo.transform.localPosition = originalLogoPosition;
        }
        
        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / animationDuration;
                
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
        
        private IEnumerator AnimateButtonsIntro()
        {
            Button[] buttons = { playButton, settingsButton, quitButton, achievementsButton, leaderboardButton };
            
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    StartCoroutine(AnimateButtonIntro(buttons[i], i));
                    yield return new WaitForSecondsRealtime(0.1f); // Stagger the animations
                }
            }
        }
        
        private IEnumerator AnimateButtonIntro(Button button, int index)
        {
            if (button == null) yield break;
            
            Vector3 startPos = originalButtonPositions[index] - Vector3.right * 300f;
            Vector3 targetPos = originalButtonPositions[index];
            
            button.transform.localPosition = startPos;
            button.transform.localScale = Vector3.zero;
            
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / animationDuration;
                float curvedProgress = buttonAnimationCurve.Evaluate(progress);
                
                button.transform.localPosition = Vector3.Lerp(startPos, targetPos, curvedProgress);
                button.transform.localScale = Vector3.Lerp(Vector3.zero, originalButtonScales[index], curvedProgress);
                yield return null;
            }
            
            button.transform.localPosition = targetPos;
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
        
        private void UpdateAnimations()
        {
            // Logo floating animation
            if (gameLogo != null && !isAnimating)
            {
                float floatOffset = Mathf.Sin(Time.time * logoFloatSpeed) * logoFloatAmount;
                gameLogo.transform.localPosition = originalLogoPosition + Vector3.up * floatOffset;
            }
        }
        
        #endregion
        
        #region Audio
        
        private void PlayBackgroundMusic()
        {
            if (titleMusicLoop != null && audioManager != null)
            {
                // TODO: Use available AudioManager methods
                // audioManager.PlayMusic(titleMusicLoop);
            }
        }
        
        private void PlayButtonSound()
        {
            if (audioManager != null)
            {
                // TODO: Implement PlayButtonSound method in AudioManager or use available method
                // audioManager.PlayButtonSound();
            }
        }
        
        private void PlayHoverSound()
        {
            if (buttonHoverSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(buttonHoverSound, sfxVolume * 0.5f);
            }
        }
        
        #endregion
        
        #region Background Effects
        
        private void StartBackgroundEffects()
        {
            if (backgroundParticles != null)
            {
                backgroundParticles.Play();
            }
            
            if (backgroundAnimator != null && enableBackgroundAnimation)
            {
                backgroundAnimator.SetBool("IsActive", true);
            }
        }
        
        private void StopBackgroundEffects()
        {
            if (backgroundParticles != null)
            {
                backgroundParticles.Stop();
            }
            
            if (backgroundAnimator != null)
            {
                backgroundAnimator.SetBool("IsActive", false);
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Manually refresh the main menu display
        /// </summary>
        public void RefreshMainMenu()
        {
            UpdateHighScore();
            UpdateVersionText();
        }
        
        /// <summary>
        /// Enable or disable button interactions
        /// </summary>
        public void SetButtonsInteractable(bool interactable)
        {
            Button[] buttons = { playButton, settingsButton, quitButton, achievementsButton, leaderboardButton };
            
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.interactable = interactable;
                }
            }
        }
        
        /// <summary>
        /// Show/hide achievements button based on platform support
        /// </summary>
        public void SetAchievementsButtonVisible(bool visible)
        {
            if (achievementsButton != null)
            {
                achievementsButton.gameObject.SetActive(visible);
            }
        }
        
        /// <summary>
        /// Show/hide leaderboard button based on platform support
        /// </summary>
        public void SetLeaderboardButtonVisible(bool visible)
        {
            if (leaderboardButton != null)
            {
                leaderboardButton.gameObject.SetActive(visible);
            }
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            // Stop background music
            if (audioManager != null)
            {
                audioManager.StopMusic();
            }
            
            // Stop background effects
            StopBackgroundEffects();
        }
        
        #endregion
    }
}