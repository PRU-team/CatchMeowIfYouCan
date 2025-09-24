using UnityEngine;

namespace CatchMeowIfYouCan.Player
{
    /// <summary>
    /// Handles input for the cat player
    /// Supports touch input for mobile (swipe gestures) and keyboard for testing
    /// </summary>
    public class CatInput : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private float swipeThreshold = 50f;
        [SerializeField] private bool enableKeyboardInput = true;
        
        // Events for input actions
        public System.Action OnSwipeLeft;
        public System.Action OnSwipeRight;
        public System.Action OnSwipeUp;
        public System.Action OnSwipeDown;
        public System.Action OnTap;
        
        // Touch input variables
        private Vector2 startTouchPosition;
        private bool isTouching = false;
        
        private void Update()
        {
            HandleTouchInput();
            
            if (enableKeyboardInput)
            {
                HandleKeyboardInput();
            }
        }
        
        private void HandleTouchInput()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        startTouchPosition = touch.position;
                        isTouching = true;
                        break;
                        
                    case TouchPhase.Ended:
                        if (isTouching)
                        {
                            Vector2 swipeVector = touch.position - startTouchPosition;
                            ProcessSwipe(swipeVector);
                        }
                        isTouching = false;
                        break;
                        
                    case TouchPhase.Canceled:
                        isTouching = false;
                        break;
                }
            }
            
            // Handle mouse input for testing in editor
            if (Application.isEditor)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    startTouchPosition = Input.mousePosition;
                    isTouching = true;
                }
                else if (Input.GetMouseButtonUp(0) && isTouching)
                {
                    Vector2 swipeVector = (Vector2)Input.mousePosition - startTouchPosition;
                    ProcessSwipe(swipeVector);
                    isTouching = false;
                }
            }
        }
        
        private void ProcessSwipe(Vector2 swipeVector)
        {
            float swipeMagnitude = swipeVector.magnitude;
            
            // Check if swipe is strong enough
            if (swipeMagnitude < swipeThreshold)
            {
                // Consider it a tap
                OnTap?.Invoke();
                return;
            }
            
            // Determine swipe direction
            float swipeAngle = Mathf.Atan2(swipeVector.y, swipeVector.x) * Mathf.Rad2Deg;
            
            // Normalize angle to 0-360
            if (swipeAngle < 0)
                swipeAngle += 360;
            
            // Determine swipe direction based on angle
            if (swipeAngle >= 45f && swipeAngle <= 135f)
            {
                // Swipe Up - Jump
                OnSwipeUp?.Invoke();
            }
            else if (swipeAngle >= 225f && swipeAngle <= 315f)
            {
                // Swipe Down - Slide
                OnSwipeDown?.Invoke();
            }
            else if (swipeAngle >= 135f && swipeAngle <= 225f)
            {
                // Swipe Left - Move Left Lane
                OnSwipeLeft?.Invoke();
            }
            else
            {
                // Swipe Right - Move Right Lane
                OnSwipeRight?.Invoke();
            }
        }
        
        private void HandleKeyboardInput()
        {
            // Keyboard controls for testing
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                OnSwipeLeft?.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                OnSwipeRight?.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space))
            {
                OnSwipeUp?.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                OnSwipeDown?.Invoke();
            }
        }
        
        // Debug visualization
        private void OnDrawGizmos()
        {
            if (isTouching)
            {
                Gizmos.color = Color.yellow;
                Vector3 worldStart = Camera.main.ScreenToWorldPoint(new Vector3(startTouchPosition.x, startTouchPosition.y, 10f));
                Gizmos.DrawWireSphere(worldStart, 0.5f);
            }
        }
    }
}