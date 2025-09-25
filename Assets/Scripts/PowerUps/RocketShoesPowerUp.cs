using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.PowerUps
{
    /// <summary>
    /// Rocket Shoes power-up that allows higher jumping and passing over obstacles
    /// Features enhanced jump mechanics, rocket trail effects, and obstacle interaction
    /// </summary>
    public class RocketShoesPowerUp : BasePowerUp
    {
        [Header("Rocket Shoes Settings")]
        [SerializeField] private float jumpBoostMultiplier = 2f;
        [SerializeField] private int maxExtraJumps = 1; // Air jumps
        [SerializeField] private bool allowObstacleJumping = true;
        [SerializeField] private float obstacleJumpHeight = 3f;
        [SerializeField] private bool givesBonusPoints = true;
        [SerializeField] private int obstacleJumpBonus = 25;
        
        [Header("Rocket Effects")]
        [SerializeField] private ParticleSystem rocketTrail;
        [SerializeField] private ParticleSystem jumpBurst;
        [SerializeField] private AudioClip rocketSound;
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private float thrustForce = 20f;
        
        [Header("Visual Enhancements")]
        [SerializeField] private Light rocketLight;
        [SerializeField] private Color rocketGlow = Color.red;
        [SerializeField] private GameObject rocketShoesModel;
        [SerializeField] private Transform[] thrusterPositions;
        [SerializeField] private ParticleSystem[] thrusterEffects;
        
        [Header("Gameplay Modifiers")]
        [SerializeField] private float fallSpeedReduction = 0.7f;
        [SerializeField] private bool enableHoverMode = false;
        [SerializeField] private float hoverDuration = 1f;
        [SerializeField] private KeyCode hoverKey = KeyCode.Space;
        
        // Runtime state
        private float originalJumpForce;
        private int extraJumpsUsed = 0;
        private bool isHovering = false;
        private bool rocketEffectsActive = false;
        private Coroutine hoverCoroutine;
        private AudioSource rocketAudioSource;
        
        // Player integration
        private bool hasOriginalJumpValues = false;
        private Rigidbody2D playerRigidbody;
        private Vector3 lastPlayerPosition;
        private float lastJumpTime;
        
        public override PowerUpType Type => PowerUpType.RocketShoes;
        
        // Properties
        public bool CanJumpOverObstacles => allowObstacleJumping && IsActive;
        public int RemainingExtraJumps => Mathf.Max(0, maxExtraJumps - extraJumpsUsed);
        public bool IsHovering => isHovering;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            // Setup rocket-specific audio
            rocketAudioSource = gameObject.AddComponent<AudioSource>();
            rocketAudioSource.playOnAwake = false;
            rocketAudioSource.loop = true;
            rocketAudioSource.volume = audioVolume * 0.7f;
            
            // Set power-up color
            powerUpColor = rocketGlow;
        }
        
        protected override void Start()
        {
            base.Start();
            
            // Get player rigidbody reference
            if (catController != null)
            {
                playerRigidbody = catController.GetComponent<Rigidbody2D>();
            }
        }
        
        protected override void UpdatePowerUp()
        {
            base.UpdatePowerUp();
            
            if (IsActive && catController != null)
            {
                // Update rocket effects
                UpdateRocketEffects();
                
                // Handle hover input
                if (enableHoverMode)
                {
                    HandleHoverInput();
                }
                
                // Check for landing to reset extra jumps
                CheckLandingReset();
                
                // Update visual effects
                UpdateVisualEffects();
            }
        }
        
        #endregion
        
        #region Power-Up Implementation
        
        protected override void OnActivate()
        {
            if (catController == null) return;
            
            // Store original jump values
            StoreOriginalValues();
            
            // Apply rocket shoes enhancements
            ApplyJumpEnhancement();
            
            // Start rocket effects
            StartRocketEffects();
            
            // Reset extra jumps
            extraJumpsUsed = 0;
            
            // Attach visual effects to player
            AttachVisualEffectsToPlayer();
            
            Debug.Log("Rocket Shoes activated! Enhanced jumping enabled.");
        }
        
        protected override void OnDeactivate()
        {
            if (catController == null) return;
            
            // Restore original jump values
            RestoreOriginalValues();
            
            // Stop rocket effects
            StopRocketEffects();
            
            // Stop hovering if active
            if (isHovering)
            {
                StopHovering();
            }
            
            // Detach visual effects
            DetachVisualEffectsFromPlayer();
            
            Debug.Log("Rocket Shoes deactivated. Normal jumping restored.");
        }
        
        protected override void OnStackAdded()
        {
            // Each stack increases jump multiplier and extra jumps
            jumpBoostMultiplier += 0.5f;
            maxExtraJumps += 1;
            
            // Enhanced effects for stacked rocket shoes
            if (rocketLight != null)
            {
                rocketLight.intensity *= 1.2f;
            }
            
            Debug.Log($"Rocket Shoes stack added! Jump boost: {jumpBoostMultiplier}x, Extra jumps: {maxExtraJumps}");
        }
        
        #endregion
        
        #region Jump Enhancement System
        
        private void StoreOriginalValues()
        {
            if (hasOriginalJumpValues) return;
            
            // TODO: Store original jump force from CatController
            // originalJumpForce = catController.JumpForce;
            originalJumpForce = 15f; // Placeholder value
            hasOriginalJumpValues = true;
        }
        
        private void RestoreOriginalValues()
        {
            if (!hasOriginalJumpValues) return;
            
            // TODO: Restore original jump force to CatController
            // catController.JumpForce = originalJumpForce;
            hasOriginalJumpValues = false;
        }
        
        private void ApplyJumpEnhancement()
        {
            if (!hasOriginalJumpValues) return;
            
            // TODO: Apply enhanced jump force to CatController
            float enhancedJumpForce = originalJumpForce * jumpBoostMultiplier;
            // catController.JumpForce = enhancedJumpForce;
            
            Debug.Log($"Jump force enhanced: {originalJumpForce} -> {enhancedJumpForce}");
        }
        
        public bool TryExtraJump()
        {
            if (!IsActive) return false;
            if (extraJumpsUsed >= maxExtraJumps) return false;
            if (catController == null || playerRigidbody == null) return false;
            
            extraJumpsUsed++;
            lastJumpTime = Time.time;
            
            // Apply extra jump force
            Vector2 jumpForce = Vector2.up * (originalJumpForce * jumpBoostMultiplier);
            playerRigidbody.linearVelocity = new Vector2(playerRigidbody.linearVelocity.x, 0); // Reset vertical velocity
            playerRigidbody.AddForce(jumpForce, ForceMode2D.Impulse);
            
            // Play jump effects
            PlayJumpEffects();
            
            Debug.Log($"Extra jump used! Remaining: {RemainingExtraJumps}");
            return true;
        }
        
        private void CheckLandingReset()
        {
            if (catController == null) return;
            
            // TODO: Check if player is grounded using CatController.IsGrounded
            // if (catController.IsGrounded && extraJumpsUsed > 0)
            // {
            //     extraJumpsUsed = 0;
            // }
        }
        
        #endregion
        
        #region Obstacle Jumping System
        
        private bool CheckObstacleJumpability(Obstacles.BaseObstacle obstacle)
        {
            if (obstacle == null) return false;
            
            // Check obstacle properties to determine if it can be jumped over
            // Most obstacles can be jumped over with rocket shoes, except special types
            
            // Get obstacle bounds to check height
            Bounds obstacleBounds = obstacle.GetComponent<Collider2D>()?.bounds ?? new Bounds();
            
            // Very tall obstacles might not be jumpable even with rocket shoes
            float obstacleHeight = obstacleBounds.size.y;
            float maxJumpableHeight = obstacleJumpHeight * jumpBoostMultiplier;
            
            if (obstacleHeight > maxJumpableHeight)
            {
                Debug.Log($"Obstacle too tall to jump: {obstacleHeight} > {maxJumpableHeight}");
                return false;
            }
            
            // Check for special obstacle types that can't be jumped
            string obstacleTag = obstacle.tag;
            if (obstacleTag == "UnJumpable" || obstacleTag == "Ceiling")
            {
                return false;
            }
            
            // Default: most obstacles can be jumped over with rocket shoes
            return true;
        }
        
        public bool CanJumpOverObstacle(Obstacles.BaseObstacle obstacle)
        {
            if (!allowObstacleJumping || !IsActive) return false;
            if (obstacle == null) return false;
            
            // Check if obstacle can be jumped over based on type and properties
            bool canJumpOver = CheckObstacleJumpability(obstacle);
            if (!canJumpOver) return false;
            
            // Check if player has sufficient height/power
            Vector3 playerPos = catController.transform.position;
            Vector3 obstaclePos = obstacle.transform.position;
            
            float requiredHeight = obstaclePos.y + obstacleJumpHeight;
            bool hasHeight = playerPos.y >= requiredHeight;
            
            return hasHeight || RemainingExtraJumps > 0;
        }
        
        public void OnObstacleJumped(Obstacles.BaseObstacle obstacle)
        {
            if (!IsActive || !givesBonusPoints) return;
            
            // Award bonus points
            if (scoreManager != null)
            {
                scoreManager.AddScore(obstacleJumpBonus);
            }
            
            // Play special effects
            PlayObstacleJumpEffects();
            
            Debug.Log($"Obstacle jumped with rocket shoes! +{obstacleJumpBonus} points");
        }
        
        private void PlayObstacleJumpEffects()
        {
            // Enhanced particle burst
            if (jumpBurst != null)
            {
                var emission = jumpBurst.emission;
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0f, 30)
                });
                jumpBurst.Play();
            }
            
            // Screen shake effect
            // TODO: Implement screen shake
            Debug.Log("Screen shake from rocket jump!");
        }
        
        #endregion
        
        #region Hover System
        
        private void HandleHoverInput()
        {
            if (Input.GetKeyDown(hoverKey) && !isHovering)
            {
                StartHovering();
            }
        }
        
        private void StartHovering()
        {
            if (isHovering || playerRigidbody == null) return;
            
            isHovering = true;
            hoverCoroutine = StartCoroutine(HoverCoroutine());
            
            Debug.Log("Rocket shoes hovering activated!");
        }
        
        private IEnumerator HoverCoroutine()
        {
            float hoverStartTime = Time.time;
            Vector2 originalGravityScale = Vector2.one * playerRigidbody.gravityScale;
            
            // Reduce gravity for hovering
            playerRigidbody.gravityScale = 0.2f;
            
            // Apply upward force to counteract gravity
            while (isHovering && Time.time - hoverStartTime < hoverDuration)
            {
                if (playerRigidbody != null)
                {
                    Vector2 hoverForce = Vector2.up * thrustForce * 0.5f;
                    playerRigidbody.AddForce(hoverForce);
                }
                
                yield return new WaitForFixedUpdate();
            }
            
            // Restore gravity
            if (playerRigidbody != null)
            {
                playerRigidbody.gravityScale = originalGravityScale.x;
            }
            
            isHovering = false;
            Debug.Log("Rocket shoes hovering ended.");
        }
        
        private void StopHovering()
        {
            if (!isHovering) return;
            
            isHovering = false;
            
            if (hoverCoroutine != null)
            {
                StopCoroutine(hoverCoroutine);
                hoverCoroutine = null;
            }
        }
        
        #endregion
        
        #region Rocket Effects System
        
        private void StartRocketEffects()
        {
            rocketEffectsActive = true;
            
            // Start rocket trail
            if (rocketTrail != null)
            {
                rocketTrail.Play();
            }
            
            // Enable rocket light
            if (rocketLight != null)
            {
                rocketLight.enabled = true;
                rocketLight.color = rocketGlow;
            }
            
            // Start thruster effects
            if (thrusterEffects != null)
            {
                foreach (var thruster in thrusterEffects)
                {
                    if (thruster != null)
                    {
                        thruster.Play();
                    }
                }
            }
            
            // Start rocket sound
            if (rocketAudioSource != null && rocketSound != null)
            {
                rocketAudioSource.clip = rocketSound;
                rocketAudioSource.Play();
            }
        }
        
        private void StopRocketEffects()
        {
            rocketEffectsActive = false;
            
            // Stop rocket trail
            if (rocketTrail != null)
            {
                rocketTrail.Stop();
            }
            
            // Disable rocket light
            if (rocketLight != null)
            {
                rocketLight.enabled = false;
            }
            
            // Stop thruster effects
            if (thrusterEffects != null)
            {
                foreach (var thruster in thrusterEffects)
                {
                    if (thruster != null)
                    {
                        thruster.Stop();
                    }
                }
            }
            
            // Stop rocket sound
            if (rocketAudioSource != null)
            {
                rocketAudioSource.Stop();
            }
        }
        
        private void UpdateRocketEffects()
        {
            if (!rocketEffectsActive || playerRigidbody == null) return;
            
            // Modulate effects based on movement
            float velocityMagnitude = playerRigidbody.linearVelocity.magnitude;
            float effectIntensity = Mathf.Clamp01(velocityMagnitude / 10f);
            
            // Update particle emission rates
            if (rocketTrail != null)
            {
                var emission = rocketTrail.emission;
                emission.rateOverTime = 20f + (effectIntensity * 30f);
            }
            
            // Update light intensity
            if (rocketLight != null)
            {
                rocketLight.intensity = 1f + (effectIntensity * 0.5f);
            }
            
            // Update audio pitch based on movement
            if (rocketAudioSource != null && rocketAudioSource.isPlaying)
            {
                rocketAudioSource.pitch = 0.8f + (effectIntensity * 0.4f);
            }
        }
        
        private void PlayJumpEffects()
        {
            // Play jump particle burst
            if (jumpBurst != null)
            {
                jumpBurst.Play();
            }
            
            // Play jump sound
            if (jumpSound != null)
            {
                if (audioManager != null)
                {
                    audioManager.PlayCustomSfx(jumpSound, audioVolume);
                }
                else if (audioSource != null)
                {
                    audioSource.PlayOneShot(jumpSound, audioVolume);
                }
            }
        }
        
        #endregion
        
        #region Visual Effects Attachment
        
        private void AttachVisualEffectsToPlayer()
        {
            if (catController == null) return;
            
            Transform playerTransform = catController.transform;
            
            // Attach rocket shoes model
            if (rocketShoesModel != null)
            {
                rocketShoesModel.transform.SetParent(playerTransform);
                rocketShoesModel.transform.localPosition = Vector3.zero;
                rocketShoesModel.SetActive(true);
            }
            
            // Attach particle effects
            if (rocketTrail != null)
            {
                rocketTrail.transform.SetParent(playerTransform);
                rocketTrail.transform.localPosition = Vector3.down * 0.5f;
            }
            
            if (jumpBurst != null)
            {
                jumpBurst.transform.SetParent(playerTransform);
                jumpBurst.transform.localPosition = Vector3.zero;
            }
            
            // Attach light
            if (rocketLight != null)
            {
                rocketLight.transform.SetParent(playerTransform);
                rocketLight.transform.localPosition = Vector3.down * 0.3f;
            }
            
            // Position thrusters
            if (thrusterPositions != null && thrusterEffects != null)
            {
                for (int i = 0; i < Mathf.Min(thrusterPositions.Length, thrusterEffects.Length); i++)
                {
                    if (thrusterPositions[i] != null && thrusterEffects[i] != null)
                    {
                        thrusterEffects[i].transform.SetParent(playerTransform);
                        thrusterEffects[i].transform.localPosition = thrusterPositions[i].localPosition;
                    }
                }
            }
        }
        
        private void DetachVisualEffectsFromPlayer()
        {
            // Detach and hide rocket shoes model
            if (rocketShoesModel != null)
            {
                rocketShoesModel.transform.SetParent(transform);
                rocketShoesModel.SetActive(false);
            }
            
            // Detach particle effects
            if (rocketTrail != null)
            {
                rocketTrail.transform.SetParent(transform);
            }
            
            if (jumpBurst != null)
            {
                jumpBurst.transform.SetParent(transform);
            }
            
            // Detach light
            if (rocketLight != null)
            {
                rocketLight.transform.SetParent(transform);
            }
            
            // Detach thrusters
            if (thrusterEffects != null)
            {
                foreach (var thruster in thrusterEffects)
                {
                    if (thruster != null)
                    {
                        thruster.transform.SetParent(transform);
                    }
                }
            }
        }
        
        private void UpdateVisualEffects()
        {
            if (catController == null) return;
            
            // Track player movement for effects
            Vector3 currentPlayerPosition = catController.transform.position;
            Vector3 movement = currentPlayerPosition - lastPlayerPosition;
            lastPlayerPosition = currentPlayerPosition;
            
            // Update trail based on movement
            if (rocketTrail != null && movement.magnitude > 0.1f)
            {
                var velocityOverLifetime = rocketTrail.velocityOverLifetime;
                velocityOverLifetime.enabled = true;
                velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get rocket shoes specific information
        /// </summary>
        public RocketShoesInfo GetRocketShoesInfo()
        {
            return new RocketShoesInfo
            {
                jumpBoostMultiplier = jumpBoostMultiplier,
                remainingExtraJumps = RemainingExtraJumps,
                maxExtraJumps = maxExtraJumps,
                canJumpOverObstacles = CanJumpOverObstacles,
                isHovering = IsHovering,
                obstacleJumpHeight = obstacleJumpHeight
            };
        }
        
        /// <summary>
        /// Check if can perform specific action
        /// </summary>
        public bool CanPerformAction(RocketShoesAction action)
        {
            if (!IsActive) return false;
            
            switch (action)
            {
                case RocketShoesAction.ExtraJump:
                    return RemainingExtraJumps > 0;
                case RocketShoesAction.ObstacleJump:
                    return allowObstacleJumping;
                case RocketShoesAction.Hover:
                    return enableHoverMode && !isHovering;
                default:
                    return false;
            }
        }
        
        #endregion
        
        #region Debug Override
        
        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $"\n" +
                   $"Jump Boost: {jumpBoostMultiplier}x\n" +
                   $"Extra Jumps: {RemainingExtraJumps}/{maxExtraJumps}\n" +
                   $"Hovering: {IsHovering}\n" +
                   $"Can Jump Obstacles: {CanJumpOverObstacles}\n" +
                   $"Rocket Effects: {rocketEffectsActive}";
        }
        
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            // Draw obstacle jump height
            if (allowObstacleJumping && IsActive)
            {
                Gizmos.color = Color.red;
                Vector3 jumpHeightPos = transform.position + Vector3.up * obstacleJumpHeight;
                Gizmos.DrawWireCube(jumpHeightPos, Vector3.one * 0.5f);
                Gizmos.DrawLine(transform.position, jumpHeightPos);
            }
        }
        
        #endregion
        
        #region Data Structures
        
        public enum RocketShoesAction
        {
            ExtraJump,
            ObstacleJump,
            Hover
        }
        
        [System.Serializable]
        public struct RocketShoesInfo
        {
            public float jumpBoostMultiplier;
            public int remainingExtraJumps;
            public int maxExtraJumps;
            public bool canJumpOverObstacles;
            public bool isHovering;
            public float obstacleJumpHeight;
        }
        
        #endregion
    }
}