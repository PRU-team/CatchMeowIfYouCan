using UnityEngine;
using UnityEngine.SceneManagement;

public class HighsocreManager : MonoBehaviour
{
    public void Menu()
    {
        SceneManager.LoadScene(0);
    }
    public void Play()
    {
        SceneManager.LoadScene(2);
    }
}
