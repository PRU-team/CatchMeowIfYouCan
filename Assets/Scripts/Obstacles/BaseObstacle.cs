using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.Obstacles
{
    /// <summary>
    /// Base class for all obstacles in the game
    /// Handles collision detection, damage system, animations, and interaction logic
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class BaseObstacle : MonoBehaviour
    {
        [Header("Obstacle Settings")]
        [SerializeField] protected int damageAmount = 1;
        [SerializeField] protected bool canBeDestroyed = false;
        [SerializeField] protected float lifeTime = 60f; // Auto-destroy after this time
        [SerializeField] protected bool isActive = true;
        
        [Header("Movement")]
        [SerializeField] protected bool enableMovement = false;
        [SerializeField] protected Vector3 moveDirection = Vector3.left;
        [SerializeField] protected float moveSpeed = 2f;
        [SerializeField] protected bool followGameSpeed = true;
        
        [Header("Visual Effects")]
        [SerializeField] protected ParticleSystem destroyEffect;
        [SerializeField] protected ParticleSystem warningEffect;
        [SerializeField] protected AudioClip hitSound;
        [SerializeField] protected AudioClip destroySound;
        [SerializeField] protected float soundVolume = 1f;
        
        [Header("Warning System")]
        [SerializeField] protected bool enableWarning = false;
        [SerializeField] protected float warningDistance = 10f;
        [SerializeField] protected float warningDuration = 2f;
        [SerializeField] protected Color warningColor = Color.red;
        
        [Header("Animation")]
        [SerializeField] protected bool enableIdleAnimation = true;
        [SerializeField] protected float animationSpeed = 1f;
        [SerializeField] protected AnimationCurve idleAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        // Runtime state
        protected bool isDestroyed = false;
        protected bool hasCollided = false;
        protected bool isWarningActive = false;
        protected float spawnTime;
        protected Vector3 initialPosition;
        protected Transform playerTransform;
        
        // Components
        protected Collider2D obstacleCollider;
        protected SpriteRenderer spriteRenderer;
        protected Animator animator;
        protected Rigidbody2D rb2d;
        
        // Animation state
        private Coroutine idleAnimationCoroutine;
        private Coroutine warningCoroutine;
        private Coroutine movementCoroutine;
        
        // Game references
        protected Core.GameManager gameManager;
        protected Core.AudioManager audioManager;
        
        public enum ObstacleType
        {
            Car,
            TrashBin,
            Barrier,
            MovingPlatform,
            Hazard
        }
        
        public enum CollisionResult
        {
            Damage,      // Player takes damage
            Destroy,     // Obstacle is destroyed
            Bounce,      // Player bounces off
            Stop,        // Player stops
            Pass         // No collision effect
        }
        
        // Properties
        public abstract ObstacleType Type { get; }
        public virtual int DamageAmount => damageAmount;
        public bool IsDestroyed => isDestroyed;
        public bool IsActive => isActive && !isDestroyed;
        public bool CanBeDestroyed => canBeDestroyed;
        
        // Events
        public System.Action<BaseObstacle, Collider2D> OnObstacleHit;
        public System.Action<BaseObstacle> OnObstacleDestroyed;
        public System.Action<BaseObstacle> OnWarningTriggered;
        
        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            obstacleCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            rb2d = GetComponent<Rigidbody2D>();
            
            // Ensure collider is set as trigger
            if (obstacleCollider != null)
            {
                obstacleCollider.isTrigger = true;
            }
        }
        
        protected virtual void Start()
        {
            InitializeObstacle();
            StartObstacleEffects();
            
            // Find player reference
            var player = FindObjectOfType<Player.CatController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            
            // Get manager references
            gameManager = Core.GameManager.Instance;
            audioManager = FindObjectOfType<Core.AudioManager>();
        }
        
        protected virtual void Update()
        {
            if (isDestroyed || !isActive) return;
            
            // Check lifetime
            if (Time.time - spawnTime > lifeTime)
            {
                DestroyObstacle();
                return;
            }
            
            // Check warning system
            if (enableWarning && !isWarningActive)
            {
                CheckWarningTrigger();
            }
            
            // Custom update logic
            UpdateObstacle();
        }
        
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (isDestroyed || !isActive) return;
            
            if (other.CompareTag("Player"))
            {
                HandlePlayerCollision(other);
            }
        }
        
        #endregion
        
        #region Initialization
        
        protected virtual void InitializeObstacle()
        {
            spawnTime = Time.time;
            initialPosition = transform.position;
            isDestroyed = false;
            hasCollided = false;
            isWarningActive = false;
            isActive = true;
            
            // Reset visual state
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            
            // Enable collider
            if (obstacleCollider != null)
            {
                obstacleCollider.enabled = true;
            }
        }
        
        protected virtual void StartObstacleEffects()
        {
            // Start idle animation
            if (enableIdleAnimation)
            {
                idleAnimationCoroutine = StartCoroutine(IdleAnimation());
            }
            
            // Start movement if enabled
            if (enableMovement)
            {
                movementCoroutine = StartCoroutine(MovementCoroutine());
            }
        }
        
        public virtual void ResetObstacle()
        {
            // Stop all coroutines
            StopAllCoroutines();
            
            // Reset state
            InitializeObstacle();
            
            // Restart effects
            StartObstacleEffects();
        }
        
        #endregion
        
        #region Animation System
        
        private IEnumerator IdleAnimation()
        {
            Vector3 originalScale = transform.localScale;
            
            while (!isDestroyed && isActive)
            {
                float time = Time.time * animationSpeed;
                float animValue = idleAnimationCurve.Evaluate((time % 1f));
                
                // Apply subtle scale animation
                Vector3 newScale = originalScale * (1f + animValue * 0.1f);
                transform.localScale = newScale;
                
                yield return null;
            }
            
            transform.localScale = originalScale;
        }
        
        #endregion
        
        #region Movement System
        
        private IEnumerator MovementCoroutine()
        {
            while (!isDestroyed && isActive)
            {
                float currentMoveSpeed = moveSpeed;
                
                // Apply game speed multiplier
                if (followGameSpeed && gameManager != null)
                {
                    currentMoveSpeed *= gameManager.CurrentGameSpeed;
                }
                
                // Move obstacle
                Vector3 movement = moveDirection.normalized * currentMoveSpeed * Time.deltaTime;
                transform.Translate(movement);
                
                // Check if obstacle has moved too far off screen
                if (transform.position.x < -20f)
                {
                    DestroyObstacle();
                    yield break;
                }
                
                yield return null;
            }
        }
        
        #endregion
        
        #region Warning System
        
        private void CheckWarningTrigger()
        {
            if (playerTransform == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= warningDistance)
            {
                TriggerWarning();
            }
        }
        
        protected virtual void TriggerWarning()
        {
            if (isWarningActive) return;
            
            isWarningActive = true;
            warningCoroutine = StartCoroutine(WarningEffect());
            
            // Trigger warning event
            OnWarningTriggered?.Invoke(this);
            
            // Play warning visual effect
            if (warningEffect != null)
            {
                warningEffect.Play();
            }
        }
        
        private IEnumerator WarningEffect()
        {
            if (spriteRenderer == null) yield break;
            
            Color originalColor = spriteRenderer.color;
            float elapsed = 0f;
            
            while (elapsed < warningDuration && !isDestroyed)
            {
                elapsed += Time.deltaTime;
                
                // Flash warning color
                float flashIntensity = Mathf.Sin(elapsed * 10f) * 0.5f + 0.5f;
                Color flashColor = Color.Lerp(originalColor, warningColor, flashIntensity * 0.3f);
                spriteRenderer.color = flashColor;
                
                yield return null;
            }
            
            if (!isDestroyed)
            {
                spriteRenderer.color = originalColor;
            }
        }
        
        #endregion
        
        #region Collision System
        
        protected virtual void HandlePlayerCollision(Collider2D playerCollider)
        {
            if (hasCollided) return;
            
            hasCollided = true;
            
            // Determine collision result
            CollisionResult result = GetCollisionResult(playerCollider);
            
            // Execute collision logic
            ExecuteCollisionResult(result, playerCollider);
            
            // Play hit sound
            PlayHitSound();
            
            // Trigger collision event
            OnObstacleHit?.Invoke(this, playerCollider);
            
            // Handle post-collision behavior
            HandlePostCollision(result, playerCollider);
        }
        
        protected virtual CollisionResult GetCollisionResult(Collider2D playerCollider)
        {
            // Check if player has special abilities
            var catController = playerCollider.GetComponent<Player.CatController>();
            if (catController != null)
            {
                // TODO: Check for power-ups like shield, rocket shoes, etc.
                // if (catController.HasActivePowerUp("Shield"))
                //     return CollisionResult.Pass;
                // if (catController.HasActivePowerUp("RocketShoes") && CanJumpOver())
                //     return CollisionResult.Pass;
            }
            
            // Default collision behavior
            return GetDefaultCollisionResult();
        }
        
        protected abstract CollisionResult GetDefaultCollisionResult();
        
        protected virtual void ExecuteCollisionResult(CollisionResult result, Collider2D playerCollider)
        {
            var catController = playerCollider.GetComponent<Player.CatController>();
            
            switch (result)
            {
                case CollisionResult.Damage:
                    if (catController != null)
                    {
                        // TODO: Implement damage system in CatController
                        // catController.TakeDamage(damageAmount);
                        Debug.Log($"Player hit by obstacle - damage: {damageAmount}");
                    }
                    break;
                    
                case CollisionResult.Destroy:
                    if (canBeDestroyed)
                    {
                        DestroyObstacle();
                    }
                    break;
                    
                case CollisionResult.Bounce:
                    if (catController != null)
                    {
                        ApplyBounceEffect(catController);
                    }
                    break;
                    
                case CollisionResult.Stop:
                    if (catController != null)
                    {
                        ApplyStopEffect(catController);
                    }
                    break;
                    
                case CollisionResult.Pass:
                    // No effect
                    break;
            }
        }
        
        protected virtual void ApplyBounceEffect(Player.CatController catController)
        {
            // Apply bounce force (implementation depends on cat controller)
            // catController.ApplyBounce(Vector3.back * 5f);
        }
        
        protected virtual void ApplyStopEffect(Player.CatController catController)
        {
            // Stop player movement temporarily
            // catController.StopMovement(1f);
        }
        
        protected virtual void HandlePostCollision(CollisionResult result, Collider2D playerCollider)
        {
            // Custom post-collision behavior for derived classes
            OnPostCollision(result, playerCollider);
        }
        
        protected abstract void OnPostCollision(CollisionResult result, Collider2D playerCollider);
        
        #endregion
        
        #region Destruction System
        
        public virtual void DestroyObstacle()
        {
            if (isDestroyed) return;
            
            isDestroyed = true;
            isActive = false;
            
            // Stop all coroutines
            StopAllCoroutines();
            
            // Play destroy effects
            PlayDestroyEffects();
            
            // Disable components
            if (obstacleCollider != null)
            {
                obstacleCollider.enabled = false;
            }
            
            // Trigger destroy event
            OnObstacleDestroyed?.Invoke(this);
            
            // Handle destruction animation
            StartCoroutine(DestroyAnimation());
        }
        
        protected virtual void PlayDestroyEffects()
        {
            // Play destroy particle effect
            if (destroyEffect != null)
            {
                destroyEffect.Play();
            }
            
            // Play destroy sound
            if (audioManager != null && destroySound != null)
            {
                audioManager.PlayCustomSfx(destroySound, soundVolume);
            }
        }
        
        private IEnumerator DestroyAnimation()
        {
            // Scale down animation
            Vector3 originalScale = transform.localScale;
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);
                
                // Fade out if has sprite renderer
                if (spriteRenderer != null)
                {
                    Color color = spriteRenderer.color;
                    color.a = 1f - progress;
                    spriteRenderer.color = color;
                }
                
                yield return null;
            }
            
            // Wait for particle effects to finish
            if (destroyEffect != null && destroyEffect.isPlaying)
            {
                yield return new WaitForSeconds(2f);
            }
            
            // Destroy game object
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
        
        #endregion
        
        #region Audio System
        
        protected virtual void PlayHitSound()
        {
            if (audioManager != null && hitSound != null)
            {
                audioManager.PlayCustomSfx(hitSound, soundVolume);
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        protected virtual bool CanJumpOver()
        {
            // Override in derived classes to specify if obstacle can be jumped over
            return false;
        }
        
        protected virtual bool CanSlideUnder()
        {
            // Override in derived classes to specify if obstacle can be slid under
            return false;
        }
        
        protected virtual void UpdateObstacle()
        {
            // Override in derived classes for custom update logic
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Force destroy this obstacle
        /// </summary>
        public void ForceDestroy()
        {
            DestroyObstacle();
        }
        
        /// <summary>
        /// Set obstacle active state
        /// </summary>
        public virtual void SetActive(bool active)
        {
            isActive = active;
            
            if (obstacleCollider != null)
            {
                obstacleCollider.enabled = active;
            }
            
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = active;
            }
        }
        
        /// <summary>
        /// Get obstacle information for debugging
        /// </summary>
        public virtual ObstacleInfo GetInfo()
        {
            return new ObstacleInfo
            {
                type = Type,
                position = transform.position,
                isActive = IsActive,
                isDestroyed = IsDestroyed,
                damageAmount = DamageAmount,
                hasCollided = hasCollided,
                timeAlive = Time.time - spawnTime
            };
        }
        
        #endregion
        
        #region Debug
        
        protected virtual void OnDrawGizmos()
        {
            // Draw warning range
            if (enableWarning)
            {
                Gizmos.color = warningColor;
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawWireSphere(transform.position, warningDistance);
            }
            
            // Draw collision bounds
            if (obstacleCollider != null)
            {
                Gizmos.color = isActive ? Color.red : Color.gray;
                Gizmos.DrawWireCube(transform.position, obstacleCollider.bounds.size);
            }
            
            // Draw movement direction
            if (enableMovement)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, moveDirection.normalized * 2f);
            }
        }
        
        public virtual string GetDebugInfo()
        {
            return $"Obstacle: {Type}\n" +
                   $"Active: {IsActive}\n" +
                   $"Destroyed: {IsDestroyed}\n" +
                   $"Damage: {DamageAmount}\n" +
                   $"Has Collided: {hasCollided}\n" +
                   $"Warning Active: {isWarningActive}\n" +
                   $"Time Alive: {Time.time - spawnTime:F1}s";
        }
        
        #endregion
        
        [System.Serializable]
        public struct ObstacleInfo
        {
            public ObstacleType type;
            public Vector3 position;
            public bool isActive;
            public bool isDestroyed;
            public int damageAmount;
            public bool hasCollided;
            public float timeAlive;
        }
    }
}