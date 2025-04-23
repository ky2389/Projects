using UnityEngine;

public class Setting : MonoBehaviour
{
    public void loadScene(string sceneName)
    {
        GameManager.instance.LoadScene(sceneName);
    }
}
