using UnityEngine;
using System.Collections.Generic;

namespace CatchMeowIfYouCan.Enemies
{
    /// <summary>
    /// Manager để quản lý tất cả Catchers trong game
    /// Handles events, coordination, và global catcher logic
    /// </summary>
    public class CatcherManager : MonoBehaviour
    {
        [Header("Catcher Management")]
        [SerializeField] private List<CatcherController> catchers = new List<CatcherController>();
        [SerializeField] private bool autoFindCatchers = true;
        [SerializeField] private bool enableGlobalDebugLogs = true;
        
        [Header("Global Settings")]
        [SerializeField] private float cooldownBetweenCatchers = 2f; // Thời gian nghỉ giữa các lần trigger catcher
        [SerializeField] private int maxActiveCatchers = 1; // Số lượng tối đa catchers active cùng lúc
        [SerializeField] private bool allowMultipleCatchers = false; // Cho phép nhiều catchers cùng lúc
        
        [Header("Game Integration")]
        [SerializeField] private bool pauseGameOnCatch = true; // Tạm dừng game khi mèo bị bắt
        [SerializeField] private float gameOverDelay = 1f; // Delay trước khi game over
        
        // State tracking
        private float lastCatcherTriggerTime = 0f;
        private int activeCatcherCount = 0;
        private bool gameIsOver = false;
        
        // Events
        public System.Action<CatcherController> OnAnyCatcherTriggered;
        public System.Action<CatcherController> OnAnyCatTouched;
        public System.Action<CatcherController> OnAnyCatCaught;
        public System.Action<CatcherController> OnAnyCatEscaped;
        public System.Action OnGameOver;
        
        private void Start()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            if (enableGlobalDebugLogs) Debug.Log("[CatcherManager] Initializing CatcherManager");
            
            // Auto-find catchers if enabled
            if (autoFindCatchers)
            {
                FindAllCatchers();
            }
            
            // Subscribe to all catcher events
            SubscribeToAllCatchers();
            
            if (enableGlobalDebugLogs)
            {
                Debug.Log($"[CatcherManager] Initialized with {catchers.Count} catchers");
                Debug.Log($"[CatcherManager] Max active catchers: {maxActiveCatchers}");
                Debug.Log($"[CatcherManager] Cooldown between catchers: {cooldownBetweenCatchers}s");
            }
        }
        
        private void FindAllCatchers()
        {
            CatcherController[] foundCatchers = FindObjectsByType<CatcherController>(FindObjectsSortMode.None);
            
            foreach (CatcherController catcher in foundCatchers)
            {
                if (!catchers.Contains(catcher))
                {
                    catchers.Add(catcher);
                    if (enableGlobalDebugLogs) Debug.Log($"[CatcherManager] Found catcher: {catcher.name}");
                }
            }
        }
        
        private void SubscribeToAllCatchers()
        {
            foreach (CatcherController catcher in catchers)
            {
                if (catcher != null)
                {
                    // Subscribe to events
                    catcher.OnCatcherTriggered += HandleCatcherTriggered;
                    catcher.OnCatTouched += HandleCatTouched;
                    catcher.OnCatCaught += HandleCatCaught;
                    catcher.OnCatEscaped += HandleCatEscaped;
                }
            }
        }
        
        private void HandleCatcherTriggered(CatcherController catcher)
        {
            if (enableGlobalDebugLogs) Debug.Log($"[CatcherManager] Catcher triggered: {catcher.name}");
            
            activeCatcherCount++;
            lastCatcherTriggerTime = Time.time;
            
            // Disable other catchers if not allowing multiple
            if (!allowMultipleCatchers)
            {
                DisableOtherCatchers(catcher);
            }
            
            OnAnyCatcherTriggered?.Invoke(catcher);
        }
        
        private void HandleCatTouched(CatcherController catcher)
        {
            if (enableGlobalDebugLogs) Debug.Log($"[CatcherManager] Cat touched by: {catcher.name}");
            OnAnyCatTouched?.Invoke(catcher);
        }

        private void HandleCatCaught(CatcherController catcher)
        {
            Debug.Log($"[CatcherManager] Cat caught by: {catcher.name}");
            gameIsOver = true;

            if (pauseGameOnCatch)
            {
                Time.timeScale = 0f; // Dừng game
            }

            OnAnyCatCaught?.Invoke(catcher);

            // Sau 1 giây gọi GameOver
            Invoke("TriggerGameOver", gameOverDelay);
        }

        private void TriggerGameOver()
        {
            Debug.Log("[CatcherManager] Game Over triggered!");
            OnGameOver?.Invoke();
           
            // Gọi GameManager để hiện panel GameOver
                GameManager.Instance.GameOver();
        }
        private void HandleCatEscaped(CatcherController catcher)
        {
            if (enableGlobalDebugLogs) Debug.Log($"[CatcherManager] Cat escaped from: {catcher.name}");
            
            activeCatcherCount = Mathf.Max(0, activeCatcherCount - 1);
            OnAnyCatEscaped?.Invoke(catcher);
        }
        
        private void DisableOtherCatchers(CatcherController activeCatcher)
        {
            foreach (CatcherController catcher in catchers)
            {
                if (catcher != null && catcher != activeCatcher && catcher.IsActive())
                {
                    catcher.ForceReset();
                    if (enableGlobalDebugLogs) Debug.Log($"[CatcherManager] Disabled catcher: {catcher.name}");
                }
            }
        }
        
        
        // Public methods
        public void ResetAllCatchers()
        {
            if (enableGlobalDebugLogs) Debug.Log("[CatcherManager] Resetting all catchers");
            
            foreach (CatcherController catcher in catchers)
            {
                if (catcher != null)
                {
                    catcher.ForceReset();
                }
            }
            
            activeCatcherCount = 0;
            gameIsOver = false;
            lastCatcherTriggerTime = 0f;
        }
        
        public void AddCatcher(CatcherController catcher)
        {
            if (catcher != null && !catchers.Contains(catcher))
            {
                catchers.Add(catcher);
                
                // Subscribe to events
                catcher.OnCatcherTriggered += HandleCatcherTriggered;
                catcher.OnCatTouched += HandleCatTouched;
                catcher.OnCatCaught += HandleCatCaught;
                catcher.OnCatEscaped += HandleCatEscaped;
                
                if (enableGlobalDebugLogs) Debug.Log($"[CatcherManager] Added catcher: {catcher.name}");
            }
        }
        
        public void RemoveCatcher(CatcherController catcher)
        {
            if (catcher != null && catchers.Contains(catcher))
            {
                // Unsubscribe from events
                catcher.OnCatcherTriggered -= HandleCatcherTriggered;
                catcher.OnCatTouched -= HandleCatTouched;
                catcher.OnCatCaught -= HandleCatCaught;
                catcher.OnCatEscaped -= HandleCatEscaped;
                
                catchers.Remove(catcher);
                
                if (enableGlobalDebugLogs) Debug.Log($"[CatcherManager] Removed catcher: {catcher.name}");
            }
        }
        
        public bool CanTriggerNewCatcher()
        {
            if (gameIsOver) return false;
            if (!allowMultipleCatchers && activeCatcherCount >= maxActiveCatchers) return false;
            if (Time.time - lastCatcherTriggerTime < cooldownBetweenCatchers) return false;
            
            return true;
        }
        
        public int GetActiveCatcherCount()
        {
            return activeCatcherCount;
        }
        
        public bool IsGameOver()
        {
            return gameIsOver;
        }
        
        // Debug methods
        [ContextMenu("Debug Manager State")]
        public void DebugManagerState()
        {
            Debug.Log("=== CATCHER MANAGER DEBUG ===");
            Debug.Log($"Total Catchers: {catchers.Count}");
            Debug.Log($"Active Catchers: {activeCatcherCount}");
            Debug.Log($"Game Over: {gameIsOver}");
            Debug.Log($"Last Trigger Time: {lastCatcherTriggerTime:F2}s");
            Debug.Log($"Current Time: {Time.time:F2}s");
            Debug.Log($"Can Trigger New: {CanTriggerNewCatcher()}");
            
            Debug.Log("Catcher States:");
            for (int i = 0; i < catchers.Count; i++)
            {
                if (catchers[i] != null)
                {
                    Debug.Log($"  [{i}] {catchers[i].name}: {catchers[i].GetCurrentState()} (Active: {catchers[i].IsActive()})");
                }
                else
                {
                    Debug.Log($"  [{i}] NULL CATCHER");
                }
            }
        }
        
        [ContextMenu("Reset All Catchers")]
        public void DebugResetAllCatchers()
        {
            ResetAllCatchers();
            Debug.Log("[CatcherManager] All catchers reset via debug command");
        }
        
        [ContextMenu("Trigger Random Catcher")]
        public void DebugTriggerRandomCatcher()
        {
            if (catchers.Count == 0)
            {
                Debug.LogWarning("[CatcherManager] No catchers to trigger!");
                return;
            }
            
            CatcherController randomCatcher = catchers[Random.Range(0, catchers.Count)];
            if (randomCatcher != null && randomCatcher.GetCurrentState() == CatcherController.CatcherState.Hidden)
            {
                randomCatcher.ForceTriggerCatcher();
                Debug.Log($"[CatcherManager] Manually triggered catcher: {randomCatcher.name}");
            }
            else
            {
                Debug.LogWarning($"[CatcherManager] Cannot trigger catcher {randomCatcher?.name} - state: {randomCatcher?.GetCurrentState()}");
            }
        }
        
        [ContextMenu("Find All Catchers")]
        public void DebugFindAllCatchers()
        {
            int oldCount = catchers.Count;
            FindAllCatchers();
            SubscribeToAllCatchers();
            Debug.Log($"[CatcherManager] Found catchers: {oldCount} → {catchers.Count}");
        }
        
        private void OnDestroy()
        {
            // Cleanup event subscriptions
            foreach (CatcherController catcher in catchers)
            {
                if (catcher != null)
                {
                    catcher.OnCatcherTriggered -= HandleCatcherTriggered;
                    catcher.OnCatTouched -= HandleCatTouched;
                    catcher.OnCatCaught -= HandleCatCaught;
                    catcher.OnCatEscaped -= HandleCatEscaped;
                }
            }
        }
    }
}