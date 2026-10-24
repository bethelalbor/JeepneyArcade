using UnityEngine;
using TMPro;


public class ScoreUI : MonoBehaviour
{
    public TMP_Text scoreText;


    void Update()
    {
        if(ScoreManager.Instance != null)
        {
            scoreText.text = ScoreManager.Instance.currentScore.ToString();
        }
    }
}
