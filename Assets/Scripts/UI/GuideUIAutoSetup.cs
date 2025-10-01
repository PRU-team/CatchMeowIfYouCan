using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

namespace CatchMeowIfYouCan.UI
{
    /// <summary>
    /// Auto setup UI cho GuideScene - tự động tạo Canvas và UI elements
    /// Chạy trong Editor để setup UI nhanh chóng
    /// </summary>
    [System.Serializable]
    public class GuideUIAutoSetup : MonoBehaviour
    {
        [Header("Auto Setup Settings")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private Color backgroundColor = new Color(0.2f, 0.6f, 0.8f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color titleColor = Color.yellow;
        
        [Header("Scene Navigation")]
        [SerializeField] private string mainMenuSceneName = "Menu";
        [SerializeField] private string gameSceneName = "SampleScene";
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupGuideUI();
            }
        }
        
        /// <summary>
        /// Tự động tạo toàn bộ UI cho Guide Scene
        /// </summary>
        [ContextMenu("Auto Setup Guide UI")]
        public void SetupGuideUI()
        {
            Debug.Log("[GuideUIAutoSetup] Setting up Guide UI...");
            
            // 1. Setup Camera background
            SetupCameraBackground();
            
            // 2. Create Canvas
            Canvas canvas = CreateOrFindCanvas();
            
            // 3. Create Background Panel
            CreateBackgroundPanel(canvas);
            
            // 4. Create Title
            CreateTitleText(canvas);
            
            // 5. Create Instruction Text
            CreateInstructionText(canvas);
            
            // 6. Create Buttons
            CreateButtons(canvas);
            
            // 7. Add GuideManager
            AddGuideManager(canvas);
            
            // 8. Ensure EventSystem exists (critical for button functionality)
            EnsureEventSystemExists();
            
            Debug.Log("[GuideUIAutoSetup] ✅ Guide UI setup completed!");
        }
        
        /// <summary>
        /// Đảm bảo EventSystem tồn tại (cần thiết để buttons hoạt động)
        /// </summary>
        private void EnsureEventSystemExists()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogWarning("[GuideUIAutoSetup] No EventSystem found - creating one...");
                CreateEventSystem();
            }
            else
            {
                Debug.Log("[GuideUIAutoSetup] ✅ EventSystem already exists");
            }
        }
        
        private void SetupCameraBackground()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = backgroundColor;
                Debug.Log("[GuideUIAutoSetup] Camera background color set");
            }
        }
        
        private Canvas CreateOrFindCanvas()
        {
            // Tìm Canvas hiện có
            Canvas existingCanvas = FindFirstObjectByType<Canvas>();
            if (existingCanvas != null)
            {
                Debug.Log("[GuideUIAutoSetup] Using existing Canvas");
                return existingCanvas;
            }
            
            // Tạo Canvas mới
            GameObject canvasObj = new GameObject("GuideCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            // Add CanvasScaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add GraphicRaycaster
            canvasObj.AddComponent<GraphicRaycaster>();
            
            Debug.Log("[GuideUIAutoSetup] Canvas created");
            return canvas;
        }
        
        private void CreateBackgroundPanel(Canvas canvas)
        {
            GameObject bgPanel = new GameObject("BackgroundPanel");
            bgPanel.transform.SetParent(canvas.transform, false);
            
            Image bgImage = bgPanel.AddComponent<Image>();
            bgImage.color = backgroundColor;
            
            // Fullscreen
            RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            Debug.Log("[GuideUIAutoSetup] Background panel created");
        }
        
        private void CreateTitleText(Canvas canvas)
        {
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(canvas.transform, false);
            
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "🐱 HƯỚNG DẪN CHƠI GAME 🐱";
            titleText.fontSize = 48f;
            titleText.color = titleColor;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            
            // Position at top
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.8f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;
            
            Debug.Log("[GuideUIAutoSetup] Title text created");
        }
        
        private void CreateInstructionText(Canvas canvas)
        {
            GameObject instructionObj = new GameObject("InstructionText");
            instructionObj.transform.SetParent(canvas.transform, false);
            
            TextMeshProUGUI instructionText = instructionObj.AddComponent<TextMeshProUGUI>();
            instructionText.text = GetInstructionText();
            instructionText.fontSize = 24f;
            instructionText.color = textColor;
            instructionText.alignment = TextAlignmentOptions.TopLeft;
            
            // Position in middle
            RectTransform instructionRect = instructionObj.GetComponent<RectTransform>();
            instructionRect.anchorMin = new Vector2(0.1f, 0.2f);
            instructionRect.anchorMax = new Vector2(0.9f, 0.8f);
            instructionRect.sizeDelta = Vector2.zero;
            instructionRect.anchoredPosition = Vector2.zero;
            
            Debug.Log("[GuideUIAutoSetup] Instruction text created");
        }
        
        private void CreateButtons(Canvas canvas)
        {
            // Back Button với functionality
            Button backButton = CreateButton(canvas, "BackButton", "◀ QUAY LẠI", new Vector2(0.1f, 0.05f), new Vector2(0.4f, 0.15f));
            
            // Play Button với functionality  
            Button playButton = CreateButton(canvas, "PlayButton", "BẮT ĐẦU CHƠI ▶", new Vector2(0.6f, 0.05f), new Vector2(0.9f, 0.15f));
            
            // Add button events
            SetupButtonEvents(backButton, playButton);
            
            Debug.Log("[GuideUIAutoSetup] Buttons created with functionality");
        }
        
        private Button CreateButton(Canvas canvas, string name, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(canvas.transform, false);
            
            // Button component
            Button button = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            
            // Button position
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.sizeDelta = Vector2.zero;
            buttonRect.anchoredPosition = Vector2.zero;
            
            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 20f;
            buttonText.color = Color.white;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            return button; // Return button component
        }
        
        /// <summary>
        /// Setup button event listeners để quay về menu và start game
        /// </summary>
        private void SetupButtonEvents(Button backButton, Button playButton)
        {
            Debug.Log("[GuideUIAutoSetup] Setting up button events...");
            
            // Back button - quay về MainMenu scene
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() => {
                    Debug.Log($"[GuideUIAutoSetup] 🔴 BACK BUTTON CLICKED! Loading scene: {mainMenuSceneName}");
                    
                    // Kiểm tra scene có tồn tại không
                    if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
                    {
                        SceneManager.LoadScene(mainMenuSceneName);
                    }
                    else
                    {
                        Debug.LogError($"[GuideUIAutoSetup] ❌ Scene '{mainMenuSceneName}' not found! Check Build Settings.");
                        // Fallback: load scene by index
                        if (SceneManager.sceneCountInBuildSettings > 0)
                        {
                            Debug.Log("[GuideUIAutoSetup] Loading scene index 0 as fallback");
                            SceneManager.LoadScene(0);
                        }
                    }
                });
                Debug.Log("[GuideUIAutoSetup] ✅ Back button events set up");
            }
            else
            {
                Debug.LogError("[GuideUIAutoSetup] ❌ Back button is NULL!");
            }
            
            // Play button - chuyển sang SampleScene để chơi game
            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(() => {
                    Debug.Log($"[GuideUIAutoSetup] 🟢 PLAY BUTTON CLICKED! Loading scene: {gameSceneName}");
                    
                    // Kiểm tra scene có tồn tại không
                    if (Application.CanStreamedLevelBeLoaded(gameSceneName))
                    {
                        SceneManager.LoadScene(gameSceneName);
                    }
                    else
                    {
                        Debug.LogError($"[GuideUIAutoSetup] ❌ Scene '{gameSceneName}' not found! Check Build Settings.");
                        // Fallback: load scene by index
                        if (SceneManager.sceneCountInBuildSettings > 1)
                        {
                            Debug.Log("[GuideUIAutoSetup] Loading scene index 1 as fallback");
                            SceneManager.LoadScene(1);
                        }
                    }
                });
                Debug.Log("[GuideUIAutoSetup] ✅ Play button events set up");
            }
            else
            {
                Debug.LogError("[GuideUIAutoSetup] ❌ Play button is NULL!");
            }
            
            Debug.Log("[GuideUIAutoSetup] Button events configuration completed");
        }
        
        private void AddGuideManager(Canvas canvas)
        {
            GuideManager guideManager = canvas.GetComponent<GuideManager>();
            if (guideManager == null)
            {
                guideManager = canvas.gameObject.AddComponent<GuideManager>();
                Debug.Log("[GuideUIAutoSetup] GuideManager added to Canvas");
            }
        }
        
        private string GetInstructionText()
        {
            return @"🎮 CÁCH CHƠI:

🏃‍♀️ DI CHUYỂN:
• Nhấn D hoặc mũi tên PHẢI để di chuyển sang phải
• Nhấn A hoặc mũi tên TRÁI để di chuyển sang trái  
• Nhấn SPACE hoặc W để NHẢY

🎯 MUC TIÊU:
• Sống sót càng lâu càng tốt!
• Tránh những con Catcher (kẻ bắt mèo)
• Thu thập điểm số và power-ups

⚠️ NGUY HIỂM:
• Catcher sẽ xuất hiện khi bạn gần rìa màn hình
• Camera sẽ rung khi Catcher xuất hiện - CẢNH BÁO!
• Nếu bị chạm, bạn sẽ bị đẩy ra xa - hãy nhanh chóng chạy trốn!

💡 MẸO:
• Giữ phím D liên tục để chống lại lực trôi ngược
• Nhảy để tránh Catcher hiệu quả hơn
• Quan sát màn hình rung để biết Catcher sắp tới!

🏆 CHÚC BẠN CHƠI VUI VẺ!";
        }
        
        /// <summary>
        /// Debug method để kiểm tra UI setup và button functionality
        /// </summary>
        [ContextMenu("Debug UI Setup")]
        public void DebugUISetup()
        {
            Debug.Log("=== GUIDE UI DEBUG ===");
            
            // Check Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            Debug.Log($"Canvas found: {(canvas != null ? canvas.name : "NULL")}");
            
            if (canvas != null)
            {
                // Check Canvas settings
                Debug.Log($"Canvas Render Mode: {canvas.renderMode}");
                Debug.Log($"Canvas Sorting Order: {canvas.sortingOrder}");
                
                // Check GraphicRaycaster
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                Debug.Log($"GraphicRaycaster: {(raycaster != null ? "Present" : "MISSING!")}");
                
                // Check EventSystem
                UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
                Debug.Log($"EventSystem: {(eventSystem != null ? "Present" : "MISSING!")}");
                
                if (eventSystem == null)
                {
                    Debug.LogError("❌ NO EVENTSYSTEM! Creating one...");
                    CreateEventSystem();
                }
            }
            
            // Check Buttons
            Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            Debug.Log($"Total buttons found: {buttons.Length}");
            
            foreach (Button btn in buttons)
            {
                Debug.Log($"Button: {btn.name}, Interactable: {btn.interactable}, Listeners: {btn.onClick.GetPersistentEventCount()}");
                
                // Check if button has Image component
                Image btnImage = btn.GetComponent<Image>();
                Debug.Log($"  - Image: {(btnImage != null ? btnImage.color.ToString() : "NULL")}");
                
                // Check if button is raycast target
                if (btnImage != null)
                {
                    Debug.Log($"  - Raycast Target: {btnImage.raycastTarget}");
                }
            }
            
            // Check scenes in build settings
            Debug.Log("=== SCENES IN BUILD SETTINGS ===");
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                Debug.Log($"Scene {i}: {sceneName} ({scenePath})");
            }
        }
        
        /// <summary>
        /// Tạo EventSystem nếu chưa có (cần thiết để buttons hoạt động)
        /// </summary>
        private void CreateEventSystem()
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[GuideUIAutoSetup] ✅ EventSystem created");
        }
        
        /// <summary>
        /// Test button click trực tiếp
        /// </summary>
        [ContextMenu("Test Button Clicks")]
        public void TestButtonClicks()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            
            foreach (Button btn in buttons)
            {
                if (btn.name.Contains("Back"))
                {
                    Debug.Log("🔴 Testing Back Button Click...");
                    btn.onClick.Invoke();
                }
                else if (btn.name.Contains("Play"))
                {
                    Debug.Log("🟢 Testing Play Button Click...");
                    btn.onClick.Invoke();
                }
            }
        }
        }
    }
