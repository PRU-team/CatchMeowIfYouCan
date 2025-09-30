using UnityEngine;

public class BackgroundSpriteChanger : MonoBehaviour
{
    public SpriteRenderer backgroundRenderer;
    public Sprite[] backgroundSprites;
    private int currentIndex = 0;
    
    public void ChangeBackground()
    {
        currentIndex = (currentIndex + 1) % backgroundSprites.Length;
        backgroundRenderer.sprite = backgroundSprites[currentIndex];
    }
    
    // Smooth transition
    public void ChangeBackgroundSmooth(float duration = 1f)
    {
        StartCoroutine(SmoothTransition(duration));
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
        currentIndex = (currentIndex + 1) % backgroundSprites.Length;
        backgroundRenderer.sprite = backgroundSprites[currentIndex];
        
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