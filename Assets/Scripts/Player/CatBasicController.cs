using UnityEngine;
using System.Collections;

public class CatController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float laneDistance = 3f;        // Khoảng cách giữa các lane
    [SerializeField] private float laneChangeSpeed = 10f;    // Tốc độ đổi lane
    [SerializeField] private float jumpForce = 12f;          // Lực nhảy
    [SerializeField] private float slideDistance = 2f;       // Khoảng cách slide
    [SerializeField] private float slideSpeed = 8f;          // Tốc độ slide
    
    [Header("Lane System")]
    [SerializeField] private int currentLane = 0;            // -1 = trái, 0 = giữa, 1 = phải
    
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;          // Point để check ground
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;          // Layer của ground
    
    // Private variables
    private Rigidbody2D rb;
    private CatchMeowIfYouCan.Player.CatAnimator catAnimator;
    private bool isGrounded = true;
    private bool isMoving = false;
    private bool isSliding = false;
    private Vector3 targetPosition;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        catAnimator = GetComponent<CatchMeowIfYouCan.Player.CatAnimator>();
        
        // Set vị trí ban đầu
        targetPosition = transform.position;
        
        // Tạo ground check point nếu chưa có
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = groundCheckObj.transform;
        }
    }
    
    void Update()
    {
        HandleInput();
        CheckGrounded();
        HandleMovement();
        UpdateAnimations();
    }
    
    void HandleInput()
    {
        // WASD Controls
        // A - Move Left
        if (Input.GetKeyDown(KeyCode.A))
        {
            ChangeLane(-1);
        }
        
        // D - Move Right  
        if (Input.GetKeyDown(KeyCode.D))
        {
            ChangeLane(1);
        }
        
        // W - Jump
        if (Input.GetKeyDown(KeyCode.W))
        {
            Jump();
        }
        
        // S - Slide
        if (Input.GetKeyDown(KeyCode.S))
        {
            Slide();
        }
        
        // Alternative controls (Arrow keys)
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            ChangeLane(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ChangeLane(1);
        if (Input.GetKeyDown(KeyCode.UpArrow))
            Jump();
        if (Input.GetKeyDown(KeyCode.DownArrow))
            Slide();
            
        // Space for jump (alternative)
        if (Input.GetKeyDown(KeyCode.Space))
            Jump();
    }
    
    void ChangeLane(int direction)
    {
        if (isMoving || isSliding) return;
        
        // Clamp lane trong khoảng -1 đến 1
        int newLane = Mathf.Clamp(currentLane + direction, -1, 1);
        
        if (newLane != currentLane)
        {
            currentLane = newLane;
            
            // Tính vị trí mới
            targetPosition = new Vector3(currentLane * laneDistance, transform.position.y, 0);
            
            StartCoroutine(MoveTo(targetPosition));
        }
    }
    
    void Jump()
    {
        if (isGrounded && !isSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reset vertical velocity
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isGrounded = false;
            
            
        }
    }
    
    void Slide()
    {
        if (isGrounded && !isSliding && !isMoving)
        {
            StartCoroutine(SlideCoroutine());
        }
    }
    
    IEnumerator SlideCoroutine()
    {
        isSliding = true;
        catAnimator?.SetSliding(true);
        
        // Slide forward
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.right * slideDistance;
        
        float elapsedTime = 0;
        float slideTime = slideDistance / slideSpeed;
        
        while (elapsedTime < slideTime)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / slideTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.position = endPos;
        
        yield return new WaitForSeconds(0.2f); // Thời gian slide
        
        isSliding = false;
        catAnimator?.SetSliding(false);
    }
    
    IEnumerator MoveTo(Vector3 targetPos)
    {
        isMoving = true;
        
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos, laneChangeSpeed * Time.deltaTime);
            newPos.y = transform.position.y; // Giữ nguyên Y position
            transform.position = newPos;
            yield return null;
        }
        
        // Đảm bảo position chính xác
        Vector3 finalPos = targetPos;
        finalPos.y = transform.position.y;
        transform.position = finalPos;
        
        isMoving = false;
    }
    
    void CheckGrounded()
    {
        // Kiểm tra có chạm ground không
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
    
    void HandleMovement()
    {
        // Tự động chạy về phía trước (nếu cần)
        // transform.Translate(Vector3.right * forwardSpeed * Time.deltaTime);
    }
    
    void UpdateAnimations()
    {
        if (catAnimator != null)
        {
            // Update animation states
            bool shouldRun = isGrounded && !isSliding;
            catAnimator.SetMoving(shouldRun);
            
        
        }
    }
    
    // Gizmos để debug
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        // Vẽ các lane positions
        Gizmos.color = Color.yellow;
        for (int i = -1; i <= 1; i++)
        {
            Vector3 lanePos = new Vector3(i * laneDistance, transform.position.y, 0);
            Gizmos.DrawLine(lanePos + Vector3.up, lanePos - Vector3.up);
        }
    }
}