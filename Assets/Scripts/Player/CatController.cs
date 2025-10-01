using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CatchMeowIfYouCan.Player
{
    /// <summary>
    /// Simple cat controller with basic movement, jumping, and ground detection
    /// NO KNOCKBACK - Only death when caught by chaser in chasing state
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class CatController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float jumpForce = 12f;
        
        [Header("Survival Mode Settings")]
        [SerializeField] private bool enableSurvivalMode = true;
        [SerializeField] private float backwardDriftForce = 3f;
        [SerializeField] private float worldDriftSpeed = 2f;
        [SerializeField] private float baseFriction = 2f;
        [SerializeField] private float highFriction = 5f;
        [SerializeField] private bool autoRun = false;
        
        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.3f;
        [SerializeField] private LayerMask groundLayerMask = 1;
        
        [Header("Character Flipping")]
        [SerializeField] private bool facingRight = true;
        
        // Components
        private Rigidbody2D rb;
        private Vector3 originalScale;
        private Animator animator;
        
        // State
        public bool IsGrounded { get; private set; }
        public bool IsAlive { get; private set; } = true;
        
        // Input
        private float horizontalInput;
        
        // Events
        public System.Action OnDeath;
        public System.Action<int> OnCoinCollected;
        public System.Action<string> OnPowerUpCollected;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            originalScale = transform.localScale;
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            CheckGroundSetup();
        }

        private void Update()
        {
            HandleInput();
            HandleGroundCheck();
            HandleMovement();
            HandleJump();
            CheckBoundaries();
            UpdateAnimation();
        }
        
        private void HandleInput()
        {
            horizontalInput = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                horizontalInput = -1f;
            }
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                horizontalInput = 1f;
            }
        }
        
        private void HandleGroundCheck()
        {
            bool wasGrounded = IsGrounded;
            
            if (groundCheck != null)
            {
                IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);
            }
            else
            {
                Vector2 rayStart = new Vector2(transform.position.x, transform.position.y - 0.5f);
                RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 1f, groundLayerMask);
                IsGrounded = hit.collider != null;
            }
        }
        
        private void HandleMovement()
        {
            // No more knockback checks - free movement always
            
            if (rb == null)
            {
                Debug.LogError("Rigidbody2D is null!");
                return;
            }
            
            Vector2 velocity = rb.linearVelocity;
            bool movingRight = horizontalInput > 0;
            bool movingLeft = horizontalInput < 0;
            bool noInput = horizontalInput == 0;
            
            if (enableSurvivalMode)
            {
                if (movingRight)
                {
                    velocity.x = horizontalInput * moveSpeed;
                    rb.linearDamping = Mathf.Lerp(rb.linearDamping, baseFriction, Time.deltaTime * 3f);
                }
                else if (movingLeft)
                {
                    velocity.x = horizontalInput * moveSpeed;
                    rb.linearDamping = Mathf.Lerp(rb.linearDamping, baseFriction * 1.5f, Time.deltaTime * 3f);
                }
                else
                {
                    var fixedBgManager = FindFirstObjectByType<CatchMeowIfYouCan.Environment.FixedBackgroundManager>();
                    bool hasFixedBackground = fixedBgManager != null;
                    
                    if (hasFixedBackground)
                    {
                        velocity.x = -worldDriftSpeed - backwardDriftForce;
                    }
                    else
                    {
                        float currentVelX = velocity.x;
                        velocity.x = currentVelX - (backwardDriftForce * Time.deltaTime);
                    }
                    
                    rb.linearDamping = Mathf.Lerp(rb.linearDamping, highFriction, Time.deltaTime * 3f);
                }
            }
            else
            {
                velocity.x = horizontalInput * moveSpeed;
                rb.linearDamping = baseFriction;
            }
            
            rb.linearVelocity = velocity;
            
            // Character flipping
            if (horizontalInput > 0 && !facingRight)
            {
                Flip();
            }
            else if (horizontalInput < 0 && facingRight)
            {
                Flip();
            }
        }
        
        private void HandleJump()
        {
            if (IsGrounded && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
            {
                Jump();
            }
        }
        
        private void Jump()
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.y = jumpForce;
            rb.linearVelocity = velocity;
        }
        
        private void Flip()
        {
            facingRight = !facingRight;
            
            if (facingRight)
            {
                transform.localScale = originalScale;
            }
            else
            {
                transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);
            }
        }
        
        private void CheckBoundaries()
        {
            if (transform.position.y < -10f)
            {
                Vector3 safePos = new Vector3(0f, 2f, transform.position.z);
                transform.position = safePos;
                
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }
        
        private void UpdateAnimation()
        {
            bool isRunning = autoRun || Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            bool isJumping = !IsGrounded;
            if (animator != null)
            {
                animator.SetBool("IsRunning", isRunning);
                animator.SetBool("IsJumping", isJumping);
            }
        }
        
        private void CheckGroundSetup()
        {
            if (groundCheck == null)
            {
                Debug.LogWarning("GroundCheck Transform is not assigned! Using fallback raycast method.");
            }
        }
        
        /// <summary>
        /// ONLY death condition: When caught by chaser in chasing state
        /// </summary>
        public void OnCaughtByChaser()
        {
            Debug.Log("[CatController] 💀 CAUGHT BY CATCHER! Game Over - Loading EndScene...");
            
            IsAlive = false;
            OnDeath?.Invoke();
            LoadEndScene();
        }
        
        private void LoadEndScene()
        {
            try
            {
                if (Application.CanStreamedLevelBeLoaded("EndScene"))
                {
                    Debug.Log("[CatController] Loading EndScene by name");
                    SceneManager.LoadScene("EndScene");
                    return;
                }
                
                if (Application.CanStreamedLevelBeLoaded("HighScore"))
                {
                    Debug.Log("[CatController] EndScene not found, loading HighScore scene as alternative");
                    SceneManager.LoadScene("HighScore");
                    return;
                }
                
                if (SceneManager.sceneCountInBuildSettings > 3)
                {
                    Debug.Log("[CatController] Loading scene by index 3 (HighScore)");
                    SceneManager.LoadScene(3);
                    return;
                }
                
                Debug.LogWarning("[CatController] No suitable end scene found, loading first scene as fallback");
                SceneManager.LoadScene(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CatController] Error loading EndScene: {e.Message}");
            }
        }
        
        // Collision Detection - ONLY for catcher in chasing state
        private void OnTriggerEnter2D(Collider2D other)
        {
            switch (other.tag)
            {
                case "Coin":
                    OnCoinCollected?.Invoke(10);
                    Destroy(other.gameObject);
                    break;
                    
                case "PowerUp":
                    OnPowerUpCollected?.Invoke(other.name);
                    Destroy(other.gameObject);
                    break;
                    
                case "Catcher":
                    Debug.Log($"[CatController] 🎯 CATCHER COLLISION DETECTED!");
                    
                    var catcherController = other.GetComponentInParent<CatchMeowIfYouCan.Enemies.CatcherController>();
                    
                    if (catcherController == null)
                    {
                        catcherController = other.GetComponent<CatchMeowIfYouCan.Enemies.CatcherController>();
                        if (catcherController == null)
                        {
                            Debug.LogError($"[CatController] ❌ CatcherController NOT FOUND in object: {other.name}");
                            return;
                        }
                    }
                    
                    bool isChasing = catcherController.IsInChasingState();
                    Debug.Log($"[CatController] 🔍 Catcher state check - IsInChasingState: {isChasing}");
                    
                    if (isChasing)
                    {
                        Debug.Log("[CatController] 🚨 CONTACTED CATCHER IN CHASING STATE! Immediate Game Over!");
                        OnCaughtByChaser();
                    }
                    else
                    {
                        Debug.Log("[CatController] Contacted catcher but not in chasing state - no effect");
                    }
                    break;
            }
        }
        
        // Public methods for external control
        public void MoveLeft()
        {
            horizontalInput = -1f;
        }
        
        public void MoveRight()
        {
            horizontalInput = 1f;
        }
        
        public void StopMovement()
        {
            horizontalInput = 0f;
        }
        
        public void JumpAction()
        {
            if (IsGrounded)
            {
                Jump();
            }
        }
        
        public void ResetPlayer()
        {
            IsAlive = true;
            horizontalInput = 0f;
            
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            
            facingRight = true;
            transform.localScale = originalScale;
            
            Vector3 resetPos = new Vector3(0f, 2f, transform.position.z);
            transform.position = resetPos;
            
            Debug.Log($"Player reset to position: {resetPos}");
        }
        
        // Debug methods
        [ContextMenu("Test Instant Game Over")]
        public void TestInstantGameOver()
        {
            Debug.Log("[CatController] 🧪 TESTING INSTANT GAME OVER!");
            OnCaughtByChaser();
        }
        
        private void OnDrawGizmos()
        {
            if (groundCheck != null)
            {
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
            
            if (Application.isPlaying && rb != null)
            {
                Gizmos.color = Color.magenta;
                Vector3 velocityEnd = transform.position + new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0) * 0.1f;
                Gizmos.DrawLine(transform.position, velocityEnd);
            }
        }
    }
}