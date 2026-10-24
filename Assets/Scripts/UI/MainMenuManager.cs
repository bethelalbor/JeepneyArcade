using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    public TMP_Text balanceText;
    public TMP_Text scoreText;


    void Start()
    {
        balanceText.text = "₱" + SavedBalance.CurrentBalance;
        scoreText.text = SavedHighScore.CurrentScore.ToString();
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
