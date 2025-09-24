using UnityEngine;

namespace CatchMeowIfYouCan.Player
{
    /// <summary>
    /// Handles cat animation states and transitions
    /// Works with Unity's Animator component to control cat animations
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CatAnimator : MonoBehaviour
    {
        [Header("Animation Parameters")]
        [SerializeField] private string groundedParam = "IsGrounded";
        [SerializeField] private string slidingParam = "IsSliding";
        [SerializeField] private string movingParam = "IsMoving";
        [SerializeField] private string jumpTrigger = "Jump";
        [SerializeField] private string slideTrigger = "Slide";
        [SerializeField] private string hitTrigger = "Hit";
        [SerializeField] private string celebrateTrigger = "Celebrate";
        
        [Header("Animation Settings")]
        [SerializeField] private float animationSpeed = 1f;
        [SerializeField] private bool flipSpriteOnTurn = true;
        
        // Components
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        
        // Animation state tracking
        private bool isGrounded = true;
        private bool isSliding = false;
        private bool isMovingBetweenLanes = false;
        private bool isAlive = true;
        
        // Hash IDs for performance
        private int groundedHash;
        private int slidingHash;
        private int movingHash;
        private int jumpHash;
        private int slideHash;
        private int hitHash;
        private int celebrateHash;
        
        private void Awake()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Cache animation parameter hashes
            groundedHash = Animator.StringToHash(groundedParam);
            slidingHash = Animator.StringToHash(slidingParam);
            movingHash = Animator.StringToHash(movingParam);
            jumpHash = Animator.StringToHash(jumpTrigger);
            slideHash = Animator.StringToHash(slideTrigger);
            hitHash = Animator.StringToHash(hitTrigger);
            celebrateHash = Animator.StringToHash(celebrateTrigger);
        }
        
        private void Start()
        {
            // Set initial animation speed
            animator.speed = animationSpeed;
            
            // Start with running animation
            SetMoving(true);
        }
        
        #region Public Animation Control Methods
        
        /// <summary>
        /// Sets whether the cat is grounded
        /// </summary>
        public void SetGrounded(bool grounded)
        {
            if (isGrounded != grounded)
            {
                isGrounded = grounded;
                animator.SetBool(groundedHash, grounded);
            }
        }
        
        /// <summary>
        /// Sets whether the cat is sliding
        /// </summary>
        public void SetSliding(bool sliding)
        {
            if (isSliding != sliding)
            {
                isSliding = sliding;
                animator.SetBool(slidingHash, sliding);
            }
        }
        
        /// <summary>
        /// Sets whether the cat is moving between lanes
        /// </summary>
        public void SetMovingBetweenLanes(bool moving)
        {
            if (isMovingBetweenLanes != moving)
            {
                isMovingBetweenLanes = moving;
                // You might want to play a special turning animation here
            }
        }
        
        /// <summary>
        /// Sets whether the cat is moving forward (running)
        /// </summary>
        public void SetMoving(bool moving)
        {
            animator.SetBool(movingHash, moving);
        }
        
        /// <summary>
        /// Triggers jump animation
        /// </summary>
        public void TriggerJump()
        {
            if (isAlive)
            {
                animator.SetTrigger(jumpHash);
            }
        }
        
        /// <summary>
        /// Triggers slide animation
        /// </summary>
        public void TriggerSlide()
        {
            if (isAlive)
            {
                animator.SetTrigger(slideHash);
            }
        }
        
        /// <summary>
        /// Triggers hit/death animation
        /// </summary>
        public void TriggerHit()
        {
            isAlive = false;
            animator.SetTrigger(hitHash);
            SetMoving(false);
        }
        
        /// <summary>
        /// Triggers celebrate animation (for power-ups or special moments)
        /// </summary>
        public void TriggerCelebrate()
        {
            if (isAlive)
            {
                animator.SetTrigger(celebrateHash);
            }
        }
        
        #endregion
        
        #region Animation Speed Control
        
        /// <summary>
        /// Sets the animation speed multiplier
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            animationSpeed = speed;
            animator.speed = speed;
        }
        
        /// <summary>
        /// Increases animation speed (for game speed progression)
        /// </summary>
        public void IncreaseSpeed(float increment)
        {
            animationSpeed += increment;
            animator.speed = animationSpeed;
        }
        
        /// <summary>
        /// Resets animation speed to default
        /// </summary>
        public void ResetSpeed()
        {
            animationSpeed = 1f;
            animator.speed = animationSpeed;
        }
        
        #endregion
        
        #region Sprite Direction Control
        
        /// <summary>
        /// Flips sprite horizontally based on movement direction
        /// </summary>
        public void FlipSprite(bool flipX)
        {
            if (spriteRenderer != null && flipSpriteOnTurn)
            {
                spriteRenderer.flipX = flipX;
            }
        }
        
        /// <summary>
        /// Updates sprite direction based on lane movement
        /// </summary>
        public void UpdateSpriteDirection(int currentLane, int targetLane)
        {
            if (!flipSpriteOnTurn || spriteRenderer == null) return;
            
            if (targetLane > currentLane)
            {
                // Moving right
                spriteRenderer.flipX = false;
            }
            else if (targetLane < currentLane)
            {
                // Moving left
                spriteRenderer.flipX = true;
            }
            // No flip when moving to same lane
        }
        
        #endregion
        
        #region Power-Up Visual Effects
        
        /// <summary>
        /// Applies visual effects for rocket shoes power-up
        /// </summary>
        public void ApplyRocketShoesEffect(bool active)
        {
            // Could change animation speed, add particle effects, etc.
            if (active)
            {
                SetAnimationSpeed(animationSpeed * 1.2f);
                // Add particle effect or glow
            }
            else
            {
                ResetSpeed();
                // Remove effects
            }
        }
        
        /// <summary>
        /// Applies visual effects for magnet power-up
        /// </summary>
        public void ApplyMagnetEffect(bool active)
        {
            // Could add a magnetic aura or glow effect
            // This would typically involve particle systems or shader effects
        }
        
        #endregion
        
        #region State Queries
        
        /// <summary>
        /// Returns current animation state info
        /// </summary>
        public AnimatorStateInfo GetCurrentStateInfo()
        {
            return animator.GetCurrentAnimatorStateInfo(0);
        }
        
        /// <summary>
        /// Checks if specific animation is currently playing
        /// </summary>
        public bool IsPlayingAnimation(string animationName)
        {
            return GetCurrentStateInfo().IsName(animationName);
        }
        
        /// <summary>
        /// Checks if cat is currently in hit/death state
        /// </summary>
        public bool IsInHitState()
        {
            return IsPlayingAnimation("CatHit") || !isAlive;
        }
        
        #endregion
        
        #region Reset and Cleanup
        
        /// <summary>
        /// Resets all animation states to default
        /// </summary>
        public void ResetAnimationStates()
        {
            isAlive = true;
            isGrounded = true;
            isSliding = false;
            isMovingBetweenLanes = false;
            
            // Reset animator parameters
            animator.SetBool(groundedHash, true);
            animator.SetBool(slidingHash, false);
            animator.SetBool(movingHash, true);
            
            // Reset animation speed
            ResetSpeed();
            
            // Reset sprite direction
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = false;
            }
        }
        
        #endregion
        
        #region Animation Events
        
        // These methods can be called by Animation Events in Unity
        
        /// <summary>
        /// Called by animation event when jump reaches peak
        /// </summary>
        public void OnJumpPeak()
        {
            // This can be used to trigger sound effects or other events
        }
        
        /// <summary>
        /// Called by animation event when slide starts
        /// </summary>
        public void OnSlideStart()
        {
            // This can be used to trigger sound effects or dust particles
        }
        
        /// <summary>
        /// Called by animation event when slide ends
        /// </summary>
        public void OnSlideEnd()
        {
            // This can be used to clean up slide effects
        }
        
        /// <summary>
        /// Called by animation event during hit animation
        /// </summary>
        public void OnHitAnimationComplete()
        {
            // This can be used to trigger game over sequence
        }
        
        #endregion
        
        private void OnValidate()
        {
            // Ensure animation speed is not negative
            if (animationSpeed < 0)
            {
                animationSpeed = 0.1f;
            }
        }
    }
}