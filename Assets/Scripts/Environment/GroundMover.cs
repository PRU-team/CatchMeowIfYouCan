using UnityEngine;

namespace CatchMeowIfYouCan.Environment
{
    public class GroundMover : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        private GroundSpawner spawner;
        private bool isMoving = true;
        
        public void Initialize(GroundSpawner groundSpawner)
        {
            spawner = groundSpawner;
        }
        
        private void Update()
        {
            if (!isMoving) return;
            MoveGround();
        }
        
        private void MoveGround()
        {
            var fixedBgManager = FindFirstObjectByType<FixedBackgroundManager>();
            
            if (fixedBgManager != null && spawner != null)
            {
                float speed = spawner.GetCurrentSpeed();
                
                if (speed <= 0f)
                {
                    speed = 5f; // Default fallback speed
                }
                
                Vector3 movement = Vector3.left * speed * Time.deltaTime;
                transform.Translate(movement);
                
                if (enableDebugLogs && Time.frameCount % 120 == 0)
                {
                    Debug.Log($"[GroundMover] {gameObject.name} moving LEFT at speed {speed}");
                }
            }
        }
        
        public void SetMoving(bool moving)
        {
            isMoving = moving;
        }
        
        private void OnDisable()
        {
            isMoving = true;
        }
    }
}