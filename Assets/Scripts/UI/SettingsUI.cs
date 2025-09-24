using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using CatchMeowIfYouCan.Core;

namespace CatchMeowIfYouCan.UI
{
    /// <summary>
    /// Settings UI controller
    /// Handles game settings including audio, graphics, controls, and gameplay options
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetDefaultsButton;
        [SerializeField] private Button applyButton;
        
        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private TextMeshProUGUI masterVolumeText;
        [SerializeField] private TextMeshProUGUI musicVolumeText;
        [SerializeField] private TextMeshProUGUI sfxVolumeText;
        
        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Slider frameRateSlider;
        [SerializeField] private TextMeshProUGUI frameRateText;
        
        [Header("Gameplay Settings")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Toggle autoJumpToggle;
        [SerializeField] private Toggle powerUpIndicatorsToggle;
        [SerializeField] private Toggle particleEffectsToggle;
        [SerializeField] private Toggle screenShakeToggle;
        [SerializeField] private TextMeshProUGUI sensitivityText;
        
        [Header("Control Settings")]
        [SerializeField] private TMP_Dropdown controlSchemeDropdown;
        [SerializeField] private Button[] keyBindingButtons;
        [SerializeField] private TextMeshProUGUI[] keyBindingTexts;
        [SerializeField] private GameObject keyBindingPanel;
        
        [Header("Advanced Settings")]
        [SerializeField] private GameObject advancedPanel;
        [SerializeField] private Button showAdvancedButton;
        [SerializeField] private Toggle debugModeToggle;
        [SerializeField] private Toggle showFPSToggle;
        [SerializeField] private Slider difficultySlider;
        [SerializeField] private TextMeshProUGUI difficultyText;
        
        [Header("Tab System")]
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private GameObject[] tabPanels;
        [SerializeField] private Color activeTabColor = Color.white;
        [SerializeField] private Color inactiveTabColor = Color.gray;
        
        [Header("Animation Settings")]
        [SerializeField] private bool enableAnimations = true;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Audio")]
        [SerializeField] private AudioClip settingsOpenSound;
        [SerializeField] private AudioClip settingsCloseSound;
        [SerializeField] private AudioClip tabSwitchSound;
        [SerializeField] private AudioClip settingChangedSound;
        [SerializeField] private float audioVolume = 0.8f;
        
        // Component references
        private AudioManager audioManager;
        private CanvasGroup canvasGroup;
        
        // Settings state
        private GameSettings currentSettings;
        private GameSettings originalSettings;
        private int currentTab = 0;
        private bool hasUnsavedChanges = false;
        private bool isKeyBinding = false;
        private int keyBindingIndex = -1;
        
        // Resolution options
        private Resolution[] availableResolutions;
        
        // Events
        public System.Action OnBackButtonClicked;
        public System.Action OnSettingsChanged;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            SetupSettingsUI();
        }
        
        private void Update()
        {
            HandleKeyBinding();
        }
        
        private void OnEnable()
        {
            LoadCurrentSettings();
            RefreshAllSettings();
            PlaySettingsOpenSound();
        }
        
        private void OnDisable()
        {
            PlaySettingsCloseSound();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            // Get component references
            audioManager = FindObjectOfType<AudioManager>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // Setup button events
            SetupButtonEvents();
            
            // Setup slider events
            SetupSliderEvents();
            
            // Setup toggle events
            SetupToggleEvents();
            
            // Setup dropdown events
            SetupDropdownEvents();
            
            // Initialize tab system
            InitializeTabSystem();
            
            // Initialize resolution options
            InitializeResolutionOptions();
        }
        
        private void SetupButtonEvents()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
            
            if (resetDefaultsButton != null)
                resetDefaultsButton.onClick.AddListener(ResetToDefaults);
            
            if (applyButton != null)
                applyButton.onClick.AddListener(ApplySettings);
            
            if (showAdvancedButton != null)
                showAdvancedButton.onClick.AddListener(ToggleAdvancedSettings);
            
            // Setup tab buttons
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int tabIndex = i; // Capture for closure
                if (tabButtons[i] != null)
                {
                    tabButtons[i].onClick.AddListener(() => SwitchTab(tabIndex));
                }
            }
            
            // Setup key binding buttons
            for (int i = 0; i < keyBindingButtons.Length; i++)
            {
                int bindingIndex = i; // Capture for closure
                if (keyBindingButtons[i] != null)
                {
                    keyBindingButtons[i].onClick.AddListener(() => StartKeyBinding(bindingIndex));
                }
            }
        }
        
        private void SetupSliderEvents()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            
            if (sensitivitySlider != null)
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            
            if (frameRateSlider != null)
                frameRateSlider.onValueChanged.AddListener(OnFrameRateChanged);
            
            if (difficultySlider != null)
                difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
        }
        
        private void SetupToggleEvents()
        {
            if (muteToggle != null)
                muteToggle.onValueChanged.AddListener(OnMuteToggled);
            
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
            
            if (vsyncToggle != null)
                vsyncToggle.onValueChanged.AddListener(OnVSyncToggled);
            
            if (autoJumpToggle != null)
                autoJumpToggle.onValueChanged.AddListener(OnAutoJumpToggled);
            
            if (powerUpIndicatorsToggle != null)
                powerUpIndicatorsToggle.onValueChanged.AddListener(OnPowerUpIndicatorsToggled);
            
            if (particleEffectsToggle != null)
                particleEffectsToggle.onValueChanged.AddListener(OnParticleEffectsToggled);
            
            if (screenShakeToggle != null)
                screenShakeToggle.onValueChanged.AddListener(OnScreenShakeToggled);
            
            if (debugModeToggle != null)
                debugModeToggle.onValueChanged.AddListener(OnDebugModeToggled);
            
            if (showFPSToggle != null)
                showFPSToggle.onValueChanged.AddListener(OnShowFPSToggled);
        }
        
        private void SetupDropdownEvents()
        {
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            
            if (controlSchemeDropdown != null)
                controlSchemeDropdown.onValueChanged.AddListener(OnControlSchemeChanged);
        }
        
        private void InitializeTabSystem()
        {
            // Show first tab by default
            SwitchTab(0);
        }
        
        private void InitializeResolutionOptions()
        {
            availableResolutions = Screen.resolutions;
            
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                List<string> resolutionOptions = new List<string>();
                
                foreach (var resolution in availableResolutions)
                {
                    string option = $"{resolution.width} x {resolution.height} @ {resolution.refreshRateRatio.value}Hz";
                    resolutionOptions.Add(option);
                }
                
                resolutionDropdown.AddOptions(resolutionOptions);
            }
        }
        
        private void SetupSettingsUI()
        {
            // Initialize settings structure
            currentSettings = new GameSettings();
            originalSettings = new GameSettings();
            
            // Setup initial state
            if (advancedPanel != null)
                advancedPanel.SetActive(false);
            
            if (keyBindingPanel != null)
                keyBindingPanel.SetActive(false);
        }
        
        #endregion
        
        #region Settings Management
        
        private void LoadCurrentSettings()
        {
            // Load settings from PlayerPrefs or save system
            currentSettings.masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            currentSettings.musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
            currentSettings.sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 0.8f);
            currentSettings.isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
            
            currentSettings.qualityLevel = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
            currentSettings.isFullscreen = PlayerPrefs.GetInt("IsFullscreen", Screen.fullScreen ? 1 : 0) == 1;
            currentSettings.vsyncEnabled = PlayerPrefs.GetInt("VSync", QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
            currentSettings.targetFrameRate = PlayerPrefs.GetInt("TargetFrameRate", 60);
            
            currentSettings.sensitivity = PlayerPrefs.GetFloat("Sensitivity", 1f);
            currentSettings.autoJump = PlayerPrefs.GetInt("AutoJump", 0) == 1;
            currentSettings.showPowerUpIndicators = PlayerPrefs.GetInt("PowerUpIndicators", 1) == 1;
            currentSettings.enableParticleEffects = PlayerPrefs.GetInt("ParticleEffects", 1) == 1;
            currentSettings.enableScreenShake = PlayerPrefs.GetInt("ScreenShake", 1) == 1;
            
            currentSettings.controlScheme = PlayerPrefs.GetInt("ControlScheme", 0);
            currentSettings.difficulty = PlayerPrefs.GetFloat("Difficulty", 1f);
            currentSettings.debugMode = PlayerPrefs.GetInt("DebugMode", 0) == 1;
            currentSettings.showFPS = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
            
            // Store original settings for comparison
            originalSettings = currentSettings.Copy();
        }
        
        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", currentSettings.masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", currentSettings.musicVolume);
            PlayerPrefs.SetFloat("SfxVolume", currentSettings.sfxVolume);
            PlayerPrefs.SetInt("IsMuted", currentSettings.isMuted ? 1 : 0);
            
            PlayerPrefs.SetInt("QualityLevel", currentSettings.qualityLevel);
            PlayerPrefs.SetInt("IsFullscreen", currentSettings.isFullscreen ? 1 : 0);
            PlayerPrefs.SetInt("VSync", currentSettings.vsyncEnabled ? 1 : 0);
            PlayerPrefs.SetInt("TargetFrameRate", currentSettings.targetFrameRate);
            
            PlayerPrefs.SetFloat("Sensitivity", currentSettings.sensitivity);
            PlayerPrefs.SetInt("AutoJump", currentSettings.autoJump ? 1 : 0);
            PlayerPrefs.SetInt("PowerUpIndicators", currentSettings.showPowerUpIndicators ? 1 : 0);
            PlayerPrefs.SetInt("ParticleEffects", currentSettings.enableParticleEffects ? 1 : 0);
            PlayerPrefs.SetInt("ScreenShake", currentSettings.enableScreenShake ? 1 : 0);
            
            PlayerPrefs.SetInt("ControlScheme", currentSettings.controlScheme);
            PlayerPrefs.SetFloat("Difficulty", currentSettings.difficulty);
            PlayerPrefs.SetInt("DebugMode", currentSettings.debugMode ? 1 : 0);
            PlayerPrefs.SetInt("ShowFPS", currentSettings.showFPS ? 1 : 0);
            
            PlayerPrefs.Save();
        }
        
        private void ApplySettings()
        {
            // Apply audio settings
            if (audioManager != null)
            {
                // TODO: Implement AudioManager volume and mute methods
                // audioManager.SetMasterVolume(currentSettings.masterVolume);
                // audioManager.SetMusicVolume(currentSettings.musicVolume);
                // audioManager.SetSfxVolume(currentSettings.sfxVolume);
                // audioManager.SetMuted(currentSettings.isMuted);
            }
            
            // Apply graphics settings
            QualitySettings.SetQualityLevel(currentSettings.qualityLevel);
            Screen.fullScreen = currentSettings.isFullscreen;
            QualitySettings.vSyncCount = currentSettings.vsyncEnabled ? 1 : 0;
            Application.targetFrameRate = currentSettings.targetFrameRate;
            
            // Save settings
            SaveSettings();
            
            // Reset unsaved changes flag
            hasUnsavedChanges = false;
            
            // Fire event
            OnSettingsChanged?.Invoke();
            
            PlaySettingChangedSound();
        }
        
        private void ResetToDefaults()
        {
            currentSettings = GameSettings.GetDefaults();
            RefreshAllSettings();
            hasUnsavedChanges = true;
        }
        
        private void RefreshAllSettings()
        {
            RefreshAudioSettings();
            RefreshGraphicsSettings();
            RefreshGameplaySettings();
            RefreshControlSettings();
            RefreshAdvancedSettings();
        }
        
        #endregion
        
        #region UI Updates
        
        private void RefreshAudioSettings()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = currentSettings.masterVolume;
                if (masterVolumeText != null)
                    masterVolumeText.text = $"{(currentSettings.masterVolume * 100):F0}%";
            }
            
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = currentSettings.musicVolume;
                if (musicVolumeText != null)
                    musicVolumeText.text = $"{(currentSettings.musicVolume * 100):F0}%";
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = currentSettings.sfxVolume;
                if (sfxVolumeText != null)
                    sfxVolumeText.text = $"{(currentSettings.sfxVolume * 100):F0}%";
            }
            
            if (muteToggle != null)
                muteToggle.isOn = currentSettings.isMuted;
        }
        
        private void RefreshGraphicsSettings()
        {
            if (qualityDropdown != null)
                qualityDropdown.value = currentSettings.qualityLevel;
            
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = currentSettings.isFullscreen;
            
            if (vsyncToggle != null)
                vsyncToggle.isOn = currentSettings.vsyncEnabled;
            
            if (frameRateSlider != null)
            {
                frameRateSlider.value = currentSettings.targetFrameRate;
                if (frameRateText != null)
                    frameRateText.text = $"{currentSettings.targetFrameRate} FPS";
            }
        }
        
        private void RefreshGameplaySettings()
        {
            if (sensitivitySlider != null)
            {
                sensitivitySlider.value = currentSettings.sensitivity;
                if (sensitivityText != null)
                    sensitivityText.text = $"{currentSettings.sensitivity:F1}";
            }
            
            if (autoJumpToggle != null)
                autoJumpToggle.isOn = currentSettings.autoJump;
            
            if (powerUpIndicatorsToggle != null)
                powerUpIndicatorsToggle.isOn = currentSettings.showPowerUpIndicators;
            
            if (particleEffectsToggle != null)
                particleEffectsToggle.isOn = currentSettings.enableParticleEffects;
            
            if (screenShakeToggle != null)
                screenShakeToggle.isOn = currentSettings.enableScreenShake;
        }
        
        private void RefreshControlSettings()
        {
            if (controlSchemeDropdown != null)
                controlSchemeDropdown.value = currentSettings.controlScheme;
        }
        
        private void RefreshAdvancedSettings()
        {
            if (difficultySlider != null)
            {
                difficultySlider.value = currentSettings.difficulty;
                if (difficultyText != null)
                {
                    string[] difficultyNames = { "Easy", "Normal", "Hard", "Expert" };
                    int diffIndex = Mathf.Clamp(Mathf.FloorToInt(currentSettings.difficulty * 4), 0, 3);
                    difficultyText.text = difficultyNames[diffIndex];
                }
            }
            
            if (debugModeToggle != null)
                debugModeToggle.isOn = currentSettings.debugMode;
            
            if (showFPSToggle != null)
                showFPSToggle.isOn = currentSettings.showFPS;
        }
        
        #endregion
        
        #region Tab System
        
        private void SwitchTab(int tabIndex)
        {
            if (tabIndex == currentTab) return;
            
            currentTab = tabIndex;
            
            // Update tab buttons
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] != null)
                {
                    var buttonImage = tabButtons[i].GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = (i == currentTab) ? activeTabColor : inactiveTabColor;
                    }
                }
            }
            
            // Update tab panels
            for (int i = 0; i < tabPanels.Length; i++)
            {
                if (tabPanels[i] != null)
                {
                    tabPanels[i].SetActive(i == currentTab);
                }
            }
            
            PlayTabSwitchSound();
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnBackClicked()
        {
            if (hasUnsavedChanges)
            {
                // TODO: Show confirmation dialog
                ApplySettings(); // For now, auto-apply
            }
            
            OnBackButtonClicked?.Invoke();
        }
        
        // Audio Events
        private void OnMasterVolumeChanged(float value)
        {
            currentSettings.masterVolume = value;
            if (masterVolumeText != null)
                masterVolumeText.text = $"{(value * 100):F0}%";
            hasUnsavedChanges = true;
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            currentSettings.musicVolume = value;
            if (musicVolumeText != null)
                musicVolumeText.text = $"{(value * 100):F0}%";
            hasUnsavedChanges = true;
        }
        
        private void OnSfxVolumeChanged(float value)
        {
            currentSettings.sfxVolume = value;
            if (sfxVolumeText != null)
                sfxVolumeText.text = $"{(value * 100):F0}%";
            hasUnsavedChanges = true;
        }
        
        private void OnMuteToggled(bool value)
        {
            currentSettings.isMuted = value;
            hasUnsavedChanges = true;
        }
        
        // Graphics Events
        private void OnQualityChanged(int value)
        {
            currentSettings.qualityLevel = value;
            hasUnsavedChanges = true;
        }
        
        private void OnResolutionChanged(int value)
        {
            if (value < availableResolutions.Length)
            {
                var resolution = availableResolutions[value];
                currentSettings.screenWidth = resolution.width;
                currentSettings.screenHeight = resolution.height;
                currentSettings.refreshRate = (int)resolution.refreshRateRatio.value;
                hasUnsavedChanges = true;
            }
        }
        
        private void OnFullscreenToggled(bool value)
        {
            currentSettings.isFullscreen = value;
            hasUnsavedChanges = true;
        }
        
        private void OnVSyncToggled(bool value)
        {
            currentSettings.vsyncEnabled = value;
            hasUnsavedChanges = true;
        }
        
        private void OnFrameRateChanged(float value)
        {
            currentSettings.targetFrameRate = Mathf.RoundToInt(value);
            if (frameRateText != null)
                frameRateText.text = $"{currentSettings.targetFrameRate} FPS";
            hasUnsavedChanges = true;
        }
        
        // Gameplay Events
        private void OnSensitivityChanged(float value)
        {
            currentSettings.sensitivity = value;
            if (sensitivityText != null)
                sensitivityText.text = $"{value:F1}";
            hasUnsavedChanges = true;
        }
        
        private void OnAutoJumpToggled(bool value)
        {
            currentSettings.autoJump = value;
            hasUnsavedChanges = true;
        }
        
        private void OnPowerUpIndicatorsToggled(bool value)
        {
            currentSettings.showPowerUpIndicators = value;
            hasUnsavedChanges = true;
        }
        
        private void OnParticleEffectsToggled(bool value)
        {
            currentSettings.enableParticleEffects = value;
            hasUnsavedChanges = true;
        }
        
        private void OnScreenShakeToggled(bool value)
        {
            currentSettings.enableScreenShake = value;
            hasUnsavedChanges = true;
        }
        
        // Control Events
        private void OnControlSchemeChanged(int value)
        {
            currentSettings.controlScheme = value;
            hasUnsavedChanges = true;
        }
        
        // Advanced Events
        private void OnDifficultyChanged(float value)
        {
            currentSettings.difficulty = value;
            if (difficultyText != null)
            {
                string[] difficultyNames = { "Easy", "Normal", "Hard", "Expert" };
                int diffIndex = Mathf.Clamp(Mathf.FloorToInt(value * 4), 0, 3);
                difficultyText.text = difficultyNames[diffIndex];
            }
            hasUnsavedChanges = true;
        }
        
        private void OnDebugModeToggled(bool value)
        {
            currentSettings.debugMode = value;
            hasUnsavedChanges = true;
        }
        
        private void OnShowFPSToggled(bool value)
        {
            currentSettings.showFPS = value;
            hasUnsavedChanges = true;
        }
        
        #endregion
        
        #region Key Binding
        
        private void StartKeyBinding(int bindingIndex)
        {
            isKeyBinding = true;
            keyBindingIndex = bindingIndex;
            
            if (keyBindingPanel != null)
                keyBindingPanel.SetActive(true);
            
            if (keyBindingButtons[bindingIndex] != null)
            {
                var buttonText = keyBindingButtons[bindingIndex].GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = "Press Key...";
            }
        }
        
        private void HandleKeyBinding()
        {
            if (!isKeyBinding) return;
            
            if (Input.inputString.Length > 0)
            {
                KeyCode newKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), Input.inputString.ToUpper());
                
                // Update key binding
                // TODO: Implement key binding storage system
                
                // Update UI
                if (keyBindingButtons[keyBindingIndex] != null)
                {
                    var buttonText = keyBindingButtons[keyBindingIndex].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                        buttonText.text = newKey.ToString();
                }
                
                // End key binding
                isKeyBinding = false;
                keyBindingIndex = -1;
                
                if (keyBindingPanel != null)
                    keyBindingPanel.SetActive(false);
                
                hasUnsavedChanges = true;
            }
            
            // Cancel on escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isKeyBinding = false;
                keyBindingIndex = -1;
                
                if (keyBindingPanel != null)
                    keyBindingPanel.SetActive(false);
            }
        }
        
        #endregion
        
        #region Advanced Settings
        
        private void ToggleAdvancedSettings()
        {
            if (advancedPanel != null)
            {
                bool isActive = !advancedPanel.activeSelf;
                advancedPanel.SetActive(isActive);
                
                if (showAdvancedButton != null)
                {
                    var buttonText = showAdvancedButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                        buttonText.text = isActive ? "Hide Advanced" : "Show Advanced";
                }
            }
        }
        
        #endregion
        
        #region Audio
        
        private void PlaySettingsOpenSound()
        {
            if (settingsOpenSound != null && audioManager != null)
            {
                // TODO: Implement PlayCustomSfx in AudioManager
                // audioManager.PlayCustomSfx(settingsOpenSound, audioVolume);
            }
        }
        
        private void PlaySettingsCloseSound()
        {
            if (settingsCloseSound != null && audioManager != null)
            {
                // TODO: Implement PlayCustomSfx in AudioManager
                // audioManager.PlayCustomSfx(settingsCloseSound, audioVolume);
            }
        }
        
        private void PlayTabSwitchSound()
        {
            if (tabSwitchSound != null && audioManager != null)
            {
                // TODO: Implement PlayCustomSfx in AudioManager
                // audioManager.PlayCustomSfx(tabSwitchSound, audioVolume * 0.7f);
            }
        }
        
        private void PlaySettingChangedSound()
        {
            if (settingChangedSound != null && audioManager != null)
            {
                // TODO: Implement PlayCustomSfx in AudioManager
                // audioManager.PlayCustomSfx(settingChangedSound, audioVolume * 0.8f);
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Check if there are unsaved changes
        /// </summary>
        public bool HasUnsavedChanges => hasUnsavedChanges;
        
        /// <summary>
        /// Get current settings
        /// </summary>
        public GameSettings GetCurrentSettings() => currentSettings.Copy();
        
        /// <summary>
        /// Force refresh all UI elements
        /// </summary>
        public void RefreshUI()
        {
            RefreshAllSettings();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Game settings data structure
    /// </summary>
    [System.Serializable]
    public class GameSettings
    {
        // Audio Settings
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 0.8f;
        public bool isMuted = false;
        
        // Graphics Settings
        public int qualityLevel = 2;
        public int screenWidth = 1920;
        public int screenHeight = 1080;
        public int refreshRate = 60;
        public bool isFullscreen = true;
        public bool vsyncEnabled = true;
        public int targetFrameRate = 60;
        
        // Gameplay Settings
        public float sensitivity = 1f;
        public bool autoJump = false;
        public bool showPowerUpIndicators = true;
        public bool enableParticleEffects = true;
        public bool enableScreenShake = true;
        
        // Control Settings
        public int controlScheme = 0;
        
        // Advanced Settings
        public float difficulty = 1f;
        public bool debugMode = false;
        public bool showFPS = false;
        
        public GameSettings Copy()
        {
            return (GameSettings)this.MemberwiseClone();
        }
        
        public static GameSettings GetDefaults()
        {
            return new GameSettings();
        }
        
        public bool Equals(GameSettings other)
        {
            if (other == null) return false;
            
            return masterVolume == other.masterVolume &&
                   musicVolume == other.musicVolume &&
                   sfxVolume == other.sfxVolume &&
                   isMuted == other.isMuted &&
                   qualityLevel == other.qualityLevel &&
                   isFullscreen == other.isFullscreen &&
                   vsyncEnabled == other.vsyncEnabled &&
                   targetFrameRate == other.targetFrameRate &&
                   sensitivity == other.sensitivity &&
                   autoJump == other.autoJump &&
                   showPowerUpIndicators == other.showPowerUpIndicators &&
                   enableParticleEffects == other.enableParticleEffects &&
                   enableScreenShake == other.enableScreenShake &&
                   controlScheme == other.controlScheme &&
                   difficulty == other.difficulty &&
                   debugMode == other.debugMode &&
                   showFPS == other.showFPS;
        }
    }
}