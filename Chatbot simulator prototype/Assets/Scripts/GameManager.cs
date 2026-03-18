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
        // Ensure the response callback is wired to this instance (prevents Inspector mis-wiring across scenes)
        chatGPT.chatGPTResponse.RemoveListener(ReceiveChatMessage);
        chatGPT.chatGPTResponse.AddListener(ReceiveChatMessage);
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

    string BuildJsonOnlySystemPrompt(string personalityPrompt)
    {
        return personalityPrompt + "\n\n" +
               "IMPORTANT OUTPUT FORMAT:\n" +
               "- Reply with ONLY a single JSON object.\n" +
               "- No extra text, no markdown, no code fences.\n" +
               "- Schema:\n" +
               "  {\"animation_name\":\"idle|shy|confused|joking|surprise|focus|angry|cheers|nod|waving_arm|proud\",\"reply_to_player\":\"...\"}\n";
    }
    void Start()
    {
        // Load selected personality from PlayerPrefs
        int selectedIndex = PlayerPrefs.GetInt("selectedIndex", 0);
        gameSettings.selectedIndex = selectedIndex;
        
        // Set the personality
        if (personalityDB != null && selectedIndex < personalityDB.personalities.Length)
        {
            npcName = personalityDB.personalities[selectedIndex].name;
            // Use personality as system prompt (more reliable than sending as a user message)
            chatGPT.ResetChat(BuildJsonOnlySystemPrompt(personalityDB.personalities[selectedIndex].initialPrompt));
            chatGPT.SendToChatGPT("{\"player_said\":\"Hello! Who are you?\"}");
        }
        else
        {
            chatGPT.ResetChat(BuildJsonOnlySystemPrompt("You are an NPC in a game. Stay in character."));
            chatGPT.SendToChatGPT("{\"player_said\":\"Hello! Who are you?\"}");
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
            tX_AIReply.text = "Thinking...";
            chatGPT.SendToChatGPT("{\"player_said\""+":\""+playerMessage+"\"}");
            ClearText();
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
            if (tX_AIReply == null)
            {
                Debug.LogWarning("GameManager: tX_AIReply is not assigned in the Inspector.");
                return;
            }
            // Try to extract a JSON object from the model output
            int firstBrace = message.IndexOf("{");
            int lastBrace = message.LastIndexOf("}");
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                message = message.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
            else if(!message.EndsWith("}"))
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
            if (npc != null)
            {
                npc.showAnimation(npcJson.animation_name);
            }
            else
            {
                Debug.LogWarning("GameManager: npc is not assigned in the Inspector.");
            }
        }
        catch (System.Exception e)
        {
            print(e.Message);
            if (tX_AIReply != null)
            {
                // If the model didn't return JSON, show the raw text instead of breaking gameplay.
                string raw = message ?? "";
                raw = raw.Trim();
                if (string.IsNullOrEmpty(raw)) raw = "I don't understand what you said.";
                tX_AIReply.text = "<color=#ff7082>" + npcName + ":</color>" + raw;
            }
            if (npc != null)
            {
                npc.showAnimation("confused");
            }
        }
    }
}
