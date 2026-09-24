using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("OnePlayerGame");
    }


    public void ExitGame()
    {
        Application.Quit();
    }
}