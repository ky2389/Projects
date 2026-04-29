using System.Collections.Generic;
using UnityEngine;

namespace ChatGPTWrapper {

    public class OllamaConversation : LLMConversationBase
    {
        [SerializeField]
        private string _model = "qwen2.5:3b";

        [TextArea(4, 20)]
        public string _initialPrompt = "You are a helpful assistant.";

        private const string _uri = "http://localhost:11434/api/chat";
        private static readonly List<(string, string)> _reqHeaders = new List<(string, string)>
        {
            ("Content-Type", "application/json")
        };
        private Requests _requests = new Requests();
        private Chat _chat;

        public override void Init()
        {
            string initialPrompt = global::AIPromptConfig.Load().ollamaInitialPrompt;
            _chat = new Chat(string.IsNullOrWhiteSpace(initialPrompt) ? _initialPrompt : initialPrompt);
        }

        public override void SendToChatGPT(string message)
        {
            _chat.AppendMessage(Chat.Speaker.User, message);

            var reqObj = new OllamaReq
            {
                model = _model,
                messages = _chat.CurrentChat,
                stream = false
            };
            string json = JsonUtility.ToJson(reqObj);

            StartCoroutine(_requests.PostReq<OllamaRes>(_uri, json, ResolveOllama, _reqHeaders));
        }

        private void ResolveOllama(OllamaRes res)
        {
            if (res.message == null || string.IsNullOrEmpty(res.message.content))
            {
                Debug.LogError("Ollama returned empty response");
                return;
            }
            string content = res.message.content;
            _chat.AppendMessage(Chat.Speaker.ChatGPT, content);
            chatGPTResponse.Invoke(content);
        }

        [System.Serializable]
        private class OllamaReq
        {
            public string model;
            public List<Message> messages;
            public bool stream;
        }

        [System.Serializable]
        private class OllamaRes
        {
            public Message message;
        }
    }
}
