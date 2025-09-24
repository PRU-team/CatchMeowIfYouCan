using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.PowerUps
{
    /// <summary>
    /// Speed Boost power-up that increases player movement speed temporarily
    /// Features acceleration mechanics, trail effects, and momentum-based gameplay
    /// </summary>
    public class SpeedBoostPowerUp : BasePowerUp
    {
        [Header("Speed Boost Settings")]
        [SerializeField] private float speedMultiplier = 2f;
        [SerializeField] private bool affectsAllMovement = true;
        [SerializeField] private bool affectsJumpSpeed = false;
        [SerializeField] private bool affectsLaneChangeSpeed = true;
        [SerializeField] private float maxSpeedMultiplier = 5f;
        
        [Header("Acceleration Settings")]
        [SerializeField] private bool useGradualAcceleration = true;
        [SerializeField] private float accelerationTime = 1f;
        [SerializeField] private float decelerationTime = 1f;
        [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve decelerationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem speedTrail;
        [SerializeField] private ParticleSystem accelerationBurst;
        [SerializeField] private ParticleSystem windEffect;
        [SerializeField] private Color speedColor = Color.yellow;
        [SerializeField] private float trailIntensityMultiplier = 2f;
        
        [Header("Audio Effects")]
        [SerializeField] private AudioClip accelerationSound;
        [SerializeField] private AudioClip speedLoopSound;
        [SerializeField] private AudioClip decelerationSound;
        [SerializeField] private float speedSoundPitch = 1.2f;
        
        [Header("Gameplay Effects")]
        [SerializeField] private bool increaseScoreMultiplier = true;
        [SerializeField] private float scoreMultiplierBonus = 1.5f;
        [SerializeField] private bool increaseDifficulty = true;
        [SerializeField] private float difficultySpeedMultiplier = 1.3f;
        [SerializeField] private bool enableSpeedStreak = true;
        [SerializeField] private int streakBonusPerSecond = 2;
        
        [Header("Screen Effects")]
        [SerializeField] private bool enableScreenEffects = true;
        [SerializeField] private float fovIncrease = 10f;
        [SerializeField] private Color screenTint = new Color(1f, 1f, 0.8f, 0.1f);
        [SerializeField] private float screenShakeIntensity = 0.1f;
        
        // Runtime state
        private float originalSpeed;
        private float currentSpeedMultiplier = 1f;
        private bool speedEffectsActive = false;
        private float originalScoreMultiplier = 1f;
        private int speedStreakScore = 0;
        private float streakTimer = 0f;
        
        // Coroutines
        private Coroutine accelerationCoroutine;
        private Coroutine decelerationCoroutine;
        private Coroutine speedEffectsCoroutine;
        private Coroutine streakCoroutine;
        
        // Audio management
        private AudioSource speedLoopAudioSource;
        
        // Player integration
        private bool hasPlayerIntegration = false;
        private Vector3 lastPlayerPosition;
        private float playerVelocityMagnitude;
        
        // Screen effects
        private Camera playerCamera;
        private float originalFOV;
        private Color originalCameraColor;
        
        public override PowerUpType Type => PowerUpType.SpeedBoost;
        
        // Properties
        public float CurrentSpeedMultiplier => currentSpeedMultiplier;
        public bool IsAccelerating => accelerationCoroutine != null;
        public bool IsDecelerating => decelerationCoroutine != null;
        public int CurrentStreakScore => speedStreakScore;
        public float AccelerationProgress => IsAccelerating ? (Time.time - activationTime) / accelerationTime : 1f;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            // Setup speed loop audio source
            speedLoopAudioSource = gameObject.AddComponent<AudioSource>();
            speedLoopAudioSource.playOnAwake = false;
            speedLoopAudioSource.loop = true;
            speedLoopAudioSource.volume = audioVolume * 0.6f;
            
            // Set power-up color
            powerUpColor = speedColor;
        }
        
        protected override void Start()
        {
            base.Start();
            
            // Find player camera
            playerCamera = Camera.main;
            if (playerCamera != null)
            {
                originalFOV = playerCamera.fieldOfView;
                // originalCameraColor = playerCamera.backgroundColor;
            }
        }
        
        protected override void UpdatePowerUp()
        {
            base.UpdatePowerUp();
            
            if (IsActive)
            {
                // Update speed effects
                UpdateSpeedEffects();
                
                // Update streak system
                if (enableSpeedStreak)
                {
                    UpdateSpeedStreak();
                }
                
                // Update visual effects intensity
                UpdateEffectsIntensity();
                
                // Update screen effects
                if (enableScreenEffects)
                {
                    UpdateScreenEffects();
                }
            }
        }
        
        #endregion
        
        #region Power-Up Implementation
        
        protected override void OnActivate()
        {
            if (catController == null) return;
            
            // Store original values
            StoreOriginalValues();
            
            // Start acceleration
            if (useGradualAcceleration)
            {
                StartAcceleration();
            }
            else
            {
                ApplySpeedBoost(speedMultiplier);
            }
            
            // Start visual effects
            StartSpeedEffects();
            
            // Start audio effects
            StartSpeedAudio();
            
            // Start streak system
            if (enableSpeedStreak)
            {
                StartSpeedStreak();
            }
            
            // Apply score multiplier
            if (increaseScoreMultiplier && scoreManager != null)
            {
                // TODO: Implement score multiplier in ScoreManager
                // originalScoreMultiplier = scoreManager.GetScoreMultiplier();
                // scoreManager.SetScoreMultiplier(originalScoreMultiplier * scoreMultiplierBonus);
            }
            
            // Increase game difficulty
            if (increaseDifficulty && gameManager != null)
            {
                // TODO: Apply difficulty speed multiplier
                // gameManager.SetTemporarySpeedMultiplier(difficultySpeedMultiplier);
            }
            
            Debug.Log($"Speed Boost activated! Multiplier: {speedMultiplier}x");
        }
        
        protected override void OnDeactivate()
        {
            if (catController == null) return;
            
            // Start deceleration
            if (useGradualAcceleration)
            {
                StartDeceleration();
            }
            else
            {
                RestoreOriginalSpeed();
            }
            
            // Stop visual effects
            StopSpeedEffects();
            
            // Stop audio effects
            StopSpeedAudio();
            
            // Stop streak system
            if (enableSpeedStreak)
            {
                StopSpeedStreak();
            }
            
            // Restore score multiplier
            if (increaseScoreMultiplier && scoreManager != null)
            {
                // TODO: Restore original score multiplier
                // scoreManager.SetScoreMultiplier(originalScoreMultiplier);
            }
            
            // Restore game difficulty
            if (increaseDifficulty && gameManager != null)
            {
                // TODO: Remove temporary speed multiplier
                // gameManager.RemoveTemporarySpeedMultiplier();
            }
            
            // Restore screen effects
            if (enableScreenEffects)
            {
                RestoreScreenEffects();
            }
            
            Debug.Log("Speed Boost deactivated.");
        }
        
        protected override void OnStackAdded()
        {
            // Each stack increases speed multiplier
            float additionalSpeed = 0.5f;
            speedMultiplier = Mathf.Min(speedMultiplier + additionalSpeed, maxSpeedMultiplier);
            
            // Enhanced visual effects for stacked speed boost
            if (speedTrail != null)
            {
                var emission = speedTrail.emission;
                emission.rateOverTime = emission.rateOverTime.constant * 1.3f;
            }
            
            // Apply new speed if already active
            if (speedEffectsActive)
            {
                ApplySpeedBoost(speedMultiplier);
            }
            
            Debug.Log($"Speed Boost stack added! New multiplier: {speedMultiplier}x");
        }
        
        #endregion
        
        #region Speed Control System
        
        private void StoreOriginalValues()
        {
            if (hasPlayerIntegration) return;
            
            // TODO: Store original speed values from CatController
            // originalSpeed = catController.ForwardSpeed;
            originalSpeed = 5f; // Placeholder value
            hasPlayerIntegration = true;
        }
        
        private void RestoreOriginalSpeed()
        {
            if (!hasPlayerIntegration) return;
            
            currentSpeedMultiplier = 1f;
            
            // TODO: Restore original speed to CatController
            // catController.ForwardSpeed = originalSpeed;
            // if (affectsLaneChangeSpeed)
            //     catController.LaneChangeSpeed = originalLaneChangeSpeed;
            
            hasPlayerIntegration = false;
        }
        
        private void ApplySpeedBoost(float multiplier)
        {
            if (!hasPlayerIntegration) return;
            
            currentSpeedMultiplier = multiplier;
            
            // TODO: Apply speed boost to CatController
            // catController.ForwardSpeed = originalSpeed * multiplier;
            // if (affectsLaneChangeSpeed)
            //     catController.LaneChangeSpeed = originalLaneChangeSpeed * multiplier;
            // if (affectsJumpSpeed)
            //     catController.JumpForce = originalJumpForce * multiplier;
            
            Debug.Log($"Speed applied: {originalSpeed} -> {originalSpeed * multiplier}");
        }
        
        private void StartAcceleration()
        {
            if (accelerationCoroutine != null)
            {
                StopCoroutine(accelerationCoroutine);
            }
            
            accelerationCoroutine = StartCoroutine(AccelerationCoroutine());
        }
        
        private IEnumerator AccelerationCoroutine()
        {
            float startTime = Time.time;
            float elapsed = 0f;
            
            // Play acceleration sound
            if (accelerationSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(accelerationSound, audioVolume);
            }
            
            // Play acceleration burst
            if (accelerationBurst != null)
            {
                accelerationBurst.Play();
            }
            
            while (elapsed < accelerationTime)
            {
                elapsed = Time.time - startTime;
                float progress = elapsed / accelerationTime;
                
                // Apply acceleration curve
                float curveValue = accelerationCurve.Evaluate(progress);
                float currentMultiplier = Mathf.Lerp(1f, speedMultiplier, curveValue);
                
                ApplySpeedBoost(currentMultiplier);
                
                yield return null;
            }
            
            // Ensure final speed is applied
            ApplySpeedBoost(speedMultiplier);
            accelerationCoroutine = null;
            
            Debug.Log("Acceleration complete.");
        }
        
        private void StartDeceleration()
        {
            if (decelerationCoroutine != null)
            {
                StopCoroutine(decelerationCoroutine);
            }
            
            decelerationCoroutine = StartCoroutine(DecelerationCoroutine());
        }
        
        private IEnumerator DecelerationCoroutine()
        {
            float startTime = Time.time;
            float elapsed = 0f;
            float startingMultiplier = currentSpeedMultiplier;
            
            // Play deceleration sound
            if (decelerationSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(decelerationSound, audioVolume);
            }
            
            while (elapsed < decelerationTime)
            {
                elapsed = Time.time - startTime;
                float progress = elapsed / decelerationTime;
                
                // Apply deceleration curve
                float curveValue = decelerationCurve.Evaluate(progress);
                float currentMultiplier = Mathf.Lerp(startingMultiplier, 1f, curveValue);
                
                ApplySpeedBoost(currentMultiplier);
                
                yield return null;
            }
            
            // Restore original speed
            RestoreOriginalSpeed();
            decelerationCoroutine = null;
            
            Debug.Log("Deceleration complete.");
        }
        
        #endregion
        
        #region Visual Effects System
        
        private void StartSpeedEffects()
        {
            speedEffectsActive = true;
            
            // Start speed trail
            if (speedTrail != null)
            {
                speedTrail.Play();
                AttachTrailToPlayer();
            }
            
            // Start wind effect
            if (windEffect != null)
            {
                windEffect.Play();
            }
            
            // Start effects update coroutine
            speedEffectsCoroutine = StartCoroutine(SpeedEffectsCoroutine());
        }
        
        private void StopSpeedEffects()
        {
            speedEffectsActive = false;
            
            // Stop speed trail
            if (speedTrail != null)
            {
                speedTrail.Stop();
            }
            
            // Stop wind effect
            if (windEffect != null)
            {
                windEffect.Stop();
            }
            
            // Stop effects coroutine
            if (speedEffectsCoroutine != null)
            {
                StopCoroutine(speedEffectsCoroutine);
                speedEffectsCoroutine = null;
            }
        }
        
        private void AttachTrailToPlayer()
        {
            if (speedTrail == null || catController == null) return;
            
            // Attach trail to player
            speedTrail.transform.SetParent(catController.transform);
            speedTrail.transform.localPosition = Vector3.back * 0.5f; // Behind player
        }
        
        private IEnumerator SpeedEffectsCoroutine()
        {
            while (speedEffectsActive)
            {
                UpdateTrailEffects();
                yield return null;
            }
        }
        
        private void UpdateSpeedEffects()
        {
            if (catController == null) return;
            
            // Calculate player velocity
            Vector3 currentPlayerPosition = catController.transform.position;
            Vector3 velocity = (currentPlayerPosition - lastPlayerPosition) / Time.deltaTime;
            playerVelocityMagnitude = velocity.magnitude;
            lastPlayerPosition = currentPlayerPosition;
        }
        
        private void UpdateEffectsIntensity()
        {
            if (!speedEffectsActive) return;
            
            // Base intensity on current speed multiplier
            float intensity = currentSpeedMultiplier / speedMultiplier;
            
            // Update trail effects
            UpdateTrailEffects(intensity);
            
            // Update wind effects
            if (windEffect != null)
            {
                var emission = windEffect.emission;
                emission.rateOverTime = 10f + (intensity * 20f);
            }
        }
        
        private void UpdateTrailEffects(float intensity = 1f)
        {
            if (speedTrail == null) return;
            
            // Update emission rate
            var emission = speedTrail.emission;
            emission.rateOverTime = (15f + (intensity * trailIntensityMultiplier * 15f));
            
            // Update trail color intensity
            var main = speedTrail.main;
            Color trailColor = speedColor;
            trailColor.a = Mathf.Lerp(0.5f, 1f, intensity);
            main.startColor = trailColor;
            
            // Update trail speed
            var velocityOverLifetime = speedTrail.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        }
        
        #endregion
        
        #region Audio Effects System
        
        private void StartSpeedAudio()
        {
            // Start speed loop sound
            if (speedLoopSound != null && speedLoopAudioSource != null)
            {
                speedLoopAudioSource.clip = speedLoopSound;
                speedLoopAudioSource.pitch = speedSoundPitch;
                speedLoopAudioSource.Play();
            }
        }
        
        private void StopSpeedAudio()
        {
            // Stop speed loop sound
            if (speedLoopAudioSource != null)
            {
                speedLoopAudioSource.Stop();
            }
        }
        
        #endregion
        
        #region Speed Streak System
        
        private void StartSpeedStreak()
        {
            speedStreakScore = 0;
            streakTimer = 0f;
            streakCoroutine = StartCoroutine(SpeedStreakCoroutine());
        }
        
        private void StopSpeedStreak()
        {
            if (streakCoroutine != null)
            {
                StopCoroutine(streakCoroutine);
                streakCoroutine = null;
            }
            
            // Award final streak bonus
            if (speedStreakScore > 0 && scoreManager != null)
            {
                scoreManager.AddScore(speedStreakScore);
                Debug.Log($"Speed streak ended! Total bonus: {speedStreakScore} points");
            }
            
            speedStreakScore = 0;
        }
        
        private IEnumerator SpeedStreakCoroutine()
        {
            while (IsActive)
            {
                yield return new WaitForSeconds(1f);
                
                streakTimer += 1f;
                speedStreakScore += streakBonusPerSecond;
                
                // Visual feedback for streak
                if (streakTimer % 5f == 0) // Every 5 seconds
                {
                    Debug.Log($"Speed streak: {streakTimer}s, Bonus: {speedStreakScore} points");
                }
            }
        }
        
        private void UpdateSpeedStreak()
        {
            // Streak is updated in coroutine
            // This method can be used for additional streak logic
        }
        
        #endregion
        
        #region Screen Effects System
        
        private void UpdateScreenEffects()
        {
            if (playerCamera == null) return;
            
            // Update field of view based on speed
            float targetFOV = originalFOV + (fovIncrease * (currentSpeedMultiplier - 1f));
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * 2f);
            
            // Screen shake effect
            if (screenShakeIntensity > 0f)
            {
                Vector3 shake = Random.insideUnitSphere * screenShakeIntensity * (currentSpeedMultiplier - 1f);
                shake.z = 0f;
                playerCamera.transform.localPosition += shake;
            }
        }
        
        private void RestoreScreenEffects()
        {
            if (playerCamera == null) return;
            
            // Restore original FOV
            StartCoroutine(RestoreFOVCoroutine());
        }
        
        private IEnumerator RestoreFOVCoroutine()
        {
            float startFOV = playerCamera.fieldOfView;
            float elapsed = 0f;
            float duration = 1f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                playerCamera.fieldOfView = Mathf.Lerp(startFOV, originalFOV, progress);
                
                yield return null;
            }
            
            playerCamera.fieldOfView = originalFOV;
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get speed boost specific information
        /// </summary>
        public SpeedBoostInfo GetSpeedBoostInfo()
        {
            return new SpeedBoostInfo
            {
                currentSpeedMultiplier = CurrentSpeedMultiplier,
                maxSpeedMultiplier = speedMultiplier,
                isAccelerating = IsAccelerating,
                isDecelerating = IsDecelerating,
                accelerationProgress = AccelerationProgress,
                currentStreakScore = CurrentStreakScore,
                streakTimer = streakTimer,
                affectsAllMovement = affectsAllMovement
            };
        }
        
        /// <summary>
        /// Boost speed immediately (for combos/bonuses)
        /// </summary>
        public void BoostSpeed(float additionalMultiplier, float boostDuration = 2f)
        {
            if (!IsActive) return;
            
            StartCoroutine(TemporarySpeedBoost(additionalMultiplier, boostDuration));
        }
        
        private IEnumerator TemporarySpeedBoost(float additionalMultiplier, float boostDuration)
        {
            float originalMultiplier = speedMultiplier;
            speedMultiplier += additionalMultiplier;
            
            ApplySpeedBoost(speedMultiplier);
            
            yield return new WaitForSeconds(boostDuration);
            
            speedMultiplier = originalMultiplier;
            ApplySpeedBoost(speedMultiplier);
            
            Debug.Log($"Temporary speed boost ended. Restored to {speedMultiplier}x");
        }
        
        #endregion
        
        #region Debug Override
        
        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $"\n" +
                   $"Speed Multiplier: {CurrentSpeedMultiplier:F1}x/{speedMultiplier:F1}x\n" +
                   $"Accelerating: {IsAccelerating}\n" +
                   $"Decelerating: {IsDecelerating}\n" +
                   $"Streak Score: {CurrentStreakScore}\n" +
                   $"Streak Time: {streakTimer:F1}s\n" +
                   $"Player Velocity: {playerVelocityMagnitude:F1}";
        }
        
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            // Draw speed effect area
            if (IsActive)
            {
                Gizmos.color = speedColor;
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                
                // Draw speed trail visualization
                Vector3 trailStart = transform.position;
                Vector3 trailEnd = trailStart - Vector3.right * (currentSpeedMultiplier * 2f);
                Gizmos.DrawLine(trailStart, trailEnd);
                
                // Draw speed indicator
                Gizmos.DrawWireSphere(transform.position, currentSpeedMultiplier * 0.5f);
            }
        }
        
        #endregion
        
        #region Data Structures
        
        [System.Serializable]
        public struct SpeedBoostInfo
        {
            public float currentSpeedMultiplier;
            public float maxSpeedMultiplier;
            public bool isAccelerating;
            public bool isDecelerating;
            public float accelerationProgress;
            public int currentStreakScore;
            public float streakTimer;
            public bool affectsAllMovement;
        }
        
        #endregion
    }
}