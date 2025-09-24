using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.PowerUps
{
    /// <summary>
    /// Shield power-up that protects player from obstacle collisions
    /// Features absorption mechanics, visual feedback, and damage mitigation
    /// </summary>
    public class ShieldPowerUp : BasePowerUp
    {
        [Header("Shield Settings")]
        [SerializeField] private int shieldHits = 1; // Number of hits shield can absorb
        [SerializeField] private bool blocksDamage = true;
        [SerializeField] private bool reflectsProjectiles = false;
        [SerializeField] private float reflectionForce = 10f;
        [SerializeField] private bool givesBonusOnHit = true;
        [SerializeField] private int hitBonusPoints = 15;
        
        [Header("Shield Regeneration")]
        [SerializeField] private bool canRegenerate = false;
        [SerializeField] private float regenerationDelay = 5f;
        [SerializeField] private float regenerationRate = 1f; // Hits per second
        [SerializeField] private int maxRegenerationHits = 3;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject shieldVisualPrefab;
        [SerializeField] private ParticleSystem shieldHitEffect;
        [SerializeField] private ParticleSystem shieldBreakEffect;
        [SerializeField] private ParticleSystem shieldRegenEffect;
        [SerializeField] private Color shieldColor = Color.cyan;
        [SerializeField] private AnimationCurve shieldPulse = AnimationCurve.EaseInOut(0f, 0.8f, 1f, 1.2f);
        
        [Header("Audio Effects")]
        [SerializeField] private AudioClip shieldHitSound;
        [SerializeField] private AudioClip shieldBreakSound;
        [SerializeField] private AudioClip shieldRegenSound;
        [SerializeField] private AudioClip reflectionSound;
        
        [Header("Gameplay Effects")]
        [SerializeField] private bool slowsTimeOnHit = false;
        [SerializeField] private float slowMotionFactor = 0.5f;
        [SerializeField] private float slowMotionDuration = 0.5f;
        [SerializeField] private bool pushesBackObstacles = false;
        [SerializeField] private float pushBackForce = 5f;
        [SerializeField] private float pushBackRadius = 2f;
        
        // Runtime state
        private int currentShieldHits;
        private bool shieldActive = false;
        private GameObject shieldVisual;
        private float lastHitTime;
        private float lastRegenerationTime;
        private bool isRegenerating = false;
        private Coroutine regenerationCoroutine;
        private Coroutine pulseCoroutine;
        
        // Shield visual components
        private SpriteRenderer shieldRenderer;
        private Light shieldLight;
        private Collider2D shieldCollider;
        
        // Integration with player
        private bool hasPlayerIntegration = false;
        private Vector3 shieldOffset = Vector3.zero;
        
        public override PowerUpType Type => PowerUpType.Shield;
        
        // Properties
        public int CurrentShieldHits => currentShieldHits;
        public int MaxShieldHits => shieldHits + (CurrentStacks * (canStack ? 1 : 0));
        public bool IsShieldActive => shieldActive && currentShieldHits > 0;
        public bool IsRegenerating => isRegenerating;
        public float RegenerationProgress => canRegenerate ? Mathf.Clamp01((Time.time - lastRegenerationTime) / regenerationDelay) : 0f;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            // Set power-up color
            powerUpColor = shieldColor;
        }
        
        protected override void UpdatePowerUp()
        {
            base.UpdatePowerUp();
            
            if (IsActive)
            {
                // Update shield regeneration
                if (canRegenerate)
                {
                    UpdateRegeneration();
                }
                
                // Update shield visual position
                UpdateShieldPosition();
                
                // Update shield visual effects
                UpdateShieldVisuals();
            }
        }
        
        #endregion
        
        #region Power-Up Implementation
        
        protected override void OnActivate()
        {
            // Initialize shield
            currentShieldHits = MaxShieldHits;
            shieldActive = true;
            lastHitTime = Time.time;
            lastRegenerationTime = Time.time;
            
            // Create shield visual
            CreateShieldVisual();
            
            // Attach to player
            AttachShieldToPlayer();
            
            // Start shield effects
            StartShieldEffects();
            
            Debug.Log($"Shield activated! Protection: {currentShieldHits} hits");
        }
        
        protected override void OnDeactivate()
        {
            // Deactivate shield
            shieldActive = false;
            
            // Stop regeneration
            StopRegeneration();
            
            // Remove shield visual
            RemoveShieldVisual();
            
            // Detach from player
            DetachShieldFromPlayer();
            
            Debug.Log("Shield deactivated.");
        }
        
        protected override void OnStackAdded()
        {
            // Each stack adds more shield hits
            if (canStack)
            {
                int additionalHits = 1;
                currentShieldHits += additionalHits;
                
                // Enhanced visual effects for stacked shields
                if (shieldLight != null)
                {
                    shieldLight.intensity *= 1.3f;
                    shieldLight.range *= 1.1f;
                }
                
                Debug.Log($"Shield stack added! Total protection: {currentShieldHits} hits");
            }
        }
        
        #endregion
        
        #region Shield Visual System
        
        private void CreateShieldVisual()
        {
            if (shieldVisualPrefab == null || catController == null) return;
            
            // Instantiate shield visual
            shieldVisual = Instantiate(shieldVisualPrefab);
            
            // Get shield components
            shieldRenderer = shieldVisual.GetComponent<SpriteRenderer>();
            shieldLight = shieldVisual.GetComponent<Light>();
            shieldCollider = shieldVisual.GetComponent<Collider2D>();
            
            // Setup shield appearance
            if (shieldRenderer != null)
            {
                shieldRenderer.color = shieldColor;
            }
            
            if (shieldLight != null)
            {
                shieldLight.color = shieldColor;
                shieldLight.enabled = true;
            }
            
            // Setup shield collider
            if (shieldCollider != null)
            {
                shieldCollider.isTrigger = true;
            }
        }
        
        private void RemoveShieldVisual()
        {
            if (shieldVisual != null)
            {
                // Play break effect before destroying
                if (shieldBreakEffect != null)
                {
                    Instantiate(shieldBreakEffect, shieldVisual.transform.position, Quaternion.identity);
                }
                
                Destroy(shieldVisual);
                shieldVisual = null;
            }
        }
        
        private void UpdateShieldPosition()
        {
            if (shieldVisual == null || catController == null) return;
            
            // Follow player position
            Vector3 playerPosition = catController.transform.position;
            shieldVisual.transform.position = playerPosition + shieldOffset;
            
            // Optional: Rotate shield to face forward
            shieldVisual.transform.rotation = catController.transform.rotation;
        }
        
        private void UpdateShieldVisuals()
        {
            if (shieldRenderer == null) return;
            
            // Update shield opacity based on remaining hits
            float healthRatio = (float)currentShieldHits / MaxShieldHits;
            Color currentColor = shieldColor;
            currentColor.a = Mathf.Lerp(0.3f, 1f, healthRatio);
            shieldRenderer.color = currentColor;
            
            // Update light intensity
            if (shieldLight != null)
            {
                shieldLight.intensity = Mathf.Lerp(0.5f, 2f, healthRatio);
            }
        }
        
        private void StartShieldEffects()
        {
            // Start pulsing animation
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }
            pulseCoroutine = StartCoroutine(ShieldPulseEffect());
        }
        
        private IEnumerator ShieldPulseEffect()
        {
            Vector3 originalScale = shieldVisual != null ? shieldVisual.transform.localScale : Vector3.one;
            
            while (shieldActive && shieldVisual != null)
            {
                float time = Time.time * 2f; // Pulse frequency
                float pulseValue = shieldPulse.Evaluate((time % 1f));
                
                shieldVisual.transform.localScale = originalScale * pulseValue;
                
                yield return null;
            }
            
            if (shieldVisual != null)
            {
                shieldVisual.transform.localScale = originalScale;
            }
        }
        
        #endregion
        
        #region Shield Mechanics
        
        public bool TryAbsorbHit(Obstacles.BaseObstacle obstacle, out bool shieldBroken)
        {
            shieldBroken = false;
            
            if (!IsShieldActive) return false;
            
            // Absorb the hit
            currentShieldHits--;
            lastHitTime = Time.time;
            
            // Play hit effects
            PlayShieldHitEffects();
            
            // Award bonus points
            if (givesBonusOnHit && scoreManager != null)
            {
                scoreManager.AddScore(hitBonusPoints);
            }
            
            // Check if shield is broken
            if (currentShieldHits <= 0)
            {
                shieldBroken = true;
                BreakShield();
            }
            
            // Special effects based on obstacle type
            HandleSpecialHitEffects(obstacle);
            
            Debug.Log($"Shield absorbed hit! Remaining: {currentShieldHits}");
            return true;
        }
        
        private void BreakShield()
        {
            shieldActive = false;
            
            // Play break effects
            PlayShieldBreakEffects();
            
            // Start regeneration if enabled
            if (canRegenerate)
            {
                StartRegeneration();
            }
            else
            {
                // Deactivate power-up if can't regenerate
                DeactivatePowerUp();
            }
            
            Debug.Log("Shield broken!");
        }
        
        private void HandleSpecialHitEffects(Obstacles.BaseObstacle obstacle)
        {
            if (obstacle == null) return;
            
            // Slow time effect
            if (slowsTimeOnHit)
            {
                StartCoroutine(SlowTimeEffect());
            }
            
            // Push back obstacles
            if (pushesBackObstacles)
            {
                PushBackNearbyObstacles();
            }
            
            // Reflect projectiles (if applicable)
            if (reflectsProjectiles)
            {
                // TODO: Implement projectile reflection
                Debug.Log("Projectile reflected by shield!");
                
                if (reflectionSound != null && audioManager != null)
                {
                    audioManager.PlayCustomSfx(reflectionSound, audioVolume);
                }
            }
        }
        
        private IEnumerator SlowTimeEffect()
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = slowMotionFactor;
            
            yield return new WaitForSecondsRealtime(slowMotionDuration);
            
            Time.timeScale = originalTimeScale;
        }
        
        private void PushBackNearbyObstacles()
        {
            Vector3 playerPosition = catController.transform.position;
            
            // Find nearby obstacles
            Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(playerPosition, pushBackRadius);
            
            foreach (var collider in nearbyColliders)
            {
                var obstacle = collider.GetComponent<Obstacles.BaseObstacle>();
                if (obstacle != null)
                {
                    // Calculate push direction
                    Vector3 pushDirection = (obstacle.transform.position - playerPosition).normalized;
                    
                    // Apply push force
                    Rigidbody2D obstacleRb = obstacle.GetComponent<Rigidbody2D>();
                    if (obstacleRb != null)
                    {
                        obstacleRb.AddForce(pushDirection * pushBackForce, ForceMode2D.Impulse);
                    }
                }
            }
        }
        
        #endregion
        
        #region Regeneration System
        
        private void UpdateRegeneration()
        {
            if (!canRegenerate || shieldActive) return;
            if (currentShieldHits >= maxRegenerationHits) return;
            
            // Check if enough time has passed since last hit
            if (Time.time - lastHitTime >= regenerationDelay)
            {
                if (!isRegenerating)
                {
                    StartRegeneration();
                }
                
                UpdateRegenerationProgress();
            }
        }
        
        private void StartRegeneration()
        {
            if (isRegenerating) return;
            
            isRegenerating = true;
            lastRegenerationTime = Time.time;
            
            regenerationCoroutine = StartCoroutine(RegenerationCoroutine());
            
            Debug.Log("Shield regeneration started.");
        }
        
        private void StopRegeneration()
        {
            if (!isRegenerating) return;
            
            isRegenerating = false;
            
            if (regenerationCoroutine != null)
            {
                StopCoroutine(regenerationCoroutine);
                regenerationCoroutine = null;
            }
        }
        
        private IEnumerator RegenerationCoroutine()
        {
            while (isRegenerating && currentShieldHits < maxRegenerationHits)
            {
                yield return new WaitForSeconds(1f / regenerationRate);
                
                if (currentShieldHits < maxRegenerationHits)
                {
                    currentShieldHits++;
                    
                    // Play regeneration effects
                    PlayShieldRegenEffects();
                    
                    // Reactivate shield if it was broken
                    if (!shieldActive && currentShieldHits > 0)
                    {
                        shieldActive = true;
                        CreateShieldVisual();
                        AttachShieldToPlayer();
                    }
                    
                    Debug.Log($"Shield regenerated! Current: {currentShieldHits}");
                }
            }
            
            isRegenerating = false;
        }
        
        private void UpdateRegenerationProgress()
        {
            // Visual feedback for regeneration progress
            if (shieldRenderer != null)
            {
                float progress = RegenerationProgress;
                Color regenColor = Color.Lerp(Color.red, shieldColor, progress);
                shieldRenderer.color = regenColor;
            }
        }
        
        #endregion
        
        #region Player Integration
        
        private void AttachShieldToPlayer()
        {
            if (catController == null || shieldVisual == null) return;
            
            hasPlayerIntegration = true;
            
            // Set shield as child of player (optional)
            // shieldVisual.transform.SetParent(catController.transform);
            
            // Calculate shield offset
            shieldOffset = Vector3.zero; // Center on player
            
            Debug.Log("Shield attached to player.");
        }
        
        private void DetachShieldFromPlayer()
        {
            if (!hasPlayerIntegration) return;
            
            hasPlayerIntegration = false;
            
            // Detach shield from player
            if (shieldVisual != null)
            {
                shieldVisual.transform.SetParent(null);
            }
            
            Debug.Log("Shield detached from player.");
        }
        
        #endregion
        
        #region Audio Effects
        
        private void PlayShieldHitEffects()
        {
            // Play hit particle effect
            if (shieldHitEffect != null)
            {
                if (shieldVisual != null)
                {
                    Instantiate(shieldHitEffect, shieldVisual.transform.position, Quaternion.identity);
                }
                else
                {
                    shieldHitEffect.Play();
                }
            }
            
            // Play hit sound
            if (shieldHitSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(shieldHitSound, audioVolume);
            }
        }
        
        private void PlayShieldBreakEffects()
        {
            // Play break particle effect
            if (shieldBreakEffect != null)
            {
                if (shieldVisual != null)
                {
                    Instantiate(shieldBreakEffect, shieldVisual.transform.position, Quaternion.identity);
                }
                else
                {
                    shieldBreakEffect.Play();
                }
            }
            
            // Play break sound
            if (shieldBreakSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(shieldBreakSound, audioVolume);
            }
        }
        
        private void PlayShieldRegenEffects()
        {
            // Play regeneration particle effect
            if (shieldRegenEffect != null)
            {
                if (shieldVisual != null)
                {
                    Instantiate(shieldRegenEffect, shieldVisual.transform.position, Quaternion.identity);
                }
                else
                {
                    shieldRegenEffect.Play();
                }
            }
            
            // Play regeneration sound
            if (shieldRegenSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(shieldRegenSound, audioVolume * 0.7f);
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get shield-specific information
        /// </summary>
        public ShieldInfo GetShieldInfo()
        {
            return new ShieldInfo
            {
                currentHits = currentShieldHits,
                maxHits = MaxShieldHits,
                isActive = IsShieldActive,
                isRegenerating = IsRegenerating,
                regenerationProgress = RegenerationProgress,
                canRegenerate = canRegenerate,
                blocksDamage = blocksDamage,
                reflectsProjectiles = reflectsProjectiles
            };
        }
        
        /// <summary>
        /// Manually trigger shield regeneration
        /// </summary>
        public void TriggerRegeneration()
        {
            if (canRegenerate && !isRegenerating)
            {
                lastHitTime = Time.time - regenerationDelay; // Force regeneration start
            }
        }
        
        /// <summary>
        /// Add shield hits (for upgrades/bonuses)
        /// </summary>
        public void AddShieldHits(int additionalHits)
        {
            currentShieldHits = Mathf.Min(currentShieldHits + additionalHits, MaxShieldHits);
            
            if (!shieldActive && currentShieldHits > 0)
            {
                shieldActive = true;
                CreateShieldVisual();
                AttachShieldToPlayer();
            }
            
            Debug.Log($"Shield hits added! Current: {currentShieldHits}");
        }
        
        #endregion
        
        #region Debug Override
        
        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $"\n" +
                   $"Shield Hits: {currentShieldHits}/{MaxShieldHits}\n" +
                   $"Shield Active: {IsShieldActive}\n" +
                   $"Regenerating: {IsRegenerating}\n" +
                   $"Regen Progress: {RegenerationProgress:P0}\n" +
                   $"Last Hit: {Time.time - lastHitTime:F1}s ago";
        }
        
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            // Draw shield visual range
            if (IsActive)
            {
                Gizmos.color = shieldColor;
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawWireSphere(transform.position, 1.5f);
            }
            
            // Draw push back radius
            if (pushesBackObstacles)
            {
                Gizmos.color = Color.red;
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
                Gizmos.DrawWireSphere(transform.position, pushBackRadius);
            }
        }
        
        #endregion
        
        #region Data Structures
        
        [System.Serializable]
        public struct ShieldInfo
        {
            public int currentHits;
            public int maxHits;
            public bool isActive;
            public bool isRegenerating;
            public float regenerationProgress;
            public bool canRegenerate;
            public bool blocksDamage;
            public bool reflectsProjectiles;
        }
        
        #endregion
    }
}