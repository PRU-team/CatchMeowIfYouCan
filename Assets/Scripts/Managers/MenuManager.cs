using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject instructionPanel;
    public void Play()
    {
        Debug.Log("[MenuManager] Loading Game Scene (index 1)");
        SceneManager.LoadScene(2);
    }
 
    public void HighSocre()
    {
        Debug.Log("[MenuManager] Loading High Score Scene (index 2)");
        SceneManager.LoadScene(3);
    }
    
    public void Guide()
    {
        Debug.Log("[MenuManager] Loading Guide Scene");
        SceneManager.LoadScene("GuideScene");
    }
    
    public void Intructions()
    {
        Debug.Log("[MenuManager] Toggling instruction panel");
        instructionPanel.SetActive(!instructionPanel.activeSelf);
    }
    
    public void Quit()
    {
        Debug.Log("[MenuManager] Quitting application");
        Application.Quit(); 
    }
}
