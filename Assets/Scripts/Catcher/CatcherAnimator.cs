using UnityEngine;

namespace CatchMeowIfYouCan.Catcher
{
    /// <summary>
    /// Handles Catcher animation states and transitions
    /// Works with Unity's Animator component to control catcher animations
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CatcherAnimator : MonoBehaviour
    {
        [Header("Animation Parameters")]
        [SerializeField] private string movingParam = "IsMoving";
        [SerializeField] private string chasingParam = "IsChasing";
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string angryParam = "IsAngry";
        [SerializeField] private string idleTrigger = "Idle";
        [SerializeField] private string catchTrigger = "Catch";
        [SerializeField] private string celebrateTrigger = "Celebrate";
        
        [Header("Animation Settings")]
        [SerializeField] private float baseAnimationSpeed = 1f;
        [SerializeField] private float maxAnimationSpeed = 2f;
        [SerializeField] private bool flipSpriteOnTurn = true;
        [SerializeField] private float angerThreshold = 0.8f; // Speed threshold for angry animation
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem dustParticles;
        [SerializeField] private ParticleSystem angerParticles;
        [SerializeField] private GameObject speedLines;
        
        // Components
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        
        // Animation state tracking
        private bool isMoving = false;
        private bool isChasing = false;
        private bool isAngry = false;
        private bool isMovingBetweenLanes = false;
        private float currentSpeed = 0f;
        private float currentAnimationSpeed = 1f;
        
        // Hash IDs for performance
        private int movingHash;
        private int chasingHash;
        private int speedHash;
        private int angryHash;
        private int idleHash;
        private int catchHash;
        private int celebrateHash;
        
        // Direction tracking
        private int lastLane = 1;
        private bool facingLeft = false;
        
        private void Awake()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Cache animation parameter hashes
            movingHash = Animator.StringToHash(movingParam);
            chasingHash = Animator.StringToHash(chasingParam);
            speedHash = Animator.StringToHash(speedParam);
            angryHash = Animator.StringToHash(angryParam);
            idleHash = Animator.StringToHash(idleTrigger);
            catchHash = Animator.StringToHash(catchTrigger);
            celebrateHash = Animator.StringToHash(celebrateTrigger);
        }
        
        private void Start()
        {
            // Set initial animation speed
            currentAnimationSpeed = baseAnimationSpeed;
            animator.speed = currentAnimationSpeed;
            
            // Initialize particle systems
            InitializeParticleEffects();
        }
        
        private void InitializeParticleEffects()
        {
            if (dustParticles != null)
            {
                dustParticles.Stop();
            }
            
            if (angerParticles != null)
            {
                angerParticles.Stop();
            }
            
            if (speedLines != null)
            {
                speedLines.SetActive(false);
            }
        }
        
        #region Public Animation Control Methods
        
        /// <summary>
        /// Sets whether the catcher is moving
        /// </summary>
        public void SetMoving(bool moving)
        {
            if (isMoving != moving)
            {
                isMoving = moving;
                animator.SetBool(movingHash, moving);
                
                // Control dust particles
                if (dustParticles != null)
                {
                    if (moving && isChasing)
                    {
                        dustParticles.Play();
                    }
                    else
                    {
                        dustParticles.Stop();
                    }
                }
            }
        }
        
        /// <summary>
        /// Sets whether the catcher is chasing the player
        /// </summary>
        public void SetChasing(bool chasing)
        {
            if (isChasing != chasing)
            {
                isChasing = chasing;
                animator.SetBool(chasingHash, chasing);
                
                // Update particle effects
                UpdateParticleEffects();
                
                if (!chasing)
                {
                    // Reset to idle when not chasing
                    TriggerIdle();
                }
            }
        }
        
        /// <summary>
        /// Sets the movement speed (0-1 normalized)
        /// </summary>
        public void SetSpeed(float normalizedSpeed)
        {
            currentSpeed = Mathf.Clamp01(normalizedSpeed);
            animator.SetFloat(speedHash, currentSpeed);
            
            // Update animation speed based on movement speed
            currentAnimationSpeed = Mathf.Lerp(baseAnimationSpeed, maxAnimationSpeed, currentSpeed);
            animator.speed = currentAnimationSpeed;
            
            // Check if catcher should be angry (high speed)
            bool shouldBeAngry = currentSpeed >= angerThreshold && isChasing;
            SetAngry(shouldBeAngry);
            
            // Update particle effects based on speed
            UpdateParticleEffects();
        }
        
        /// <summary>
        /// Sets whether the catcher is angry (high-speed chase mode)
        /// </summary>
        public void SetAngry(bool angry)
        {
            if (isAngry != angry)
            {
                isAngry = angry;
                animator.SetBool(angryHash, angry);
                
                // Control anger particles
                if (angerParticles != null)
                {
                    if (angry)
                    {
                        angerParticles.Play();
                    }
                    else
                    {
                        angerParticles.Stop();
                    }
                }
            }
        }
        
        /// <summary>
        /// Sets whether the catcher is moving between lanes
        /// </summary>
        public void SetMovingBetweenLanes(bool movingBetweenLanes)
        {
            isMovingBetweenLanes = movingBetweenLanes;
            // Could trigger special turning animation here
        }
        
        /// <summary>
        /// Triggers idle animation
        /// </summary>
        public void TriggerIdle()
        {
            animator.SetTrigger(idleHash);
        }
        
        /// <summary>
        /// Triggers catch animation
        /// </summary>
        public void TriggerCatch()
        {
            animator.SetTrigger(catchHash);
            
            // Stop all movement particles
            if (dustParticles != null)
            {
                dustParticles.Stop();
            }
            
            if (speedLines != null)
            {
                speedLines.SetActive(false);
            }
        }
        
        /// <summary>
        /// Triggers celebrate animation (when player is caught)
        /// </summary>
        public void TriggerCelebrate()
        {
            animator.SetTrigger(celebrateHash);
            
            // Stop chase particles, start celebration effects
            if (angerParticles != null)
            {
                angerParticles.Stop();
            }
        }
        
        #endregion
        
        #region Sprite Direction Control
        
        /// <summary>
        /// Updates sprite direction based on lane movement
        /// </summary>
        public void UpdateSpriteDirection(int currentLane)
        {
            if (!flipSpriteOnTurn || spriteRenderer == null) return;
            
            if (currentLane > lastLane)
            {
                // Moving right
                facingLeft = false;
                spriteRenderer.flipX = false;
            }
            else if (currentLane < lastLane)
            {
                // Moving left
                facingLeft = true;
                spriteRenderer.flipX = true;
            }
            
            lastLane = currentLane;
        }
        
        /// <summary>
        /// Manually flip sprite
        /// </summary>
        public void FlipSprite(bool flipX)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = flipX;
                facingLeft = flipX;
            }
        }
        
        #endregion
        
        #region Particle Effects
        
        private void UpdateParticleEffects()
        {
            // Dust particles - only when moving and chasing
            if (dustParticles != null)
            {
                if (isMoving && isChasing && currentSpeed > 0.3f)
                {
                    if (!dustParticles.isPlaying)
                    {
                        dustParticles.Play();
                    }
                    
                    // Adjust emission rate based on speed
                    var emission = dustParticles.emission;
                    emission.rateOverTime = Mathf.Lerp(10f, 30f, currentSpeed);
                }
                else
                {
                    dustParticles.Stop();
                }
            }
            
            // Speed lines - only at high speed
            if (speedLines != null)
            {
                bool shouldShowSpeedLines = currentSpeed >= 0.8f && isChasing;
                speedLines.SetActive(shouldShowSpeedLines);
            }
        }
        
        #endregion
        
        #region Animation Events
        
        // These methods can be called by Animation Events in Unity
        
        /// <summary>
        /// Called by animation event during running animation
        /// </summary>
        public void OnFootstep()
        {
            // This can be used to trigger footstep sound effects
            // Also can spawn dust particles at feet
        }
        
        /// <summary>
        /// Called by animation event when catch animation starts
        /// </summary>
        public void OnCatchStart()
        {
            // This can be used to trigger catch sound effects
        }
        
        /// <summary>
        /// Called by animation event when catch animation completes
        /// </summary>
        public void OnCatchComplete()
        {
            // This can be used to trigger game over sequence
            TriggerCelebrate();
        }
        
        /// <summary>
        /// Called by animation event during angry animation
        /// </summary>
        public void OnAngryBurst()
        {
            // This can be used to trigger anger sound effects or screen shake
            if (angerParticles != null && !angerParticles.isPlaying)
            {
                angerParticles.Play();
            }
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
        /// Checks if catcher is currently in catch state
        /// </summary>
        public bool IsInCatchState()
        {
            return IsPlayingAnimation("CatcherCatch");
        }
        
        /// <summary>
        /// Checks if catcher is currently angry
        /// </summary>
        public bool IsAngry()
        {
            return isAngry;
        }
        
        #endregion
        
        #region Reset and Cleanup
        
        /// <summary>
        /// Resets all animation states to default
        /// </summary>
        public void ResetAnimationStates()
        {
            isMoving = false;
            isChasing = false;
            isAngry = false;
            isMovingBetweenLanes = false;
            currentSpeed = 0f;
            lastLane = 1;
            facingLeft = false;
            
            // Reset animator parameters
            animator.SetBool(movingHash, false);
            animator.SetBool(chasingHash, false);
            animator.SetFloat(speedHash, 0f);
            animator.SetBool(angryHash, false);
            
            // Reset animation speed
            currentAnimationSpeed = baseAnimationSpeed;
            animator.speed = currentAnimationSpeed;
            
            // Reset sprite direction
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = false;
            }
            
            // Stop all particle effects
            StopAllParticleEffects();
        }
        
        private void StopAllParticleEffects()
        {
            if (dustParticles != null)
            {
                dustParticles.Stop();
            }
            
            if (angerParticles != null)
            {
                angerParticles.Stop();
            }
            
            if (speedLines != null)
            {
                speedLines.SetActive(false);
            }
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Get animation debug info
        /// </summary>
        public string GetAnimationDebugInfo()
        {
            return $"Moving: {isMoving}\n" +
                   $"Chasing: {isChasing}\n" +
                   $"Angry: {isAngry}\n" +
                   $"Speed: {currentSpeed:F2}\n" +
                   $"Anim Speed: {currentAnimationSpeed:F2}\n" +
                   $"Facing Left: {facingLeft}";
        }
        
        #endregion
        
        private void OnValidate()
        {
            // Ensure animation speeds are valid
            if (baseAnimationSpeed < 0.1f)
            {
                baseAnimationSpeed = 0.1f;
            }
            
            if (maxAnimationSpeed < baseAnimationSpeed)
            {
                maxAnimationSpeed = baseAnimationSpeed + 0.5f;
            }
            
            if (angerThreshold < 0f || angerThreshold > 1f)
            {
                angerThreshold = Mathf.Clamp01(angerThreshold);
            }
        }
    }
}