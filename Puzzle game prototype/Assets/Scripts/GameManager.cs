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
            // Find and disable all UI canvases in the current scene before loading Settings
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            // foreach (Canvas canvas in canvases)
            // {
            //     // Store the original state in a tag so we can restore it later
            //     canvas.gameObject.tag = canvas.gameObject.activeSelf ? "EnabledCanvas" : "DisabledCanvas";
            //     canvas.gameObject.SetActive(false);
            //     Debug.Log("Canvas disabled: " + canvas.gameObject.name);
            // }
            
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            return;
        }
        else if (sceneName == "MyScene")
        {
            // If we're returning to the game scene from settings, unload the settings scene first
            if (SceneManager.GetSceneByName("Settings").isLoaded)
            {
                SceneManager.UnloadSceneAsync("Settings");
                
                // // Re-enable canvases that were previously enabled
                // Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                // foreach (Canvas canvas in canvases)
                // {
                //     if (canvas.gameObject.CompareTag("EnabledCanvas"))
                //     {
                //         canvas.gameObject.SetActive(true);
                //         canvas.gameObject.tag = "Untagged";
                //         Debug.Log("Canvas re-enabled: " + canvas.gameObject.name);
                //     }
                // }
                return;
            }
            SceneManager.LoadScene(sceneName);
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
