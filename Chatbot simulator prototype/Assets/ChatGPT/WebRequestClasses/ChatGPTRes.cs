using System;
using System.Collections.Generic;

namespace ChatGPTWrapper {
    [Serializable]
    public class ChatGPTRes
    {
        // For OpenAI-style responses (kept for compatibility)
        public ChatGPTUsage usage;
        public List<ChatGPTChoices> choices;

        // For Ollama /api/chat responses (single message)
        public Message message;
        public bool done;
        public int eval_count;
        public int prompt_eval_count;
    }
}
