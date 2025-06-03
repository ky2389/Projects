using UnityEngine;
using ChatGPTWrapper;
using TMPro;
public class GameManager : MonoBehaviour
{
    static GameManager instance = null;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        chatGPT.Init();
    }
    
    [SerializeField]
    ChatGPTConversation chatGPT;
    [SerializeField]
    TMP_InputField iF_Playertalk;
    [SerializeField]
    TextMeshProUGUI tX_AIReply;
    [SerializeField]
    NPCController npc;
    [SerializeField]
    PersonalityDB personalityDB;
    [SerializeField]
    GameSettings gameSettings;
    string npcName = "Coco";
    string playerName = "Player";
    void Start()
    {
        // Load selected personality from PlayerPrefs
        int selectedIndex = PlayerPrefs.GetInt("selectedIndex", 0);
        gameSettings.selectedIndex = selectedIndex;
        
        // Set the personality
        if (personalityDB != null && selectedIndex < personalityDB.personalities.Length)
        {
            npcName = personalityDB.personalities[selectedIndex].name;
            // Send the initial prompt for the selected personality
            chatGPT.SendToChatGPT(personalityDB.personalities[selectedIndex].initialPrompt);
        }
        else
        {
            chatGPT.SendToChatGPT("{\"player_said\""+":\"Hello! Who are you?\"}");
        }
    }
    void Update()
    {
        if(Input.GetButtonUp("Submit"))
        {
            SubmitChatMessage();
        }
    }
    public void SubmitChatMessage()
    {
        string playerMessage = iF_Playertalk.text;
        if(!string.IsNullOrEmpty(playerMessage))
        {
            chatGPT.SendToChatGPT("{\"player_said\""+":\""+playerMessage+"\"}");
            ClearText();
            tX_AIReply.text = "Thinking...";
        }
    }
    void ClearText()
    {
        iF_Playertalk.text = string.Empty;
    }
    public void ReceiveChatMessage(string message)
    {
        print(message);
        try{
            if(!message.EndsWith("}"))
            {
                if(message.Contains("}"))
                {
                    message = message.Substring(0,message.LastIndexOf("}")+1);
                }
                else
                {
                    message = message + "}";
                }
            }
            message=message.Replace("\\","\\\\");
            message=message.Replace("\\\\\"","\\\"");
            NPCJsonReceiver npcJson = JsonUtility.FromJson<NPCJsonReceiver>(message);
            string talkline = npcJson.reply_to_player;
            tX_AIReply.text = "<color=#ff7082>"+npcName+":</color>"+talkline;
            npc.showAnimation(npcJson.animation_name);
        }
        catch (System.Exception e)
        {
            print(e.Message);
            tX_AIReply.text = "<color=#ff7082>"+npcName+":</color>"+"I don't understand what you said.";
        }
    }
}
