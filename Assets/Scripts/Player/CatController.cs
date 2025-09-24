using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.Player
{
    /// <summary>
    /// Main controller for the cat player
    /// Handles movement between lanes, jumping, sliding, and collision detection
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class CatController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float forwardSpeed = 5f;
        [SerializeField] private float laneChangeSpeed = 10f;
        [SerializeField] private float jumpForce = 15f;
        [SerializeField] private float slideHeight = 0.5f;
        [SerializeField] private float slideDuration = 1f;
        
        [Header("Lane Settings")]
        [SerializeField] private float[] lanePositions = { -2f, 0f, 2f }; // Left, Center, Right
        [SerializeField] private int currentLane = 1; // Start in center lane
        
        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.3f;
        [SerializeField] private LayerMask groundLayerMask = 1;
        
        [Header("Power-Ups")]
        [SerializeField] private bool hasRocketShoes = false;
        [SerializeField] private float rocketShoesMultiplier = 2f;
        
        // Components
        private Rigidbody2D rb;
        private Collider2D col;
        private CatInput input;
        private CatAnimator catAnimator;
        
        // State tracking
        public bool IsGrounded { get; private set; }
        public bool IsSliding { get; private set; }
        public bool IsMovingBetweenLanes { get; private set; }
        public bool IsAlive { get; private set; } = true;
        
        // Movement
        private Vector3 targetPosition;
        private Coroutine slideCoroutine;
        
        // Events
        public System.Action<int> OnCoinCollected;
        public System.Action<string> OnPowerUpCollected;
        public System.Action OnDeath;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            input = GetComponent<CatInput>();
            catAnimator = GetComponent<CatAnimator>();
            
            // Set initial position
            targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, transform.position.z);
            transform.position = targetPosition;
        }
        
        private void Start()
        {
            SetupInputEvents();
        }
        
        private void SetupInputEvents()
        {
            if (input != null)
            {
                input.OnSwipeLeft += MoveLeft;
                input.OnSwipeRight += MoveRight;
                input.OnSwipeUp += Jump;
                input.OnSwipeDown += Slide;
            }
        }
        
        private void Update()
        {
            if (!IsAlive) return;
            
            CheckGrounded();
            MoveForward();
            HandleLaneMovement();
            UpdateAnimatorStates();
        }
        
        private void CheckGrounded()
        {
            IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);
        }
        
        private void MoveForward()
        {
            // Constant forward movement
            transform.Translate(Vector3.right * forwardSpeed * Time.deltaTime);
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
        
        private void UpdateAnimatorStates()
        {
            if (catAnimator != null)
            {
                catAnimator.SetGrounded(IsGrounded);
                catAnimator.SetSliding(IsSliding);
                catAnimator.SetMovingBetweenLanes(IsMovingBetweenLanes);
            }
        }
        
        #region Input Handlers
        
        private void MoveLeft()
        {
            if (!IsAlive || IsMovingBetweenLanes) return;
            
            if (currentLane > 0)
            {
                currentLane--;
                targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, transform.position.z);
                IsMovingBetweenLanes = true;
            }
        }
        
        private void MoveRight()
        {
            if (!IsAlive || IsMovingBetweenLanes) return;
            
            if (currentLane < lanePositions.Length - 1)
            {
                currentLane++;
                targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, transform.position.z);
                IsMovingBetweenLanes = true;
            }
        }
        
        private void Jump()
        {
            if (!IsAlive || !IsGrounded || IsSliding) return;
            
            float jumpMultiplier = hasRocketShoes ? rocketShoesMultiplier : 1f;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce * jumpMultiplier);
            
            if (catAnimator != null)
            {
                catAnimator.TriggerJump();
            }
        }
        
        private void Slide()
        {
            if (!IsAlive || !IsGrounded || IsSliding) return;
            
            if (slideCoroutine != null)
            {
                StopCoroutine(slideCoroutine);
            }
            
            slideCoroutine = StartCoroutine(SlideCoroutine());
        }
        
        #endregion
        
        private IEnumerator SlideCoroutine()
        {
            IsSliding = true;
            
            // Lower the collider
            Vector3 originalScale = col.transform.localScale;
            col.transform.localScale = new Vector3(originalScale.x, slideHeight, originalScale.z);
            
            // Trigger slide animation
            if (catAnimator != null)
            {
                catAnimator.TriggerSlide();
            }
            
            yield return new WaitForSeconds(slideDuration);
            
            // Restore collider
            col.transform.localScale = originalScale;
            IsSliding = false;
            slideCoroutine = null;
        }
        
        #region Collision Detection
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsAlive) return;
            
            switch (other.tag)
            {
                case "Obstacle":
                    HandleObstacleCollision();
                    break;
                    
                case "Catcher":
                    HandleCatcherCollision();
                    break;
                    
                case "Coin":
                    HandleCoinCollection(other);
                    break;
                    
                case "PowerUp":
                    HandlePowerUpCollection(other);
                    break;
            }
        }
        
        private void HandleObstacleCollision()
        {
            // Check if can jump over with rocket shoes
            if (hasRocketShoes && !IsGrounded)
            {
                return; // Player jumped over the obstacle
            }
            
            Die();
        }
        
        private void HandleCatcherCollision()
        {
            Die();
        }
        
        private void HandleCoinCollection(Collider2D coinCollider)
        {
            // For now, use default coin value until FishCoin is created
            int coinValue = 10; // default value
            OnCoinCollected?.Invoke(coinValue);
            
            // Destroy the coin object
            Destroy(coinCollider.gameObject);
        }
        
        private void HandlePowerUpCollection(Collider2D powerUpCollider)
        {
            // For now, handle power-ups by tag until PowerUpBase is created
            string powerUpType = powerUpCollider.name; // or use a custom component
            OnPowerUpCollected?.Invoke(powerUpType);
            
            // Destroy the power-up object
            Destroy(powerUpCollider.gameObject);
            
            // Apply power-up effects
            ApplyPowerUp(powerUpType);
        }
        
        #endregion
        
        #region Power-Up System
        
        private void ApplyPowerUp(string powerUpType)
        {
            switch (powerUpType)
            {
                case "RocketShoes":
                    StartCoroutine(RocketShoesPowerUp());
                    break;
                    
                case "Magnet":
                    StartCoroutine(MagnetPowerUp());
                    break;
            }
        }
        
        private IEnumerator RocketShoesPowerUp()
        {
            hasRocketShoes = true;
            yield return new WaitForSeconds(10f); // 10 seconds duration
            hasRocketShoes = false;
        }
        
        private IEnumerator MagnetPowerUp()
        {
            // Enable coin magnet effect
            // This would be handled by a separate magnet script
            yield return new WaitForSeconds(8f); // 8 seconds duration
        }
        
        #endregion
        
        private void Die()
        {
            if (!IsAlive) return;
            
            IsAlive = false;
            rb.velocity = Vector2.zero;
            
            if (catAnimator != null)
            {
                catAnimator.TriggerHit();
            }
            
            OnDeath?.Invoke();
        }
        
        // Public methods for external access
        public void ResetPlayer()
        {
            IsAlive = true;
            currentLane = 1;
            targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, transform.position.z);
            transform.position = targetPosition;
            IsSliding = false;
            IsMovingBetweenLanes = false;
            hasRocketShoes = false;
            
            if (slideCoroutine != null)
            {
                StopCoroutine(slideCoroutine);
                slideCoroutine = null;
            }
        }
        
        private void OnDrawGizmos()
        {
            // Draw lane positions
            Gizmos.color = Color.green;
            for (int i = 0; i < lanePositions.Length; i++)
            {
                Vector3 lanePos = new Vector3(lanePositions[i], transform.position.y, transform.position.z);
                Gizmos.DrawWireCube(lanePos, Vector3.one * 0.5f);
            }
            
            // Draw ground check
            if (groundCheck != null)
            {
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup events
            if (input != null)
            {
                input.OnSwipeLeft -= MoveLeft;
                input.OnSwipeRight -= MoveRight;
                input.OnSwipeUp -= Jump;
                input.OnSwipeDown -= Slide;
            }
        }
    }
}