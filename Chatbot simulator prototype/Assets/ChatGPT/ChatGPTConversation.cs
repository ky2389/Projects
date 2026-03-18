using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.Networking;

namespace ChatGPTWrapper {

    public class ChatGPTConversation : MonoBehaviour
    {
        [SerializeField]
        private string _apiKey = null;

        public enum Model {
            ChatGPT,
            Davinci,
            Curie
        }
        [SerializeField]
        public Model _model = Model.ChatGPT;
        private string _selectedModel = null;
        [SerializeField]
        private int _maxTokens = 3072;
        [SerializeField]
        private float _temperature = 0.6f;
        
        private string _uri;
        private string _healthUri;
        private List<(string, string)> _reqHeaders;
        

        private Requests requests = new Requests();
        private Prompt _prompt;
        private Chat _chat;
        private string _lastUserMsg;
        private string _lastChatGPTMsg;
        private bool _ollamaAvailable = false;

        [SerializeField]
        private string _chatbotName = "ChatGPT";

        [TextArea(4,6)]
        public string _initialPrompt = "You are ChatGPT, a large language model trained by OpenAI.";


        public UnityStringEvent chatGPTResponse = new UnityStringEvent();



        public void Init()
        {
            _reqHeaders = new List<(string, string)>
            { 
                ("Content-Type", "application/json")
            };
            switch (_model) {
                case Model.ChatGPT:
                    _chat = new Chat(_initialPrompt);
                    // Use local Ollama chat endpoint with Qwen2.5 model
                    _uri = "http://localhost:11434/api/chat";
                    _healthUri = "http://localhost:11434/api/version";
                    _selectedModel = "qwen2.5:3b";
                    StartCoroutine(CheckOllamaAvailability());
                    break;
                case Model.Davinci:
                    _prompt = new Prompt(_chatbotName, _initialPrompt);
                    _uri = "https://api.openai.com/v1/completions";
                    _selectedModel = "text-davinci-003";
                    break;
                case Model.Curie:
                    _prompt = new Prompt(_chatbotName, _initialPrompt);
                    _uri = "https://api.openai.com/v1/completions";
                    _selectedModel = "text-curie-001";
                    break;
            }
        }

        public void ResetChat(string initialPrompt) {
            switch (_model) {
                case Model.ChatGPT:
                    _chat = new Chat(initialPrompt);
                    break;
                default:
                    _prompt = new Prompt(_chatbotName, initialPrompt);
                    break;
            }
        }

        public void SendToChatGPT(string message)
        {
            _lastUserMsg = message;

            if (_model == Model.ChatGPT) {
                if (!_ollamaAvailable)
                {
                    // Keep gameplay running even if Ollama isn't installed/running.
                    chatGPTResponse.Invoke(GetOllamaNotRunningFallbackJson());
                    StartCoroutine(CheckOllamaAvailability());
                    return;
                }

                _chat.AppendMessage(Chat.Speaker.User, message);

                ChatGPTReq reqObj = new ChatGPTReq();
                reqObj.model = _selectedModel;
                reqObj.messages = _chat.CurrentChat;
        
                string json = JsonUtility.ToJson(reqObj);

                // Use a request flow that always resolves (or falls back) so UI won't get stuck.
                StartCoroutine(PostOllamaChat(json));

               

            } else {
                _prompt.AppendText(Prompt.Speaker.User, message);

                GPTReq reqObj = new GPTReq();
                reqObj.model = _selectedModel;
                reqObj.prompt = _prompt.CurrentPrompt;
                reqObj.max_tokens = _maxTokens;
                reqObj.temperature = _temperature;
                string json = JsonUtility.ToJson(reqObj);

                StartCoroutine(requests.PostReq<GPTRes>(_uri, json, ResolveGPT, _reqHeaders));
            }
        }

        private void ResolveChatGPT(ChatGPTRes res)
        {
            // Support both OpenAI-style (choices) and Ollama-style (single message) responses
            if (res.choices != null && res.choices.Count > 0 && res.choices[0].message != null)
            {
                _lastChatGPTMsg = res.choices[0].message.content;
            }
            else if (res.message != null)
            {
                _lastChatGPTMsg = res.message.content;
            }
            else
            {
                _lastChatGPTMsg = "";
            }

            _chat.AppendMessage(Chat.Speaker.ChatGPT, _lastChatGPTMsg);
            chatGPTResponse.Invoke(_lastChatGPTMsg);

            //If tokens over limitation, remove the oldest message after initial prompt
			int totalToken = 0;
            if (res.usage != null)
            {
                totalToken = res.usage.total_tokens;
            }
            else
            {
                totalToken = res.eval_count + res.prompt_eval_count;
            }
			if (totalToken > 3850)
			{
				_chat.RemoveOldestMessage();
			}
			print("token: " + totalToken);
		}

        private IEnumerator CheckOllamaAvailability()
        {
            if (string.IsNullOrEmpty(_healthUri))
            {
                _ollamaAvailable = false;
                yield break;
            }

            using (var req = UnityWebRequest.Get(_healthUri))
            {
                req.timeout = 2;
                yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
                _ollamaAvailable = req.result == UnityWebRequest.Result.Success;
#else
                _ollamaAvailable = string.IsNullOrWhiteSpace(req.error);
#endif
            }
        }

        private IEnumerator PostOllamaChat(string json)
        {
            using (var req = new UnityWebRequest(_uri, "POST"))
            {
                req.timeout = 30;
                byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                for (int i = 0; i < _reqHeaders.Count; i++)
                {
                    req.SetRequestHeader(_reqHeaders[i].Item1, _reqHeaders[i].Item2);
                }

                yield return req.SendWebRequest();

                bool ok;
#if UNITY_2020_3_OR_NEWER
                ok = req.result == UnityWebRequest.Result.Success;
#else
                ok = string.IsNullOrWhiteSpace(req.error);
#endif
                if (!ok)
                {
                    _ollamaAvailable = false;
                    chatGPTResponse.Invoke(GetOllamaNotRunningFallbackJson());
                    yield break;
                }

                var responseJson = JsonUtility.FromJson<ChatGPTRes>(req.downloadHandler.text);
                ResolveChatGPT(responseJson);
            }
        }

        private string GetOllamaNotRunningFallbackJson()
        {
            return "{\"animation_name\":\"confused\",\"reply_to_player\":\"Local AI is not running. Please start Ollama: 1) open PowerShell 2) run: ollama serve 3) make sure the model exists: ollama list\"}";
        }

        private void ResolveGPT(GPTRes res)
        {
            _lastChatGPTMsg = res.choices[0].text
                .TrimStart('\n')
                .Replace("<|im_end|>", "");

            _prompt.AppendText(Prompt.Speaker.Bot, _lastChatGPTMsg);
            chatGPTResponse.Invoke(_lastChatGPTMsg);
        }
    }
}
