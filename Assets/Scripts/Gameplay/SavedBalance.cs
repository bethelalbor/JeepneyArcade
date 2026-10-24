using UnityEngine;

public static class SavedBalance
{
    private const string BalanceKey = "JeepneyArcade.SavedBalance";


    public static int CurrentBalance => PlayerPrefs.GetInt(BalanceKey, 0);


    public static void Add(int amount)
    {
        PlayerPrefs.SetInt(BalanceKey, CurrentBalance + amount);
        PlayerPrefs.Save();
    }
}
