using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.Obstacles
{
    /// <summary>
    /// Trash bin obstacle that must be avoided by dodging left/right
    /// Features trash scattering, smell effects, and special interactions
    /// </summary>
    public class TrashBinObstacle : BaseObstacle
    {
        [Header("Trash Bin Settings")]
        [SerializeField] private TrashBinType binType = TrashBinType.Regular;
        [SerializeField] private bool isOverflowing = false;
        [SerializeField] private float binHeight = 2f;
        [SerializeField] private bool blocksAllLanes = false;
        
        [Header("Trash Contents")]
        [SerializeField] private GameObject[] trashPrefabs;
        [SerializeField] private int minTrashCount = 3;
        [SerializeField] private int maxTrashCount = 8;
        [SerializeField] private float trashScatterRadius = 2f;
        [SerializeField] private bool spawnTrashOnDestroy = true;
        
        [Header("Smell System")]
        [SerializeField] private bool hasSmellEffect = true;
        [SerializeField] private float smellRadius = 4f;
        [SerializeField] private float smellSlowdownFactor = 0.7f;
        [SerializeField] private ParticleSystem smellEffect;
        [SerializeField] private Color smellColor = Color.green;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem fliesEffect;
        [SerializeField] private AudioClip smellSound;
        [SerializeField] private AudioClip crashSound;
        [SerializeField] private AudioClip fliesSound;
        [SerializeField] private Material damagedMaterial;
        
        [Header("Interaction Settings")]
        [SerializeField] private bool canBeKnockedOver = true;
        [SerializeField] private float knockOverForce = 5f;
        [SerializeField] private int baseDamage = 1;
        [SerializeField] private int overflowDamageBonus = 1;
        [SerializeField] private float slowEffectDuration = 2f;
        
        [Header("Special Properties")]
        [SerializeField] private bool containsCollectibles = false;
        [SerializeField] private float collectibleSpawnChance = 0.3f;
        [SerializeField] private bool attractsFlies = true;
        [SerializeField] private bool createsSmellZone = true;
        
        // Runtime state
        private bool isKnockedOver = false;
        private bool playerInSmellZone = false;
        private bool hasSpawnedTrash = false;
        private Coroutine smellEffectCoroutine;
        private Coroutine fliesCoroutine;
        private AudioSource smellAudioSource;
        private AudioSource fliesAudioSource;
        
        // Spawned objects
        private GameObject[] spawnedTrash;
        private bool[] trashSpawned;
        
        public enum TrashBinType
        {
            Regular,        // Standard trash bin
            Overflowing,    // Overflowing with extra trash
            Recycling,      // Recycling bin - gives bonus when avoided
            Hazardous,      // Hazardous waste - extra damage and effects
            Broken          // Broken bin - already scattered trash
        }
        
        public override ObstacleType Type => ObstacleType.Barrier;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            // Configure based on bin type
            ConfigureBinType();
            
            // Cannot be jumped over or slid under - must be avoided
            enableWarning = true;
            warningDistance = 6f;
            warningDuration = 1f;
            
            // Static obstacle
            enableMovement = false;
            canBeDestroyed = canBeKnockedOver;
            
            // Setup audio sources
            SetupAudioSources();
        }
        
        protected override void Start()
        {
            base.Start();
            
            // Start trash bin effects
            StartTrashBinEffects();
            
            // Setup initial trash if broken bin
            if (binType == TrashBinType.Broken)
            {
                SpawnInitialTrash();
            }
        }
        
        protected override void UpdateObstacle()
        {
            base.UpdateObstacle();
            
            // Check smell zone effect
            if (hasSmellEffect && createsSmellZone)
            {
                CheckSmellZoneEffect();
            }
        }
        
        protected override void OnTriggerEnter2D(Collider2D other)
        {
            base.OnTriggerEnter2D(other);
            
            if (other.CompareTag("Player") && hasSmellEffect)
            {
                EnterSmellZone(other);
            }
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") && hasSmellEffect)
            {
                ExitSmellZone(other);
            }
        }
        
        #endregion
        
        #region Bin Configuration
        
        private void ConfigureBinType()
        {
            switch (binType)
            {
                case TrashBinType.Regular:
                    damageAmount = baseDamage;
                    isOverflowing = false;
                    hasSmellEffect = true;
                    attractsFlies = true;
                    break;
                    
                case TrashBinType.Overflowing:
                    damageAmount = baseDamage + overflowDamageBonus;
                    isOverflowing = true;
                    hasSmellEffect = true;
                    attractsFlies = true;
                    smellRadius *= 1.5f; // Larger smell radius
                    maxTrashCount = Mathf.RoundToInt(maxTrashCount * 1.5f);
                    break;
                    
                case TrashBinType.Recycling:
                    damageAmount = baseDamage;
                    isOverflowing = false;
                    hasSmellEffect = false; // Clean recycling
                    attractsFlies = false;
                    containsCollectibles = true;
                    collectibleSpawnChance = 0.5f;
                    break;
                    
                case TrashBinType.Hazardous:
                    damageAmount = baseDamage + 1;
                    isOverflowing = false;
                    hasSmellEffect = true;
                    attractsFlies = false; // Too toxic for flies
                    smellSlowdownFactor = 0.5f; // More severe slowdown
                    slowEffectDuration = 3f;
                    smellColor = Color.yellow; // Toxic color
                    break;
                    
                case TrashBinType.Broken:
                    damageAmount = baseDamage;
                    isKnockedOver = true;
                    hasSmellEffect = true;
                    attractsFlies = true;
                    spawnTrashOnDestroy = false; // Already has trash
                    canBeKnockedOver = false; // Already knocked over
                    break;
            }
        }
        
        #endregion
        
        #region Trash Bin Effects
        
        private void StartTrashBinEffects()
        {
            // Start smell effect
            if (hasSmellEffect && createsSmellZone)
            {
                smellEffectCoroutine = StartCoroutine(SmellEffectCoroutine());
            }
            
            // Start flies effect
            if (attractsFlies)
            {
                fliesCoroutine = StartCoroutine(FliesEffectCoroutine());
            }
            
            // Setup visual state
            UpdateVisualState();
        }
        
        private IEnumerator SmellEffectCoroutine()
        {
            while (!isDestroyed && isActive)
            {
                // Play smell particle effect
                if (smellEffect != null)
                {
                    if (!smellEffect.isPlaying)
                    {
                        var main = smellEffect.main;
                        main.startColor = smellColor;
                        smellEffect.Play();
                    }
                }
                
                // Play smell sound occasionally
                if (smellAudioSource != null && smellSound != null && !smellAudioSource.isPlaying)
                {
                    if (Random.Range(0f, 1f) < 0.1f) // 10% chance per frame
                    {
                        smellAudioSource.PlayOneShot(smellSound, soundVolume * 0.5f);
                    }
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        private IEnumerator FliesEffectCoroutine()
        {
            while (!isDestroyed && isActive)
            {
                // Play flies particle effect
                if (fliesEffect != null)
                {
                    if (!fliesEffect.isPlaying)
                    {
                        fliesEffect.Play();
                    }
                }
                
                // Play flies sound
                if (fliesAudioSource != null && fliesSound != null && !fliesAudioSource.isPlaying)
                {
                    fliesAudioSource.clip = fliesSound;
                    fliesAudioSource.volume = soundVolume * 0.3f;
                    fliesAudioSource.loop = true;
                    fliesAudioSource.Play();
                }
                
                yield return new WaitForSeconds(1f);
            }
        }
        
        private void UpdateVisualState()
        {
            if (spriteRenderer == null) return;
            
            // Apply visual changes based on state
            if (isKnockedOver)
            {
                // Rotate to show knocked over state
                transform.rotation = Quaternion.Euler(0, 0, 90);
                
                if (damagedMaterial != null)
                {
                    spriteRenderer.material = damagedMaterial;
                }
            }
            
            // Adjust color based on bin type
            switch (binType)
            {
                case TrashBinType.Recycling:
                    spriteRenderer.color = Color.blue;
                    break;
                case TrashBinType.Hazardous:
                    spriteRenderer.color = Color.yellow;
                    break;
                case TrashBinType.Broken:
                    spriteRenderer.color = Color.gray;
                    break;
            }
        }
        
        #endregion
        
        #region Smell Zone System
        
        private void CheckSmellZoneEffect()
        {
            if (playerTransform == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            bool inSmellZone = distanceToPlayer <= smellRadius;
            
            if (inSmellZone && !playerInSmellZone)
            {
                EnterSmellZone(playerTransform.GetComponent<Collider2D>());
            }
            else if (!inSmellZone && playerInSmellZone)
            {
                ExitSmellZone(playerTransform.GetComponent<Collider2D>());
            }
        }
        
        private void EnterSmellZone(Collider2D playerCollider)
        {
            if (playerInSmellZone) return;
            
            playerInSmellZone = true;
            
            var catController = playerCollider.GetComponent<Player.CatController>();
            if (catController != null)
            {
                // Apply slowdown effect
                // TODO: Implement slowdown effect
                // catController.ApplySpeedMultiplier(smellSlowdownFactor, slowEffectDuration);
                Debug.Log($"Player entered smell zone - speed reduced to {smellSlowdownFactor * 100}%");
                
                // Visual effect on player
                // TODO: Add smell visual effect to player
                // catController.ShowSmellEffect(smellColor);
            }
            
            // Play enter smell sound
            if (audioManager != null && smellSound != null)
            {
                audioManager.PlayCustomSfx(smellSound, soundVolume * 0.7f);
            }
        }
        
        private void ExitSmellZone(Collider2D playerCollider)
        {
            if (!playerInSmellZone) return;
            
            playerInSmellZone = false;
            
            var catController = playerCollider.GetComponent<Player.CatController>();
            if (catController != null)
            {
                // Remove slowdown effect
                // TODO: Implement speed restoration
                // catController.RestoreNormalSpeed();
                Debug.Log("Player exited smell zone - speed restored");
            }
        }
        
        #endregion
        
        #region Trash Spawning System
        
        private void SpawnInitialTrash()
        {
            if (binType == TrashBinType.Broken && !hasSpawnedTrash)
            {
                SpawnTrashItems();
            }
        }
        
        private void SpawnTrashItems()
        {
            if (hasSpawnedTrash || trashPrefabs == null || trashPrefabs.Length == 0) return;
            
            hasSpawnedTrash = true;
            int trashCount = Random.Range(minTrashCount, maxTrashCount + 1);
            
            spawnedTrash = new GameObject[trashCount];
            trashSpawned = new bool[trashCount];
            
            for (int i = 0; i < trashCount; i++)
            {
                SpawnSingleTrashItem(i);
            }
        }
        
        private void SpawnSingleTrashItem(int index)
        {
            if (trashPrefabs == null || trashPrefabs.Length == 0) return;
            
            // Select random trash prefab
            GameObject trashPrefab = trashPrefabs[Random.Range(0, trashPrefabs.Length)];
            if (trashPrefab == null) return;
            
            // Random position around bin
            Vector2 randomOffset = Random.insideUnitCircle * trashScatterRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);
            
            // Random rotation
            Quaternion randomRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            
            // Spawn trash item
            GameObject trashItem = Instantiate(trashPrefab, spawnPosition, randomRotation);
            
            // Add physics for natural scattering
            Rigidbody2D trashRb = trashItem.GetComponent<Rigidbody2D>();
            if (trashRb == null)
            {
                trashRb = trashItem.AddComponent<Rigidbody2D>();
            }
            
            // Apply random force
            Vector2 scatterForce = Random.insideUnitCircle.normalized * Random.Range(1f, 3f);
            trashRb.AddForce(scatterForce, ForceMode2D.Impulse);
            
            // Add slight upward force
            trashRb.AddForce(Vector2.up * Random.Range(0.5f, 1.5f), ForceMode2D.Impulse);
            
            // Store reference
            if (index < spawnedTrash.Length)
            {
                spawnedTrash[index] = trashItem;
                trashSpawned[index] = true;
            }
            
            // Auto-destroy trash after time
            Destroy(trashItem, 10f);
        }
        
        private void SpawnCollectibles()
        {
            if (!containsCollectibles || Random.Range(0f, 1f) > collectibleSpawnChance) return;
            
            // TODO: Spawn collectible items from the bin
            // var collectibleManager = FindObjectOfType<Collectibles.CollectibleManager>();
            // if (collectibleManager != null)
            // {
            //     collectibleManager.SpawnCollectible(transform.position);
            // }
            Debug.Log($"Spawned collectible from {binType} bin!");
        }
        
        #endregion
        
        #region Collision System Override
        
        protected override CollisionResult GetDefaultCollisionResult()
        {
            // Trash bins always damage the player
            return CollisionResult.Damage;
        }
        
        protected override void OnPostCollision(CollisionResult result, Collider2D playerCollider)
        {
            if (result == CollisionResult.Damage)
            {
                // Knock over the bin if possible
                if (canBeKnockedOver && !isKnockedOver)
                {
                    KnockOverBin();
                }
                
                // Apply special effects based on bin type
                HandleSpecialCollisionEffects(playerCollider);
                
                // Spawn trash on collision
                if (spawnTrashOnDestroy)
                {
                    SpawnTrashItems();
                }
                
                // Play crash sound
                if (audioManager != null && crashSound != null)
                {
                    audioManager.PlayCustomSfx(crashSound, soundVolume);
                }
            }
        }
        
        private void KnockOverBin()
        {
            if (isKnockedOver) return;
            
            isKnockedOver = true;
            
            // Animate bin falling over
            StartCoroutine(KnockOverAnimation());
            
            // Spawn trash items
            if (spawnTrashOnDestroy)
            {
                SpawnTrashItems();
            }
            
            // Spawn collectibles if recycling bin
            if (binType == TrashBinType.Recycling)
            {
                SpawnCollectibles();
            }
        }
        
        private IEnumerator KnockOverAnimation()
        {
            Vector3 originalRotation = transform.eulerAngles;
            Vector3 targetRotation = new Vector3(0, 0, 90); // Fall to the side
            Vector3 originalPosition = transform.position;
            Vector3 targetPosition = originalPosition + Vector3.right * 0.5f; // Slide a bit
            
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                // Smooth rotation and position change
                transform.eulerAngles = Vector3.Lerp(originalRotation, targetRotation, progress);
                transform.position = Vector3.Lerp(originalPosition, targetPosition, progress);
                
                yield return null;
            }
            
            transform.eulerAngles = targetRotation;
            transform.position = targetPosition;
        }
        
        private void HandleSpecialCollisionEffects(Collider2D playerCollider)
        {
            var catController = playerCollider.GetComponent<Player.CatController>();
            if (catController == null) return;
            
            switch (binType)
            {
                case TrashBinType.Hazardous:
                    // Apply poison effect
                    // TODO: Implement poison effect
                    // catController.ApplyPoisonEffect(5f);
                    Debug.Log("Player poisoned by hazardous waste!");
                    break;
                    
                case TrashBinType.Overflowing:
                    // Slower recovery due to mess
                    // TODO: Implement slower recovery
                    // catController.SetRecoverySpeed(0.5f, 3f);
                    Debug.Log("Player slowed by overflowing trash!");
                    break;
                    
                case TrashBinType.Broken:
                    // Extra damage from sharp edges
                    // TODO: Implement damage system in CatController  
                    // catController.TakeDamage(1);
                    Debug.Log("Player cut by broken bin!");
                    break;
            }
        }
        
        #endregion
        
        #region Audio Setup
        
        private void SetupAudioSources()
        {
            // Setup smell audio source
            smellAudioSource = gameObject.AddComponent<AudioSource>();
            smellAudioSource.playOnAwake = false;
            smellAudioSource.loop = false;
            smellAudioSource.volume = soundVolume * 0.5f;
            smellAudioSource.pitch = Random.Range(0.8f, 1.2f);
            
            // Setup flies audio source
            fliesAudioSource = gameObject.AddComponent<AudioSource>();
            fliesAudioSource.playOnAwake = false;
            fliesAudioSource.loop = true;
            fliesAudioSource.volume = soundVolume * 0.3f;
            fliesAudioSource.pitch = Random.Range(0.9f, 1.1f);
        }
        
        #endregion
        
        #region Cleanup
        
        public override void DestroyObstacle()
        {
            // Stop smell zone effect
            if (playerInSmellZone && playerTransform != null)
            {
                ExitSmellZone(playerTransform.GetComponent<Collider2D>());
            }
            
            // Stop audio sources
            if (smellAudioSource != null)
            {
                smellAudioSource.Stop();
            }
            
            if (fliesAudioSource != null)
            {
                fliesAudioSource.Stop();
            }
            
            // Cleanup spawned trash
            CleanupSpawnedTrash();
            
            base.DestroyObstacle();
        }
        
        private void CleanupSpawnedTrash()
        {
            if (spawnedTrash != null)
            {
                for (int i = 0; i < spawnedTrash.Length; i++)
                {
                    if (spawnedTrash[i] != null)
                    {
                        Destroy(spawnedTrash[i]);
                    }
                }
            }
        }
        
        public override void ResetObstacle()
        {
            base.ResetObstacle();
            
            // Reset trash bin specific state
            isKnockedOver = (binType == TrashBinType.Broken);
            playerInSmellZone = false;
            hasSpawnedTrash = false;
            
            // Cleanup old trash
            CleanupSpawnedTrash();
            
            // Reset visual state
            UpdateVisualState();
            
            // Restart effects
            StartTrashBinEffects();
            
            // Spawn initial trash if broken
            if (binType == TrashBinType.Broken)
            {
                SpawnInitialTrash();
            }
        }
        
        #endregion
        
        #region Utility Methods Override
        
        protected override bool CanJumpOver()
        {
            return false; // Trash bins are too tall to jump over
        }
        
        protected override bool CanSlideUnder()
        {
            return false; // Trash bins block sliding
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Set the trash bin type and reconfigure
        /// </summary>
        public void SetBinType(TrashBinType newBinType)
        {
            binType = newBinType;
            ConfigureBinType();
            UpdateVisualState();
        }
        
        /// <summary>
        /// Get trash bin type
        /// </summary>
        public TrashBinType GetBinType()
        {
            return binType;
        }
        
        /// <summary>
        /// Check if player is in smell zone
        /// </summary>
        public bool IsPlayerInSmellZone()
        {
            return playerInSmellZone;
        }
        
        /// <summary>
        /// Force knock over the bin
        /// </summary>
        public void ForceKnockOver()
        {
            if (canBeKnockedOver)
            {
                KnockOverBin();
            }
        }
        
        #endregion
        
        #region Debug Override
        
        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $"\n" +
                   $"Bin Type: {binType}\n" +
                   $"Is Knocked Over: {isKnockedOver}\n" +
                   $"Player In Smell Zone: {playerInSmellZone}\n" +
                   $"Has Spawned Trash: {hasSpawnedTrash}\n" +
                   $"Smell Radius: {smellRadius}\n" +
                   $"Contains Collectibles: {containsCollectibles}";
        }
        
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            // Draw smell zone
            if (hasSmellEffect && createsSmellZone)
            {
                Gizmos.color = smellColor;
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
                Gizmos.DrawSphere(transform.position, smellRadius);
            }
            
            // Draw trash scatter area
            if (spawnTrashOnDestroy)
            {
                Gizmos.color = new Color(0.6f, 0.3f, 0.1f); // Brown color
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawWireSphere(transform.position, trashScatterRadius);
            }
            
            // Draw bin height
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * binHeight);
        }
        
        #endregion
    }
}