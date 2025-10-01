using UnityEngine;


// Sử dụng lại enum GameStage đã khai báo ở file khác

public class BackgroundSpriteChanger : MonoBehaviour
{
    public SpriteRenderer backgroundRenderer;
    public Sprite[] stageBackgrounds; // 0: Dust, 1: Day, 2: Dawn
    public float[] stageTimes = { 0f, 10f, 20f }; // Thời điểm chuyển stage (giây)
    private GameStage currentStage = GameStage.Dust;
    private float timer = 0f;

    void Start()
    {
        UpdateBackground();
    }

    void Update()
    {
        timer += Time.deltaTime;
        CheckStageByTime(timer);
    }

    void CheckStageByTime(float time)
    {
        if (time < stageTimes[1])
            SetStage(GameStage.Dust);
        else if (time < stageTimes[2])
            SetStage(GameStage.Day);
        else
            SetStage(GameStage.Dawn);
    }

    public void SetStage(GameStage stage)
    {
        if (currentStage != stage)
        {
            currentStage = stage;
            StartCoroutine(SmoothTransition(1f));
        }
    }

    void UpdateBackground()
    {
        backgroundRenderer.sprite = stageBackgrounds[(int)currentStage];
    }

    private System.Collections.IEnumerator SmoothTransition(float duration)
    {
        Color startColor = backgroundRenderer.color;

        // Fade out
        for (float t = 0; t < duration/2; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(1f, 0f, t / (duration/2));
            backgroundRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // Change sprite
        UpdateBackground();

        // Fade in
        for (float t = 0; t < duration/2; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(0f, 1f, t / (duration/2));
            backgroundRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        backgroundRenderer.color = startColor;
    }
}