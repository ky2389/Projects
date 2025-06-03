using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Scriptable Objects/GameSettings", order=1)]
public class GameSettings : ScriptableObject
{
    public float gameTimer=0;
    public int selectedIndex;
    public void ResetGame()
    {
        gameTimer=0;
        selectedIndex=-1;
    }
}
