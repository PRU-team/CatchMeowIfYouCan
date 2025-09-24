using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.PowerUps
{
    /// <summary>
    /// Base class for all power-ups in the game
    /// Handles activation, duration, effects, and visual feedback systems
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class BasePowerUp : MonoBehaviour
    {
        [Header("Power-Up Settings")]
        [SerializeField] protected float duration = 10f;
        [SerializeField] protected bool canStack = false;
        [SerializeField] protected int maxStacks = 1;
        [SerializeField] protected float cooldownTime = 0f;
        [SerializeField] protected bool autoActivate = true;
        
        [Header("Collection Settings")]
        [SerializeField] protected int scoreValue = 50;
        [SerializeField] protected bool consumeOnPickup = true;
        [SerializeField] protected float pickupRadius = 1f;
        [SerializeField] protected bool magneticPickup = false;
        [SerializeField] protected float magneticRange = 3f;
        
        [Header("Visual Effects")]
        [SerializeField] protected ParticleSystem collectEffect;
        [SerializeField] protected ParticleSystem activeEffect;
        [SerializeField] protected GameObject visualModel;
        [SerializeField] protected Light powerUpLight;
        [SerializeField] protected Color powerUpColor = Color.white;
        
        [Header("Audio")]
        [SerializeField] protected AudioClip collectSound;
        [SerializeField] protected AudioClip activateSound;
        [SerializeField] protected AudioClip deactivateSound;
        [SerializeField] protected float audioVolume = 1f;
        
        [Header("Animation")]
        [SerializeField] protected bool enableFloating = true;
        [SerializeField] protected float floatAmplitude = 0.5f;
        [SerializeField] protected float floatFrequency = 1f;
        [SerializeField] protected bool enableRotation = true;
        [SerializeField] protected float rotationSpeed = 45f;
        [SerializeField] protected Vector3 rotationAxis = Vector3.up;
        
        // Runtime state
        protected bool isActive = false;
        protected bool isCollected = false;
        protected bool isOnCooldown = false;
        protected float activationTime;
        protected float cooldownStartTime;
        protected int currentStacks = 0;
        protected Vector3 initialPosition;
        
        // Components
        protected Collider2D powerUpCollider;
        protected SpriteRenderer spriteRenderer;
        protected AudioSource audioSource;
        
        // Animation coroutines
        private Coroutine floatingCoroutine;
        private Coroutine rotationCoroutine;
        private Coroutine durationCoroutine;
        private Coroutine magneticCoroutine;
        
        // Game references
        protected Core.GameManager gameManager;
        protected Core.AudioManager audioManager;
        protected Core.ScoreManager scoreManager;
        protected Player.CatController catController;
        
        public enum PowerUpType
        {
            None,
            RocketShoes,
            Shield,
            SpeedBoost,
            Magnet,
            DoubleScore,
            Invincibility,
            SlowMotion,
            ExtraLife
        }
        
        public enum PowerUpState
        {
            Spawned,        // Just spawned, waiting to be collected
            Collected,      // Collected but not yet activated
            Active,         // Currently active and providing effects
            Expired,        // Duration ended, effects removed
            OnCooldown,     // Waiting for cooldown to end
            Destroyed       // Destroyed/consumed
        }
        
        // Properties
        public abstract PowerUpType Type { get; }
        public PowerUpState CurrentState { get; protected set; } = PowerUpState.Spawned;
        public bool IsActive => isActive && CurrentState == PowerUpState.Active;
        public bool IsCollected => isCollected;
        public bool IsOnCooldown => isOnCooldown;
        public float RemainingDuration => isActive ? Mathf.Max(0f, duration - (Time.time - activationTime)) : 0f;
        public float RemainingCooldown => isOnCooldown ? Mathf.Max(0f, cooldownTime - (Time.time - cooldownStartTime)) : 0f;
        public int CurrentStacks => currentStacks;
        
        // Events
        public System.Action<BasePowerUp> OnPowerUpCollected;
        public System.Action<BasePowerUp> OnPowerUpActivated;
        public System.Action<BasePowerUp> OnPowerUpDeactivated;
        public System.Action<BasePowerUp> OnPowerUpExpired;
        public System.Action<BasePowerUp, int> OnStacksChanged;
        
        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            powerUpCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();
            
            // Ensure collider is trigger
            if (powerUpCollider != null)
            {
                powerUpCollider.isTrigger = true;
            }
            
            // Setup audio source
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.volume = audioVolume;
        }
        
        protected virtual void Start()
        {
            InitializePowerUp();
            StartVisualEffects();
            
            // Get game references
            gameManager = Core.GameManager.Instance;
            audioManager = FindObjectOfType<Core.AudioManager>();
            scoreManager = FindObjectOfType<Core.ScoreManager>();
            catController = FindObjectOfType<Player.CatController>();
        }
        
        protected virtual void Update()
        {
            if (CurrentState == PowerUpState.Spawned && magneticPickup)
            {
                CheckMagneticPickup();
            }
            
            UpdatePowerUp();
        }
        
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") && CurrentState == PowerUpState.Spawned)
            {
                CollectPowerUp(other);
            }
        }
        
        #endregion
        
        #region Initialization
        
        protected virtual void InitializePowerUp()
        {
            initialPosition = transform.position;
            CurrentState = PowerUpState.Spawned;
            isCollected = false;
            isActive = false;
            isOnCooldown = false;
            currentStacks = 0;
            
            // Setup visual appearance
            if (spriteRenderer != null)
            {
                spriteRenderer.color = powerUpColor;
            }
            
            // Setup light
            if (powerUpLight != null)
            {
                powerUpLight.color = powerUpColor;
                powerUpLight.enabled = true;
            }
        }
        
        protected virtual void StartVisualEffects()
        {
            // Start floating animation
            if (enableFloating)
            {
                floatingCoroutine = StartCoroutine(FloatingAnimation());
            }
            
            // Start rotation animation
            if (enableRotation)
            {
                rotationCoroutine = StartCoroutine(RotationAnimation());
            }
            
            // Start magnetic effect if enabled
            if (magneticPickup)
            {
                magneticCoroutine = StartCoroutine(MagneticEffect());
            }
        }
        
        #endregion
        
        #region Animation System
        
        private IEnumerator FloatingAnimation()
        {
            while (CurrentState == PowerUpState.Spawned)
            {
                float time = Time.time * floatFrequency;
                float yOffset = Mathf.Sin(time) * floatAmplitude;
                transform.position = initialPosition + Vector3.up * yOffset;
                yield return null;
            }
        }
        
        private IEnumerator RotationAnimation()
        {
            while (CurrentState == PowerUpState.Spawned)
            {
                transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
                yield return null;
            }
        }
        
        private IEnumerator MagneticEffect()
        {
            while (CurrentState == PowerUpState.Spawned)
            {
                if (catController != null)
                {
                    float distanceToPlayer = Vector3.Distance(transform.position, catController.transform.position);
                    
                    if (distanceToPlayer <= magneticRange)
                    {
                        // Move towards player
                        Vector3 direction = (catController.transform.position - transform.position).normalized;
                        float magneticForce = 1f - (distanceToPlayer / magneticRange);
                        transform.position += direction * magneticForce * 5f * Time.deltaTime;
                    }
                }
                
                yield return null;
            }
        }
        
        #endregion
        
        #region Collection System
        
        protected virtual void CollectPowerUp(Collider2D playerCollider)
        {
            if (isCollected) return;
            
            isCollected = true;
            CurrentState = PowerUpState.Collected;
            
            // Stop visual animations
            StopVisualAnimations();
            
            // Play collect effects
            PlayCollectEffects();
            
            // Award score
            if (scoreManager != null && scoreValue > 0)
            {
                scoreManager.AddScore(scoreValue);
            }
            
            // Trigger collection event
            OnPowerUpCollected?.Invoke(this);
            
            // Auto-activate if enabled
            if (autoActivate)
            {
                ActivatePowerUp();
            }
            
            // Hide visual model
            if (visualModel != null)
            {
                visualModel.SetActive(false);
            }
            
            // Disable collider
            if (powerUpCollider != null)
            {
                powerUpCollider.enabled = false;
            }
        }
        
        private void CheckMagneticPickup()
        {
            if (catController == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, catController.transform.position);
            
            if (distanceToPlayer <= pickupRadius)
            {
                CollectPowerUp(catController.GetComponent<Collider2D>());
            }
        }
        
        private void PlayCollectEffects()
        {
            // Play particle effect
            if (collectEffect != null)
            {
                collectEffect.Play();
            }
            
            // Play sound effect
            if (collectSound != null)
            {
                if (audioManager != null)
                {
                    audioManager.PlayCustomSfx(collectSound, audioVolume);
                }
                else if (audioSource != null)
                {
                    audioSource.PlayOneShot(collectSound, audioVolume);
                }
            }
        }
        
        #endregion
        
        #region Activation System
        
        public virtual bool CanActivate()
        {
            if (!isCollected) return false;
            if (isOnCooldown) return false;
            if (isActive && !canStack) return false;
            if (canStack && currentStacks >= maxStacks) return false;
            
            return true;
        }
        
        public virtual void ActivatePowerUp()
        {
            if (!CanActivate()) return;
            
            // Handle stacking
            if (canStack && isActive)
            {
                AddStack();
                return;
            }
            
            // Activate power-up
            isActive = true;
            activationTime = Time.time;
            CurrentState = PowerUpState.Active;
            currentStacks = 1;
            
            // Apply power-up effects
            OnActivate();
            
            // Play activation effects
            PlayActivationEffects();
            
            // Start duration countdown
            if (duration > 0)
            {
                durationCoroutine = StartCoroutine(DurationCountdown());
            }
            
            // Trigger activation event
            OnPowerUpActivated?.Invoke(this);
            OnStacksChanged?.Invoke(this, currentStacks);
        }
        
        protected virtual void AddStack()
        {
            if (!canStack || currentStacks >= maxStacks) return;
            
            currentStacks++;
            activationTime = Time.time; // Reset duration
            
            // Apply additional stack effects
            OnStackAdded();
            
            // Trigger stack change event
            OnStacksChanged?.Invoke(this, currentStacks);
        }
        
        private IEnumerator DurationCountdown()
        {
            yield return new WaitForSeconds(duration);
            
            if (isActive)
            {
                DeactivatePowerUp();
            }
        }
        
        protected virtual void PlayActivationEffects()
        {
            // Play active particle effect
            if (activeEffect != null)
            {
                activeEffect.Play();
            }
            
            // Play activation sound
            if (activateSound != null)
            {
                if (audioManager != null)
                {
                    audioManager.PlayCustomSfx(activateSound, audioVolume);
                }
                else if (audioSource != null)
                {
                    audioSource.PlayOneShot(activateSound, audioVolume);
                }
            }
        }
        
        #endregion
        
        #region Deactivation System
        
        public virtual void DeactivatePowerUp()
        {
            if (!isActive) return;
            
            isActive = false;
            CurrentState = PowerUpState.Expired;
            
            // Remove power-up effects
            OnDeactivate();
            
            // Play deactivation effects
            PlayDeactivationEffects();
            
            // Start cooldown if applicable
            if (cooldownTime > 0)
            {
                StartCooldown();
            }
            
            // Trigger deactivation events
            OnPowerUpDeactivated?.Invoke(this);
            OnPowerUpExpired?.Invoke(this);
            
            // Reset stacks
            currentStacks = 0;
            OnStacksChanged?.Invoke(this, currentStacks);
            
            // Destroy if consumable
            if (consumeOnPickup)
            {
                DestroyPowerUp();
            }
        }
        
        protected virtual void PlayDeactivationEffects()
        {
            // Stop active particle effect
            if (activeEffect != null)
            {
                activeEffect.Stop();
            }
            
            // Play deactivation sound
            if (deactivateSound != null)
            {
                if (audioManager != null)
                {
                    audioManager.PlayCustomSfx(deactivateSound, audioVolume);
                }
                else if (audioSource != null)
                {
                    audioSource.PlayOneShot(deactivateSound, audioVolume);
                }
            }
        }
        
        #endregion
        
        #region Cooldown System
        
        protected virtual void StartCooldown()
        {
            isOnCooldown = true;
            cooldownStartTime = Time.time;
            CurrentState = PowerUpState.OnCooldown;
            
            StartCoroutine(CooldownCountdown());
        }
        
        private IEnumerator CooldownCountdown()
        {
            yield return new WaitForSeconds(cooldownTime);
            
            isOnCooldown = false;
            
            if (CurrentState == PowerUpState.OnCooldown)
            {
                CurrentState = PowerUpState.Expired;
            }
        }
        
        #endregion
        
        #region Cleanup
        
        public virtual void DestroyPowerUp()
        {
            CurrentState = PowerUpState.Destroyed;
            
            // Stop all coroutines
            StopAllCoroutines();
            
            // Clean up effects
            if (isActive)
            {
                OnDeactivate();
            }
            
            // Wait for particle effects to finish
            StartCoroutine(DelayedDestroy());
        }
        
        private IEnumerator DelayedDestroy()
        {
            // Wait for effects to finish
            yield return new WaitForSeconds(2f);
            
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
        
        private void StopVisualAnimations()
        {
            if (floatingCoroutine != null)
            {
                StopCoroutine(floatingCoroutine);
                floatingCoroutine = null;
            }
            
            if (rotationCoroutine != null)
            {
                StopCoroutine(rotationCoroutine);
                rotationCoroutine = null;
            }
            
            if (magneticCoroutine != null)
            {
                StopCoroutine(magneticCoroutine);
                magneticCoroutine = null;
            }
        }
        
        #endregion
        
        #region Abstract Methods
        
        /// <summary>
        /// Called when the power-up is activated
        /// Implement the actual power-up effects here
        /// </summary>
        protected abstract void OnActivate();
        
        /// <summary>
        /// Called when the power-up is deactivated
        /// Remove power-up effects here
        /// </summary>
        protected abstract void OnDeactivate();
        
        /// <summary>
        /// Called when a stack is added (if stacking is enabled)
        /// Implement additional stack effects here
        /// </summary>
        protected virtual void OnStackAdded()
        {
            // Default implementation - can be overridden
        }
        
        /// <summary>
        /// Called every frame while power-up is active
        /// Implement continuous effects here
        /// </summary>
        protected virtual void UpdatePowerUp()
        {
            // Default implementation - can be overridden
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Force activate the power-up
        /// </summary>
        public void ForceActivate()
        {
            if (!isCollected)
            {
                CollectPowerUp(catController?.GetComponent<Collider2D>());
            }
            
            ActivatePowerUp();
        }
        
        /// <summary>
        /// Force deactivate the power-up
        /// </summary>
        public void ForceDeactivate()
        {
            if (isActive)
            {
                DeactivatePowerUp();
            }
        }
        
        /// <summary>
        /// Extend the duration of the power-up
        /// </summary>
        public void ExtendDuration(float additionalTime)
        {
            if (isActive)
            {
                duration += additionalTime;
                
                // Restart duration coroutine
                if (durationCoroutine != null)
                {
                    StopCoroutine(durationCoroutine);
                }
                
                float remainingTime = duration - (Time.time - activationTime);
                if (remainingTime > 0)
                {
                    durationCoroutine = StartCoroutine(DelayedDeactivation(remainingTime));
                }
            }
        }
        
        private IEnumerator DelayedDeactivation(float delay)
        {
            yield return new WaitForSeconds(delay);
            DeactivatePowerUp();
        }
        
        /// <summary>
        /// Get power-up information for UI/debugging
        /// </summary>
        public virtual PowerUpInfo GetInfo()
        {
            return new PowerUpInfo
            {
                type = Type,
                state = CurrentState,
                isActive = IsActive,
                remainingDuration = RemainingDuration,
                remainingCooldown = RemainingCooldown,
                currentStacks = CurrentStacks,
                maxStacks = maxStacks,
                canStack = canStack
            };
        }
        
        #endregion
        
        #region Debug
        
        public virtual string GetDebugInfo()
        {
            return $"PowerUp: {Type}\n" +
                   $"State: {CurrentState}\n" +
                   $"Active: {IsActive}\n" +
                   $"Stacks: {CurrentStacks}/{maxStacks}\n" +
                   $"Duration: {RemainingDuration:F1}s\n" +
                   $"Cooldown: {RemainingCooldown:F1}s\n" +
                   $"Collected: {IsCollected}";
        }
        
        protected virtual void OnDrawGizmos()
        {
            // Draw pickup radius
            Gizmos.color = powerUpColor;
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
            
            // Draw magnetic range
            if (magneticPickup)
            {
                Gizmos.color = Color.yellow;
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
                Gizmos.DrawWireSphere(transform.position, magneticRange);
            }
        }
        
        #endregion
        
        [System.Serializable]
        public struct PowerUpInfo
        {
            public PowerUpType type;
            public PowerUpState state;
            public bool isActive;
            public float remainingDuration;
            public float remainingCooldown;
            public int currentStacks;
            public int maxStacks;
            public bool canStack;
        }
    }
}