using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    public TMP_Text balanceText;
    public TMP_Text scoreText;

    [Header("Saved Progress Defaults")]
    [SerializeField] private int initialEarnings;
    [SerializeField] private int initialScore;
    [Tooltip("When enabled, replaces the saved balance with Initial Earnings each time MainMenu opens.")]
    [SerializeField] private bool resetSavedBalanceOnStart;
    [Tooltip("When enabled, replaces the saved high score with Initial Score each time MainMenu opens.")]
    [SerializeField] private bool resetSavedScoreOnStart;
    private bool startingGame;

    void Start()
    {
        if(resetSavedBalanceOnStart)
        {
            SavedBalance.Set(initialEarnings);
        }

        if(resetSavedScoreOnStart)
        {
            SavedHighScore.Set(initialScore);
        }

        balanceText.text = "₱" + SavedBalance.CurrentBalance;
        scoreText.text = SavedHighScore.CurrentScore.ToString();
    }


    public void PlayGame()
    {
        if (startingGame)
            return;
        startingGame = true;
        Time.timeScale = 1f;
        PersistentBackgroundMusic.StopMusic();
        SceneManager.LoadScene("Loading");
    }

    public void Settings()
    {
        SceneManager.LoadScene("Settings");
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
