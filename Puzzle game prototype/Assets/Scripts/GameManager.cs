using UnityEngine;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }
    
    public void LoadScene(string sceneName)
    {
        if (sceneName == "Settings")
        {
            SceneManager.LoadScene(sceneName,LoadSceneMode.Additive);
            return;
        }
        else{
            SceneManager.LoadScene(sceneName);
        }
    }
    void Update()
    {
        //if in scene "settings", activate the cursor
        if(SceneManager.GetActiveScene().name == "Settings" || SceneManager.GetActiveScene().name == "Home Page")
        {
            Cursor.visible = true;
        }
        else
        {
            Cursor.visible = false;
        }
    }
}
