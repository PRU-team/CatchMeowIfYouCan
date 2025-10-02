using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace CatchMeowIfYouCan.UI
{
    /// <summary>
    /// GuideManager - Quản lý trang hướng dẫn chơi game
    /// Hiển thị các hướng dẫn cơ bản về di chuyển và gameplay
    /// </summary>
    public class GuideManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas guideCanvas;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button playButton;
        
        [Header("Background Settings")]
        [SerializeField] private Color backgroundColor = new Color(0.2f, 0.6f, 0.8f, 1f); // Màu xanh
        
        [Header("Text Settings")]
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color titleColor = Color.yellow;
        
        [Header("Scene Navigation")]
        [SerializeField] private string mainMenuSceneName = "Menu";
        [SerializeField] private string gameSceneName = "SampleScene";
        
        private void Awake()
        {
            SetupGuideUI();
        }
        
        private void Start()
        {
            SetupButtonEvents();
            SetupBackgroundAndColors();
            SetupGuideText();
        }
        
        /// <summary>
        /// Tự động tạo UI nếu chưa có references
        /// </summary>
        private void SetupGuideUI()
        {
            // Tìm hoặc tạo Canvas
            if (guideCanvas == null)
            {
                GameObject canvasObj = GameObject.Find("GuideCanvas");
                if (canvasObj != null)
                {
                    guideCanvas = canvasObj.GetComponent<Canvas>();
                }
            }
            
            // Tự động tìm các UI components
            if (guideCanvas != null)
            {
                backgroundImage = guideCanvas.GetComponentInChildren<Image>();
                titleText = GameObject.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                instructionText = GameObject.Find("InstructionText")?.GetComponent<TextMeshProUGUI>();
                backButton = GameObject.Find("BackButton")?.GetComponent<Button>();
                playButton = GameObject.Find("PlayButton")?.GetComponent<Button>();
            }
        }
        
        /// <summary>
        /// Setup màu nền và màu chữ
        /// </summary>
        private void SetupBackgroundAndColors()
        {
            // Set background color
            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }
            else
            {
                // Set camera background nếu không có UI background
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    mainCamera.backgroundColor = backgroundColor;
                }
            }
            
            // Set text colors
            if (titleText != null)
            {
                titleText.color = titleColor;
            }
            
            if (instructionText != null)
            {
                instructionText.color = textColor;
            }
        }
        
        /// <summary>
        /// Setup nội dung hướng dẫn
        /// </summary>
        private void SetupGuideText()
        {
            // Title
            if (titleText != null)
            {
                titleText.text = "🐱 HƯỚNG DẪN CHƠI GAME 🐱";
                titleText.fontSize = 48f;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
            }
            
            // Instructions
            if (instructionText != null)
            {
                instructionText.text = GetInstructionText();
                instructionText.fontSize = 24f;
                instructionText.alignment = TextAlignmentOptions.Left;
                instructionText.fontStyle = FontStyles.Normal;
            }
        }
        
        /// <summary>
        /// Nội dung hướng dẫn chi tiết
        /// </summary>
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
        /// Setup button events
        /// </summary>
        private void SetupButtonEvents()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(GoBackToMainMenu);
            }
            
            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(StartGame);
            }
        }
        
        /// <summary>
        /// Quay về main menu
        /// </summary>
        public void GoBackToMainMenu()
        {
            Debug.Log("[GuideManager] Returning to Main Menu...");
            SceneManager.LoadScene(mainMenuSceneName);
        }
        
        /// <summary>
        /// Bắt đầu chơi game
        /// </summary>
        public void StartGame()
        {
            Debug.Log("[GuideManager] Starting game...");
            SceneManager.LoadScene(gameSceneName);
        }
        
        /// <summary>
        /// Bật/tắt guide panel
        /// </summary>
        public void ToggleGuide()
        {
            if (guideCanvas != null)
            {
                guideCanvas.gameObject.SetActive(!guideCanvas.gameObject.activeInHierarchy);
            }
        }
        
        /// <summary>
        /// Hiển thị guide
        /// </summary>
        public void ShowGuide()
        {
            if (guideCanvas != null)
            {
                guideCanvas.gameObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// Ẩn guide
        /// </summary>
        public void HideGuide()
        {
            if (guideCanvas != null)
            {
                guideCanvas.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Update màu nền runtime
        /// </summary>
        [ContextMenu("Apply Background Color")]
        public void ApplyBackgroundColor()
        {
            SetupBackgroundAndColors();
        }
        
        /// <summary>
        /// Test button functionality
        /// </summary>
        [ContextMenu("Test Button Events")]
        public void TestButtonEvents()
        {
            Debug.Log("[GuideManager] Testing button events...");
            Debug.Log($"Back Button: {(backButton != null ? "Found" : "Missing")}");
            Debug.Log($"Play Button: {(playButton != null ? "Found" : "Missing")}");
        }
        
        private void Update()
        {
            // ESC để quay về main menu
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GoBackToMainMenu();
            }
            
            // Enter để start game
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                StartGame();
            }
        }
        
        private void OnValidate()
        {
            // Tự động apply màu khi thay đổi trong Inspector
            if (Application.isPlaying)
            {
                SetupBackgroundAndColors();
            }
        }
    }
}