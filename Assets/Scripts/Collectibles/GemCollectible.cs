using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.Collectibles
{
    /// <summary>
    /// Premium gem collectible with higher value and special effects
    /// Rarer than coins but provides significant score boost
    /// </summary>
    public class GemCollectible : BaseCollectible
    {
        [Header("Gem Settings")]
        [SerializeField] private GemType gemType = GemType.Ruby;
        [SerializeField] private bool enableAuraEffect = true;
        [SerializeField] private float auraIntensity = 1f;
        
        [Header("Gem Visual Effects")]
        [SerializeField] private ParticleSystem auraParticles;
        [SerializeField] private Light gemLight;
        [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0.8f, 1f, 1.2f);
        [SerializeField] private float pulseSpeed = 1.5f;
        
        [Header("Special Properties")]
        [SerializeField] private bool enableBonusEffect = true;
        [SerializeField] private float bonusRadius = 2f;
        [SerializeField] private bool canTriggerCombo = true;
        [SerializeField] private int comboMultiplier = 3;
        
        [Header("Rarity Effects")]
        [SerializeField] private bool enableRainbowTrail = false;
        [SerializeField] private TrailRenderer rainbowTrail;
        [SerializeField] private Gradient rainbowGradient;
        
        // Gem type enum with values
        public enum GemType
        {
            Ruby = 100,      // Red gem
            Sapphire = 150,  // Blue gem
            Emerald = 200,   // Green gem  
            Diamond = 500    // Ultra rare white gem
        }
        
        // Runtime state
        private Coroutine auraCoroutine;
        private Coroutine pulseCoroutine;
        private float initialLightIntensity;
        private Vector3 initialScale;
        
        // Combo tracking
        private static float lastGemCollectionTime = 0f;
        private static int gemComboCount = 0;
        private const float comboWindow = 3f;
        
        // Properties
        public override CollectibleType Type => CollectibleType.Gem;
        public GemType CurrentGemType => gemType;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            initialScale = transform.localScale;
            
            if (gemLight != null)
            {
                initialLightIntensity = gemLight.intensity;
            }
        }
        
        protected override void Start()
        {
            base.Start();
            
            // Set base value based on gem type
            baseValue = (int)gemType;
            
            // Initialize gem appearance
            InitializeGemAppearance();
            
            // Start special effects
            StartGemEffects();
        }
        
        #endregion
        
        #region Gem Initialization
        
        private void InitializeGemAppearance()
        {
            UpdateGemVisuals();
            
            // Set up light if available
            if (gemLight != null)
            {
                gemLight.color = GetGemLightColor();
                gemLight.intensity = initialLightIntensity * auraIntensity;
            }
            
            // Set up trail if enabled
            if (enableRainbowTrail && rainbowTrail != null)
            {
                rainbowTrail.colorGradient = rainbowGradient;
                rainbowTrail.enabled = gemType == GemType.Diamond;
            }
        }
        
        private void StartGemEffects()
        {
            if (enableAuraEffect)
            {
                auraCoroutine = StartCoroutine(AuraEffect());
            }
            
            pulseCoroutine = StartCoroutine(PulseEffect());
            
            // Start aura particles
            if (auraParticles != null)
            {
                var main = auraParticles.main;
                main.startColor = GetGemColor();
                auraParticles.Play();
            }
        }
        
        #endregion
        
        #region Gem Specific Logic
        
        protected override void OnItemCollected()
        {
            base.OnItemCollected();
            
            // Handle gem combo system
            if (canTriggerCombo)
            {
                HandleGemCombo();
            }
            
            // Trigger bonus area effect
            if (enableBonusEffect)
            {
                TriggerBonusAreaEffect();
            }
            
            // Play gem-specific effects
            PlayGemCollectionEffects();
            
            // Stop gem effects
            StopGemEffects();
        }
        
        private void HandleGemCombo()
        {
            float timeSinceLastGem = Time.time - lastGemCollectionTime;
            
            if (timeSinceLastGem <= comboWindow)
            {
                // Continue combo
                gemComboCount++;
                
                // Apply combo bonus
                if (scoreManager != null)
                {
                    int comboBonus = Value * comboMultiplier * gemComboCount;
                    scoreManager.AddScore(comboBonus);
                    
                    // Show combo notification
                    ShowComboNotification(gemComboCount, comboBonus);
                }
            }
            else
            {
                // Reset combo
                gemComboCount = 1;
            }
            
            lastGemCollectionTime = Time.time;
        }
        
        private void ShowComboNotification(int combo, int bonusPoints)
        {
            // TODO: Show combo notification when UI system is implemented
            // var uiManager = FindObjectOfType<UI.UIManager>();
            // if (uiManager != null)
            // {
            //     string comboText = $"GEM COMBO x{combo}! +{bonusPoints}";
            //     Color comboColor = combo >= 5 ? Color.magenta : Color.yellow;
            //     uiManager.ShowFloatingText(comboText, transform.position, comboColor);
            // }
            
            // Play escalating combo sound
            if (audioManager != null)
            {
                // TODO: Use appropriate sound for gem combo
                audioManager.PlayFishCollectSound();
            }
        }
        
        private void TriggerBonusAreaEffect()
        {
            // Find nearby collectibles and apply bonus
            Collider2D[] nearbyCollectibles = Physics2D.OverlapCircleAll(transform.position, bonusRadius);
            
            foreach (var collider in nearbyCollectibles)
            {
                var collectible = collider.GetComponent<BaseCollectible>();
                if (collectible != null && collectible != this && collectible.CanBeCollected)
                {
                    // Double the value of nearby collectibles
                    collectible.SetValue(collectible.Value * 2);
                    
                    // Visual indicator
                    StartCoroutine(ApplyBonusGlow(collectible.gameObject));
                }
            }
            
            // Spawn area effect particles
            SpawnAreaBonusEffect();
        }
        
        private IEnumerator ApplyBonusGlow(GameObject target)
        {
            var renderer = target.GetComponent<SpriteRenderer>();
            if (renderer == null) yield break;
            
            Color originalColor = renderer.color;
            Color glowColor = GetGemColor();
            glowColor.a = 0.7f;
            
            float duration = 1f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float intensity = Mathf.Sin(elapsed * Mathf.PI * 4f) * 0.5f + 0.5f;
                renderer.color = Color.Lerp(originalColor, glowColor, intensity * 0.3f);
                yield return null;
            }
            
            renderer.color = originalColor;
        }
        
        private void SpawnAreaBonusEffect()
        {
            // Create expanding ring effect
            var effect = new GameObject("GemBonusEffect");
            effect.transform.position = transform.position;
            
            var particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 2f;
            main.startSpeed = 3f;
            main.startColor = GetGemColor();
            main.maxParticles = 50;
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = bonusRadius;
            
            // Auto-destroy effect
            Destroy(effect, 3f);
        }
        
        private void PlayGemCollectionEffects()
        {
            // Play gem-specific sound
            if (audioManager != null)
            {
                audioManager.PlayFishCollectSound(); // Use fish collect sound for gems temporarily
            }
            
            // Enhanced particle effect
            EnhanceCollectionParticles();
            
            // Screen shake for rare gems
            if (gemType == GemType.Diamond)
            {
                TriggerScreenShake();
            }
        }
        
        private void EnhanceCollectionParticles()
        {
            if (collectEffect != null)
            {
                var main = collectEffect.main;
                main.startColor = GetGemColor();
                main.maxParticles = (int)gemType / 10; // More particles for rarer gems
                
                var emission = collectEffect.emission;
                emission.rateOverTime = (int)gemType / 5;
            }
        }
        
        private void TriggerScreenShake()
        {
            // TODO: Implement screen shake when camera controller is available
            // var cameraController = FindObjectOfType<CameraController>();
            // if (cameraController != null)
            // {
            //     cameraController.ShakeCamera(0.3f, 0.5f);
            // }
        }
        
        #endregion
        
        #region Visual Effects
        
        private IEnumerator AuraEffect()
        {
            while (!isCollected)
            {
                if (gemLight != null)
                {
                    float intensity = initialLightIntensity * auraIntensity;
                    intensity *= (0.8f + Mathf.Sin(Time.time * 2f) * 0.2f); // Subtle pulsing
                    gemLight.intensity = intensity;
                }
                
                yield return null;
            }
        }
        
        private IEnumerator PulseEffect()
        {
            while (!isCollected)
            {
                float time = Time.time * pulseSpeed;
                float pulseValue = pulseCurve.Evaluate(time % 1f);
                transform.localScale = initialScale * pulseValue;
                
                yield return null;
            }
        }
        
        private void StopGemEffects()
        {
            if (auraCoroutine != null)
            {
                StopCoroutine(auraCoroutine);
            }
            
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }
            
            if (auraParticles != null)
            {
                auraParticles.Stop();
            }
            
            if (gemLight != null)
            {
                gemLight.intensity = 0f;
            }
        }
        
        #endregion
        
        #region Color and Visual Helpers
        
        private Color GetGemColor()
        {
            switch (gemType)
            {
                case GemType.Ruby:
                    return new Color(1f, 0.2f, 0.2f, 1f); // Red
                case GemType.Sapphire:
                    return new Color(0.2f, 0.4f, 1f, 1f); // Blue
                case GemType.Emerald:
                    return new Color(0.2f, 1f, 0.4f, 1f); // Green
                case GemType.Diamond:
                    return new Color(1f, 1f, 1f, 1f); // White
                default:
                    return Color.white;
            }
        }
        
        private Color GetGemLightColor()
        {
            Color baseColor = GetGemColor();
            baseColor.a = 1f;
            return baseColor;
        }
        
        private void UpdateGemVisuals()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = GetGemColor();
            }
            
            // Adjust scale based on rarity
            float scaleMultiplier = 1f + ((int)gemType - 100) * 0.001f;
            initialScale = Vector3.one * scaleMultiplier;
            transform.localScale = initialScale;
        }
        
        private string GetGemSoundName()
        {
            switch (gemType)
            {
                case GemType.Ruby:
                    return "GemRuby";
                case GemType.Sapphire:
                    return "GemSapphire";
                case GemType.Emerald:
                    return "GemEmerald";
                case GemType.Diamond:
                    return "GemDiamond";
                default:
                    return "GemRuby";
            }
        }
        
        #endregion
        
        #region Value Calculation Override
        
        protected override int GetAdjustedValue()
        {
            int finalValue = base.GetAdjustedValue();
            
            // Apply gem rarity multiplier
            switch (gemType)
            {
                case GemType.Sapphire:
                    finalValue = Mathf.RoundToInt(finalValue * 1.3f);
                    break;
                case GemType.Emerald:
                    finalValue = Mathf.RoundToInt(finalValue * 1.6f);
                    break;
                case GemType.Diamond:
                    finalValue = Mathf.RoundToInt(finalValue * 2.5f);
                    break;
            }
            
            return finalValue;
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Set the gem type and update all visual effects
        /// </summary>
        public void SetGemType(GemType newType)
        {
            gemType = newType;
            baseValue = (int)gemType;
            
            // Update all visuals
            InitializeGemAppearance();
            
            // Restart effects with new settings
            StopGemEffects();
            StartGemEffects();
        }
        
        /// <summary>
        /// Get current gem combo count
        /// </summary>
        public static int GetGemCombo()
        {
            return gemComboCount;
        }
        
        /// <summary>
        /// Reset gem combo counter
        /// </summary>
        public static void ResetGemCombo()
        {
            gemComboCount = 0;
            lastGemCollectionTime = 0f;
        }
        
        /// <summary>
        /// Check if this gem can create combos with nearby gems
        /// </summary>
        public bool CanCreateCombo()
        {
            return canTriggerCombo && Time.time - lastGemCollectionTime <= comboWindow;
        }
        
        #endregion
        
        #region Reset Override
        
        public override void ResetCollectible()
        {
            base.ResetCollectible();
            
            // Reset scale
            transform.localScale = initialScale;
            
            // Restart gem effects
            InitializeGemAppearance();
            StartGemEffects();
        }
        
        #endregion
        
        #region Debug Override
        
        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + 
                   $"\nGem Type: {gemType}\n" +
                   $"Gem Combo: {gemComboCount}\n" +
                   $"Bonus Radius: {bonusRadius}\n" +
                   $"Light Intensity: {(gemLight != null ? gemLight.intensity : 0f):F2}";
        }
        
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            // Draw bonus radius
            if (enableBonusEffect)
            {
                Gizmos.color = GetGemColor();
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawSphere(transform.position, bonusRadius);
            }
            
            // Draw gem type indicator
            Gizmos.color = GetGemColor();
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.8f, Vector3.one * 0.3f);
        }
        
        #endregion
    }
}