using UnityEngine;

public class ReturnButton : MonoBehaviour
{
    public void ReturnToGame()
    {
        // Call the GameManager to load the Game scene
        GameManager.instance.LoadScene("MyScene");
    }
}
