using UnityEngine;
using UnityEngine.Events;

namespace ChatGPTWrapper {

    public abstract class LLMConversationBase : MonoBehaviour
    {
        public UnityStringEvent chatGPTResponse = new UnityStringEvent();

        public abstract void Init();
        public abstract void SendToChatGPT(string message);
    }
}
