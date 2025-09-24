using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.Collectibles
{
    /// <summary>
    /// Base class for all collectible items in the game
    /// Handles common functionality like collision detection, animation, and scoring
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class BaseCollectible : MonoBehaviour
    {
        [Header("Collectible Settings")]
        [SerializeField] protected int baseValue = 10;
        [SerializeField] protected bool canBeCollected = true;
        [SerializeField] protected float lifeTime = 30f; // Auto-destroy after this time
        
        [Header("Visual Effects")]
        [SerializeField] protected ParticleSystem collectEffect;
        [SerializeField] protected AudioClip collectSound;
        [SerializeField] protected float collectSoundVolume = 1f;
        
        [Header("Animation")]
        [SerializeField] protected bool enableFloatAnimation = true;
        [SerializeField] protected float floatAmplitude = 0.3f;
        [SerializeField] protected float floatSpeed = 2f;
        [SerializeField] protected bool enableRotation = true;
        [SerializeField] protected float rotationSpeed = 90f;
        
        [Header("Magnet Effect")]
        [SerializeField] protected bool canBeMagnetized = true;
        [SerializeField] protected float magnetRange = 3f;
        [SerializeField] protected float magnetSpeed = 8f;
        
        // Runtime state
        protected bool isCollected = false;
        protected bool isMagnetized = false;
        protected Transform playerTransform;
        protected Vector3 initialPosition;
        protected float spawnTime;
        protected Collider2D collectibleCollider;
        protected SpriteRenderer spriteRenderer;
        protected Animator animator;
        
        // Animation state
        private Coroutine floatCoroutine;
        private Coroutine rotationCoroutine;
        private Coroutine magnetCoroutine;
        
        // Components references
        protected Core.ScoreManager scoreManager;
        protected Core.AudioManager audioManager;
        
        public enum CollectibleType
        {
            Coin,
            Gem,
            PowerUp,
            Special
        }
        
        // Properties
        public abstract CollectibleType Type { get; }
        public virtual int Value => GetAdjustedValue();
        public bool IsCollected => isCollected;
        public bool CanBeCollected => canBeCollected && !isCollected;
        
        // Events
        public System.Action<BaseCollectible> OnCollected;
        public System.Action<BaseCollectible> OnDestroyed;
        
        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            collectibleCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            
            // Set up collider as trigger
            collectibleCollider.isTrigger = true;
        }
        
        protected virtual void Start()
        {
            InitializeCollectible();
            StartAnimations();
            
            // Find player reference
            var player = FindObjectOfType<Player.CatController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            
            // Get manager references
            scoreManager = FindObjectOfType<Core.ScoreManager>();
            audioManager = FindObjectOfType<Core.AudioManager>();
        }
        
        protected virtual void Update()
        {
            if (isCollected) return;
            
            // Check lifetime
            if (Time.time - spawnTime > lifeTime)
            {
                DestroyCollectible();
                return;
            }
            
            // Handle magnet effect
            if (canBeMagnetized && playerTransform != null)
            {
                CheckMagnetEffect();
            }
            
            // Custom update logic
            UpdateCollectible();
        }
        
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (isCollected || !canBeCollected) return;
            
            // Check if it's the player
            if (other.CompareTag("Player"))
            {
                CollectItem();
            }
        }
        
        #endregion
        
        #region Initialization
        
        protected virtual void InitializeCollectible()
        {
            spawnTime = Time.time;
            initialPosition = transform.position;
            isCollected = false;
            isMagnetized = false;
            canBeCollected = true;
            
            // Reset visual state
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            
            // Enable collider
            if (collectibleCollider != null)
            {
                collectibleCollider.enabled = true;
            }
        }
        
        public virtual void ResetCollectible()
        {
            // Stop all coroutines
            StopAllCoroutines();
            
            // Reset state
            InitializeCollectible();
            
            // Restart animations
            StartAnimations();
        }
        
        #endregion
        
        #region Animation System
        
        protected virtual void StartAnimations()
        {
            if (enableFloatAnimation)
            {
                floatCoroutine = StartCoroutine(FloatAnimation());
            }
            
            if (enableRotation)
            {
                rotationCoroutine = StartCoroutine(RotationAnimation());
            }
        }
        
        protected virtual void StopAnimations()
        {
            if (floatCoroutine != null)
            {
                StopCoroutine(floatCoroutine);
                floatCoroutine = null;
            }
            
            if (rotationCoroutine != null)
            {
                StopCoroutine(rotationCoroutine);
                rotationCoroutine = null;
            }
        }
        
        private IEnumerator FloatAnimation()
        {
            while (!isCollected)
            {
                float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                Vector3 newPosition = initialPosition + Vector3.up * yOffset;
                
                if (!isMagnetized)
                {
                    transform.position = newPosition;
                }
                
                yield return null;
            }
        }
        
        private IEnumerator RotationAnimation()
        {
            while (!isCollected)
            {
                transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
                yield return null;
            }
        }
        
        #endregion
        
        #region Magnet System
        
        protected virtual void CheckMagnetEffect()
        {
            if (playerTransform == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            // Check if player has magnet power-up active
            var catController = playerTransform.GetComponent<Player.CatController>();
            bool playerHasMagnet = false; // TODO: Implement power-up system
            
            if (playerHasMagnet && distanceToPlayer <= magnetRange && !isMagnetized)
            {
                StartMagnetEffect();
            }
            else if (isMagnetized && (!playerHasMagnet || distanceToPlayer > magnetRange * 1.5f))
            {
                StopMagnetEffect();
            }
        }
        
        protected virtual void StartMagnetEffect()
        {
            if (isMagnetized) return;
            
            isMagnetized = true;
            magnetCoroutine = StartCoroutine(MagnetMovement());
            
            // Visual feedback for magnet effect
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.cyan;
            }
        }
        
        protected virtual void StopMagnetEffect()
        {
            if (!isMagnetized) return;
            
            isMagnetized = false;
            
            if (magnetCoroutine != null)
            {
                StopCoroutine(magnetCoroutine);
                magnetCoroutine = null;
            }
            
            // Reset visual state
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }
        
        private IEnumerator MagnetMovement()
        {
            while (isMagnetized && playerTransform != null && !isCollected)
            {
                Vector3 direction = (playerTransform.position - transform.position).normalized;
                transform.position += direction * magnetSpeed * Time.deltaTime;
                
                yield return null;
            }
        }
        
        #endregion
        
        #region Collection System
        
        public virtual void CollectItem()
        {
            if (isCollected || !canBeCollected) return;
            
            isCollected = true;
            canBeCollected = false;
            
            // Stop animations
            StopAnimations();
            
            // Stop magnet effect
            if (magnetCoroutine != null)
            {
                StopCoroutine(magnetCoroutine);
            }
            
            // Execute collection logic
            OnItemCollected();
            
            // Add score
            if (scoreManager != null)
            {
                scoreManager.AddScore(Value);
            }
            
            // Play sound effect
            PlayCollectSound();
            
            // Play visual effect
            PlayCollectEffect();
            
            // Trigger events
            OnCollected?.Invoke(this);
            
            // Hide or destroy the collectible
            StartCoroutine(HandleCollectionComplete());
        }
        
        protected virtual void OnItemCollected()
        {
            // Override in derived classes for specific collection behavior
        }
        
        protected virtual void PlayCollectSound()
        {
            if (audioManager != null && collectSound != null)
            {
                audioManager.PlayCustomSfx(collectSound, collectSoundVolume);
            }
        }
        
        protected virtual void PlayCollectEffect()
        {
            if (collectEffect != null)
            {
                collectEffect.Play();
            }
        }
        
        private IEnumerator HandleCollectionComplete()
        {
            // Disable collider
            if (collectibleCollider != null)
            {
                collectibleCollider.enabled = false;
            }
            
            // Scale down animation
            float scaleTime = 0.3f;
            Vector3 originalScale = transform.localScale;
            float elapsed = 0f;
            
            while (elapsed < scaleTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / scaleTime;
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);
                
                yield return null;
            }
            
            // Wait for particle effect to finish
            if (collectEffect != null && collectEffect.isPlaying)
            {
                yield return new WaitForSeconds(1f);
            }
            
            // Destroy or return to pool
            DestroyCollectible();
        }
        
        #endregion
        
        #region Value Calculation
        
        protected virtual int GetAdjustedValue()
        {
            int finalValue = baseValue;
            
            // Apply multipliers from game state
            var gameManager = Core.GameManager.Instance;
            if (gameManager != null)
            {
                // Example: increase value based on game speed/difficulty
                float speedMultiplier = gameManager.CurrentGameSpeed;
                finalValue = Mathf.RoundToInt(baseValue * speedMultiplier);
            }
            
            // Apply score multiplier if player has relevant power-up
            if (playerTransform != null)
            {
                var catController = playerTransform.GetComponent<Player.CatController>();
                // TODO: Implement power-up system
                // if (catController != null && catController.HasActivePowerUp("ScoreMultiplier"))
                // {
                //     finalValue *= 2; // Double points
                // }
            }
            
            return finalValue;
        }
        
        #endregion
        
        #region Lifecycle Management
        
        public virtual void DestroyCollectible()
        {
            // Trigger destroy event
            OnDestroyed?.Invoke(this);
            
            // Clean up
            StopAllCoroutines();
            
            // Destroy or return to pool
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
        
        protected virtual void UpdateCollectible()
        {
            // Override in derived classes for custom update logic
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Force collect this item (used by power-ups like magnet)
        /// </summary>
        public void ForceCollect()
        {
            if (CanBeCollected)
            {
                CollectItem();
            }
        }
        
        /// <summary>
        /// Set the collectible value
        /// </summary>
        public virtual void SetValue(int newValue)
        {
            baseValue = newValue;
        }
        
        /// <summary>
        /// Get collectible information for UI/debugging
        /// </summary>
        public virtual CollectibleInfo GetInfo()
        {
            return new CollectibleInfo
            {
                type = Type,
                value = Value,
                position = transform.position,
                isCollected = IsCollected,
                isMagnetized = isMagnetized,
                timeRemaining = Mathf.Max(0f, lifeTime - (Time.time - spawnTime))
            };
        }
        
        #endregion
        
        #region Debug
        
        protected virtual void OnDrawGizmos()
        {
            // Draw magnet range when selected
            if (canBeMagnetized)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, magnetRange);
            }
            
            // Draw collection area
            if (collectibleCollider != null)
            {
                Gizmos.color = isCollected ? Color.red : Color.green;
                Gizmos.DrawWireCube(transform.position, collectibleCollider.bounds.size);
            }
        }
        
        public virtual string GetDebugInfo()
        {
            return $"Collectible: {Type}\n" +
                   $"Value: {Value}\n" +
                   $"Collected: {IsCollected}\n" +
                   $"Magnetized: {isMagnetized}\n" +
                   $"Time Left: {Mathf.Max(0f, lifeTime - (Time.time - spawnTime)):F1}s";
        }
        
        #endregion
        
        [System.Serializable]
        public struct CollectibleInfo
        {
            public CollectibleType type;
            public int value;
            public Vector3 position;
            public bool isCollected;
            public bool isMagnetized;
            public float timeRemaining;
        }
    }
}