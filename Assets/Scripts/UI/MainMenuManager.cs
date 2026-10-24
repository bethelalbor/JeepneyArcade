using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    public TMP_Text balanceText;


    void Start()
    {
        balanceText.text = "₱" + SavedBalance.CurrentBalance;
    }


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
