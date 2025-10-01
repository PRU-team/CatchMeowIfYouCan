using Assets.Scripts.Managers;
using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    public TMP_Text[] scoreTexts; // Kéo 5 TMP_Text vào trong Inspector

    private void Start()
    {
        var data = LeaderboardManager.Load();

        for (int i = 0; i < scoreTexts.Length; i++)
        {
            if (i < data.scores.Count)
                scoreTexts[i].text = (i + 1) + ". " + data.scores[i].ToString();
            else
                scoreTexts[i].text = (i + 1) + ". ---";
        }
    }
}
