using UnityEngine;

public class StartGame : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Customize");
    }
}
