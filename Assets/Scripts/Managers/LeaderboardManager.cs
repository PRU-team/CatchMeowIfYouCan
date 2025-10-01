using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    [System.Serializable]
    public class LeaderboardData
    {
        public List<int> scores = new List<int>();
    }

    public static class LeaderboardManager
    {
        private const string KEY = "Leaderboard";

        // Lấy danh sách điểm đã lưu
        public static LeaderboardData Load()
        {
            string json = PlayerPrefs.GetString(KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                return new LeaderboardData();
            }
            return JsonUtility.FromJson<LeaderboardData>(json);
        }

        // Lưu danh sách điểm
        public static void Save(LeaderboardData data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(KEY, json);
            PlayerPrefs.Save();
        }

        // Thêm điểm mới
        public static void AddScore(int score)
        {
            LeaderboardData data = Load();
            data.scores.Add(score);

            // Sắp xếp giảm dần
            data.scores.Sort((a, b) => b.CompareTo(a));

            // Giữ tối đa 5 lần chơi
            if (data.scores.Count > 5)
                data.scores.RemoveAt(5);

            Save(data);
        }
    }
}
