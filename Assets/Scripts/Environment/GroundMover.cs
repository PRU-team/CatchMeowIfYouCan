using UnityEngine;

namespace CatchMeowIfYouCan.Environment
{
    /// <summary>
    /// Component để di chuyển ground pieces tạo cảm giác endless running
    /// </summary>
    public class GroundMover : MonoBehaviour
    {
        private GroundSpawner spawner;
        private Transform playerTransform;
        private bool isMoving = true;
        
        /// <summary>
        /// Khởi tạo với reference đến spawner
        /// </summary>
        public void Initialize(GroundSpawner groundSpawner)
        {
            spawner = groundSpawner;
            
            // Tìm player
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        private void Update()
        {
            if (!isMoving || spawner == null) return;
            
            MoveGround();
        }
        
        /// <summary>
        /// Di chuyển ground về phía trái để tạo hiệu ứng endless running
        /// Khi background cố định, ground cần di chuyển để tạo cảm giác chuyển động
        /// </summary>
        private void MoveGround()
        {
            // Kiểm tra xem có FixedBackgroundManager không
            var fixedBgManager = FindFirstObjectByType<FixedBackgroundManager>();
            bool shouldMoveGround = fixedBgManager != null; // Nếu có background cố định thì ground phải di chuyển
            
            if (shouldMoveGround && spawner != null)
            {
                // Di chuyển ground về phía trái với tốc độ từ spawner
                float speed = spawner.GetCurrentSpeed();
                transform.Translate(Vector3.left * speed * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// Bật/tắt di chuyển
        /// </summary>
        public void SetMoving(bool moving)
        {
            isMoving = moving;
        }
        
        /// <summary>
        /// Khi ground bị disable, đảm bảo reset state
        /// </summary>
        private void OnDisable()
        {
            isMoving = true; // Reset về default khi return to pool
        }
    }
}