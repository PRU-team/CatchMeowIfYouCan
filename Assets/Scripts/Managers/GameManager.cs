using Assets.Scripts.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float initialGameSpeed = 5f;
    public float gameSpeedIncrease = 0.1f;
    public float gameSpeed { get; private set; }
    public TMP_Text totalCoinText;  // TextMeshPro text
    public GameObject pauseMenuPanel;
    public GameObject gameOverPanel;
    private int totalCoins;
    private Player player;
    private Spawner spawner;
    public int HighScore { get; private set; }
    private bool isPaused;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        player = FindObjectOfType<Player>();
        spawner = FindObjectOfType<Spawner>();
        HighScore = PlayerPrefs.GetInt("HighScore", 0);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        NewGame();
    }

    private void NewGame()
    {

        totalCoins = 0;
        UpdateCoinUI();
        gameSpeed = initialGameSpeed;
        enabled = true;

        player.gameObject.SetActive(true);
        spawner.gameObject.SetActive(true);
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        gameSpeed = 0f;
        enabled = false;

        player.gameObject.SetActive(false);
        spawner.gameObject.SetActive(false);
        if (totalCoins > HighScore)
        {
            HighScore = totalCoins;
            PlayerPrefs.SetInt("HighScore", HighScore);
            PlayerPrefs.Save();
        }
        LeaderboardManager.AddScore(totalCoins);
         gameOverPanel.SetActive(true);
        Time.timeScale = 0f;

        Debug.Log("Game Over!");
    }
    public int GetTotalCoins()
    {
        return totalCoins;
    }
    private void Update()
    {
        gameSpeed += gameSpeedIncrease * Time.deltaTime;
    }
    public void AddCoin(int amount = 1)
    {
        totalCoins += amount;
        UpdateCoinUI();
    }
    private void UpdateCoinUI()
    {
        if (totalCoinText != null)
            totalCoinText.text = totalCoins.ToString();
    }
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // dừng game
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // chạy lại game
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }
    public void QuitGame()
    {
        if (GameManager.Instance != null)
        {
            int finalCoins = GetTotalCoins();   // lấy số coin hiện tại
            LeaderboardManager.AddScore(finalCoins);
            Debug.Log($"[GameManager] Saved {finalCoins} coins to Leaderboard before quitting.");
        }

        Time.timeScale = 1f; // reset lại để tránh scene menu bị dừng
        SceneManager.LoadScene(0); // quay lại Menu
    }
    public void Play()
    {
        SceneManager.LoadScene(1);
    }
    public void ScoreScene()
    {
        SceneManager.LoadScene(3);
    }
}
