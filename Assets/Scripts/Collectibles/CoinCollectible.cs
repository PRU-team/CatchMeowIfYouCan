using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.Collectibles
{
    /// <summary>
    /// Basic coin collectible that provides score points
    /// Most common collectible type in the game
    /// </summary>
    public class CoinCollectible : BaseCollectible
    {
        [Header("Coin Settings")]
        [SerializeField] private CoinType coinType = CoinType.Bronze;
        [SerializeField] private bool enableShimmerEffect = true;
        [SerializeField] private float shimmerInterval = 2f;
        
        [Header("Coin Visual Effects")]
        [SerializeField] private ParticleSystem shimmerParticles;
        [SerializeField] private AudioClip shimmerSound;
        [SerializeField] private float shimmerSoundVolume = 0.5f;
        
        [Header("Collection Bonus")]
        [SerializeField] private bool enableStreakBonus = true;
        [SerializeField] private float streakTimeWindow = 2f;
        [SerializeField] private int maxStreakMultiplier = 5;
        
        // Coin type enum
        public enum CoinType
        {
            Bronze = 10,
            Silver = 25,
            Gold = 50
        }
        
        // Static streak tracking
        private static int currentStreak = 0;
        private static float lastCollectionTime = 0f;
        
        // Runtime state
        private Coroutine shimmerCoroutine;
        private bool hasShimmered = false;
        
        // Properties
        public override CollectibleType Type => CollectibleType.Coin;
        public CoinType CurrentCoinType => coinType;
        
        #region Unity Lifecycle
        
        protected override void Start()
        {
            base.Start();
            
            // Set base value based on coin type
            baseValue = (int)coinType;
            
            // Start shimmer effect
            if (enableShimmerEffect)
            {
                shimmerCoroutine = StartCoroutine(ShimmerEffect());
            }
        }
        
        #endregion
        
        #region Coin Specific Logic
        
        protected override void OnItemCollected()
        {
            base.OnItemCollected();
            
            // Handle streak bonus
            if (enableStreakBonus)
            {
                HandleStreakBonus();
            }
            
            // Stop shimmer effect
            if (shimmerCoroutine != null)
            {
                StopCoroutine(shimmerCoroutine);
            }
            
            // Play coin-specific collection effects
            PlayCoinCollectionEffects();
        }
        
        private void HandleStreakBonus()
        {
            float timeSinceLastCollection = Time.time - lastCollectionTime;
            
            if (timeSinceLastCollection <= streakTimeWindow)
            {
                // Continue streak
                currentStreak++;
            }
            else
            {
                // Reset streak
                currentStreak = 1;
            }
            
            lastCollectionTime = Time.time;
            
            // Apply streak bonus to score
            if (currentStreak > 1 && scoreManager != null)
            {
                int streakMultiplier = Mathf.Min(currentStreak, maxStreakMultiplier);
                int bonusPoints = (Value * (streakMultiplier - 1)) / 2; // Half value per streak level
                scoreManager.AddScore(bonusPoints);
                
                // Show streak notification
                ShowStreakNotification(currentStreak, bonusPoints);
            }
        }
        
        private void ShowStreakNotification(int streak, int bonusPoints)
        {
            // TODO: Create floating text for streak bonus when UI system is implemented
            // var uiManager = FindObjectOfType<UI.UIManager>();
            // if (uiManager != null)
            // {
            //     string streakText = $"STREAK x{streak}! +{bonusPoints}";
            //     uiManager.ShowFloatingText(streakText, transform.position, Color.yellow);
            // }
            
            // Play streak sound
            if (audioManager != null && streak >= 3)
            {
                // TODO: Use appropriate sound clip for streak bonus
                audioManager.PlayCoinCollectSound();
            }
        }
        
        private void PlayCoinCollectionEffects()
        {
            // Play different sounds based on coin type
            if (audioManager != null)
            {
                audioManager.PlayCoinCollectSound();
            }
            
            // Spawn coin-specific particles
            SpawnCoinParticles();
        }
        
        private string GetCoinSoundName()
        {
            switch (coinType)
            {
                case CoinType.Bronze:
                    return "CoinBronze";
                case CoinType.Silver:
                    return "CoinSilver";
                case CoinType.Gold:
                    return "CoinGold";
                default:
                    return "CoinBronze";
            }
        }
        
        private void SpawnCoinParticles()
        {
            if (collectEffect != null)
            {
                // Adjust particle color based on coin type
                var main = collectEffect.main;
                
                switch (coinType)
                {
                    case CoinType.Bronze:
                        main.startColor = new Color(0.8f, 0.5f, 0.2f); // Bronze color
                        break;
                    case CoinType.Silver:
                        main.startColor = new Color(0.9f, 0.9f, 0.9f); // Silver color
                        break;
                    case CoinType.Gold:
                        main.startColor = new Color(1f, 0.8f, 0f); // Gold color
                        break;
                }
            }
        }
        
        #endregion
        
        #region Shimmer Effect
        
        private IEnumerator ShimmerEffect()
        {
            while (!isCollected)
            {
                yield return new WaitForSeconds(shimmerInterval + Random.Range(-0.5f, 0.5f));
                
                if (!isCollected && !hasShimmered)
                {
                    PlayShimmerEffect();
                    hasShimmered = true;
                    
                    // Reset shimmer flag after some time
                    yield return new WaitForSeconds(1f);
                    hasShimmered = false;
                }
            }
        }
        
        private void PlayShimmerEffect()
        {
            // Play shimmer particles
            if (shimmerParticles != null)
            {
                shimmerParticles.Play();
            }
            
            // Play shimmer sound
            if (audioManager != null && shimmerSound != null)
            {
                audioManager.PlayCustomSfx(shimmerSound, shimmerSoundVolume);
            }
            
            // Shimmer animation
            StartCoroutine(ShimmerAnimation());
        }
        
        private IEnumerator ShimmerAnimation()
        {
            if (spriteRenderer == null) yield break;
            
            float duration = 0.5f;
            Color originalColor = spriteRenderer.color;
            Color shimmerColor = GetShimmerColor();
            
            // Fade to shimmer color
            float elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (duration * 0.5f);
                spriteRenderer.color = Color.Lerp(originalColor, shimmerColor, progress);
                yield return null;
            }
            
            // Fade back to original color
            elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (duration * 0.5f);
                spriteRenderer.color = Color.Lerp(shimmerColor, originalColor, progress);
                yield return null;
            }
            
            spriteRenderer.color = originalColor;
        }
        
        private Color GetShimmerColor()
        {
            switch (coinType)
            {
                case CoinType.Bronze:
                    return new Color(1f, 0.7f, 0.3f, 1f);
                case CoinType.Silver:
                    return new Color(1f, 1f, 1f, 1f);
                case CoinType.Gold:
                    return new Color(1f, 1f, 0.5f, 1f);
                default:
                    return Color.white;
            }
        }
        
        #endregion
        
        #region Value Calculation Override
        
        protected override int GetAdjustedValue()
        {
            int finalValue = base.GetAdjustedValue();
            
            // Apply coin type multiplier
            switch (coinType)
            {
                case CoinType.Silver:
                    finalValue = Mathf.RoundToInt(finalValue * 1.2f);
                    break;
                case CoinType.Gold:
                    finalValue = Mathf.RoundToInt(finalValue * 1.5f);
                    break;
            }
            
            return finalValue;
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Set the coin type and update visual appearance
        /// </summary>
        public void SetCoinType(CoinType newType)
        {
            coinType = newType;
            baseValue = (int)coinType;
            
            // Update visual appearance based on type
            UpdateCoinAppearance();
        }
        
        /// <summary>
        /// Get current streak count
        /// </summary>
        public static int GetCurrentStreak()
        {
            return currentStreak;
        }
        
        /// <summary>
        /// Reset streak counter (called when player crashes or misses coins)
        /// </summary>
        public static void ResetStreak()
        {
            currentStreak = 0;
            lastCollectionTime = 0f;
        }
        
        /// <summary>
        /// Force trigger shimmer effect
        /// </summary>
        public void TriggerShimmer()
        {
            if (!isCollected && !hasShimmered)
            {
                PlayShimmerEffect();
            }
        }
        
        #endregion
        
        #region Visual Updates
        
        private void UpdateCoinAppearance()
        {
            if (spriteRenderer == null) return;
            
            // Update sprite color based on coin type
            Color coinColor = Color.white;
            
            switch (coinType)
            {
                case CoinType.Bronze:
                    coinColor = new Color(0.8f, 0.5f, 0.2f, 1f);
                    break;
                case CoinType.Silver:
                    coinColor = new Color(0.9f, 0.9f, 0.9f, 1f);
                    break;
                case CoinType.Gold:
                    coinColor = new Color(1f, 0.8f, 0f, 1f);
                    break;
            }
            
            spriteRenderer.color = coinColor;
            
            // Update scale based on value
            float scaleMultiplier = 1f + ((int)coinType - 10) * 0.05f;
            transform.localScale = Vector3.one * scaleMultiplier;
        }
        
        #endregion
        
        #region Reset Override
        
        public override void ResetCollectible()
        {
            base.ResetCollectible();
            
            hasShimmered = false;
            
            // Restart shimmer effect
            if (enableShimmerEffect && shimmerCoroutine == null)
            {
                shimmerCoroutine = StartCoroutine(ShimmerEffect());
            }
            
            // Update appearance
            UpdateCoinAppearance();
        }
        
        #endregion
        
        #region Debug Override
        
        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + 
                   $"\nCoin Type: {coinType}\n" +
                   $"Current Streak: {currentStreak}\n" +
                   $"Shimmer Ready: {!hasShimmered}";
        }
        
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            // Draw coin type indicator
            Gizmos.color = GetShimmerColor();
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.2f);
        }
        
        #endregion
    }
}