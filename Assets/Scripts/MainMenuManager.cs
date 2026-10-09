using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("OnePlayerGame");
    }

  public void Customize()
      {
          SceneManager.LoadScene("Customization");
      }


    public void ExitGame()
    {
        Application.Quit();
    }
}