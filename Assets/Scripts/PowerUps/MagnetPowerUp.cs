using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CatchMeowIfYouCan.PowerUps
{
    /// <summary>
    /// Magnet power-up that attracts collectibles from a distance towards the player
    /// Features magnetic field effects, collectible attraction mechanics, and bonus scoring
    /// </summary>
    public class MagnetPowerUp : BasePowerUp
    {
        [Header("Magnet Settings")]
        [SerializeField] private float magnetRange = 8f;
        [SerializeField] private float attractionForce = 15f;
        [SerializeField] private bool affectsAllCollectibles = true;
        [SerializeField] private LayerMask collectibleLayers = -1;
        [SerializeField] private float maxMagnetRange = 15f;
        
        [Header("Attraction Mechanics")]
        [SerializeField] private AnimationCurve attractionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float attractionSpeedMultiplier = 2f;
        [SerializeField] private bool useGradualAttraction = true;
        [SerializeField] private float attractionDelay = 0.1f;
        [SerializeField] private bool enableInstantCollection = false;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem magnetField;
        [SerializeField] private ParticleSystem attractionLines;
        [SerializeField] private LineRenderer magnetFieldRenderer;
        [SerializeField] private Color magnetColor = Color.blue;
        [SerializeField] private float fieldPulseSpeed = 2f;
        [SerializeField] private Material magnetFieldMaterial;
        
        [Header("Audio Effects")]
        [SerializeField] private AudioClip magnetActivationSound;
        [SerializeField] private AudioClip attractionSound;
        [SerializeField] private AudioClip magnetCollectSound;
        [SerializeField] private float magnetHumVolume = 0.3f;
        [SerializeField] private bool enableSpatialAudio = true;
        
        [Header("Bonus System")]
        [SerializeField] private bool enableMagnetBonus = true;
        [SerializeField] private float bonusMultiplier = 1.5f;
        [SerializeField] private int streakBonusThreshold = 5;
        [SerializeField] private float streakMultiplier = 2f;
        [SerializeField] private bool enableComboSystem = true;
        
        [Header("Performance Settings")]
        [SerializeField] private int maxTrackedCollectibles = 20;
        [SerializeField] private float updateFrequency = 0.1f;
        [SerializeField] private bool useDistanceCulling = true;
        [SerializeField] private float cullingDistance = 20f;
        
        // Runtime state
        private List<Collectibles.BaseCollectible> trackedCollectibles = new List<Collectibles.BaseCollectible>();
        private Dictionary<Collectibles.BaseCollectible, Coroutine> attractionCoroutines = new Dictionary<Collectibles.BaseCollectible, Coroutine>();
        private bool magnetEffectsActive = false;
        private float currentMagnetRange;
        private int collectibleStreak = 0;
        private float lastCollectionTime = 0f;
        private int totalMagnetizedCollectibles = 0;
        
        // Visual effects state
        private List<LineRenderer> attractionLineRenderers = new List<LineRenderer>();
        private float fieldPulseTimer = 0f;
        private float originalFieldIntensity = 1f;
        
        // Audio management
        private AudioSource magnetHumAudioSource;
        private bool humSoundPlaying = false;
        
        // Performance optimization
        private float lastUpdateTime = 0f;
        private Coroutine magnetFieldCoroutine;
        private Coroutine attractionUpdateCoroutine;
        
        public override PowerUpType Type => PowerUpType.Magnet;
        
        // Properties
        public float CurrentMagnetRange => currentMagnetRange;
        public int TrackedCollectiblesCount => trackedCollectibles.Count;
        public int CollectibleStreak => collectibleStreak;
        public int TotalMagnetizedCollectibles => totalMagnetizedCollectibles;
        public bool IsMagnetActive => magnetEffectsActive && IsActive;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            // Setup magnet hum audio source
            magnetHumAudioSource = gameObject.AddComponent<AudioSource>();
            magnetHumAudioSource.playOnAwake = false;
            magnetHumAudioSource.loop = true;
            magnetHumAudioSource.volume = magnetHumVolume;
            magnetHumAudioSource.spatialBlend = enableSpatialAudio ? 1f : 0f;
            
            // Set power-up color
            powerUpColor = magnetColor;
            currentMagnetRange = magnetRange;
        }
        
        protected override void Start()
        {
            base.Start();
            
            // Setup line renderer for magnetic field
            if (magnetFieldRenderer == null)
            {
                magnetFieldRenderer = gameObject.AddComponent<LineRenderer>();
                SetupMagnetFieldRenderer();
            }
        }
        
        protected override void UpdatePowerUp()
        {
            base.UpdatePowerUp();
            
            if (IsActive && magnetEffectsActive)
            {
                // Throttled updates for performance
                if (Time.time - lastUpdateTime >= updateFrequency)
                {
                    UpdateMagnetField();
                    lastUpdateTime = Time.time;
                }
                
                // Update visual effects
                UpdateMagnetVisuals();
                
                // Update audio effects
                UpdateMagnetAudio();
            }
        }
        
        #endregion
        
        #region Power-Up Implementation
        
        protected override void OnActivate()
        {
            // Start magnet effects
            StartMagnetEffects();
            
            // Reset streak and stats
            collectibleStreak = 0;
            totalMagnetizedCollectibles = 0;
            
            // Play activation sound
            if (magnetActivationSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(magnetActivationSound, audioVolume);
            }
            
            Debug.Log($"Magnet activated! Range: {currentMagnetRange}m");
        }
        
        protected override void OnDeactivate()
        {
            // Stop magnet effects
            StopMagnetEffects();
            
            // Award final streak bonus
            if (enableMagnetBonus && collectibleStreak >= streakBonusThreshold)
            {
                AwardStreakBonus();
            }
            
            // Clear tracked collectibles
            ClearTrackedCollectibles();
            
            Debug.Log($"Magnet deactivated. Total collectibles magnetized: {totalMagnetizedCollectibles}");
        }
        
        protected override void OnStackAdded()
        {
            // Each stack increases magnet range and attraction force
            currentMagnetRange = Mathf.Min(currentMagnetRange + 2f, maxMagnetRange);
            attractionForce += 5f;
            
            // Enhanced visual effects for stacked magnet
            if (magnetField != null)
            {
                var shape = magnetField.shape;
                shape.radius = currentMagnetRange;
                
                var emission = magnetField.emission;
                emission.rateOverTime = emission.rateOverTime.constant * 1.2f;
            }
            
            Debug.Log($"Magnet stack added! New range: {currentMagnetRange}m");
        }
        
        #endregion
        
        #region Magnet Field System
        
        private void StartMagnetEffects()
        {
            magnetEffectsActive = true;
            currentMagnetRange = magnetRange;
            
            // Start magnetic field particles
            if (magnetField != null)
            {
                magnetField.Play();
                UpdateMagnetFieldSize();
            }
            
            // Start magnetic field coroutine
            magnetFieldCoroutine = StartCoroutine(MagnetFieldCoroutine());
            
            // Start attraction update coroutine
            attractionUpdateCoroutine = StartCoroutine(AttractionUpdateCoroutine());
            
            // Start magnet hum sound
            StartMagnetHum();
        }
        
        private void StopMagnetEffects()
        {
            magnetEffectsActive = false;
            
            // Stop magnetic field particles
            if (magnetField != null)
            {
                magnetField.Stop();
            }
            
            // Stop attraction lines
            if (attractionLines != null)
            {
                attractionLines.Stop();
            }
            
            // Stop coroutines
            if (magnetFieldCoroutine != null)
            {
                StopCoroutine(magnetFieldCoroutine);
                magnetFieldCoroutine = null;
            }
            
            if (attractionUpdateCoroutine != null)
            {
                StopCoroutine(attractionUpdateCoroutine);
                attractionUpdateCoroutine = null;
            }
            
            // Stop all attraction coroutines
            StopAllAttractionCoroutines();
            
            // Stop magnet hum sound
            StopMagnetHum();
            
            // Clear attraction line renderers
            ClearAttractionLines();
        }
        
        private IEnumerator MagnetFieldCoroutine()
        {
            while (magnetEffectsActive)
            {
                // Scan for collectibles in range
                ScanForCollectibles();
                
                // Update magnetic field visualization
                UpdateMagnetFieldVisualization();
                
                yield return new WaitForSeconds(updateFrequency);
            }
        }
        
        private IEnumerator AttractionUpdateCoroutine()
        {
            while (magnetEffectsActive)
            {
                // Update attraction for tracked collectibles
                UpdateCollectibleAttraction();
                
                yield return new WaitForSeconds(updateFrequency * 0.5f); // More frequent for smooth attraction
            }
        }
        
        private void UpdateMagnetField()
        {
            if (!magnetEffectsActive) return;
            
            // Update magnet field size based on current range
            UpdateMagnetFieldSize();
            
            // Update field renderer
            UpdateMagnetFieldRenderer();
        }
        
        #endregion
        
        #region Collectible Scanning & Tracking
        
        private void ScanForCollectibles()
        {
            if (catController == null) return;
            
            Vector3 playerPosition = catController.transform.position;
            
            // Find all collectibles in range
            Collider2D[] colliders = Physics2D.OverlapCircleAll(playerPosition, currentMagnetRange, collectibleLayers);
            
            // Process found collectibles
            foreach (var collider in colliders)
            {
                var collectible = collider.GetComponent<Collectibles.BaseCollectible>();
                if (collectible != null && CanAttractCollectible(collectible))
                {
                    AddCollectibleToTracking(collectible);
                }
            }
            
            // Remove collectibles that are too far or collected
            CleanupTrackedCollectibles();
        }
        
        private bool CanAttractCollectible(Collectibles.BaseCollectible collectible)
        {
            if (collectible == null) return false;
            if (!affectsAllCollectibles) return false; // TODO: Add specific collectible type filtering
            
            // Check if already being attracted
            if (attractionCoroutines.ContainsKey(collectible)) return false;
            
            // Check distance culling
            if (useDistanceCulling && catController != null)
            {
                float distance = Vector3.Distance(collectible.transform.position, catController.transform.position);
                if (distance > cullingDistance) return false;
            }
            
            return true;
        }
        
        private void AddCollectibleToTracking(Collectibles.BaseCollectible collectible)
        {
            if (trackedCollectibles.Contains(collectible)) return;
            if (trackedCollectibles.Count >= maxTrackedCollectibles) return;
            
            trackedCollectibles.Add(collectible);
            StartCollectibleAttraction(collectible);
            
            totalMagnetizedCollectibles++;
        }
        
        private void CleanupTrackedCollectibles()
        {
            for (int i = trackedCollectibles.Count - 1; i >= 0; i--)
            {
                var collectible = trackedCollectibles[i];
                
                if (collectible == null || !collectible.gameObject.activeInHierarchy)
                {
                    RemoveCollectibleFromTracking(collectible);
                    continue;
                }
                
                // Check if still in range
                if (catController != null)
                {
                    float distance = Vector3.Distance(collectible.transform.position, catController.transform.position);
                    if (distance > currentMagnetRange * 1.2f) // Add some buffer
                    {
                        RemoveCollectibleFromTracking(collectible);
                    }
                }
            }
        }
        
        private void RemoveCollectibleFromTracking(Collectibles.BaseCollectible collectible)
        {
            if (collectible != null && attractionCoroutines.ContainsKey(collectible))
            {
                StopCoroutine(attractionCoroutines[collectible]);
                attractionCoroutines.Remove(collectible);
            }
            
            trackedCollectibles.Remove(collectible);
        }
        
        private void ClearTrackedCollectibles()
        {
            StopAllAttractionCoroutines();
            trackedCollectibles.Clear();
            attractionCoroutines.Clear();
        }
        
        #endregion
        
        #region Collectible Attraction System
        
        private void StartCollectibleAttraction(Collectibles.BaseCollectible collectible)
        {
            if (collectible == null || attractionCoroutines.ContainsKey(collectible)) return;
            
            var attractionCoroutine = StartCoroutine(AttractCollectibleCoroutine(collectible));
            attractionCoroutines[collectible] = attractionCoroutine;
        }
        
        private IEnumerator AttractCollectibleCoroutine(Collectibles.BaseCollectible collectible)
        {
            if (collectible == null || catController == null) yield break;
            
            // Initial delay
            if (attractionDelay > 0f)
            {
                yield return new WaitForSeconds(attractionDelay);
            }
            
            // Get collectible rigidbody or transform
            Rigidbody2D collectibleRb = collectible.GetComponent<Rigidbody2D>();
            Transform collectibleTransform = collectible.transform;
            Vector3 startPosition = collectibleTransform.position;
            
            float attractionStartTime = Time.time;
            
            while (collectible != null && collectible.gameObject.activeInHierarchy && magnetEffectsActive)
            {
                Vector3 playerPosition = catController.transform.position;
                Vector3 collectiblePosition = collectibleTransform.position;
                
                // Calculate attraction
                Vector3 direction = (playerPosition - collectiblePosition).normalized;
                float distance = Vector3.Distance(playerPosition, collectiblePosition);
                
                // Stop attracting if too close (let normal collection handle it)
                if (distance < 0.5f)
                {
                    if (enableInstantCollection)
                    {
                        CollectCollectible(collectible);
                    }
                    break;
                }
                
                // Calculate attraction force based on distance and curve
                float normalizedDistance = Mathf.Clamp01(distance / currentMagnetRange);
                float curveValue = attractionCurve.Evaluate(1f - normalizedDistance);
                float currentAttractionForce = attractionForce * curveValue * attractionSpeedMultiplier;
                
                // Apply attraction
                if (collectibleRb != null)
                {
                    // Physics-based attraction
                    Vector2 attractionForceVector = direction * currentAttractionForce;
                    collectibleRb.AddForce(attractionForceVector);
                }
                else
                {
                    // Transform-based attraction
                    Vector3 attractionMovement = direction * currentAttractionForce * Time.deltaTime;
                    collectibleTransform.position += attractionMovement;
                }
                
                // Play attraction effects
                if (Time.time - attractionStartTime > 0.1f) // Avoid spam
                {
                    PlayAttractionEffects(collectible, direction);
                    attractionStartTime = Time.time;
                }
                
                yield return null;
            }
            
            // Clean up
            if (attractionCoroutines.ContainsKey(collectible))
            {
                attractionCoroutines.Remove(collectible);
            }
        }
        
        private void UpdateCollectibleAttraction()
        {
            // Update attraction line visuals
            UpdateAttractionLines();
        }
        
        private void StopAllAttractionCoroutines()
        {
            foreach (var coroutine in attractionCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            attractionCoroutines.Clear();
        }
        
        #endregion
        
        #region Collection & Bonus System
        
        private void CollectCollectible(Collectibles.BaseCollectible collectible)
        {
            if (collectible == null) return;
            
            // Update streak
            collectibleStreak++;
            lastCollectionTime = Time.time;
            
            // Apply magnet collection bonus
            if (enableMagnetBonus)
            {
                ApplyCollectionBonus(collectible);
            }
            
            // Play collection effects
            PlayCollectionEffects();
            
            // Remove from tracking
            RemoveCollectibleFromTracking(collectible);
            
            // TODO: Trigger actual collection via collectible system
            // collectible.Collect();
            
            Debug.Log($"Magnet collected! Streak: {collectibleStreak}");
        }
        
        private void ApplyCollectionBonus(Collectibles.BaseCollectible collectible)
        {
            if (scoreManager == null) return;
            
            // Base magnet bonus
            int bonusPoints = Mathf.RoundToInt(10 * bonusMultiplier); // TODO: Get actual collectible value
            
            // Streak bonus
            if (collectibleStreak >= streakBonusThreshold && enableComboSystem)
            {
                bonusPoints = Mathf.RoundToInt(bonusPoints * streakMultiplier);
            }
            
            scoreManager.AddScore(bonusPoints);
            Debug.Log($"Magnet bonus: +{bonusPoints} points");
        }
        
        private void AwardStreakBonus()
        {
            if (scoreManager == null) return;
            
            int streakBonus = collectibleStreak * 5; // 5 points per collectible in streak
            scoreManager.AddScore(streakBonus);
            
            Debug.Log($"Magnet streak bonus: +{streakBonus} points for {collectibleStreak} collectibles");
        }
        
        #endregion
        
        #region Visual Effects System
        
        private void SetupMagnetFieldRenderer()
        {
            if (magnetFieldRenderer == null) return;
            
            magnetFieldRenderer.material = magnetFieldMaterial;
            magnetFieldRenderer.startColor = magnetColor;
            magnetFieldRenderer.endColor = magnetColor;
            magnetFieldRenderer.startWidth = 0.1f;
            magnetFieldRenderer.endWidth = 0.1f;
            magnetFieldRenderer.useWorldSpace = true;
            magnetFieldRenderer.positionCount = 32; // Circle resolution
        }
        
        private void UpdateMagnetFieldSize()
        {
            if (magnetField == null) return;
            
            var shape = magnetField.shape;
            shape.radius = currentMagnetRange;
            
            var emission = magnetField.emission;
            emission.rateOverTime = 10f + (currentMagnetRange * 2f);
        }
        
        private void UpdateMagnetFieldVisualization()
        {
            // Update magnetic field circle
            UpdateMagnetFieldRenderer();
            
            // Update field pulse effect
            UpdateFieldPulse();
        }
        
        private void UpdateMagnetFieldRenderer()
        {
            if (magnetFieldRenderer == null || catController == null) return;
            
            Vector3 center = catController.transform.position;
            
            // Create circle points
            for (int i = 0; i < magnetFieldRenderer.positionCount; i++)
            {
                float angle = i * 2f * Mathf.PI / magnetFieldRenderer.positionCount;
                Vector3 point = center + new Vector3(
                    Mathf.Cos(angle) * currentMagnetRange,
                    Mathf.Sin(angle) * currentMagnetRange,
                    0f
                );
                magnetFieldRenderer.SetPosition(i, point);
            }
        }
        
        private void UpdateFieldPulse()
        {
            fieldPulseTimer += Time.deltaTime * fieldPulseSpeed;
            float pulseValue = Mathf.Sin(fieldPulseTimer) * 0.5f + 0.5f;
            
            // Update field renderer alpha
            if (magnetFieldRenderer != null)
            {
                Color fieldColor = magnetColor;
                fieldColor.a = 0.3f + (pulseValue * 0.4f);
                magnetFieldRenderer.startColor = fieldColor;
                magnetFieldRenderer.endColor = fieldColor;
            }
            
            // Update particle intensity
            if (magnetField != null)
            {
                var main = magnetField.main;
                Color particleColor = magnetColor;
                particleColor.a = 0.5f + (pulseValue * 0.5f);
                main.startColor = particleColor;
            }
        }
        
        private void UpdateAttractionLines()
        {
            // Clear old lines
            ClearAttractionLines();
            
            if (catController == null) return;
            
            Vector3 playerPosition = catController.transform.position;
            
            // Create attraction lines for tracked collectibles
            for (int i = 0; i < trackedCollectibles.Count && i < 5; i++) // Limit visual lines for performance
            {
                var collectible = trackedCollectibles[i];
                if (collectible != null)
                {
                    CreateAttractionLine(playerPosition, collectible.transform.position);
                }
            }
        }
        
        private void CreateAttractionLine(Vector3 start, Vector3 end)
        {
            GameObject lineObj = new GameObject("AttractionLine");
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            
            line.material = magnetFieldMaterial;
            line.startColor = magnetColor;
            line.endColor = magnetColor;
            line.startWidth = 0.05f;
            line.endWidth = 0.02f;
            line.positionCount = 2;
            line.useWorldSpace = true;
            
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            
            attractionLineRenderers.Add(line);
            
            // Auto-destroy line after short time
            Destroy(lineObj, 0.2f);
        }
        
        private void ClearAttractionLines()
        {
            foreach (var line in attractionLineRenderers)
            {
                if (line != null)
                {
                    Destroy(line.gameObject);
                }
            }
            attractionLineRenderers.Clear();
        }
        
        private void UpdateMagnetVisuals()
        {
            // Visual updates are handled in coroutines for performance
        }
        
        #endregion
        
        #region Audio Effects System
        
        private void StartMagnetHum()
        {
            if (magnetHumAudioSource == null || humSoundPlaying) return;
            
            // TODO: Set magnet hum clip
            // magnetHumAudioSource.clip = magnetHumClip;
            magnetHumAudioSource.Play();
            humSoundPlaying = true;
        }
        
        private void StopMagnetHum()
        {
            if (magnetHumAudioSource == null || !humSoundPlaying) return;
            
            magnetHumAudioSource.Stop();
            humSoundPlaying = false;
        }
        
        private void UpdateMagnetAudio()
        {
            if (magnetHumAudioSource == null) return;
            
            // Modulate hum volume based on tracked collectibles
            float targetVolume = magnetHumVolume * (1f + (trackedCollectibles.Count * 0.1f));
            magnetHumAudioSource.volume = Mathf.Lerp(magnetHumAudioSource.volume, targetVolume, Time.deltaTime * 2f);
        }
        
        private void PlayAttractionEffects(Collectibles.BaseCollectible collectible, Vector3 direction)
        {
            // Play attraction particle burst
            if (attractionLines != null)
            {
                attractionLines.transform.position = collectible.transform.position;
                if (!attractionLines.isPlaying)
                {
                    attractionLines.Play();
                }
            }
            
            // Play attraction sound occasionally
            if (attractionSound != null && audioManager != null && Random.Range(0f, 1f) < 0.1f)
            {
                audioManager.PlayCustomSfx(attractionSound, audioVolume * 0.5f);
            }
        }
        
        private void PlayCollectionEffects()
        {
            // Play collection sound
            if (magnetCollectSound != null && audioManager != null)
            {
                audioManager.PlayCustomSfx(magnetCollectSound, audioVolume);
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get magnet-specific information
        /// </summary>
        public MagnetInfo GetMagnetInfo()
        {
            return new MagnetInfo
            {
                currentMagnetRange = CurrentMagnetRange,
                maxMagnetRange = maxMagnetRange,
                trackedCollectiblesCount = TrackedCollectiblesCount,
                collectibleStreak = CollectibleStreak,
                totalMagnetizedCollectibles = TotalMagnetizedCollectibles,
                attractionForce = attractionForce,
                isMagnetActive = IsMagnetActive
            };
        }
        
        /// <summary>
        /// Manually trigger attraction for specific collectible
        /// </summary>
        public bool AttractCollectible(Collectibles.BaseCollectible collectible)
        {
            if (!IsActive || collectible == null) return false;
            
            AddCollectibleToTracking(collectible);
            return true;
        }
        
        /// <summary>
        /// Boost magnet power temporarily
        /// </summary>
        public void BoostMagnetPower(float rangeMultiplier, float forceMultiplier, float duration = 3f)
        {
            if (!IsActive) return;
            
            StartCoroutine(TemporaryMagnetBoost(rangeMultiplier, forceMultiplier, duration));
        }
        
        private IEnumerator TemporaryMagnetBoost(float rangeMultiplier, float forceMultiplier, float duration)
        {
            float originalRange = currentMagnetRange;
            float originalForce = attractionForce;
            
            currentMagnetRange *= rangeMultiplier;
            attractionForce *= forceMultiplier;
            
            // Clamp to max values
            currentMagnetRange = Mathf.Min(currentMagnetRange, maxMagnetRange);
            
            yield return new WaitForSeconds(duration);
            
            currentMagnetRange = originalRange;
            attractionForce = originalForce;
            
            Debug.Log("Temporary magnet boost ended.");
        }
        
        #endregion
        
        #region Debug Override
        
        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $"\n" +
                   $"Magnet Range: {CurrentMagnetRange:F1}m/{magnetRange:F1}m\n" +
                   $"Tracked Collectibles: {TrackedCollectiblesCount}/{maxTrackedCollectibles}\n" +
                   $"Attraction Force: {attractionForce:F1}\n" +
                   $"Collectible Streak: {CollectibleStreak}\n" +
                   $"Total Magnetized: {TotalMagnetizedCollectibles}\n" +
                   $"Magnet Active: {IsMagnetActive}";
        }
        
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            // Draw magnet range
            if (IsActive)
            {
                Gizmos.color = magnetColor;
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawWireSphere(transform.position, currentMagnetRange);
                
                // Draw attraction lines to tracked collectibles
                Gizmos.color = magnetColor;
                foreach (var collectible in trackedCollectibles)
                {
                    if (collectible != null)
                    {
                        Gizmos.DrawLine(transform.position, collectible.transform.position);
                    }
                }
            }
        }
        
        #endregion
        
        #region Data Structures
        
        [System.Serializable]
        public struct MagnetInfo
        {
            public float currentMagnetRange;
            public float maxMagnetRange;
            public int trackedCollectiblesCount;
            public int collectibleStreak;
            public int totalMagnetizedCollectibles;
            public float attractionForce;
            public bool isMagnetActive;
        }
        
        #endregion
    }
}