using UnityEngine;
using Debug = UnityEngine.Debug;
using CatchMeowIfYouCan.Effects;
using CatchMeowIfYouCan.Player;

namespace CatchMeowIfYouCan.Enemies
{
    /// <summary>
    /// Catcher thông minh - xuất hiện khi mèo gần boundary, chase 1 lần, retreat nếu mèo thoát
    /// Logic: Hidden -> Triggered -> Chasing -> (Success/Retreat)
    /// </summary>
    public class CatcherController : MonoBehaviour
    {
        [Header("Catcher References")]
        [SerializeField] private Transform catTarget; // Reference đến mèo
        [SerializeField] private Camera gameCamera; // Camera để tính boundary
        [SerializeField] private TouchCheck touchCheck; // TouchCheck component để detect collision với mèo
        
        [Header("Position Settings")]
        [SerializeField] private Vector3 hiddenPosition = new Vector3(0, -10f, 0); // Vị trí ẩn ngoài camera
        [SerializeField] private Vector3 activePosition = new Vector3(0, 0, 0); // Vị trí xuất hiện
        [SerializeField] private float hiddenOffset = 3f; // Khoảng cách ẩn dưới camera
        
        [Header("Boundary Detection")]
        [SerializeField] private float boundaryTriggerDistance = 1f; // Khoảng cách từ boundary để trigger (giảm từ 2f xuống 1f để catcher xuất hiện sát hơn)
        [SerializeField] private BoundaryDirection triggerBoundary = BoundaryDirection.Left; // Boundary nào trigger catcher
        
        [Header("Movement Settings")]
        [SerializeField] private float riseSpeed = 15f; // Tốc độ xuất hiện (tăng từ 8f lên 15f để xuất hiện nhanh hơn)
        [SerializeField] private float chaseSpeed = 8f; // Tốc độ chase mèo (tăng từ 6f lên 8f)
        [SerializeField] private float retreatSpeed = 10f; // Tốc độ lùi về (tăng từ 4f lên 10f để về nhanh hơn)
        
        [Header("Chase Logic")]
        [SerializeField] private float touchDetectionRadius = 1.5f; // Bán kính để phát hiện touch
        [SerializeField] private float escapeDistance = 4f; // Khoảng cách mèo cần thoát để catcher retreat
        [SerializeField] private float chaseTimeout = 8f; // Thời gian tối đa chase (giảm xuống 8s)
        [SerializeField] private float triggerCooldown = 0.5f; // Cooldown giữa các lần trigger (0.5 giây)
        [SerializeField] private float maxChaseDistance = 6f; // Khoảng cách tối đa chase trước khi retreat
        
        [Header("Visual Settings")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool enableDebugLogs = true;
        
        // State management
        public enum CatcherState
        {
            Hidden,        // Ẩn ngoài camera
            Rising,        // Đang xuất hiện
            Chasing,       // Đang chase mèo
            Retreating,    // Đang lùi về
            Success        // Đã bắt được mèo
        }
        
        public enum BoundaryDirection
        {
            Left,
            Right,
            Top,
            Bottom
        }
        
        [Header("State Debug")]
        [SerializeField] private CatcherState currentState = CatcherState.Hidden;
        [SerializeField] private float chaseTimer = 0f;
        [SerializeField] private float cooldownTimer = 0f; // Timer cho cooldown
        [SerializeField] private Vector3 initialCatPosition; // Vị trí mèo khi bắt đầu chase
        
        // Events
        public System.Action<CatcherController> OnCatcherTriggered;
        public System.Action<CatcherController> OnCatTouched;
        public System.Action<CatcherController> OnCatCaught;
        public System.Action<CatcherController> OnCatEscaped;
        
        // Internal references
        private Vector3 targetPosition;
        private Vector3 originalHiddenPosition;
        private CatchMeowIfYouCan.Player.CatController catController;
        
        private void Start()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            if (enableDebugLogs) Debug.Log($"[CatcherController] Initializing {gameObject.name}");
            
            // Find cat if not assigned
            if (catTarget == null)
            {
                GameObject catObj = GameObject.FindWithTag("Player");
                if (catObj != null)
                {
                    catTarget = catObj.transform;
                    catController = catObj.GetComponent<CatchMeowIfYouCan.Player.CatController>();
                    if (enableDebugLogs) Debug.Log($"[CatcherController] Auto-found cat: {catObj.name}");
                }
                else
                {
                    Debug.LogError($"[CatcherController] {gameObject.name}: Cannot find cat with 'Player' tag!");
                }
            }
            
            // Find camera if not assigned
            if (gameCamera == null)
            {
                gameCamera = Camera.main;
                if (gameCamera == null)
                {
                    gameCamera = FindFirstObjectByType<Camera>();
                }
                if (gameCamera != null)
                {
                    if (enableDebugLogs) Debug.Log($"[CatcherController] Auto-found camera: {gameCamera.name}");
                }
                else
                {
                    Debug.LogError($"[CatcherController] {gameObject.name}: Cannot find camera!");
                }
            }
            
            // Calculate initial hidden position based on camera
            CalculateHiddenPosition();
            
            // Set initial state
            SetState(CatcherState.Hidden);
            transform.position = hiddenPosition;
            originalHiddenPosition = hiddenPosition;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[CatcherController] Initialized - Hidden: {hiddenPosition}, Active: {activePosition}");
                Debug.Log($"[CatcherController] Trigger boundary: {triggerBoundary}, Distance: {boundaryTriggerDistance}");
                Debug.Log($"[CatcherController] Cat target: {(catTarget != null ? catTarget.name : "NULL")}");
                Debug.Log($"[CatcherController] Camera: {(gameCamera != null ? gameCamera.name : "NULL")}");
            }
        }
        
        private void CalculateHiddenPosition()
        {
            if (gameCamera == null) return;
            
            Vector3 cameraPos = gameCamera.transform.position;
            float cameraHeight = 2f * gameCamera.orthographicSize;
            float cameraWidth = cameraHeight * gameCamera.aspect;
            
            // Calculate hidden position based on trigger boundary
            switch (triggerBoundary)
            {
                case BoundaryDirection.Left:
                    hiddenPosition = new Vector3(cameraPos.x - (cameraWidth * 0.5f) - hiddenOffset, activePosition.y, activePosition.z);
                    break;
                case BoundaryDirection.Right:
                    hiddenPosition = new Vector3(cameraPos.x + (cameraWidth * 0.5f) + hiddenOffset, activePosition.y, activePosition.z);
                    break;
                case BoundaryDirection.Bottom:
                    hiddenPosition = new Vector3(activePosition.x, cameraPos.y - (cameraHeight * 0.5f) - hiddenOffset, activePosition.z);
                    break;
                case BoundaryDirection.Top:
                    hiddenPosition = new Vector3(activePosition.x, cameraPos.y + (cameraHeight * 0.5f) + hiddenOffset, activePosition.z);
                    break;
            }
        }
        
        private void CalculateActivePosition()
        {
            if (gameCamera == null || catTarget == null) return;
            
            Vector3 cameraPos = gameCamera.transform.position;
            float cameraHeight = 2f * gameCamera.orthographicSize;
            float cameraWidth = cameraHeight * gameCamera.aspect;
            Vector3 catPos = catTarget.position;
            
            // Calculate active position based on trigger boundary và vị trí mèo
            switch (triggerBoundary)
            {
                case BoundaryDirection.Left:
                    // Xuất hiện từ bên trái, gần với mèo
                    activePosition = new Vector3(catPos.x + 1f, catPos.y, 0);
                    break;
                case BoundaryDirection.Right:
                    // Xuất hiện từ bên phải, gần với mèo
                    activePosition = new Vector3(catPos.x - 1f, catPos.y, 0);
                    break;
                case BoundaryDirection.Bottom:
                    // Xuất hiện từ dưới, gần với mèo
                    activePosition = new Vector3(catPos.x, catPos.y + 1f, 0);
                    break;
                case BoundaryDirection.Top:
                    // Xuất hiện từ trên, gần với mèo
                    activePosition = new Vector3(catPos.x, catPos.y - 1f, 0);
                    break;
            }
            
            if (enableDebugLogs) Debug.Log($"[CatcherController] Calculated active position: {activePosition} based on cat at: {catPos}");
        }
        
        private void Update()
        {
            // Enhanced null checks with debug logging
            if (catTarget == null)
            {
                if (enableDebugLogs && Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
                {
                    UnityEngine.Debug.LogWarning($"[CatcherController] {gameObject.name}: Cat target is NULL!");
                }
                return;
            }
            
            if (gameCamera == null)
            {
                if (enableDebugLogs && Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
                {
                    UnityEngine.Debug.LogWarning($"[CatcherController] {gameObject.name}: Game camera is NULL!");
                }
                return;
            }
            
            // Check if game is paused (Time.timeScale)
            if (Time.timeScale == 0f) return;
            
            UpdateStateMachine();
            UpdateMovement();
            UpdateChaseLogic();
            UpdateCooldownTimer();
            
            // Debug logging for boundary detection
            if (enableDebugLogs && currentState == CatcherState.Hidden && Time.frameCount % 120 == 0) // Every 2 seconds
            {
                DebugBoundaryStatus();
            }
        }
        
        private void DebugBoundaryStatus()
        {
            if (catTarget == null || gameCamera == null) return;
            
            Vector3 catPos = catTarget.position;
            Vector3 cameraPos = gameCamera.transform.position;
            float cameraHeight = 2f * gameCamera.orthographicSize;
            float cameraWidth = cameraHeight * gameCamera.aspect;
            
            float leftBoundary = cameraPos.x - (cameraWidth * 0.5f);
            float distanceToLeft = catPos.x - leftBoundary;
            
            if (triggerBoundary == BoundaryDirection.Left)
            {
                bool shouldTrigger = distanceToLeft <= boundaryTriggerDistance;
                UnityEngine.Debug.Log($"[CatcherController] {gameObject.name}: Cat distance to LEFT boundary: {distanceToLeft:F2}, Should trigger: {shouldTrigger} (trigger distance: {boundaryTriggerDistance})");
            }
        }
        
        private void UpdateStateMachine()
        {
            switch (currentState)
            {
                case CatcherState.Hidden:
                    // Debug Hidden state mỗi 3 giây để theo dõi
                    if (enableDebugLogs && Time.frameCount % 180 == 0) // Every 3 seconds
                    {
                        Debug.Log($"[CatcherController] Hidden state - Cooldown: {cooldownTimer:F2}s, Checking boundary trigger...");
                    }
                    CheckBoundaryTrigger();
                    break;
                    
                case CatcherState.Rising:
                    chaseTimer += Time.deltaTime; // Bắt đầu đếm thời gian từ khi rising
                    float distanceToActive = Vector3.Distance(transform.position, activePosition);
                    if (enableDebugLogs && Time.frameCount % 60 == 0) // Debug every second
                    {
                        Debug.Log($"[CatcherController] Rising... Distance to active: {distanceToActive:F2}, Current pos: {transform.position}, Active pos: {activePosition}");
                    }
                    
                    if (distanceToActive < 0.5f || chaseTimer > 2f) // Tăng threshold hoặc timeout sau 2s
                    {
                        transform.position = activePosition; // Force exact position
                        SetState(CatcherState.Chasing);
                        if (enableDebugLogs) Debug.Log($"[CatcherController] Switched to Chasing state");
                    }
                    break;
                    
                case CatcherState.Chasing:
                    chaseTimer += Time.deltaTime; // Đảm bảo chase timer tăng
                    CheckTouchDetection();
                    CheckChaseTimeout();
                    break;
                    
                case CatcherState.Retreating:
                    float distanceToHidden = Vector3.Distance(transform.position, hiddenPosition);
                    
                    // Debug retreat chi tiết mỗi giây
                    if (enableDebugLogs && Time.frameCount % 60 == 0) // Debug every second
                    {
                        Debug.Log($"[CatcherController] 🔄 RETREATING... Distance: {distanceToHidden:F2}, Timer: {chaseTimer:F2}s");
                        Debug.Log($"[CatcherController] Current: {transform.position}, Hidden: {hiddenPosition}");
                        Debug.Log($"[CatcherController] Threshold: 0.5f, Should stop: {distanceToHidden < 0.5f}");
                    }
                    
                    // Multiple conditions để đảm bảo luôn escape khỏi Retreating state
                    bool shouldStopRetreating = distanceToHidden < 2f || // Tăng threshold lên 2f cho trường hợp position sai
                                              chaseTimer > 10f || // Giảm timeout xuống 10 giây
                                              Vector3.Distance(transform.position, hiddenPosition) < 3f; // Backup condition lớn hơn
                    
                    if (shouldStopRetreating)
                    {
                        transform.position = hiddenPosition; // Force exact position
                        if (enableDebugLogs) Debug.Log($"[CatcherController] 🎯 FORCING POSITION TO HIDDEN: {hiddenPosition}");
                        SetState(CatcherState.Hidden);
                        ResetCatcher();
                        if (enableDebugLogs) Debug.Log($"[CatcherController] ✅ SUCCESSFULLY RETURNED TO HIDDEN - STATE CHANGED!");
                    }
                    break;
                    
                case CatcherState.Success:
                    // Game over state - handled by external systems
                    break;
            }
        }
        
        private void UpdateCooldownTimer()
        {
            if (cooldownTimer > 0f)
            {
                float previousCooldown = cooldownTimer;
                cooldownTimer -= Time.deltaTime;
                
                // Debug cooldown countdown mỗi 0.1s để theo dõi
                if (enableDebugLogs && Time.frameCount % 6 == 0) // Every ~0.1s at 60fps
                {
                    Debug.Log($"[CatcherController] Cooldown: {cooldownTimer:F2}s remaining...");
                }
                
                if (cooldownTimer <= 0f)
                {
                    cooldownTimer = 0f;
                    if (enableDebugLogs) Debug.Log($"[CatcherController] ✅ COOLDOWN FINISHED - READY TO TRIGGER AGAIN! State: {currentState}");
                }
            }
        }
        
        private void CheckBoundaryTrigger()
        {
            if (catTarget == null || gameCamera == null) 
            {
                if (enableDebugLogs && Time.frameCount % 300 == 0) // Debug every 5 seconds
                {
                    Debug.Log($"[CatcherController] CheckBoundaryTrigger failed - Cat: {(catTarget != null ? "OK" : "NULL")}, Camera: {(gameCamera != null ? "OK" : "NULL")}");
                }
                return;
            }
            
            // Check cooldown - không trigger nếu vẫn trong cooldown
            if (cooldownTimer > 0f) 
            {
                if (enableDebugLogs && Time.frameCount % 120 == 0) // Debug every 2 seconds
                {
                    Debug.Log($"[CatcherController] Cannot trigger - still in cooldown: {cooldownTimer:F2}s remaining");
                }
                return;
            }
            
            Vector3 catPos = catTarget.position;
            Vector3 cameraPos = gameCamera.transform.position;
            float cameraHeight = 2f * gameCamera.orthographicSize;
            float cameraWidth = cameraHeight * gameCamera.aspect;
            
            bool shouldTrigger = false;
            string debugInfo = "";
            
            switch (triggerBoundary)
            {
                case BoundaryDirection.Left:
                    float leftBoundary = cameraPos.x - (cameraWidth * 0.5f);
                    float distanceToLeft = catPos.x - leftBoundary;
                    shouldTrigger = distanceToLeft <= boundaryTriggerDistance;
                    debugInfo = $"LEFT boundary: {distanceToLeft:F2} ≤ {boundaryTriggerDistance} = {shouldTrigger}";
                    
                    // Log mỗi giây khi ở Hidden state để theo dõi
                    if (enableDebugLogs && Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[CatcherController] {gameObject.name}: {debugInfo}, Cat: {catPos.x:F2}, Boundary: {leftBoundary:F2}");
                    }
                    break;
                    
                case BoundaryDirection.Right:
                    float rightBoundary = cameraPos.x + (cameraWidth * 0.5f);
                    float distanceToRight = rightBoundary - catPos.x;
                    shouldTrigger = distanceToRight <= boundaryTriggerDistance;
                    break;
                    
                case BoundaryDirection.Bottom:
                    float bottomBoundary = cameraPos.y - (cameraHeight * 0.5f);
                    float distanceToBottom = catPos.y - bottomBoundary;
                    shouldTrigger = distanceToBottom <= boundaryTriggerDistance;
                    break;
                    
                case BoundaryDirection.Top:
                    float topBoundary = cameraPos.y + (cameraHeight * 0.5f);
                    float distanceToTop = topBoundary - catPos.y;
                    shouldTrigger = distanceToTop <= boundaryTriggerDistance;
                    break;
            }
            
            if (shouldTrigger)
            {
                if (enableDebugLogs) Debug.Log($"[CatcherController] ⚡ TRIGGERING {gameObject.name}! {debugInfo}, Cooldown: {cooldownTimer:F2}s, State: {currentState}");
                TriggerCatcher();
            }
            else if (enableDebugLogs && Time.frameCount % 180 == 0) // Debug every 3 seconds when not triggering
            {
                Debug.Log($"[CatcherController] ❌ NOT TRIGGERING {gameObject.name}: {debugInfo}, State: {currentState}, Cooldown: {cooldownTimer:F2}s");
            }
        }
        
        private void TriggerCatcher()
        {
            if (enableDebugLogs) Debug.Log($"[CatcherController] 🚀 NEW CYCLE STARTED! Cat near {triggerBoundary} boundary - Camera shake ready for first touch");
            
            // Lưu vị trí ban đầu của mèo để tính toán chase distance
            initialCatPosition = catTarget.position;
            
            // Tính toán activePosition dựa trên vị trí hiện tại của mèo
            CalculateActivePosition();
            
            SetState(CatcherState.Rising);
            OnCatcherTriggered?.Invoke(this);
        }
        
        private void CheckTouchDetection()
        {
            if (catTarget == null) return;
            
            float distance = Vector3.Distance(transform.position, catTarget.position);
            
            // Check if TouchCheck is properly set up
            bool touchCheckAvailable = touchCheck != null;
            
            // Only use distance detection as fallback if TouchCheck is NOT available
            bool shouldUseDistance = !touchCheckAvailable;
            bool distanceDetected = shouldUseDistance && distance <= touchDetectionRadius;
            
            // Debug touch detection
            if (enableDebugLogs && Time.frameCount % 60 == 0) // Every second
            {
                Debug.Log($"[CatcherController] === TOUCH DETECTION DEBUG ===");
                Debug.Log($"[CatcherController] Distance: {distance:F2}, Radius: {touchDetectionRadius}");
                if (touchCheckAvailable)
                {
                    Debug.Log($"[CatcherController] ✅ TouchCheck available: {(touchCheck.IsTouchingCat ? "DETECTING CAT" : "No contact")}");
                }
                else
                {
                    Debug.Log($"[CatcherController] ❌ TouchCheck NULL - Using distance fallback: {(distanceDetected ? "DETECTED" : "No Contact")}");
                }
            }
            
            // Distance fallback - direct death when detected
            if (shouldUseDistance && distanceDetected)
            {
                if (enableDebugLogs) Debug.Log($"[CatcherController] 🎯 CAT DETECTED via DISTANCE FALLBACK! Distance: {distance:F2} - DIRECT DEATH!");
                
                OnCatTouched?.Invoke(this);
                
                // Direct death - no knockback
                if (catController != null)
                {
                    if (enableDebugLogs) Debug.Log($"[CatcherController] 💀 DISTANCE DETECTION DEATH - calling OnCaughtByChaser!");
                    catController.OnCaughtByChaser();
                }
                
                SetState(CatcherState.Success);
            }
        }
        
        private void CheckChaseTimeout()
        {
            chaseTimer += Time.deltaTime;
            
            // Timeout nếu chase quá lâu
            bool shouldTimeout = chaseTimer >= chaseTimeout;
            
            if (shouldTimeout)
            {
                if (enableDebugLogs) Debug.Log($"[CatcherController] chase timeout! Retreating... (timer: {chaseTimer:F1}s)");
                SetState(CatcherState.Retreating);
            }
        }
        
        private void UpdateMovement()
        {
            switch (currentState)
            {
                case CatcherState.Hidden:
                    // Chỉ force position nếu catcher không ở đúng hidden position
                    float distanceToHidden = Vector3.Distance(transform.position, hiddenPosition);
                    if (distanceToHidden > 0.1f)
                    {
                        transform.position = hiddenPosition;
                        if (enableDebugLogs) Debug.Log($"[CatcherController] Corrected Hidden position: {transform.position} → {hiddenPosition}");
                    }
                    break;
                    
                case CatcherState.Rising:
                    targetPosition = activePosition;
                    MoveTowardsTarget(riseSpeed);
                    break;
                    
                case CatcherState.Chasing:
                    if (catTarget != null)
                    {
                        targetPosition = catTarget.position;
                        MoveTowardsTarget(chaseSpeed);
                    }
                    break;
                    
                case CatcherState.Retreating:
                    targetPosition = hiddenPosition;
                    MoveTowardsTarget(retreatSpeed);
                    break;
                    
                case CatcherState.Success:
                    // Không di chuyển khi đã thành công
                    break;
            }
        }
        
        private void UpdateChaseLogic()
        {
            // Additional chase logic can be added here
            // For example: dynamic speed based on distance, special behaviors, etc.
            
            if (currentState != CatcherState.Chasing) return;
            
            // Optional: Dynamic chase behavior based on distance to cat
            if (catTarget != null)
            {
                float distanceToCat = Vector3.Distance(transform.position, catTarget.position);
                
                // Example: Speed up when getting close to cat
                // This is optional and can be customized based on gameplay needs
                if (distanceToCat < touchDetectionRadius * 2f)
                {
                    // Could modify chase speed here if needed
                    // chaseSpeed = originalChaseSpeed * 1.2f;
                }
            }
        }
        
        private void MoveTowardsTarget(float speed)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            float moveDistance = speed * Time.deltaTime;
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            
            // Ngăn overshoot - nếu moveDistance lớn hơn khoảng cách còn lại, chỉ di chuyển đến target
            if (moveDistance >= distanceToTarget)
            {
                transform.position = targetPosition;
                if (enableDebugLogs && currentState == CatcherState.Retreating)
                {
                    Debug.Log($"[CatcherController] Reached target exactly - no overshoot");
                }
            }
            else
            {
                Vector3 newPosition = transform.position + direction * moveDistance;
                transform.position = newPosition;
            }
            
            // Debug movement during retreat
            if (currentState == CatcherState.Retreating && enableDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[CatcherController] Retreating movement - Speed: {speed}, Distance to target: {distanceToTarget:F3}, Move distance: {moveDistance:F3}");
            }
        }
        
        private void SetState(CatcherState newState)
        {
            if (currentState != newState)
            {
                CatcherState previousState = currentState;
                if (enableDebugLogs) Debug.Log($"[CatcherController] 🔄 STATE CHANGE: {currentState} → {newState}, Cooldown: {cooldownTimer:F2}s");
                currentState = newState;
                
                // 🌋 TRIGGER CAMERA SHAKE when catcher emerges (Hidden → Rising/Chasing)
                if (previousState == CatcherState.Hidden && (newState == CatcherState.Rising || newState == CatcherState.Chasing))
                {
                    if (CameraShake.Instance != null)
                    {
                        CameraShake.Instance.ShakeOnCatcherEmergence();
                        if (enableDebugLogs) Debug.Log("[CatcherController] 🌋📳 EMERGENCE CAMERA SHAKE TRIGGERED! Catcher appearing!");
                    }
                    else
                    {
                        if (enableDebugLogs) Debug.LogWarning("[CatcherController] ⚠️ CameraShake.Instance is NULL - no emergence shake triggered");
                    }
                }
                
                // State entry logic
                switch (newState)
                {
                    case CatcherState.Rising:
                        if (enableDebugLogs) Debug.Log($"[CatcherController] 🚀 CATCHER RISING! Moving from {hiddenPosition} to {activePosition}");
                        break;
                        
                    case CatcherState.Chasing:
                        chaseTimer = 0f;
                        if (enableDebugLogs) Debug.Log($"[CatcherController] 🏃 CATCHER CHASING! Target: {(catTarget != null ? catTarget.name : "NULL")}");
                        break;
                        
                    case CatcherState.Retreating:
                        // Recalculate hidden position để đảm bảo đúng vị trí
                        CalculateHiddenPosition();
                        if (enableDebugLogs) Debug.Log($"[CatcherController] ↩️ CATCHER RETREATING to: {hiddenPosition}");
                        break;
                        
                    case CatcherState.Hidden:
                        if (enableDebugLogs) Debug.Log($"[CatcherController] 👻 CATCHER HIDDEN at: {hiddenPosition}");
                        break;
                }
            }
        }
        
        private void ResetCatcher()
        {
            chaseTimer = 0f;
            cooldownTimer = triggerCooldown; // Bắt đầu cooldown khi reset
            initialCatPosition = Vector3.zero;
            if (enableDebugLogs) Debug.Log($"[CatcherController] 🔄 CATCHER RESET - READY FOR NEW CYCLE");
            
            // Reset TouchCheck nếu có
            if (touchCheck != null)
            {
                touchCheck.ResetTouchStatus();
            }
        }
        
        /// <summary>
        /// Called by TouchCheck when cat enters touch area - PRIMARY TOUCH DETECTION METHOD
        /// </summary>
        public void OnCatTouchedByTouchCheck(GameObject cat)
        {
            if (currentState == CatcherState.Chasing)
            {
                float distance = Vector3.Distance(transform.position, cat.transform.position);
                
                if (enableDebugLogs) 
                {
                    Debug.Log($"[CatcherController] 🎯🎯🎯 CAT TOUCHED VIA TOUCHCHECK! {cat.name} - DIRECT CONTACT");
                    Debug.Log($"[CatcherController] Distance at contact: {distance:F2}");
                }
                
                OnCatTouched?.Invoke(this);
                
                // Direct catch - no knockback, immediate death
                CatController catController = cat.GetComponent<CatController>();
                if (catController != null)
                {
                    if (enableDebugLogs) Debug.Log($"[CatcherController] � DIRECT DEATH - No knockback, calling OnCaughtByChaser!");
                    catController.OnCaughtByChaser();
                }
                else
                {
                    if (enableDebugLogs) Debug.LogWarning($"[CatcherController] No CatController found on {cat.name}");
                }
                
                // Set success state
                SetState(CatcherState.Success);
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[CatcherController] TouchCheck contact ignored - wrong state ({currentState})");
                }
            }
        }
        
        /// <summary>
        /// Called by TouchCheck when cat exits touch area
        /// </summary>
        public void OnCatExitedTouchCheck(GameObject cat)
        {
            if (enableDebugLogs) Debug.Log($"[CatcherController] 👋 Cat exited TouchCheck area: {cat.name}");
            // Additional logic can be added here if needed
        }
        
        // Public methods for external control
        public void ForceReset()
        {
            SetState(CatcherState.Hidden);
            transform.position = hiddenPosition;
            cooldownTimer = 0f; // Clear cooldown khi force reset
            ResetCatcher();
        }
        
        public bool IsActive()
        {
            return currentState != CatcherState.Hidden;
        }
        
        public CatcherState GetCurrentState()
        {
            return currentState;
        }
        
        /// <summary>
        /// Kiểm tra xem catcher có đang trong trạng thái chasing không
        /// </summary>
        public bool IsInChasingState()
        {
            return currentState == CatcherState.Chasing;
        }
        
        // Debug methods
        [ContextMenu("Debug Catcher State")]
        public void DebugCatcherState()
        {
            Debug.Log("=== CATCHER STATE DEBUG ===");
            Debug.Log($"Current State: {currentState}");
            Debug.Log($"Chase Timer: {chaseTimer:F2}s");
            Debug.Log($"Cooldown Timer: {cooldownTimer:F2}s");
            Debug.Log($"Position: {transform.position}");
            Debug.Log($"Target Position: {targetPosition}");
            Debug.Log($"Hidden Position: {hiddenPosition}");
            Debug.Log($"Active Position: {activePosition}");
            Debug.Log($"Initial Cat Position: {initialCatPosition}");
            
            if (catTarget != null)
            {
                float distance = Vector3.Distance(transform.position, catTarget.position);
                Debug.Log($"Distance to Cat: {distance:F2}");
                Debug.Log($"Cat Position: {catTarget.position}");
            }
        }
        
        [ContextMenu("Force Trigger Catcher")]
        public void ForceTriggerCatcher()
        {
            if (currentState == CatcherState.Hidden)
            {
                cooldownTimer = 0f; // Clear cooldown for manual trigger
                TriggerCatcher();
                Debug.Log("[CatcherController] Manually triggered catcher - cooldown cleared");
            }
            else
            {
                Debug.Log($"[CatcherController] Cannot trigger - current state: {currentState}");
            }
        }
        
        [ContextMenu("Clear Cooldown")]
        public void ClearCooldown()
        {
            cooldownTimer = 0f;
            Debug.Log($"[CatcherController] Cooldown cleared - ready to trigger");
        }
        
        [ContextMenu("Force Reset Catcher")]
        public void ForceResetCatcher()
        {
            ForceReset();
            Debug.Log("[CatcherController] Manually reset catcher");
        }
        
        [ContextMenu("Force Retreat Catcher")]
        public void ForceRetreatCatcher()
        {
            if (currentState == CatcherState.Chasing || currentState == CatcherState.Rising)
            {
                SetState(CatcherState.Retreating);
                Debug.Log("[CatcherController] Manually forced retreat");
            }
            else
            {
                Debug.Log($"[CatcherController] Cannot retreat - current state: {currentState}");
            }
        }
        
        [ContextMenu("Force Back to Hidden")]
        public void ForceBackToHidden()
        {
            // Recalculate hidden position trước khi set
            CalculateHiddenPosition();
            
            if (currentState == CatcherState.Retreating)
            {
                transform.position = hiddenPosition;
                SetState(CatcherState.Hidden);
                ResetCatcher();
                Debug.Log($"[CatcherController] Manually forced back to Hidden state at {hiddenPosition}");
            }
            else
            {
                transform.position = hiddenPosition;
                SetState(CatcherState.Hidden);
                cooldownTimer = 0f; // Clear cooldown
                ResetCatcher();
                Debug.Log($"[CatcherController] Force reset to Hidden from {currentState} at {hiddenPosition}");
            }
        }
        
        [ContextMenu("Debug Current Position")]
        public void DebugCurrentPosition()
        {
            Debug.Log("=== CURRENT POSITION DEBUG ===");
            Debug.Log($"Current Transform Position: {transform.position}");
            Debug.Log($"Hidden Position: {hiddenPosition}");
            Debug.Log($"Distance to Hidden: {Vector3.Distance(transform.position, hiddenPosition):F2}");
            Debug.Log($"Current State: {currentState}");
            Debug.Log($"Cooldown Timer: {cooldownTimer:F2}s");
            
            if (gameCamera != null)
            {
                Vector3 cameraPos = gameCamera.transform.position;
                Debug.Log($"Camera Position: {cameraPos}");
                float cameraHeight = 2f * gameCamera.orthographicSize;
                float cameraWidth = cameraHeight * gameCamera.aspect;
                Debug.Log($"Camera Size: {cameraWidth:F2} x {cameraHeight:F2}");
            }
        }
        
        [ContextMenu("Test Retreat Speed")]
        public void TestRetreatSpeed()
        {
            Debug.Log($"[CatcherController] Retreat Speed: {retreatSpeed}");
            Debug.Log($"[CatcherController] Current Position: {transform.position}");
            Debug.Log($"[CatcherController] Hidden Position: {hiddenPosition}");
            Debug.Log($"[CatcherController] Distance to Hidden: {Vector3.Distance(transform.position, hiddenPosition):F2}");
        }
        
        [ContextMenu("Test All Systems")]
        public void TestAllSystems()
        {
            Debug.Log("=== TESTING ALL CATCHER SYSTEMS ===");
            Debug.Log($"Cat Target: {(catTarget != null ? catTarget.name + " at " + catTarget.position : "NULL")}");
            Debug.Log($"Game Camera: {(gameCamera != null ? gameCamera.name : "NULL")}");
            Debug.Log($"Current State: {currentState}");
            Debug.Log($"Chase Timer: {chaseTimer:F2}s");
            Debug.Log($"Cooldown Timer: {cooldownTimer:F2}s ({(cooldownTimer > 0 ? "BLOCKING TRIGGERS" : "READY TO TRIGGER")})");
            Debug.Log($"Trigger Cooldown Setting: {triggerCooldown}s");
            Debug.Log($"Touch Detection Radius: {touchDetectionRadius}");
            Debug.Log($"Active Position: {activePosition}");
            Debug.Log($"Hidden Position: {hiddenPosition}");
            
            if (catTarget != null)
            {
                float distance = Vector3.Distance(transform.position, catTarget.position);
                Debug.Log($"Distance to Cat: {distance:F2} (Touch radius: {touchDetectionRadius})");
                Debug.Log($"Will trigger immediate death: {(distance <= touchDetectionRadius)}");
                
                // TouchCheck info
                if (touchCheck != null)
                {
                    Debug.Log($"TouchCheck Status: {(touchCheck.IsTouchingCat ? "TOUCHING" : "NOT TOUCHING")}");
                    Debug.Log($"TouchCheck Contact Count: {touchCheck.CatContactCount}");
                    Debug.Log($"TouchCheck Cat: {(touchCheck.TouchedCat != null ? touchCheck.TouchedCat.name : "NULL")}");
                }
                else
                {
                    Debug.Log($"TouchCheck: NULL - assign TouchCheck component!");
                }
            }
        }
        
        [ContextMenu("Debug TouchCheck Setup")]
        public void DebugTouchCheckSetup()
        {
            Debug.Log("=== TOUCHCHECK SETUP DEBUG ===");
            
            if (touchCheck == null)
            {
                Debug.LogError("[CatcherController] ❌ TouchCheck is NULL!");
                Debug.Log("SETUP REQUIRED:");
                Debug.Log("1. Create child GameObject named 'TouchCheck'");
                Debug.Log("2. Add TouchCheck.cs component");
                Debug.Log("3. Add Collider2D component (set as Trigger)");
                Debug.Log("4. Assign TouchCheck reference in CatcherController");
                
                // Check if there's a child with TouchCheck component
                TouchCheck[] touchChecks = GetComponentsInChildren<TouchCheck>();
                if (touchChecks.Length > 0)
                {
                    Debug.Log($"Found {touchChecks.Length} TouchCheck component(s) in children - assign one to touchCheck field!");
                    for (int i = 0; i < touchChecks.Length; i++)
                    {
                        Debug.Log($"TouchCheck {i}: {touchChecks[i].gameObject.name}");
                    }
                }
                return;
            }
            
            Debug.Log($"✅ TouchCheck found: {touchCheck.gameObject.name}");
            
            // Check collider setup
            Collider2D col = touchCheck.GetComponent<Collider2D>();
            if (col == null)
            {
                Debug.LogError($"❌ TouchCheck '{touchCheck.gameObject.name}' has NO Collider2D!");
            }
            else
            {
                Debug.Log($"✅ Collider2D found: {col.GetType().Name}");
                Debug.Log($"Is Trigger: {(col.isTrigger ? "✅ YES" : "❌ NO - Must be trigger!")}");
                Debug.Log($"Collider bounds: {col.bounds.size}");
            }
            
            // Check TouchCheck settings
            Debug.Log($"TouchCheck found: {touchCheck.gameObject.name}");
            Debug.Log($"Current contacts: {touchCheck.CatContactCount}");
            Debug.Log($"Is touching cat: {touchCheck.IsTouchingCat}");
            
            if (touchCheck.IsTouchingCat && touchCheck.TouchedCat != null)
            {
                Debug.Log($"Currently touching: {touchCheck.TouchedCat.name}");
            }
        }
        
        [ContextMenu("Test Hidden Position")]
        public void TestHiddenPosition()
        {
            Debug.Log("=== HIDDEN POSITION TEST ===");
            Debug.Log($"Current Hidden Position: {hiddenPosition}");
            Debug.Log($"Current Transform Position: {transform.position}");
            Debug.Log($"Trigger Boundary: {triggerBoundary}");
            Debug.Log($"Hidden Offset: {hiddenOffset}");
            
            if (gameCamera != null)
            {
                Vector3 cameraPos = gameCamera.transform.position;
                float cameraHeight = 2f * gameCamera.orthographicSize;
                float cameraWidth = cameraHeight * gameCamera.aspect;
                Debug.Log($"Camera Position: {cameraPos}");
                Debug.Log($"Camera Size: {cameraWidth}x{cameraHeight}");
                
                // Recalculate để kiểm tra
                CalculateHiddenPosition();
                Debug.Log($"Recalculated Hidden Position: {hiddenPosition}");
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            // Draw hidden position
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(hiddenPosition, 0.5f);
            
            // Draw active position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(activePosition, 0.5f);
            
            // Draw current position
            Gizmos.color = GetStateColor();
            Gizmos.DrawSphere(transform.position, 0.3f);
            
            // Draw touch detection radius
            if (currentState == CatcherState.Chasing)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, touchDetectionRadius);
                
                // Draw escape distance
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, escapeDistance);
            }
            
            // Draw boundary trigger area
            if (gameCamera != null && currentState == CatcherState.Hidden)
            {
                Vector3 cameraPos = gameCamera.transform.position;
                float cameraHeight = 2f * gameCamera.orthographicSize;
                float cameraWidth = cameraHeight * gameCamera.aspect;
                
                Gizmos.color = Color.cyan;
                switch (triggerBoundary)
                {
                    case BoundaryDirection.Left:
                        float leftBoundary = cameraPos.x - (cameraWidth * 0.5f) + boundaryTriggerDistance;
                        Vector3 leftStart = new Vector3(leftBoundary, cameraPos.y - (cameraHeight * 0.5f), 0);
                        Vector3 leftEnd = new Vector3(leftBoundary, cameraPos.y + (cameraHeight * 0.5f), 0);
                        Gizmos.DrawLine(leftStart, leftEnd);
                        break;
                        
                    case BoundaryDirection.Right:
                        float rightBoundary = cameraPos.x + (cameraWidth * 0.5f) - boundaryTriggerDistance;
                        Vector3 rightStart = new Vector3(rightBoundary, cameraPos.y - (cameraHeight * 0.5f), 0);
                        Vector3 rightEnd = new Vector3(rightBoundary, cameraPos.y + (cameraHeight * 0.5f), 0);
                        Gizmos.DrawLine(rightStart, rightEnd);
                        break;
                }
            }
            
            // Draw line to target
            if (currentState == CatcherState.Chasing && catTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, catTarget.position);
            }
        }
        
        [ContextMenu("Test Camera Shake")]
        public void TestCameraShake()
        {
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.ShakeOnCatcherContact();
                Debug.Log("[CatcherController] 📳 Manual camera shake test triggered!");
            }
            else
            {
                Debug.LogError("[CatcherController] ❌ CameraShake.Instance is NULL! Camera shake system not available.");
            }
        }
        
        [ContextMenu("Test Emergence Shake")]
        public void TestEmergenceShake()
        {
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.ShakeOnCatcherEmergence();
                Debug.Log("[CatcherController] 🌋📳 Manual EMERGENCE shake test triggered!");
            }
            else
            {
                Debug.LogError("[CatcherController] ❌ CameraShake.Instance is NULL! Emergence shake system not available.");
            }
        }
        
        private Color GetStateColor()
        {
            switch (currentState)
            {
                case CatcherState.Hidden: return Color.gray;
                case CatcherState.Rising: return Color.yellow;
                case CatcherState.Chasing: return Color.red;
                case CatcherState.Retreating: return Color.blue;
                case CatcherState.Success: return Color.green;
                default: return Color.white;
            }
        }
    }
}