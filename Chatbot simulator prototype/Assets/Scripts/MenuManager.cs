using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;
public class MenuManager : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI tX_SelectedPersonality;
    [SerializeField]
    PersonalityDB personalityDB;
    [SerializeField]
    GameSettings gameSettings;
    [SerializeField]
    ToggleGroup toggleGroup;
    [SerializeField]
    Toggle[] toggles;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for(int i=0;i<toggles.Length;i++){
            toggles[i].GetComponentInChildren<Text>().text=personalityDB.personalities[i].name;
        }
        //Load the data
        gameSettings.gameTimer=PlayerPrefs.GetFloat("gameTimer",0);
        gameSettings.selectedIndex=PlayerPrefs.GetInt("selectedIndex",0);
        tX_SelectedPersonality.text="Current Personality: "+personalityDB.personalities[gameSettings.selectedIndex].name;
        toggles[gameSettings.selectedIndex].isOn=true;
    }
    public void OnValueChanged(){
        var currentToggle=toggleGroup.ActiveToggles().FirstOrDefault();
        int currentSelectedIndex=0;
        for(int i=0;i<toggles.Length;i++){
            if(currentToggle==toggles[i]){
                currentSelectedIndex=i;
                break;
            }
        }
        gameSettings.selectedIndex=currentSelectedIndex;
        tX_SelectedPersonality.text="Current Personality: "+personalityDB.personalities[currentSelectedIndex].name;
        PlayerPrefs.SetInt("selectedIndex",currentSelectedIndex);
        PlayerPrefs.SetFloat("gameTimer",gameSettings.gameTimer);
        
    }
    public void StartGame(){
    SceneManager.LoadScene("GameState");
}
}

