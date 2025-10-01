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
        //Obstacle[] obstacles = FindObjectsOfType<Obstacle>();

        //foreach (Obstacle obstacle in obstacles)
        //{
        //    Destroy(obstacle.gameObject);
        //}
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
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
        Debug.Log("Game Over!");
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
        SceneManager.LoadScene(0);
    }
    public void Play()
    {
        SceneManager.LoadScene(1);
    }
    public void ScoreScene()
    {
        SceneManager.LoadScene(2);
    }
}
