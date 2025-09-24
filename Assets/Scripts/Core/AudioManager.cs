using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CatchMeowIfYouCan.Core
{
    /// <summary>
    /// Manages all audio in the game including music, sound effects, and audio settings
    /// Provides centralized audio control with volume management and audio pooling
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private int sfxPoolSize = 10;
        
        [Header("Music Clips")]
        [SerializeField] private AudioClip mainMenuMusic;
        [SerializeField] private AudioClip gameplayMusic1;
        [SerializeField] private AudioClip gameplayMusic2;
        [SerializeField] private AudioClip gameOverMusic;
        
        [Header("Player SFX")]
        [SerializeField] private AudioClip catMeow;
        [SerializeField] private AudioClip footsteps;
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip slideSound;
        
        [Header("Collectible SFX")]
        [SerializeField] private AudioClip coinCollectSound;
        [SerializeField] private AudioClip fishCollectSound;
        [SerializeField] private AudioClip powerUpPickupSound;
        
        [Header("Obstacle SFX")]
        [SerializeField] private AudioClip carHornSound;
        [SerializeField] private AudioClip crashSound;
        [SerializeField] private AudioClip dogBarkSound;
        
        [Header("PowerUp SFX")]
        [SerializeField] private AudioClip magnetActivateSound;
        [SerializeField] private AudioClip shieldActivateSound;
        [SerializeField] private AudioClip speedBoostSound;
        
        [Header("UI SFX")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip menuSelectSound;
        [SerializeField] private AudioClip gameOverSound;
        [SerializeField] private AudioClip gameStartSound;
        
        [Header("Audio Settings")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 0.7f;
        [SerializeField] private float sfxVolume = 0.8f;
        [SerializeField] private float uiVolume = 0.9f;
        [SerializeField] private bool muteOnStart = false;
        
        [Header("Advanced Settings")]
        [SerializeField] private float fadeTime = 1f;
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);
        
        // Audio pools
        private List<AudioSource> sfxPool;
        private int currentSfxIndex = 0;
        
        // State tracking
        private bool isMusicMuted = false;
        private bool isSfxMuted = false;
        private bool isUiMuted = false;
        private AudioClip currentMusicClip;
        private Coroutine musicFadeCoroutine;
        
        // Volume keys for PlayerPrefs
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string MUSIC_VOLUME_KEY = "MusicVolume";
        private const string SFX_VOLUME_KEY = "SfxVolume";
        private const string UI_VOLUME_KEY = "UiVolume";
        
        // Events
        public System.Action<float> OnMasterVolumeChanged;
        public System.Action<float> OnMusicVolumeChanged;
        public System.Action<float> OnSfxVolumeChanged;
        public System.Action<bool> OnMusicMuteChanged;
        public System.Action<bool> OnSfxMuteChanged;
        
        private void Awake()
        {
            InitializeAudioSources();
            CreateSfxPool();
            LoadAudioSettings();
        }
        
        private void Start()
        {
            if (muteOnStart)
            {
                MuteMaster(true);
            }
            
            // Start with menu music
            PlayMenuMusic();
        }
        
        #region Initialization
        
        private void InitializeAudioSources()
        {
            // Create audio sources if not assigned
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SfxSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
            
            if (uiSource == null)
            {
                GameObject uiObj = new GameObject("UiSource");
                uiObj.transform.SetParent(transform);
                uiSource = uiObj.AddComponent<AudioSource>();
                uiSource.playOnAwake = false;
            }
        }
        
        private void CreateSfxPool()
        {
            sfxPool = new List<AudioSource>();
            
            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject poolObj = new GameObject($"SfxPool_{i}");
                poolObj.transform.SetParent(transform);
                AudioSource poolSource = poolObj.AddComponent<AudioSource>();
                poolSource.playOnAwake = false;
                sfxPool.Add(poolSource);
            }
        }
        
        #endregion
        
        #region Music Control
        
        public void PlayMenuMusic()
        {
            PlayMusic(mainMenuMusic);
        }
        
        public void PlayGameplayMusic()
        {
            // Randomly choose between gameplay tracks
            AudioClip musicToPlay = Random.value > 0.5f ? gameplayMusic1 : gameplayMusic2;
            PlayMusic(musicToPlay);
        }
        
        public void PlayGameOverMusic()
        {
            PlayMusic(gameOverMusic);
        }
        
        private void PlayMusic(AudioClip clip)
        {
            if (clip == null || clip == currentMusicClip) return;
            
            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }
            
            musicFadeCoroutine = StartCoroutine(FadeToNewMusic(clip));
        }
        
        private IEnumerator FadeToNewMusic(AudioClip newClip)
        {
            // Fade out current music
            if (musicSource.isPlaying)
            {
                float startVolume = musicSource.volume;
                while (musicSource.volume > 0)
                {
                    musicSource.volume -= startVolume * Time.deltaTime / fadeTime;
                    yield return null;
                }
                musicSource.Stop();
            }
            
            // Switch to new music
            currentMusicClip = newClip;
            musicSource.clip = newClip;
            musicSource.volume = 0f;
            musicSource.Play();
            
            // Fade in new music
            float targetVolume = musicVolume * masterVolume;
            while (musicSource.volume < targetVolume)
            {
                musicSource.volume += targetVolume * Time.deltaTime / fadeTime;
                yield return null;
            }
            
            musicSource.volume = targetVolume;
        }
        
        public void PauseMusic()
        {
            if (musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }
        
        public void ResumeMusic()
        {
            if (!musicSource.isPlaying && musicSource.clip != null)
            {
                musicSource.UnPause();
            }
        }
        
        public void StopMusic()
        {
            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }
            
            musicSource.Stop();
            currentMusicClip = null;
        }
        
        #endregion
        
        #region Sound Effects
        
        // Player sounds
        public void PlayCatMeow()
        {
            PlaySfx(catMeow);
        }
        
        public void PlayFootsteps()
        {
            PlaySfx(footsteps, 0.5f); // Lower volume for footsteps
        }
        
        public void PlayJumpSound()
        {
            PlaySfx(jumpSound);
        }
        
        public void PlaySlideSound()
        {
            PlaySfx(slideSound);
        }
        
        // Collectible sounds
        public void PlayCoinCollectSound()
        {
            PlaySfx(coinCollectSound);
        }
        
        public void PlayFishCollectSound()
        {
            PlaySfx(fishCollectSound);
        }
        
        public void PlayPowerUpSound()
        {
            PlaySfx(powerUpPickupSound);
        }
        
        // Obstacle sounds
        public void PlayCarHornSound()
        {
            PlaySfx(carHornSound);
        }
        
        public void PlayCrashSound()
        {
            PlaySfx(crashSound);
        }
        
        public void PlayDogBarkSound()
        {
            PlaySfx(dogBarkSound);
        }
        
        // PowerUp sounds
        public void PlayMagnetActivateSound()
        {
            PlaySfx(magnetActivateSound);
        }
        
        public void PlayShieldActivateSound()
        {
            PlaySfx(shieldActivateSound);
        }
        
        public void PlaySpeedBoostSound()
        {
            PlaySfx(speedBoostSound);
        }
        
        // UI sounds
        public void PlayButtonClickSound()
        {
            PlayUiSfx(buttonClickSound);
        }
        
        public void PlayMenuSelectSound()
        {
            PlayUiSfx(menuSelectSound);
        }
        
        public void PlayGameOverSound()
        {
            PlayUiSfx(gameOverSound);
        }
        
        public void PlayGameStartSound()
        {
            PlayUiSfx(gameStartSound);
        }
        
        #endregion
        
        #region Audio Playback
        
        private void PlaySfx(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null || isSfxMuted) return;
            
            AudioSource source = GetNextSfxSource();
            source.clip = clip;
            source.volume = sfxVolume * masterVolume * volumeMultiplier;
            
            if (randomizePitch)
            {
                source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            }
            else
            {
                source.pitch = 1f;
            }
            
            source.Play();
        }
        
        private void PlayUiSfx(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null || isUiMuted) return;
            
            uiSource.clip = clip;
            uiSource.volume = uiVolume * masterVolume * volumeMultiplier;
            uiSource.pitch = 1f; // UI sounds should not have random pitch
            uiSource.Play();
        }
        
        private AudioSource GetNextSfxSource()
        {
            AudioSource source = sfxPool[currentSfxIndex];
            currentSfxIndex = (currentSfxIndex + 1) % sfxPool.Count;
            return source;
        }
        
        #endregion
        
        #region Volume Control
        
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            SaveAudioSettings();
            OnMasterVolumeChanged?.Invoke(masterVolume);
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateMusicVolume();
            SaveAudioSettings();
            OnMusicVolumeChanged?.Invoke(musicVolume);
        }
        
        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            SaveAudioSettings();
            OnSfxVolumeChanged?.Invoke(sfxVolume);
        }
        
        public void SetUiVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            SaveAudioSettings();
        }
        
        private void UpdateAllVolumes()
        {
            UpdateMusicVolume();
            // SFX and UI volumes are applied per-play, not to source directly
        }
        
        private void UpdateMusicVolume()
        {
            if (musicSource != null && !isMusicMuted)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
        }
        
        #endregion
        
        #region Mute Control
        
        public void MuteMaster(bool mute)
        {
            MuteMusic(mute);
            MuteSfx(mute);
            MuteUi(mute);
        }
        
        public void MuteMusic(bool mute)
        {
            isMusicMuted = mute;
            musicSource.volume = mute ? 0f : musicVolume * masterVolume;
            OnMusicMuteChanged?.Invoke(mute);
        }
        
        public void MuteSfx(bool mute)
        {
            isSfxMuted = mute;
            OnSfxMuteChanged?.Invoke(mute);
        }
        
        public void MuteUi(bool mute)
        {
            isUiMuted = mute;
        }
        
        public void ToggleMusicMute()
        {
            MuteMusic(!isMusicMuted);
        }
        
        public void ToggleSfxMute()
        {
            MuteSfx(!isSfxMuted);
        }
        
        #endregion
        
        #region Audio Settings Persistence
        
        private void LoadAudioSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, masterVolume);
            musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, musicVolume);
            sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, sfxVolume);
            uiVolume = PlayerPrefs.GetFloat(UI_VOLUME_KEY, uiVolume);
            
            UpdateAllVolumes();
        }
        
        private void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
            PlayerPrefs.SetFloat(UI_VOLUME_KEY, uiVolume);
            PlayerPrefs.Save();
        }
        
        #endregion
        
        #region Public Interface
        
        public bool IsMusicMuted() => isMusicMuted;
        public bool IsSfxMuted() => isSfxMuted;
        public bool IsUiMuted() => isUiMuted;
        
        public float GetMasterVolume() => masterVolume;
        public float GetMusicVolume() => musicVolume;
        public float GetSfxVolume() => sfxVolume;
        public float GetUiVolume() => uiVolume;
        
        /// <summary>
        /// Play a custom audio clip as SFX
        /// </summary>
        public void PlayCustomSfx(AudioClip clip, float volume = 1f)
        {
            PlaySfx(clip, volume);
        }
        
        /// <summary>
        /// Stop all currently playing audio
        /// </summary>
        public void StopAllAudio()
        {
            StopMusic();
            
            foreach (AudioSource source in sfxPool)
            {
                source.Stop();
            }
            
            uiSource.Stop();
        }
        
        /// <summary>
        /// Play button sound (for UI integration)
        /// </summary>
        public void PlayButtonSound()
        {
            PlayUiSfx(buttonClickSound);
        }
        
        /// <summary>
        /// Check if audio is muted (for UI integration)
        /// </summary>
        public bool IsMuted()
        {
            return isMusicMuted && isSfxMuted;
        }
        
        /// <summary>
        /// Set master mute state (for UI integration)
        /// </summary>
        public void SetMuted(bool muted)
        {
            isMusicMuted = muted;
            isSfxMuted = muted;
            isUiMuted = muted;
            
            musicSource.mute = muted;
            sfxSource.mute = muted;
            uiSource.mute = muted;
            
            foreach (AudioSource source in sfxPool)
            {
                source.mute = muted;
            }
            
            SaveAudioSettings();
        }
        
        /// <summary>
        /// Check if music is paused (for UI integration)
        /// </summary>
        public bool IsMusicPaused()
        {
            return !musicSource.isPlaying && musicSource.clip != null;
        }
        
        #endregion
        
        #region Debug
        
        public string GetAudioDebugInfo()
        {
            return $"Master: {masterVolume:F2} | Music: {musicVolume:F2} | SFX: {sfxVolume:F2}\n" +
                   $"Music Muted: {isMusicMuted} | SFX Muted: {isSfxMuted}\n" +
                   $"Current Music: {(currentMusicClip ? currentMusicClip.name : "None")}\n" +
                   $"Music Playing: {musicSource.isPlaying}";
        }
        
        #endregion
        
        private void OnDestroy()
        {
            SaveAudioSettings();
        }
    }
}