using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject instructionPanel;
    public void Play()
    {
        SceneManager.LoadScene(1);
    }
 
    public void HighSocre()
    {
        SceneManager.LoadScene(2);
    }
    public void Intructions()
    {
        instructionPanel.SetActive(!instructionPanel.activeSelf);
    }
}
