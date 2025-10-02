using UnityEngine;

namespace CatchMeowIfYouCan.Environment
{
    /// <summary>
    /// Manager tổng thể cho endless running environment
    /// </summary>
    public class EndlessRunManager : MonoBehaviour
    {
        [Header("Environment Components")]
        [SerializeField] private GroundSpawner groundSpawner;
        [SerializeField] private Transform player;
        
        [Header("Game Settings")]
        [SerializeField] private float baseSpeed = 5f;
        [SerializeField] private float speedIncreaseRate = 0.1f;
        [SerializeField] private float maxSpeed = 15f;
        
        [Header("Camera Follow")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private bool followPlayer = true;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 5, -10);
        [SerializeField] private float cameraFollowSpeed = 5f;
        
        // State
        private float currentGameSpeed;
        private bool isGameRunning = true;
        
        // Events
        public System.Action<float> OnSpeedChanged;
        public System.Action OnGameStarted;
        public System.Action OnGameStopped;
        
        private void Start()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            currentGameSpeed = baseSpeed;
            FindComponents();
            
            if (groundSpawner != null)
            {
                groundSpawner.SetSpeed(currentGameSpeed);
                groundSpawner.OnSpeedChanged += HandleSpeedChanged;
            }
            
            if (gameCamera == null)
            {
                gameCamera = Camera.main;
            }
            
            StartGame();
        }
        
        private void FindComponents()
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
            }
            
            if (groundSpawner == null)
            {
                groundSpawner = FindFirstObjectByType<GroundSpawner>();
            }
        }
        
        private void Update()
        {
            if (!isGameRunning) return;
            
            UpdateGameSpeed();
            UpdateCameraFollow();
        }
        
        private void UpdateGameSpeed()
        {
            // Tăng tốc độ dựa trên thời gian từ khi bắt đầu game, không phải Time.time tổng
            float gameTime = isGameRunning ? Time.time - Time.fixedTime : 0f;
            float targetSpeed = Mathf.Min(baseSpeed + (gameTime * speedIncreaseRate), maxSpeed);
            
            if (Mathf.Abs(currentGameSpeed - targetSpeed) > 0.1f)
            {
                currentGameSpeed = Mathf.Lerp(currentGameSpeed, targetSpeed, Time.deltaTime * 2f);
                
                if (groundSpawner != null)
                {
                    groundSpawner.SetSpeed(currentGameSpeed);
                }
                
                OnSpeedChanged?.Invoke(currentGameSpeed);
            }
        }
        
        private void UpdateCameraFollow()
        {
            if (!followPlayer || gameCamera == null || player == null) return;
            
            Vector3 targetPosition = player.position + cameraOffset;
            Vector3 currentPosition = gameCamera.transform.position;
            
            targetPosition.y = currentPosition.y;
            targetPosition.z = currentPosition.z;
            
            gameCamera.transform.position = Vector3.Lerp(
                currentPosition, 
                targetPosition, 
                cameraFollowSpeed * Time.deltaTime
            );
        }
        
        public void StartGame()
        {
            isGameRunning = true;
            currentGameSpeed = baseSpeed;
            
            if (groundSpawner != null)
            {
                groundSpawner.SetSpeed(currentGameSpeed);
            }
            
            OnGameStarted?.Invoke();
            Debug.Log("Endless running game started!");
        }
        
        public void StopGame()
        {
            isGameRunning = false;
            
            if (groundSpawner != null)
            {
                groundSpawner.SetSpeed(0f);
            }
            
            OnGameStopped?.Invoke();
            Debug.Log("Endless running game stopped!");
        }
        
        public void ResetGame()
        {
            currentGameSpeed = baseSpeed;
            
            if (groundSpawner != null)
            {
                groundSpawner.ResetSpawner();
                groundSpawner.SetSpeed(currentGameSpeed);
            }
            
            StartGame();
        }
        
        private void HandleSpeedChanged(float newSpeed)
        {
            Debug.Log($"Game speed changed to: {newSpeed:F1}");
        }
        
        public float GetCurrentSpeed()
        {
            return currentGameSpeed;
        }
        
        public void SetBaseSpeed(float speed)
        {
            baseSpeed = speed;
            currentGameSpeed = speed;
        }
        
        public void SetCameraFollow(bool follow)
        {
            followPlayer = follow;
        }
        
        public bool IsGameRunning()
        {
            return isGameRunning;
        }
    }
}