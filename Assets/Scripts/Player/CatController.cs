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
        
        [Header("Survival Mode Settings")]
        [SerializeField] private bool enableSurvivalMode = true; // Yêu cầu giữ D để chống lại drift
        [SerializeField] private float backwardDriftForce = 3f; // Lực kéo về phía sau
        [SerializeField] private float worldDriftSpeed = 2f; // Tốc độ trôi theo thế giới khi không input
        [SerializeField] private float baseFriction = 2f; // Ma sát cơ bản
        [SerializeField] private float highFriction = 5f; // Ma sát cao khi không bấm D
        [SerializeField] private bool autoRun = false; // Tắt auto run
        
        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.3f;
        [SerializeField] private LayerMask groundLayerMask = 1;
        
        [Header("Character Flipping")]
        [SerializeField] private bool facingRight = true;
        
        [Header("Knockback Settings")]
        [SerializeField] private float knockbackForce = 15f; // Lực đẩy khi chạm catcher
        [SerializeField] private float knockbackUpwardForce = 5f; // Lực đẩy lên trên
        [SerializeField] private float knockbackDuration = 0.3f; // Thời gian bị knockback
        [SerializeField] private bool enableKnockbackDebug = true;
        
        // Components
        private Rigidbody2D rb;
        private Vector3 originalScale;
        private Animator animator;
        
        // State
        public bool IsGrounded { get; private set; }
        public bool IsAlive { get; private set; } = true;
        
        // Knockback state
        private bool isKnockedBack = false;
        private float knockbackTimer = 0f;
        private Vector2 knockbackVelocity = Vector2.zero;
        
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
            // Trong endless runner, player luôn chạy nên IsRunning luôn true
            bool isRunning = autoRun || Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            bool isJumping = !IsGrounded;
            if (animator != null)
            {
                animator.SetBool("IsRunning", isRunning);
                animator.SetBool("IsJumping", isJumping);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Obstacle"))
            {
                GameManager.Instance.GameOver();
            }
            else if (other.CompareTag("ColectItem"))
            {
                Destroy(other.gameObject);       
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

            HandleKnockback(); // Handle knockback first
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
        /// Xử lý knockback khi bị catcher chạm
        /// </summary>
        private void HandleKnockback()
        {
            if (!isKnockedBack) return;
            
            knockbackTimer -= Time.deltaTime;
            
            if (knockbackTimer <= 0f)
            {
                // Kết thúc knockback
                isKnockedBack = false;
                knockbackVelocity = Vector2.zero;
                
                if (enableKnockbackDebug)
                {
                    Debug.Log("[CatController] 🔄 Knockback ended - player can move normally");
                }
            }
            else
            {
                // Áp dụng knockback velocity
                if (rb != null)
                {
                    // Giảm dần knockback theo thời gian
                    float knockbackProgress = knockbackTimer / knockbackDuration;
                    Vector2 currentKnockback = knockbackVelocity * knockbackProgress;
                    
                    rb.linearVelocity = new Vector2(currentKnockback.x, rb.linearVelocity.y + currentKnockback.y);
                    
                    if (enableKnockbackDebug && Time.frameCount % 30 == 0)
                    {
                        Debug.Log($"[CatController] Knockback active - Time left: {knockbackTimer:F2}s, Velocity: {currentKnockback}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Xử lý di chuyển ngang với backward drift survival mechanics
        /// </summary>
        private void HandleMovement()
        {
            // Không di chuyển nếu đang bị knockback
            if (isKnockedBack)
            {
                if (enableKnockbackDebug && Time.frameCount % 60 == 0)
                {
                    Debug.Log("[CatController] Movement disabled during knockback");
                }
                return;
            }
            
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
            
            // FORCE CLEAR: Đảm bảo không có constraints nào khác
            if (rb.constraints != RigidbodyConstraints2D.FreezeRotation)
            {
                Debug.LogWarning($"Clearing unexpected constraints: {rb.constraints}");
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            
            // Di chuyển ngang với survival mechanics
            Vector2 velocity = rb.linearVelocity;
            Vector2 oldVelocity = velocity;
            
            // Kiểm tra input states
            bool movingRight = horizontalInput > 0;
            bool movingLeft = horizontalInput < 0;
            bool noInput = horizontalInput == 0;
            
            if (enableSurvivalMode && IsAlive)
            {
                // SURVIVAL MODE: Mèo trôi theo thế giới + backward drift
                if (movingRight)
                {
                    // Player đang cố gắng chạy về phải - chống lại world drift và backward drift
                    velocity.x = horizontalInput * moveSpeed;
                    rb.linearDamping = Mathf.Lerp(rb.linearDamping, baseFriction, Time.deltaTime * 3f);
                }
                else if (movingLeft)
                {
                    // Player đang chạy về trái - cho phép nhưng với friction cao hơn
                    velocity.x = horizontalInput * moveSpeed;
                    rb.linearDamping = Mathf.Lerp(rb.linearDamping, baseFriction * 1.5f, Time.deltaTime * 3f);
                }
                else
                {
                    // Không có input - mèo trôi theo thế giới + backward drift force
                    
                    // Kiểm tra xem có FixedBackgroundManager không để biết ground có di chuyển không
                    var fixedBgManager = FindFirstObjectByType<CatchMeowIfYouCan.Environment.FixedBackgroundManager>();
                    bool hasFixedBackground = fixedBgManager != null;
                    
                    if (hasFixedBackground)
                    {
                        // Với fixed background: ground di chuyển, mèo cần trôi cùng + backward drift
                        velocity.x = -worldDriftSpeed - backwardDriftForce;
                    }
                    else
                    {
                        // Không có fixed background: chỉ backward drift
                        float currentVelX = velocity.x;
                        velocity.x = currentVelX - (backwardDriftForce * Time.deltaTime);
                    }
                    
                    // Tăng friction để tạo cảm giác "bị kéo lùi"
                    rb.linearDamping = Mathf.Lerp(rb.linearDamping, highFriction, Time.deltaTime * 3f);
                    
                    // REMOVED: Giới hạn tốc độ drift - này ngăn mèo trôi đến boundary để trigger catcher
                    // velocity.x = Mathf.Max(velocity.x, -moveSpeed * 0.8f);
                }
            }
            else
            {
                // Normal movement mode (không survival)
                velocity.x = horizontalInput * moveSpeed;
                rb.linearDamping = baseFriction;
            }
            
            rb.linearVelocity = velocity;
            
            // Debug movement với thông tin về world drift
            if (horizontalInput != 0 || enableSurvivalMode)
            {
                string survivalInfo = "";
                if (enableSurvivalMode)
                {
                    var fixedBgManager = FindFirstObjectByType<CatchMeowIfYouCan.Environment.FixedBackgroundManager>();
                    bool hasFixedBackground = fixedBgManager != null;
                    
                    string inputState = movingRight ? "FIGHTING DRIFT" : (movingLeft ? "LEFT" : "WORLD DRIFT");
                    string driftInfo = noInput ? $"WorldDrift: {worldDriftSpeed:F1}, BackDrift: {backwardDriftForce:F1}" : "";
                    survivalInfo = $"Survival: {enableSurvivalMode}, State: {inputState}, FixedBG: {hasFixedBackground}, {driftInfo}, Friction: {rb.linearDamping:F1}";
                }
                
                // ENHANCED DEBUG: Thêm thông tin về position và boundary checking  
                string boundaryInfo = $"Pos: {transform.position.x:F1}, Vel: {velocity.x:F1}";
                if (velocity.x < -3f)
                {
                    Debug.LogWarning($"DRIFTING LEFT FAST! {boundaryInfo}, {survivalInfo}");
                }
                else
                {
                    Debug.Log($"Movement - Input: {horizontalInput:F1}, {survivalInfo}, {boundaryInfo}");
                }
            }
            
            // Lật nhân vật theo hướng di chuyển manual
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
        
        /// <summary>
        /// Đẩy mèo ra xa khi chạm catcher - gọi từ CatcherController
        /// </summary>
        /// <param name="catcherPosition">Vị trí của catcher để tính hướng đẩy</param>
        /// <param name="forceMultiplier">Hệ số nhân lực đẩy (optional)</param>
        public void ApplyKnockback(Vector3 catcherPosition, float forceMultiplier = 1f)
        {
            if (!IsAlive || rb == null) return;
            
            // Tính hướng đẩy (từ catcher ra xa)
            Vector2 knockbackDirection = (transform.position - catcherPosition).normalized;
            
            // Đảm bảo có component ngang và hơi lên trên
            if (Mathf.Abs(knockbackDirection.x) < 0.3f)
            {
                // Nếu catcher ở thẳng trên/dưới, đẩy về phía player đang quay mặt
                knockbackDirection.x = facingRight ? 1f : -1f;
            }
            
            // Tính toán lực đẩy
            Vector2 finalKnockbackForce = new Vector2(
                knockbackDirection.x * knockbackForce * forceMultiplier,
                knockbackUpwardForce * forceMultiplier
            );
            
            // Áp dụng knockback
            isKnockedBack = true;
            knockbackTimer = knockbackDuration;
            knockbackVelocity = finalKnockbackForce;
            
            // Áp dụng lực ngay lập tức
            rb.linearVelocity = new Vector2(finalKnockbackForce.x, rb.linearVelocity.y + finalKnockbackForce.y);
            
            if (enableKnockbackDebug)
            {
                Debug.Log($"[CatController] 💥 KNOCKBACK APPLIED! Direction: {knockbackDirection}, Force: {finalKnockbackForce}, Duration: {knockbackDuration}s");
                Debug.Log($"[CatController] Catcher at: {catcherPosition}, Cat at: {transform.position}");
            }
        }
        
        /// <summary>
        /// Kiểm tra xem có đang bị knockback không
        /// </summary>
        public bool IsKnockedBack => isKnockedBack;
        

        
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
        
        /// <summary>
        /// Test: Disable ALL scripts that might interfere
        /// </summary>
        [ContextMenu("Test Disable All Interfering Scripts")]
        public void TestDisableAllInterferingScripts()
        {
            Debug.LogError("DISABLING ALL POTENTIAL INTERFERING SCRIPTS...");
            
            // Disable FixedBackgroundManager
            var fixedBgManager = FindFirstObjectByType<CatchMeowIfYouCan.Environment.FixedBackgroundManager>();
            if (fixedBgManager != null)
            {
                fixedBgManager.enabled = false;
                Debug.LogError("Disabled FixedBackgroundManager");
            }
            
            // Disable EndlessRunManager  
            var endlessRunManager = FindFirstObjectByType<CatchMeowIfYouCan.Environment.EndlessRunManager>();
            if (endlessRunManager != null)
            {
                endlessRunManager.enabled = false;
                Debug.LogError("Disabled EndlessRunManager");
            }
            
            // Disable BackgroundScroller
            var backgroundScroller = FindFirstObjectByType<CatchMeowIfYouCan.Environment.BackgroundScroller>();
            if (backgroundScroller != null)
            {
                backgroundScroller.enabled = false;
                Debug.LogError("Disabled BackgroundScroller");
            }
            
            // Force clear constraints
            rb.constraints = RigidbodyConstraints2D.None;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            
            // Disable survival mode
            enableSurvivalMode = false;
            
            Debug.LogError("ALL SCRIPTS DISABLED - Now testing movement...");
            
            // Test movement
            rb.linearVelocity = new Vector2(-10f, 0f);
            Invoke(nameof(CheckFreeMovementResult), 1f);
        }
        
        private void CheckFreeMovementResult()
        {
            Debug.LogError($"FREE MOVEMENT RESULT - Position: {transform.position}, Velocity: {rb.linearVelocity}");
            if (rb.linearVelocity.x > -5f)
            {
                Debug.LogError("MOVEMENT IS STILL BLOCKED! There must be a hidden Unity constraint or collider!");
            }
            else
            {
                Debug.Log("Movement is working - scripts were interfering");
            }
        }
        [ContextMenu("Test Force Teleport Left")]
        public void TestForceTeleportLeft()
        {
            Debug.LogError("TESTING: Force teleporting cat far left...");
            
            // Disable physics
            rb.bodyType = RigidbodyType2D.Kinematic;
            
            // Teleport directly  
            transform.position = new Vector3(-20f, transform.position.y, transform.position.z);
            
            Debug.LogError($"TELEPORTED! New position: {transform.position}");
            
            // Re-enable physics
            rb.bodyType = RigidbodyType2D.Dynamic;
            
            // Check if position stays
            Invoke(nameof(CheckTeleportResult), 0.1f);
        }
        
        private void CheckTeleportResult()
        {
            Debug.LogError($"TELEPORT RESULT - Position after 0.1s: {transform.position}");
            if (transform.position.x > -15f)
            {
                Debug.LogError("TELEPORT FAILED! Something moved the cat back! There's definitely a constraint!");
            }
            else
            {
                Debug.Log("Teleport successful - cat stayed in new position");
            }
        }
        [ContextMenu("Test Force Move Left")]
        public void TestForceMoveLeft()
        {
            if (rb != null)
            {
                Debug.LogWarning("TESTING: Forcing cat to move left rapidly...");
                
                // Tắt tất cả logic có thể can thiệp
                enableSurvivalMode = false;
                
                // Force velocity trực tiếp
                rb.linearVelocity = new Vector2(-15f, rb.linearVelocity.y);
                
                // Disable drag
                rb.linearDamping = 0f;
                
                Debug.LogError($"Applied velocity: {rb.linearVelocity}, Position: {transform.position}, Drag: {rb.linearDamping}");
                
                // Invoke sau 1 giây để check kết quả
                Invoke(nameof(CheckTestResult), 1f);
            }
        }
        
        private void CheckTestResult()
        {
            Debug.LogError($"TEST RESULT - Position: {transform.position}, Velocity: {rb.linearVelocity}");
            if (transform.position.x > -5f)
            {
                Debug.LogError("CAT IS STILL BLOCKED! Something is preventing leftward movement!");
            }
            else
            {
                Debug.Log("Cat moved left successfully!");
            }
        }
        
        /// <summary>
        /// Test: Disable tất cả boundary logic
        /// </summary>
        [ContextMenu("Test Disable All Boundaries")]
        public void TestDisableAllBoundaries()
        {
            var fixedBgManager = FindFirstObjectByType<CatchMeowIfYouCan.Environment.FixedBackgroundManager>();
            if (fixedBgManager != null)
            {
                fixedBgManager.SetBackgroundLocked(false);
                fixedBgManager.SetCameraLocked(false);
                Debug.LogWarning("Disabled all FixedBackgroundManager boundaries!");
            }
            
            // Tắt survival mode tạm thời để test
            enableSurvivalMode = false;
            Debug.LogWarning("Disabled survival mode for testing!");
        }
        
        /// <summary>
        /// Debug player movement state
        /// </summary>
        [ContextMenu("Debug Movement State")]
        public void DebugMovementState()
        {
            Debug.Log("=== CAT WORLD DRIFT SURVIVAL DEBUG ===");
            Debug.Log($"Survival Mode: {enableSurvivalMode}");
            Debug.Log($"World Drift Speed: {worldDriftSpeed}");
            Debug.Log($"Backward Drift Force: {backwardDriftForce}");
            Debug.Log($"Base Friction: {baseFriction}");
            Debug.Log($"High Friction: {highFriction}");
            Debug.Log($"Move Speed: {moveSpeed}");
            Debug.Log($"Is Alive: {IsAlive}");
            Debug.Log($"Is Grounded: {IsGrounded}");
            
            if (rb != null)
            {
                Debug.Log($"Velocity: {rb.linearVelocity}");
                Debug.Log($"Current Drag: {rb.linearDamping}");
                Debug.Log($"Constraints: {rb.constraints}");
            }
            
            var fixedBgManager = FindFirstObjectByType<CatchMeowIfYouCan.Environment.FixedBackgroundManager>();
            Debug.Log($"Has Fixed Background: {fixedBgManager != null}");
            
            Debug.Log($"Current Input: H={horizontalInput}");
            Debug.Log($"Position: {transform.position}");
            
            // Survival status with world drift info
            bool movingRight = horizontalInput > 0;
            bool movingLeft = horizontalInput < 0;
            bool noInput = horizontalInput == 0;
            
            string survivalStatus;
            if (enableSurvivalMode)
            {
                if (movingRight)
                    survivalStatus = "FIGHTING WORLD + BACKWARD DRIFT";
                else if (movingLeft)
                    survivalStatus = "MOVING LEFT (HIGH FRICTION)";
                else
                    survivalStatus = fixedBgManager != null ? "DRIFTING WITH WORLD + BACKWARD" : "BACKWARD DRIFT ONLY";
            }
            else
            {
                survivalStatus = "NORMAL MODE";
            }
            
            Debug.Log($"Survival Status: {survivalStatus}");
        }
        
        /// <summary>
        /// Toggle survival mode for testing
        /// </summary>
        [ContextMenu("Toggle Survival Mode")]
        public void ToggleSurvivalMode()
        {
            enableSurvivalMode = !enableSurvivalMode;
            Debug.Log($"Survival Mode: {(enableSurvivalMode ? "ENABLED" : "DISABLED")}");
            
            if (rb != null)
            {
                // Reset friction to base level when toggling
                rb.linearDamping = baseFriction;
            }
        }
        
        /// <summary>
        /// Test backward drift - should show backward movement
        /// </summary>
        [ContextMenu("Test Backward Drift")]
        public void TestBackwardDrift()
        {
            Debug.Log("=== TESTING BACKWARD DRIFT ===");
            Debug.Log("Simulating no input for 5 seconds - cat should drift backward...");
            
            StartCoroutine(SimulateInput(0f, 5f));
        }
        
        /// <summary>
        /// Test fighting against drift
        /// </summary>
        [ContextMenu("Test Fight Drift")]
        public void TestFightDrift()
        {
            Debug.Log("=== TESTING FIGHT DRIFT ===");
            Debug.Log("Simulating holding D for 3 seconds to fight backward drift...");
            
            StartCoroutine(SimulateInput(1f, 3f));
        }
        
        /// <summary>
        /// Test manual control override
        /// </summary>
        [ContextMenu("Test Left Movement")]
        public void TestLeftMovement()
        {
            Debug.Log("=== TESTING LEFT MOVEMENT ===");
            Debug.Log("Simulating left input for 2 seconds...");
            
            StartCoroutine(SimulateInput(-1f, 2f));
        }
        
        private System.Collections.IEnumerator SimulateInput(float inputValue, float duration)
        {
            float originalInput = horizontalInput;
            float startTime = Time.time;
            
            while (Time.time - startTime < duration)
            {
                horizontalInput = inputValue;
                yield return null;
            }
            
            horizontalInput = originalInput;
            Debug.Log("Input simulation completed");
        }
        
        [ContextMenu("Test Knockback Right")]
        public void TestKnockbackRight()
        {
            Vector3 fakeCatcherPosition = transform.position + Vector3.left * 2f; // Catcher ở bên trái
            ApplyKnockback(fakeCatcherPosition, 1f);
            Debug.Log("[CatController] 💥 Testing knockback to the RIGHT");
        }
        
        [ContextMenu("Test Knockback Left")]
        public void TestKnockbackLeft()
        {
            Vector3 fakeCatcherPosition = transform.position + Vector3.right * 2f; // Catcher ở bên phải
            ApplyKnockback(fakeCatcherPosition, 1f);
            Debug.Log("[CatController] 💥 Testing knockback to the LEFT");
        }
        
        [ContextMenu("Test Strong Knockback")]
        public void TestStrongKnockback()
        {
            Vector3 fakeCatcherPosition = transform.position + Vector3.left * 1f; // Catcher gần bên trái
            ApplyKnockback(fakeCatcherPosition, 2f); // 2x force
            Debug.Log("[CatController] 💥💥 Testing STRONG knockback!");
        }
        
        #endregion
    }
}
