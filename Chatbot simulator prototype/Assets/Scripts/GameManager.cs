using UnityEngine;
using ChatGPTWrapper;
using TMPro;
using PonyuDev.SherpaOnnx.Tts;
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
    [SerializeField]
    TtsOrchestrator tts;
    // Dedicated AudioSource that all TTS speech is routed through. Must live on the
    // same GameObject as the uLipSync analyzer so lip sync can hook via OnAudioFilterRead.
    [SerializeField]
    AudioSource ttsAudioSource;
    bool ttsInitializedHookSet = false;
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

        ApplyTtsProfileForCurrentPersonality();
    }

    // Switches the TTS engine to the voice configured for the currently selected
    // personality. Async safe: if the engine is not initialized yet, defers until it is.
    void ApplyTtsProfileForCurrentPersonality()
    {
        if (tts == null) return;
        if (!tts.IsInitialized)
        {
            if (!ttsInitializedHookSet)
            {
                tts.Initialized += ApplyTtsProfileForCurrentPersonality;
                ttsInitializedHookSet = true;
            }
            return;
        }
        if (ttsInitializedHookSet)
        {
            tts.Initialized -= ApplyTtsProfileForCurrentPersonality;
            ttsInitializedHookSet = false;
        }

        int idx = gameSettings != null ? gameSettings.selectedIndex : PlayerPrefs.GetInt("selectedIndex", 0);
        if (personalityDB == null || idx < 0 || idx >= personalityDB.personalities.Length) return;
        string profileName = personalityDB.personalities[idx].ttsProfileName;
        if (string.IsNullOrEmpty(profileName)) return;
        try
        {
            tts.Service.SwitchProfile(profileName);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("GameManager: failed to switch TTS profile to '" + profileName + "': " + e.Message);
        }
    }

    // Plays the given line through the NPC's voice. Routed through ttsAudioSource so
    // uLipSync (on the same GameObject) can analyze the stream via OnAudioFilterRead.
    // Silently no-ops if TTS isn't ready so the chat loop keeps working.
    void SpeakTalkline(string text)
    {
        if (tts == null || !tts.IsInitialized) return;
        if (ttsAudioSource == null)
        {
            Debug.LogWarning("GameManager: ttsAudioSource is not assigned; lip sync will not run.");
            return;
        }
        if (string.IsNullOrWhiteSpace(text)) return;
        try
        {
            _ = tts.Service.GenerateAndPlayAsync(text, ttsAudioSource);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("GameManager: TTS GenerateAndPlay failed: " + e.Message);
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
        string original = message ?? "";
        print(original);
        string finalReply;
        string finalAnimation;
        try
        {
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
            else if (!message.EndsWith("}"))
            {
                if (message.Contains("}"))
                {
                    message = message.Substring(0, message.LastIndexOf("}") + 1);
                }
                else
                {
                    message = message + "}";
                }
            }
            message = message.Replace("\\", "\\\\");
            message = message.Replace("\\\\\"", "\\\"");
            NPCJsonReceiver npcJson = JsonUtility.FromJson<NPCJsonReceiver>(message);
            if (npcJson == null || string.IsNullOrEmpty(npcJson.reply_to_player))
            {
                // JsonUtility silently returns empty fields for malformed JSON; treat as parse failure.
                throw new System.Exception("Parsed JSON has empty reply_to_player.");
            }
            finalReply = npcJson.reply_to_player;
            finalAnimation = string.IsNullOrEmpty(npcJson.animation_name) ? "idle" : npcJson.animation_name;
        }
        catch (System.Exception e)
        {
            print(e.Message);
            // Fallback: try to recover fields from near-JSON like {animation: happy reply_to_player:"..."}
            finalReply = ExtractJsonField(original, "reply_to_player");
            finalAnimation = ExtractJsonField(original, "animation_name");
            if (string.IsNullOrEmpty(finalAnimation)) finalAnimation = ExtractJsonField(original, "animation");
            if (string.IsNullOrEmpty(finalAnimation)) finalAnimation = "confused";
            if (string.IsNullOrEmpty(finalReply)) finalReply = "I don't understand what you said.";
        }

        if (tX_AIReply != null)
        {
            tX_AIReply.text = "<color=#ff7082>" + npcName + ":</color>" + finalReply;
        }
        if (npc != null)
        {
            npc.showAnimation(finalAnimation);
        }
        else
        {
            Debug.LogWarning("GameManager: npc is not assigned in the Inspector.");
        }
        // Replace the just-appended assistant message with canonical clean JSON so the
        // model never sees its own malformed output as a few-shot example next turn.
        if (chatGPT != null)
        {
            chatGPT.ReplaceLastBotMessage(BuildCanonicalReplyJson(finalAnimation, finalReply));
        }
        SpeakTalkline(finalReply);
    }

    static string BuildCanonicalReplyJson(string animationName, string reply)
    {
        return "{\"animation_name\":\"" + EscapeJson(animationName) +
               "\",\"reply_to_player\":\"" + EscapeJson(reply) + "\"}";
    }

    static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
    }

    // Matches well-formed and unquoted-key variants: "name":"v", name:"v", "name" : "v", etc.
    static string ExtractJsonField(string source, string fieldName)
    {
        if (string.IsNullOrEmpty(source)) return "";
        var pattern = "\"?" + System.Text.RegularExpressions.Regex.Escape(fieldName) +
                      "\"?\\s*:\\s*\"([^\"]*)\"";
        var match = System.Text.RegularExpressions.Regex.Match(source, pattern);
        return match.Success ? match.Groups[1].Value : "";
    }
}
