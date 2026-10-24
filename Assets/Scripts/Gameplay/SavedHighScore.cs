using UnityEngine;

public static class SavedHighScore
{
    private const string HighScoreKey = "JeepneyArcade.HighScore";


    public static int CurrentScore => PlayerPrefs.GetInt(HighScoreKey, 0);


    public static void SetIfHigher(int score)
    {
        if(score <= CurrentScore)
            return;

        PlayerPrefs.SetInt(HighScoreKey, score);
        PlayerPrefs.Save();
    }


    public static void Set(int score)
    {
        PlayerPrefs.SetInt(HighScoreKey, score);
        PlayerPrefs.Save();
    }
}
