using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.Obstacles
{
    /// <summary>
    /// Car obstacle that can be jumped over with rocket shoes power-up
    /// Features multiple car types, warning system, and special destruction effects
    /// </summary>
    public class CarObstacle : BaseObstacle
    {
        [Header("Car Settings")]
        [SerializeField] private CarType carType = CarType.Regular;
        [SerializeField] private float carLength = 3f;
        [SerializeField] private bool hasHorn = true;
        [SerializeField] private AudioClip hornSound;
        [SerializeField] private float hornInterval = 5f;
        
        [Header("Movement Settings")]
        [SerializeField] private bool moveWithTraffic = true;
        [SerializeField] private float trafficSpeed = 3f;
        [SerializeField] private AnimationCurve speedVariationCurve = AnimationCurve.Linear(0f, 0.8f, 1f, 1.2f);
        [SerializeField] private float speedVariationFrequency = 2f;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem engineSmoke;
        [SerializeField] private ParticleSystem exhaustFumes;
        [SerializeField] private Light[] headlights;
        [SerializeField] private Light[] taillights;
        [SerializeField] private GameObject[] wheels;
        
        [Header("Damage Settings")]
        [SerializeField] private int baseDamage = 2;
        [SerializeField] private int bonusDamageForBigCar = 1;
        [SerializeField] private float stunDuration = 1.5f;
        
        [Header("Special Interactions")]
        [SerializeField] private bool canBeJumpedOver = true;
        [SerializeField] private float jumpOverHeight = 2f;
        [SerializeField] private bool givesPointsWhenJumped = true;
        [SerializeField] private int jumpOverPoints = 100;
        
        // Runtime state
        private float baseSpeed;
        private float currentSpeedMultiplier = 1f;
        private Coroutine hornCoroutine;
        private Coroutine wheelRotationCoroutine;
        private Coroutine speedVariationCoroutine;
        private bool playerHasJumped = false;
        private bool hasGivenJumpPoints = false;
        
        // Car type configuration
        public enum CarType
        {
            Regular,      // Standard car - medium damage
            SportsCar,    // Fast car - higher damage
            Truck,        // Big truck - highest damage
            Taxi,         // Special taxi - medium damage but gives coins when avoided
            PoliceCar     // Police car - attracts catcher attention
        }
        
        public override ObstacleType Type => ObstacleType.Car;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            // Configure based on car type
            ConfigureCarType();
            
            // Set default movement
            enableMovement = moveWithTraffic;
            moveDirection = Vector3.left;
            moveSpeed = trafficSpeed;
            
            // Set warning system
            enableWarning = true;
            warningDistance = 8f;
            warningDuration = 1.5f;
            
            // Can be jumped over
            canBeDestroyed = false;
            
            baseSpeed = moveSpeed;
        }
        
        protected override void Start()
        {
            base.Start();
            
            // Start car-specific effects
            StartCarEffects();
        }
        
        protected override void UpdateObstacle()
        {
            base.UpdateObstacle();
            
            // Check if player is jumping over
            CheckPlayerJumpOver();
        }
        
        #endregion
        
        #region Car Configuration
        
        private void ConfigureCarType()
        {
            switch (carType)
            {
                case CarType.Regular:
                    damageAmount = baseDamage;
                    trafficSpeed = 3f;
                    hornInterval = 8f;
                    break;
                    
                case CarType.SportsCar:
                    damageAmount = baseDamage + 1;
                    trafficSpeed = 5f;
                    hornInterval = 3f;
                    // Sports cars are faster and more aggressive
                    warningDistance = 10f;
                    break;
                    
                case CarType.Truck:
                    damageAmount = baseDamage + bonusDamageForBigCar;
                    trafficSpeed = 2f;
                    hornInterval = 10f;
                    carLength = 5f;
                    // Trucks are slower but deal more damage
                    stunDuration = 2f;
                    break;
                    
                case CarType.Taxi:
                    damageAmount = baseDamage;
                    trafficSpeed = 4f;
                    hornInterval = 5f;
                    // Taxis give bonus points when successfully avoided
                    givesPointsWhenJumped = true;
                    jumpOverPoints = 150;
                    break;
                    
                case CarType.PoliceCar:
                    damageAmount = baseDamage + 1;
                    trafficSpeed = 4f;
                    hornInterval = 4f;
                    // Police cars alert the catcher
                    enableWarning = true;
                    warningDistance = 12f;
                    break;
            }
        }
        
        #endregion
        
        #region Car Effects
        
        private void StartCarEffects()
        {
            // Start horn coroutine
            if (hasHorn && hornSound != null)
            {
                hornCoroutine = StartCoroutine(HornCoroutine());
            }
            
            // Start wheel rotation
            if (wheels != null && wheels.Length > 0)
            {
                wheelRotationCoroutine = StartCoroutine(WheelRotation());
            }
            
            // Start speed variation
            speedVariationCoroutine = StartCoroutine(SpeedVariation());
            
            // Start particle effects
            StartEngineEffects();
            
            // Configure lights
            SetupLights();
        }
        
        private IEnumerator HornCoroutine()
        {
            while (!isDestroyed && isActive)
            {
                yield return new WaitForSeconds(hornInterval + Random.Range(-1f, 1f));
                
                if (!isDestroyed && isActive)
                {
                    PlayHorn();
                }
            }
        }
        
        private void PlayHorn()
        {
            if (audioManager != null && hornSound != null)
            {
                audioManager.PlayCustomSfx(hornSound, soundVolume * 0.8f);
            }
            
            // Visual horn effect
            if (headlights != null)
            {
                StartCoroutine(FlashLights(headlights, 0.2f));
            }
        }
        
        private IEnumerator WheelRotation()
        {
            while (!isDestroyed && isActive)
            {
                if (wheels != null)
                {
                    float rotationSpeed = currentSpeedMultiplier * trafficSpeed * 50f;
                    
                    foreach (var wheel in wheels)
                    {
                        if (wheel != null)
                        {
                            wheel.transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
                        }
                    }
                }
                
                yield return null;
            }
        }
        
        private IEnumerator SpeedVariation()
        {
            while (!isDestroyed && isActive)
            {
                float time = Time.time * speedVariationFrequency;
                currentSpeedMultiplier = speedVariationCurve.Evaluate(time % 1f);
                
                // Update move speed
                moveSpeed = baseSpeed * currentSpeedMultiplier;
                
                yield return null;
            }
        }
        
        private void StartEngineEffects()
        {
            // Start engine smoke
            if (engineSmoke != null)
            {
                engineSmoke.Play();
            }
            
            // Start exhaust fumes
            if (exhaustFumes != null)
            {
                exhaustFumes.Play();
            }
        }
        
        private void SetupLights()
        {
            // Enable headlights
            if (headlights != null)
            {
                foreach (var light in headlights)
                {
                    if (light != null)
                    {
                        light.enabled = true;
                        light.intensity = 1f;
                    }
                }
            }
            
            // Enable taillights
            if (taillights != null)
            {
                foreach (var light in taillights)
                {
                    if (light != null)
                    {
                        light.enabled = true;
                        light.intensity = 0.5f;
                        light.color = Color.red;
                    }
                }
            }
        }
        
        private IEnumerator FlashLights(Light[] lights, float duration)
        {
            if (lights == null) yield break;
            
            // Store original intensities
            float[] originalIntensities = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                {
                    originalIntensities[i] = lights[i].intensity;
                }
            }
            
            // Flash effect
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float intensity = Mathf.Sin(elapsed * 20f) > 0 ? 2f : 0.5f;
                
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null)
                    {
                        lights[i].intensity = intensity;
                    }
                }
                
                yield return null;
            }
            
            // Restore original intensities
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                {
                    lights[i].intensity = originalIntensities[i];
                }
            }
        }
        
        #endregion
        
        #region Jump Over System
        
        private void CheckPlayerJumpOver()
        {
            if (!canBeJumpedOver || playerHasJumped || hasGivenJumpPoints) return;
            if (playerTransform == null) return;
            
            // Check if player is above car and moving over it
            Vector3 playerPos = playerTransform.position;
            Vector3 carPos = transform.position;
            
            // Player must be above jump height and within car length
            bool isAboveJumpHeight = playerPos.y > carPos.y + jumpOverHeight;
            bool isWithinCarBounds = Mathf.Abs(playerPos.x - carPos.x) < carLength;
            
            if (isAboveJumpHeight && isWithinCarBounds)
            {
                playerHasJumped = true;
                
                // Check if player has rocket shoes or similar jump boost
                var catController = playerTransform.GetComponent<Player.CatController>();
                if (catController != null)
                {
                    // TODO: Check for rocket shoes power-up
                    // bool hasRocketShoes = catController.HasActivePowerUp("RocketShoes");
                    bool hasRocketShoes = false; // Placeholder
                    
                    if (hasRocketShoes || playerPos.y > carPos.y + jumpOverHeight * 1.2f)
                    {
                        OnPlayerJumpedOver();
                    }
                }
            }
            
            // Check if player has passed the car safely
            if (playerPos.x > carPos.x + carLength && !hasCollided && !hasGivenJumpPoints)
            {
                OnPlayerAvoidedCar();
            }
        }
        
        private void OnPlayerJumpedOver()
        {
            if (hasGivenJumpPoints) return;
            
            hasGivenJumpPoints = true;
            
            // Award points
            if (givesPointsWhenJumped && gameManager != null)
            {
                var scoreManager = FindObjectOfType<Core.ScoreManager>();
                if (scoreManager != null)
                {
                    scoreManager.AddScore(jumpOverPoints);
                }
                
                // Show floating text effect
                // TODO: Implement floating text system
                Debug.Log($"Jumped over {carType}! +{jumpOverPoints} points");
            }
            
            // Special effects for successful jump
            PlayJumpOverEffects();
            
            // Special behavior for different car types
            HandleSpecialJumpBehavior();
        }
        
        private void OnPlayerAvoidedCar()
        {
            if (hasGivenJumpPoints) return;
            
            hasGivenJumpPoints = true;
            
            // Give smaller bonus for avoiding
            if (gameManager != null)
            {
                var scoreManager = FindObjectOfType<Core.ScoreManager>();
                if (scoreManager != null)
                {
                    int avoidBonus = jumpOverPoints / 2;
                    scoreManager.AddScore(avoidBonus);
                    Debug.Log($"Avoided {carType}! +{avoidBonus} points");
                }
            }
        }
        
        private void PlayJumpOverEffects()
        {
            // Play success sound
            if (audioManager != null)
            {
                // TODO: Add jump success sound
                // audioManager.PlayCustomSfx(jumpSuccessSound, soundVolume);
            }
            
            // Flash lights in celebration
            if (headlights != null)
            {
                StartCoroutine(FlashLights(headlights, 1f));
            }
            
            // Particle burst
            if (destroyEffect != null)
            {
                var emission = destroyEffect.emission;
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0f, 20)
                });
                destroyEffect.Play();
            }
        }
        
        private void HandleSpecialJumpBehavior()
        {
            switch (carType)
            {
                case CarType.Taxi:
                    // Taxi drops coins when jumped over
                    // TODO: Spawn coin collectibles
                    Debug.Log("Taxi dropped coins!");
                    break;
                    
                case CarType.PoliceCar:
                    // Police car alerts catcher less when jumped over
                    // TODO: Reduce catcher aggression
                    Debug.Log("Jumped over police car - catcher confused!");
                    break;
                    
                case CarType.SportsCar:
                    // Sports car gives style bonus
                    if (gameManager != null)
                    {
                        var scoreManager = FindObjectOfType<Core.ScoreManager>();
                        if (scoreManager != null)
                        {
                            scoreManager.AddScore(50); // Style bonus
                        }
                    }
                    break;
            }
        }
        
        #endregion
        
        #region Collision System Override
        
        protected override CollisionResult GetDefaultCollisionResult()
        {
            // Cars damage the player unless they have special abilities
            return CollisionResult.Damage;
        }
        
        protected override void OnPostCollision(CollisionResult result, Collider2D playerCollider)
        {
            if (result == CollisionResult.Damage)
            {
                // Apply stun effect
                var catController = playerCollider.GetComponent<Player.CatController>();
                if (catController != null)
                {
                    // TODO: Implement stun effect
                    // catController.ApplyStun(stunDuration);
                    Debug.Log($"Player stunned by {carType} for {stunDuration}s");
                }
                
                // Special collision effects for different car types
                HandleSpecialCollisionBehavior(playerCollider);
                
                // Stop car effects
                StopCarEffects();
            }
        }
        
        private void HandleSpecialCollisionBehavior(Collider2D playerCollider)
        {
            switch (carType)
            {
                case CarType.PoliceCar:
                    // Police car alerts catcher
                    AlertCatcher();
                    break;
                    
                case CarType.Truck:
                    // Truck creates more dramatic collision
                    CreateTruckCollisionEffect();
                    break;
                    
                case CarType.SportsCar:
                    // Sports car bounces player back more
                    BouncePlayerBack(playerCollider);
                    break;
            }
        }
        
        private void AlertCatcher()
        {
            // TODO: Alert catcher to player position
            var catcher = FindObjectOfType<Catcher.CatcherController>();
            if (catcher != null)
            {
                // catcher.AlertToPlayerPosition(playerTransform.position);
                Debug.Log("Catcher alerted by police car collision!");
            }
        }
        
        private void CreateTruckCollisionEffect()
        {
            // Screen shake effect
            // TODO: Implement screen shake
            Debug.Log("Truck collision - screen shake!");
            
            // Extra particles
            if (destroyEffect != null)
            {
                var emission = destroyEffect.emission;
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0f, 50)
                });
                destroyEffect.Play();
            }
        }
        
        private void BouncePlayerBack(Collider2D playerCollider)
        {
            var catController = playerCollider.GetComponent<Player.CatController>();
            if (catController != null)
            {
                // TODO: Apply bounce effect
                // catController.ApplyBounce(Vector3.left * 3f);
                Debug.Log("Player bounced back by sports car!");
            }
        }
        
        #endregion
        
        #region Car Management
        
        private void StopCarEffects()
        {
            // Stop all car-related coroutines
            if (hornCoroutine != null)
            {
                StopCoroutine(hornCoroutine);
            }
            
            if (wheelRotationCoroutine != null)
            {
                StopCoroutine(wheelRotationCoroutine);
            }
            
            if (speedVariationCoroutine != null)
            {
                StopCoroutine(speedVariationCoroutine);
            }
            
            // Stop particle effects
            if (engineSmoke != null)
            {
                engineSmoke.Stop();
            }
            
            if (exhaustFumes != null)
            {
                exhaustFumes.Stop();
            }
            
            // Turn off lights
            TurnOffLights();
        }
        
        private void TurnOffLights()
        {
            if (headlights != null)
            {
                foreach (var light in headlights)
                {
                    if (light != null)
                    {
                        light.enabled = false;
                    }
                }
            }
            
            if (taillights != null)
            {
                foreach (var light in taillights)
                {
                    if (light != null)
                    {
                        light.enabled = false;
                    }
                }
            }
        }
        
        public override void ResetObstacle()
        {
            base.ResetObstacle();
            
            // Reset car-specific state
            playerHasJumped = false;
            hasGivenJumpPoints = false;
            currentSpeedMultiplier = 1f;
            
            // Restart car effects
            StopCarEffects();
            StartCarEffects();
        }
        
        #endregion
        
        #region Utility Methods Override
        
        protected override bool CanJumpOver()
        {
            return canBeJumpedOver;
        }
        
        protected override bool CanSlideUnder()
        {
            return false; // Cars cannot be slid under
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Set the car type and reconfigure
        /// </summary>
        public void SetCarType(CarType newCarType)
        {
            carType = newCarType;
            ConfigureCarType();
        }
        
        /// <summary>
        /// Get car type
        /// </summary>
        public CarType GetCarType()
        {
            return carType;
        }
        
        /// <summary>
        /// Check if player can currently jump over this car
        /// </summary>
        public bool CanPlayerJumpOver()
        {
            if (!canBeJumpedOver) return false;
            
            var catController = playerTransform?.GetComponent<Player.CatController>();
            if (catController != null)
            {
                // TODO: Check for jump-enhancing power-ups
                // return catController.HasActivePowerUp("RocketShoes") || 
                //        catController.HasActivePowerUp("SuperJump");
                return true; // Placeholder - assume player can always jump
            }
            
            return false;
        }
        
        #endregion
        
        #region Debug Override
        
        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $"\n" +
                   $"Car Type: {carType}\n" +
                   $"Speed Multiplier: {currentSpeedMultiplier:F2}\n" +
                   $"Player Jumped: {playerHasJumped}\n" +
                   $"Given Jump Points: {hasGivenJumpPoints}\n" +
                   $"Can Jump Over: {canBeJumpedOver}";
        }
        
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            // Draw jump over zone
            if (canBeJumpedOver)
            {
                Gizmos.color = Color.green;
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Vector3 jumpZoneSize = new Vector3(carLength, jumpOverHeight, 1f);
                Vector3 jumpZoneCenter = transform.position + Vector3.up * (jumpOverHeight * 0.5f);
                Gizmos.DrawCube(jumpZoneCenter, jumpZoneSize);
            }
            
            // Draw car length
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position - Vector3.right * carLength * 0.5f,
                           transform.position + Vector3.right * carLength * 0.5f);
        }
        
        #endregion
    }
}