using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.Catcher
{
    /// <summary>
    /// AI logic for the Catcher character
    /// Handles intelligent decision making, lane prediction, and adaptive behavior
    /// </summary>
    public class CatcherAI : MonoBehaviour
    {
        [Header("AI Behavior Settings")]
        [SerializeField] private float reactionTime = 0.5f;
        [SerializeField] private float predictionTime = 1f;
        [SerializeField] private bool enablePrediction = true;
        [SerializeField] private bool enableLearning = true;
        
        [Header("Decision Making")]
        [SerializeField] private float aggressionLevel = 0.7f; // 0 = defensive, 1 = aggressive
        [SerializeField] private float smartnessLevel = 0.8f; // 0 = simple, 1 = very smart
        [SerializeField] private float adaptationRate = 0.1f;
        
        [Header("Prediction Settings")]
        [SerializeField] private int maxPredictionSteps = 5;
        [SerializeField] private float confidenceThreshold = 0.6f;
        [SerializeField] private bool showPredictionDebug = false;
        
        // Components
        private CatcherController controller;
        private Transform player;
        private CatchMeowIfYouCan.Player.CatController playerController;
        
        // AI State
        private AIState currentState = AIState.Idle;
        private float lastDecisionTime;
        private float stateTimer;
        
        // Player tracking
        private Vector3 lastPlayerPosition;
        private Vector3 playerVelocity;
        private int lastPlayerLane = 1;
        private float playerLaneChangeTime;
        
        // Learning data
        private int[] playerLanePreferences = new int[3]; // Track which lanes player uses most
        private float[] reactionTimes = new float[10]; // Track reaction times
        private int reactionTimeIndex = 0;
        
        // Prediction
        private Vector3 predictedPlayerPosition;
        private int predictedPlayerLane;
        private float predictionConfidence;
        
        public enum AIState
        {
            Idle,
            Chasing,
            Predicting,
            Intercepting,
            Catching
        }
        
        public AIState CurrentState => currentState;
        public float PredictionConfidence => predictionConfidence;
        
        public void Initialize(CatcherController catcherController, Transform playerTransform)
        {
            controller = catcherController;
            player = playerTransform;
            
            if (player != null)
            {
                playerController = player.GetComponent<CatchMeowIfYouCan.Player.CatController>();
                lastPlayerPosition = player.position;
            }
            
            // Initialize learning data
            for (int i = 0; i < playerLanePreferences.Length; i++)
            {
                playerLanePreferences[i] = 0;
            }
            
            for (int i = 0; i < reactionTimes.Length; i++)
            {
                reactionTimes[i] = reactionTime;
            }
        }
        
        public void UpdateAI()
        {
            if (player == null || !controller.IsActive) return;
            
            UpdatePlayerTracking();
            UpdateAIState();
            MakeDecision();
            
            stateTimer += Time.deltaTime;
        }
        
        private void UpdatePlayerTracking()
        {
            // Calculate player velocity
            Vector3 currentPlayerPos = player.position;
            playerVelocity = (currentPlayerPos - lastPlayerPosition) / Time.deltaTime;
            lastPlayerPosition = currentPlayerPos;
            
            // Track lane changes
            int currentPlayerLane = controller.GetPlayerLane();
            if (currentPlayerLane != lastPlayerLane)
            {
                playerLaneChangeTime = Time.time;
                
                // Update learning data
                if (enableLearning && currentPlayerLane >= 0 && currentPlayerLane < playerLanePreferences.Length)
                {
                    playerLanePreferences[currentPlayerLane]++;
                }
                
                lastPlayerLane = currentPlayerLane;
            }
            
            // Update prediction
            if (enablePrediction)
            {
                UpdatePrediction();
            }
        }
        
        private void UpdatePrediction()
        {
            // Predict where the player will be
            Vector3 futurePosition = player.position + playerVelocity * predictionTime;
            predictedPlayerPosition = futurePosition;
            
            // Predict which lane the player will be in
            predictedPlayerLane = GetLaneFromPosition(futurePosition);
            
            // Calculate confidence based on player behavior consistency
            float velocityMagnitude = playerVelocity.magnitude;
            float timeSinceLastLaneChange = Time.time - playerLaneChangeTime;
            
            predictionConfidence = Mathf.Clamp01(
                (velocityMagnitude * 0.3f) + 
                (timeSinceLastLaneChange * 0.1f) + 
                (smartnessLevel * 0.6f)
            );
        }
        
        private void UpdateAIState()
        {
            AIState newState = currentState;
            
            switch (currentState)
            {
                case AIState.Idle:
                    if (controller.IsChasing)
                    {
                        newState = AIState.Chasing;
                    }
                    break;
                    
                case AIState.Chasing:
                    if (!controller.IsChasing)
                    {
                        newState = AIState.Idle;
                    }
                    else if (enablePrediction && predictionConfidence > confidenceThreshold)
                    {
                        newState = AIState.Predicting;
                    }
                    else if (controller.CanCatchPlayer())
                    {
                        newState = AIState.Catching;
                    }
                    break;
                    
                case AIState.Predicting:
                    if (!controller.IsChasing)
                    {
                        newState = AIState.Idle;
                    }
                    else if (predictionConfidence < confidenceThreshold * 0.5f)
                    {
                        newState = AIState.Chasing;
                    }
                    else if (ShouldIntercept())
                    {
                        newState = AIState.Intercepting;
                    }
                    break;
                    
                case AIState.Intercepting:
                    if (!controller.IsChasing)
                    {
                        newState = AIState.Idle;
                    }
                    else if (controller.CanCatchPlayer())
                    {
                        newState = AIState.Catching;
                    }
                    else if (stateTimer > 2f) // Timeout
                    {
                        newState = AIState.Chasing;
                    }
                    break;
                    
                case AIState.Catching:
                    if (!controller.IsChasing || !controller.CanCatchPlayer())
                    {
                        newState = AIState.Chasing;
                    }
                    break;
            }
            
            if (newState != currentState)
            {
                ChangeState(newState);
            }
        }
        
        private void ChangeState(AIState newState)
        {
            currentState = newState;
            stateTimer = 0f;
            
            if (showPredictionDebug)
            {
                Debug.Log($"CatcherAI: State changed to {newState}");
            }
        }
        
        private void MakeDecision()
        {
            // Don't make decisions too frequently
            if (Time.time - lastDecisionTime < GetCurrentReactionTime())
            {
                return;
            }
            
            switch (currentState)
            {
                case AIState.Chasing:
                    DecideChasing();
                    break;
                    
                case AIState.Predicting:
                    DecidePredicting();
                    break;
                    
                case AIState.Intercepting:
                    DecideIntercepting();
                    break;
                    
                case AIState.Catching:
                    DecideCatching();
                    break;
            }
            
            lastDecisionTime = Time.time;
        }
        
        private void DecideChasing()
        {
            // Basic chasing behavior - follow player lane with some delay
            int playerLane = controller.GetPlayerLane();
            int currentLane = GetCurrentCatcherLane();
            
            // Add some randomness based on aggression level
            float randomFactor = Random.Range(0f, 1f);
            
            if (randomFactor < aggressionLevel)
            {
                // Aggressive: Move to player lane immediately
                controller.MoveToPlayerLane();
            }
            else
            {
                // Conservative: Wait a bit or move to a strategic position
                if (Time.time - playerLaneChangeTime > reactionTime * 2f)
                {
                    controller.MoveToPlayerLane();
                }
            }
        }
        
        private void DecidePredicting()
        {
            // Use prediction to make smarter moves
            if (predictedPlayerLane != -1)
            {
                int currentLane = GetCurrentCatcherLane();
                
                if (predictedPlayerLane != currentLane)
                {
                    // Move to predicted lane
                    controller.MoveToLane(predictedPlayerLane);
                }
            }
        }
        
        private void DecideIntercepting()
        {
            // Try to intercept player at predicted position
            float distanceToPlayer = controller.GetDistanceToPlayer();
            
            if (distanceToPlayer > 3f)
            {
                // Too far, switch back to chasing
                ChangeState(AIState.Chasing);
                return;
            }
            
            // Try to cut off player's escape routes
            int playerLane = controller.GetPlayerLane();
            int preferredLane = GetMostUsedPlayerLane();
            
            // Block the player's preferred lane
            if (preferredLane != playerLane && preferredLane != -1)
            {
                controller.MoveToLane(preferredLane);
            }
        }
        
        private void DecideCatching()
        {
            // Final catching phase - be very aggressive
            controller.MoveToPlayerLane();
            
            // Increase speed for final sprint
            controller.SetSpeedMultiplier(1.2f);
        }
        
        #region Helper Methods
        
        private bool ShouldIntercept()
        {
            float distanceToPlayer = controller.GetDistanceToPlayer();
            float playerSpeed = playerVelocity.magnitude;
            
            // Intercept if player is close and moving predictably
            return distanceToPlayer < 5f && 
                   playerSpeed > 1f && 
                   predictionConfidence > confidenceThreshold &&
                   aggressionLevel > 0.5f;
        }
        
        private int GetCurrentCatcherLane()
        {
            float catcherX = transform.position.x;
            float[] lanePositions = { -2f, 0f, 2f }; // Same as controller
            
            // Find closest lane to catcher position
            int closestLane = 0;
            float closestDistance = Mathf.Abs(catcherX - lanePositions[0]);
            
            for (int i = 1; i < lanePositions.Length; i++)
            {
                float distance = Mathf.Abs(catcherX - lanePositions[i]);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestLane = i;
                }
            }
            
            return closestLane;
        }
        
        private int GetLaneFromPosition(Vector3 position)
        {
            float x = position.x;
            float[] lanePositions = { -2f, 0f, 2f }; // Same as controller
            
            // Find closest lane
            int closestLane = 0;
            float closestDistance = Mathf.Abs(x - lanePositions[0]);
            
            for (int i = 1; i < lanePositions.Length; i++)
            {
                float distance = Mathf.Abs(x - lanePositions[i]);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestLane = i;
                }
            }
            
            return closestLane;
        }
        
        private int GetMostUsedPlayerLane()
        {
            int maxUsage = 0;
            int mostUsedLane = 1; // Default to center
            
            for (int i = 0; i < playerLanePreferences.Length; i++)
            {
                if (playerLanePreferences[i] > maxUsage)
                {
                    maxUsage = playerLanePreferences[i];
                    mostUsedLane = i;
                }
            }
            
            return mostUsedLane;
        }
        
        private float GetCurrentReactionTime()
        {
            // Adaptive reaction time based on smartness level
            float baseReaction = reactionTime;
            float smartnessMultiplier = 1f - (smartnessLevel * 0.7f); // Smarter = faster reactions
            
            return baseReaction * smartnessMultiplier;
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Adjust AI difficulty
        /// </summary>
        public void SetDifficulty(float difficulty)
        {
            // difficulty: 0 = easy, 1 = hard
            aggressionLevel = Mathf.Clamp01(0.3f + difficulty * 0.7f);
            smartnessLevel = Mathf.Clamp01(0.4f + difficulty * 0.6f);
            reactionTime = Mathf.Lerp(1f, 0.2f, difficulty);
        }
        
        /// <summary>
        /// Reset AI learning data
        /// </summary>
        public void ResetLearning()
        {
            for (int i = 0; i < playerLanePreferences.Length; i++)
            {
                playerLanePreferences[i] = 0;
            }
            
            reactionTimeIndex = 0;
        }
        
        /// <summary>
        /// Get AI state for debugging
        /// </summary>
        public string GetAIDebugInfo()
        {
            return $"State: {currentState}\n" +
                   $"Confidence: {predictionConfidence:F2}\n" +
                   $"Aggression: {aggressionLevel:F2}\n" +
                   $"Smartness: {smartnessLevel:F2}\n" +
                   $"Predicted Lane: {predictedPlayerLane}";
        }
        
        #endregion
        
        private void OnDrawGizmos()
        {
            if (!showPredictionDebug || player == null) return;
            
            // Draw prediction
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(predictedPlayerPosition, 0.5f);
            
            // Draw confidence
            Gizmos.color = Color.Lerp(Color.red, Color.green, predictionConfidence);
            Gizmos.DrawLine(transform.position, predictedPlayerPosition);
            
            // Draw state
            Vector3 statePos = transform.position + Vector3.up * 2f;
            
#if UNITY_EDITOR
            UnityEditor.Handles.Label(statePos, currentState.ToString());
#endif
        }
    }
}