using UnityEngine;

namespace CatchMeowIfYouCan.Player
{
    /// <summary>
    /// Simple cat controller with basic movement, jumping, and ground detection
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class CatController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float jumpForce = 12f;
        
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
        
        // Events - Thêm các events mà các script khác cần
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

            // Kiểm tra ground setup
            CheckGroundSetup();
        }

        private void UpdateAnimation()
        {
            bool isRunning = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            bool isJumping = !IsGrounded;
            if (animator != null)
            {
                animator.SetBool("IsRunning", isRunning);
                animator.SetBool("IsJumpping", isJumping);
            }
        }

        /// <summary>
        /// Kiểm tra và cảnh báo về ground setup
        /// </summary>
        private void CheckGroundSetup()
        {
            if (groundCheck == null)
            {
                Debug.LogWarning("GroundCheck Transform is not assigned! Using fallback raycast method.");
            }

            // Kiểm tra xem có ground objects nào trong scene không
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            bool foundGroundLayer = false;

            foreach (GameObject obj in allObjects)
            {
                if (((1 << obj.layer) & groundLayerMask) != 0)
                {
                    foundGroundLayer = true;
                    Debug.Log($"Found ground object: {obj.name} on layer {obj.layer}");
                    break;
                }
            }
        }

        private void Update()
        {
            if (!IsAlive) return;

            HandleInput();
            HandleGroundCheck();
            HandleMovement();
            HandleJump();
            CheckBoundaries();
            UpdateAnimation();
        }
        
        /// <summary>
        /// Kiểm tra và giới hạn player trong boundaries
        /// </summary>
        private void CheckBoundaries()
        {
            // Nếu player rơi quá sâu, reset về vị trí an toàn
            if (transform.position.y < -10f)
            {
                Debug.LogWarning($"Player fell too deep (Y: {transform.position.y}), resetting to safe position");
                Vector3 safePos = new Vector3(0f, 2f, transform.position.z);
                transform.position = safePos;
                
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }
        
        /// <summary>
        /// Xử lý input từ bàn phím
        /// </summary>
        private void HandleInput()
        {
            // Horizontal movement input
            horizontalInput = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                horizontalInput = -1f;
                Debug.Log("Input: Moving LEFT");
            }
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                horizontalInput = 1f;
                Debug.Log("Input: Moving RIGHT");
            }
            
            // Debug input
            if (Time.frameCount % 60 == 0) // Log mỗi giây
            {
                Debug.Log($"Input: {horizontalInput}, IsAlive: {IsAlive}");
            }
        }
        
        /// <summary>
        /// Kiểm tra nhân vật có đang trên mặt đất không
        /// </summary>
        private void HandleGroundCheck()
        {
            bool wasGrounded = IsGrounded;
            
            if (groundCheck != null)
            {
                IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);
            }
            else
            {
                // Fallback: kiểm tra bằng raycast xuống dưới từ center của nhân vật
                Vector2 rayStart = new Vector2(transform.position.x, transform.position.y - 0.5f);
                RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 1f, groundLayerMask);
                IsGrounded = hit.collider != null;
                
                // Debug log chỉ khi có thay đổi
                if (hit.collider != null && !wasGrounded)
                {
                    Debug.Log($"Ground detected: {hit.collider.name} on layer {hit.collider.gameObject.layer}");
                }
                
                // Kiểm tra tất cả các layer để debug
                if (!IsGrounded)
                {
                    RaycastHit2D allLayersHit = Physics2D.Raycast(rayStart, Vector2.down, 1f);
                    if (Time.frameCount % 120 == 0 && allLayersHit.collider != null) // Log mỗi 2 giây
                    {
                        Debug.Log($"Found collider but wrong layer: {allLayersHit.collider.name} on layer {allLayersHit.collider.gameObject.layer}, LayerMask: {groundLayerMask.value}");
                    }
                }
            }
            
            // Debug thêm thông tin - giảm tần suất
            if (Time.frameCount % 120 == 0) // Log mỗi 2 giây
            {
                Debug.Log($"IsGrounded: {IsGrounded}, Position Y: {transform.position.y:F2}, Velocity Y: {rb.linearVelocity.y:F2}, RB BodyType: {rb.bodyType}");
            }
        }
        
        /// <summary>
        /// Xử lý di chuyển ngang và lật nhân vật
        /// </summary>
        private void HandleMovement()
        {
            // Kiểm tra Rigidbody2D constraints
            if (rb == null)
            {
                Debug.LogError("Rigidbody2D is null!");
                return;
            }
            
            // Kiểm tra nếu position bị freeze
            if ((rb.constraints & RigidbodyConstraints2D.FreezePositionX) != 0)
            {
                Debug.LogWarning("X position is frozen in Rigidbody2D constraints!");
                rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Chỉ freeze rotation
            }
            
            // Di chuyển ngang
            Vector2 velocity = rb.linearVelocity;
            Vector2 oldVelocity = velocity;
            velocity.x = horizontalInput * moveSpeed;
            rb.linearVelocity = velocity;
            
            // Debug movement
            if (horizontalInput != 0)
            {
                Debug.Log($"Movement - Input: {horizontalInput}, OldVel: {oldVelocity.x}, NewVel: {velocity.x}, Position: {transform.position.x}");
            }
            
            // Lật nhân vật theo hướng di chuyển
            if (horizontalInput > 0 && !facingRight)
            {
                Flip();
            }
            else if (horizontalInput < 0 && facingRight)
            {
                Flip();
            }
        }
        
        /// <summary>
        /// Xử lý nhảy đơn giản
        /// </summary>
        private void HandleJump()
        {
            // Chỉ nhảy khi đang trên mặt đất
            if (IsGrounded && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
            {
                Jump();
            }
        }
        
        /// <summary>
        /// Thực hiện nhảy
        /// </summary>
        private void Jump()
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.y = jumpForce;
            rb.linearVelocity = velocity;
        }
        
        /// <summary>
        /// Lật nhân vật
        /// </summary>
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
        
        /// <summary>
        /// Reset nhân vật về trạng thái ban đầu
        /// </summary>
        public void ResetPlayer()
        {
            IsAlive = true;
            horizontalInput = 0f;
            
            // Reset velocity
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            
            // Reset facing direction
            facingRight = true;
            transform.localScale = originalScale;
            
            // Reset position to safe location
            Vector3 resetPos = new Vector3(0f, 2f, transform.position.z); // Đặt ở Y = 2 thay vì giữ nguyên Y hiện tại
            transform.position = resetPos;
            
            Debug.Log($"Player reset to position: {resetPos}");
        }
        
        /// <summary>
        /// Vẽ gizmos để debug
        /// </summary>
        private void OnDrawGizmos()
        {
            // Vẽ ground check
            if (groundCheck != null)
            {
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
                
                // Vẽ thêm line để thấy rõ hơn
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, groundCheck.position);
            }
            else
            {
                // Vẽ raycast fallback
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Vector3 rayStart = new Vector3(transform.position.x, transform.position.y - 0.1f, transform.position.z);
                Vector3 rayEnd = rayStart + Vector3.down * 0.6f;
                Gizmos.DrawLine(rayStart, rayEnd);
                
                // Vẽ điểm bắt đầu ray
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(rayStart, 0.05f);
            }
            
            // Vẽ velocity vector để debug chuyển động
            if (Application.isPlaying && rb != null)
            {
                Gizmos.color = Color.magenta;
                Vector3 velocityEnd = transform.position + new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0) * 0.1f;
                Gizmos.DrawLine(transform.position, velocityEnd);
            }
        }
        
        #region Public Methods for External Control
        
        /// <summary>
        /// Di chuyển trái (có thể gọi từ UI hoặc touch input)
        /// </summary>
        public void MoveLeft()
        {
            if (IsAlive) horizontalInput = -1f;
        }
        
        /// <summary>
        /// Di chuyển phải (có thể gọi từ UI hoặc touch input)
        /// </summary>
        public void MoveRight()
        {
            if (IsAlive) horizontalInput = 1f;
        }
        
        /// <summary>
        /// Dừng di chuyển (có thể gọi từ UI hoặc touch input)
        /// </summary>
        public void StopMovement()
        {
            horizontalInput = 0f;
        }
        
        /// <summary>
        /// Nhảy (có thể gọi từ UI hoặc touch input)
        /// </summary>
        public void JumpAction()
        {
            if (IsAlive && IsGrounded)
            {
                Jump();
            }
        }
        

        
        #endregion

        #region Missing Methods - Các hàm mà scripts khác đang tìm kiếm

        /// <summary>
        /// Giết nhân vật
        /// </summary>
        public void Die()
        {
            if (!IsAlive) return;

            IsAlive = false;

            // Dừng chuyển động
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            // Gọi event OnDeath
            OnDeath?.Invoke();
        }
        
        /// <summary>
        /// Xử lý thu thập coin
        /// </summary>
        /// <param name="coinValue">Giá trị của coin</param>
        public void CollectCoin(int coinValue = 10)
        {
            if (!IsAlive) return;
            
            // Gọi event OnCoinCollected
            OnCoinCollected?.Invoke(coinValue);
        }
        
        /// <summary>
        /// Xử lý thu thập power-up
        /// </summary>
        /// <param name="powerUpType">Loại power-up</param>
        public void CollectPowerUp(string powerUpType)
        {
            if (!IsAlive) return;
            
            // Gọi event OnPowerUpCollected
            OnPowerUpCollected?.Invoke(powerUpType);
        }
        
        /// <summary>
        /// Xử lý va chạm với trigger
        /// </summary>
        /// <param name="other">Collider va chạm</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsAlive) return;
            
            switch (other.tag)
            {
                case "Coin":
                    CollectCoin(10);
                    Destroy(other.gameObject);
                    break;
                    
                case "PowerUp":
                    CollectPowerUp(other.name);
                    Destroy(other.gameObject);
                    break;
                    
                case "Obstacle":
                case "Enemy":
                case "Catcher":
                    Die();
                    break;
            }
        }
        
        /// <summary>
        /// Xử lý va chạm với collider
        /// </summary>
        /// <param name="collision">Collision data</param>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsAlive) return;
            
            switch (collision.gameObject.tag)
            {
                case "Obstacle":
                case "Enemy":
                case "Catcher":
                    Die();
                    break;
            }
        }
        
        #endregion
    }
}
