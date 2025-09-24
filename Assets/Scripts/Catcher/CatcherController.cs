using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.Catcher
{
    /// <summary>
    /// Main controller for the Catcher character
    /// Handles movement, chasing behavior, and catching the player
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class CatcherController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float baseSpeed = 3f;
        [SerializeField] private float maxSpeed = 8f;
        [SerializeField] private float speedIncreaseRate = 0.1f;
        [SerializeField] private float laneChangeSpeed = 8f;
        
        [Header("Chasing Behavior")]
        [SerializeField] private float catchDistance = 1.5f;
        [SerializeField] private float distanceFromPlayer = 8f;
        [SerializeField] private float aggroDistance = 12f;
        [SerializeField] private bool maintainDistance = true;
        
        [Header("Lane Settings")]
        [SerializeField] private float[] lanePositions = { -2f, 0f, 2f }; // Left, Center, Right
        [SerializeField] private int currentLane = 1; // Start in center lane
        
        [Header("AI Settings")]
        [SerializeField] private float reactionTime = 0.5f;
        [SerializeField] private float laneChangeDelay = 1f;
        [SerializeField] private bool enableSmartAI = true;
        
        // Components
        private Rigidbody2D rb;
        private Collider2D col;
        private CatcherAI catcherAI;
        private CatcherAnimator catcherAnimator;
        
        // Target tracking
        private Transform player;
        private CatchMeowIfYouCan.Player.CatController playerController;
        
        // State tracking
        public bool IsActive { get; private set; } = true;
        public bool IsChasing { get; private set; } = false;
        public bool IsMovingBetweenLanes { get; private set; } = false;
        public float CurrentSpeed { get; private set; }
        
        // Movement
        private Vector3 targetPosition;
        private Coroutine laneChangeCoroutine;
        private float lastLaneChangeTime;
        
        // Events
        public System.Action OnPlayerCaught;
        public System.Action<bool> OnChasingStateChanged;
        public System.Action<float> OnSpeedChanged;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            catcherAI = GetComponent<CatcherAI>();
            catcherAnimator = GetComponent<CatcherAnimator>();
            
            CurrentSpeed = baseSpeed;
            
            // Set initial position
            targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, transform.position.z);
        }
        
        private void Start()
        {
            FindPlayer();
            SetupAI();
        }
        
        private void FindPlayer()
        {
            // Find player in scene
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerController = playerObj.GetComponent<CatchMeowIfYouCan.Player.CatController>();
                
                if (playerController != null)
                {
                    // Subscribe to player death event
                    playerController.OnDeath += OnPlayerDied;
                }
            }
            else
            {
                Debug.LogError("CatcherController: Player not found! Make sure player has 'Player' tag.");
            }
        }
        
        private void SetupAI()
        {
            if (catcherAI != null)
            {
                catcherAI.Initialize(this, player);
            }
        }
        
        private void Update()
        {
            if (!IsActive || player == null) return;
            
            UpdateChasing();
            HandleMovement();
            UpdateAnimator();
            CheckCatchPlayer();
        }
        
        private void UpdateChasing()
        {
            if (player == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            bool shouldChase = distanceToPlayer <= aggroDistance && playerController.IsAlive;
            
            if (IsChasing != shouldChase)
            {
                IsChasing = shouldChase;
                OnChasingStateChanged?.Invoke(IsChasing);
                
                if (catcherAnimator != null)
                {
                    catcherAnimator.SetChasing(IsChasing);
                }
            }
        }
        
        private void HandleMovement()
        {
            if (!IsChasing) return;
            
            MoveForward();
            HandleLaneMovement();
            
            // AI decision making
            if (enableSmartAI && catcherAI != null)
            {
                catcherAI.UpdateAI();
            }
        }
        
        private void MoveForward()
        {
            // Calculate target speed based on distance to player
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            if (maintainDistance)
            {
                // Slow down if too close, speed up if too far
                if (distanceToPlayer < distanceFromPlayer)
                {
                    CurrentSpeed = Mathf.Max(baseSpeed * 0.5f, CurrentSpeed - speedIncreaseRate * Time.deltaTime);
                }
                else if (distanceToPlayer > distanceFromPlayer * 1.5f)
                {
                    CurrentSpeed = Mathf.Min(maxSpeed, CurrentSpeed + speedIncreaseRate * Time.deltaTime * 2f);
                }
            }
            else
            {
                // Always try to catch up
                CurrentSpeed = Mathf.Min(maxSpeed, CurrentSpeed + speedIncreaseRate * Time.deltaTime);
            }
            
            // Move forward
            transform.Translate(Vector3.right * CurrentSpeed * Time.deltaTime);
            
            OnSpeedChanged?.Invoke(CurrentSpeed);
        }
        
        private void HandleLaneMovement()
        {
            if (IsMovingBetweenLanes)
            {
                // Smooth movement to target lane
                Vector3 currentPos = transform.position;
                Vector3 newPos = Vector3.MoveTowards(currentPos, targetPosition, laneChangeSpeed * Time.deltaTime);
                transform.position = newPos;
                
                // Check if reached target
                if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
                {
                    transform.position = targetPosition;
                    IsMovingBetweenLanes = false;
                }
            }
        }
        
        private void UpdateAnimator()
        {
            if (catcherAnimator != null)
            {
                catcherAnimator.SetMoving(IsChasing);
                catcherAnimator.SetMovingBetweenLanes(IsMovingBetweenLanes);
                catcherAnimator.SetSpeed(CurrentSpeed / maxSpeed);
            }
        }
        
        private void CheckCatchPlayer()
        {
            if (!IsChasing || player == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            if (distanceToPlayer <= catchDistance && playerController.IsAlive)
            {
                CatchPlayer();
            }
        }
        
        #region Lane Movement
        
        /// <summary>
        /// Move to a specific lane
        /// </summary>
        public void MoveToLane(int laneIndex)
        {
            if (laneIndex < 0 || laneIndex >= lanePositions.Length) return;
            if (currentLane == laneIndex || IsMovingBetweenLanes) return;
            
            // Check lane change delay
            if (Time.time - lastLaneChangeTime < laneChangeDelay) return;
            
            currentLane = laneIndex;
            targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, transform.position.z);
            IsMovingBetweenLanes = true;
            lastLaneChangeTime = Time.time;
            
            // Update animator
            if (catcherAnimator != null)
            {
                catcherAnimator.UpdateSpriteDirection(currentLane);
            }
        }
        
        /// <summary>
        /// Move to the same lane as the player
        /// </summary>
        public void MoveToPlayerLane()
        {
            if (player == null) return;
            
            int playerLane = GetPlayerLane();
            if (playerLane != -1)
            {
                MoveToLane(playerLane);
            }
        }
        
        /// <summary>
        /// Get the current lane index of the player
        /// </summary>
        public int GetPlayerLane()
        {
            if (player == null) return -1;
            
            float playerX = player.position.x;
            int closestLane = 0;
            float closestDistance = Mathf.Abs(playerX - lanePositions[0]);
            
            for (int i = 1; i < lanePositions.Length; i++)
            {
                float distance = Mathf.Abs(playerX - lanePositions[i]);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestLane = i;
                }
            }
            
            return closestLane;
        }
        
        #endregion
        
        #region Speed Control
        
        /// <summary>
        /// Increase base speed (called by game progression)
        /// </summary>
        public void IncreaseBaseSpeed(float amount)
        {
            baseSpeed += amount;
            maxSpeed += amount;
            CurrentSpeed = Mathf.Max(CurrentSpeed, baseSpeed);
        }
        
        /// <summary>
        /// Set speed multiplier
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            CurrentSpeed = baseSpeed * multiplier;
            CurrentSpeed = Mathf.Clamp(CurrentSpeed, baseSpeed * 0.5f, maxSpeed);
        }
        
        #endregion
        
        #region Catching Logic
        
        private void CatchPlayer()
        {
            if (!playerController.IsAlive) return;
            
            IsActive = false;
            
            // Trigger catch animation
            if (catcherAnimator != null)
            {
                catcherAnimator.TriggerCatch();
            }
            
            // Stop movement
            rb.velocity = Vector2.zero;
            
            // Notify systems
            OnPlayerCaught?.Invoke();
            
            Debug.Log("Player caught by Catcher!");
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnPlayerDied()
        {
            // Player died from other causes, stop chasing
            IsChasing = false;
            
            if (catcherAnimator != null)
            {
                catcherAnimator.SetChasing(false);
                catcherAnimator.TriggerIdle();
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Reset catcher to initial state
        /// </summary>
        public void ResetCatcher()
        {
            IsActive = true;
            IsChasing = false;
            IsMovingBetweenLanes = false;
            CurrentSpeed = baseSpeed;
            currentLane = 1;
            
            targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, transform.position.z);
            transform.position = targetPosition;
            
            if (laneChangeCoroutine != null)
            {
                StopCoroutine(laneChangeCoroutine);
                laneChangeCoroutine = null;
            }
            
            if (catcherAnimator != null)
            {
                catcherAnimator.ResetAnimationStates();
            }
        }
        
        /// <summary>
        /// Pause/unpause catcher
        /// </summary>
        public void SetActive(bool active)
        {
            IsActive = active;
            
            if (!active)
            {
                rb.velocity = Vector2.zero;
            }
        }
        
        /// <summary>
        /// Get distance to player
        /// </summary>
        public float GetDistanceToPlayer()
        {
            return player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;
        }
        
        /// <summary>
        /// Check if catcher can catch player (within catch distance)
        /// </summary>
        public bool CanCatchPlayer()
        {
            return GetDistanceToPlayer() <= catchDistance && playerController != null && playerController.IsAlive;
        }
        
        #endregion
        
        private void OnDrawGizmos()
        {
            // Draw catch distance
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, catchDistance);
            
            // Draw aggro distance
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aggroDistance);
            
            // Draw lane positions
            Gizmos.color = Color.blue;
            for (int i = 0; i < lanePositions.Length; i++)
            {
                Vector3 lanePos = new Vector3(lanePositions[i], transform.position.y, transform.position.z);
                Gizmos.DrawWireCube(lanePos, Vector3.one * 0.3f);
            }
            
            // Draw line to player
            if (player != null)
            {
                Gizmos.color = IsChasing ? Color.red : Color.gray;
                Gizmos.DrawLine(transform.position, player.position);
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup events
            if (playerController != null)
            {
                playerController.OnDeath -= OnPlayerDied;
            }
        }
    }
}