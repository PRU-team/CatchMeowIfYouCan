using UnityEngine;

public enum GameStage
{
    Dust,
    Day,
    Dawn
}

public class StageManager : MonoBehaviour
{
    public SpriteRenderer backgroundRenderer;
    public Sprite[] stageBackgrounds; // 0: Dust, 1: Day, 2: Dawn
    public GameStage currentStage = GameStage.Dust;

    public float[] stageTimes = { 0f, 10f, 20f }; // Thời điểm chuyển stage (giây)
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
            UpdateBackground();
        }
    }

    void UpdateBackground()
    {
        backgroundRenderer.sprite = stageBackgrounds[(int)currentStage];
    }
}
