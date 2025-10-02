using UnityEngine;
using System.Collections;

namespace CatchMeowIfYouCan.Effects
{
    /// <summary>
    /// Camera Shake System - Tạo hiệu ứng rung màn hình khi mèo chạm catcher
    /// Sử dụng Singleton pattern để dễ dàng gọi từ bất cứ đâu
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [Header("Shake Settings")]
        [SerializeField] private float defaultShakeIntensity = 0.8f; // Cường độ rung mặc định
        [SerializeField] private float defaultShakeDuration = 0.3f; // Thời gian rung mặc định
        [SerializeField] private AnimationCurve shakeFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0); // Curve giảm dần cường độ
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showShakeGizmo = false;
        
        // Singleton instance
        public static CameraShake Instance { get; private set; }
        
        // Internal variables
        private Camera targetCamera;
        private Vector3 originalPosition;
        private Coroutine currentShakeCoroutine;
        private bool isShaking = false;
        
        // Statistics
        [Header("Runtime Info")]
        [SerializeField] private float currentShakeTime = 0f;
        [SerializeField] private float currentIntensity = 0f;
        [SerializeField] private int totalShakeCount = 0;
        
        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Initialize()
        {
            // Tìm camera chính
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindFirstObjectByType<Camera>();
            }
            
            if (targetCamera != null)
            {
                originalPosition = targetCamera.transform.localPosition;
                if (enableDebugLogs) Debug.Log($"[CameraShake] Initialized with camera: {targetCamera.name}");
            }
            else
            {
                Debug.LogError("[CameraShake] No camera found! Camera shake will not work.");
            }
        }
        
        /// <summary>
        /// Shake camera với default settings - dùng cho catcher contact
        /// </summary>
        public void ShakeOnCatcherContact()
        {
            ShakeCamera(defaultShakeIntensity, defaultShakeDuration);
            if (enableDebugLogs) Debug.Log("[CameraShake] Catcher contact shake triggered!");
        }
        
        /// <summary>
        /// Shake camera khi catcher emergence (xuất hiện từ Hidden sang Rising/Chasing)
        /// Hiệu ứng mạnh hơn để tạo cảm giác dramatic
        /// </summary>
        public void ShakeOnCatcherEmergence()
        {
            float emergenceIntensity = defaultShakeIntensity * 1.5f; // Mạnh hơn 50%
            float emergenceDuration = defaultShakeDuration * 1.2f;   // Lâu hơn 20%
            ShakeCamera(emergenceIntensity, emergenceDuration);
            if (enableDebugLogs) Debug.Log("[CameraShake] 🌋 CATCHER EMERGENCE SHAKE! More dramatic effect!");
        }
        
        /// <summary>
        /// Shake camera với custom settings
        /// </summary>
        /// <param name="intensity">Cường độ rung (0-1)</param>
        /// <param name="duration">Thời gian rung (seconds)</param>
        public void ShakeCamera(float intensity, float duration)
        {
            if (targetCamera == null)
            {
                Debug.LogWarning("[CameraShake] No target camera available for shaking!");
                return;
            }
            
            // Dừng shake hiện tại nếu có
            if (currentShakeCoroutine != null)
            {
                StopCoroutine(currentShakeCoroutine);
            }
            
            currentShakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
            totalShakeCount++;
            
            if (enableDebugLogs) 
                Debug.Log($"[CameraShake] Starting shake - Intensity: {intensity}, Duration: {duration}s");
        }
        
        /// <summary>
        /// Dừng shake và reset camera về vị trí ban đầu
        /// </summary>
        public void StopShake()
        {
            if (currentShakeCoroutine != null)
            {
                StopCoroutine(currentShakeCoroutine);
                currentShakeCoroutine = null;
            }
            
            ResetCamera();
            if (enableDebugLogs) Debug.Log("[CameraShake] Shake stopped manually");
        }
        
        private IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            isShaking = true;
            currentShakeTime = 0f;
            
            while (currentShakeTime < duration)
            {
                currentShakeTime += Time.deltaTime;
                
                // Tính toán cường độ theo curve falloff
                float normalizedTime = currentShakeTime / duration;
                currentIntensity = intensity * shakeFalloff.Evaluate(normalizedTime);
                
                // Tạo offset ngẫu nhiên
                Vector3 randomOffset = new Vector3(
                    Random.Range(-currentIntensity, currentIntensity),
                    Random.Range(-currentIntensity, currentIntensity),
                    0f
                );
                
                // Áp dụng shake
                targetCamera.transform.localPosition = originalPosition + randomOffset;
                
                yield return null;
            }
            
            // Kết thúc shake
            ResetCamera();
            isShaking = false;
            currentShakeCoroutine = null;
            currentIntensity = 0f;
            
            if (enableDebugLogs) Debug.Log("[CameraShake] Shake completed");
        }
        
        private void ResetCamera()
        {
            if (targetCamera != null)
            {
                targetCamera.transform.localPosition = originalPosition;
            }
        }
        
        /// <summary>
        /// Preset shake cho các tình huống khác nhau
        /// </summary>
        public void ShakeMild() => ShakeCamera(0.3f, 0.2f);
        public void ShakeModerate() => ShakeCamera(0.6f, 0.4f);
        public void ShakeStrong() => ShakeCamera(1.0f, 0.6f);
        public void ShakeEmergence() => ShakeOnCatcherEmergence(); // Alias cho catcher emergence
        
        /// <summary>
        /// Kiểm tra xem có đang shake không
        /// </summary>
        public bool IsShaking => isShaking;
        
        /// <summary>
        /// Test function để thử shake từ Editor
        /// </summary>
        [ContextMenu("Test Shake - Catcher Contact")]
        public void TestShakeFromEditor()
        {
            if (Application.isPlaying)
            {
                ShakeOnCatcherContact();
            }
            else
            {
                Debug.Log("[CameraShake] Test shake can only be used in Play mode");
            }
        }
        
        [ContextMenu("Test Shake - Catcher Emergence")]
        public void TestEmergenceShakeFromEditor()
        {
            if (Application.isPlaying)
            {
                ShakeOnCatcherEmergence();
            }
            else
            {
                Debug.Log("[CameraShake] Test emergence shake can only be used in Play mode");
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showShakeGizmo || !isShaking) return;
            
            // Vẽ gizmo cho shake area
            Gizmos.color = Color.red;
            if (targetCamera != null)
            {
                Vector3 shakeArea = new Vector3(currentIntensity * 2, currentIntensity * 2, 0.1f);
                Gizmos.DrawWireCube(targetCamera.transform.position, shakeArea);
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        // Auto-setup nếu không có CameraShake trong scene
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSetup()
        {
            if (Instance == null)
            {
                GameObject shakeObj = new GameObject("CameraShake");
                shakeObj.AddComponent<CameraShake>();
                Debug.Log("[CameraShake] Auto-created CameraShake instance");
            }
        }
    }
}