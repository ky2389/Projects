# Chatbot Simulator (Unity) — Local LLM NPC Dialogue

Chatbot Simulator is a Unity prototype game where NPC dialogue is generated at runtime from the player's text input. The NPC responds with **both** a line of dialogue and an **animation cue**, which drives the character’s expression/gesture in-game.

This project was originally built using an online ChatGPT API, and later adapted to run **locally** via **Ollama** so it can work without any cloud API keys.

## Demo screenshots


![Home Page](demo-1.png)


![GamePlay](demo-2.png)

## What I built (mechanics / technical design)

- **Player → NPC conversation loop**
  - The player types a message in a UI input field.
  - The game sends the player message to an LLM conversation component.
  - The NPC reply is displayed in the UI and an animation is triggered on the NPC.

- **Structured model output (JSON)**
  - The model is prompted to return a single JSON object:
    - `reply_to_player`: the NPC’s line
    - `animation_name`: which animation/expression to play (e.g. `idle`, `confused`, `shy`, etc.)
  - Unity parses the JSON and uses it to update both text and animation.

- **Personality system**
  - NPC “personalities” are stored in a `ScriptableObject` database.
  - Each personality provides an initial prompt that sets the NPC’s style/role.
  - The menu scene lets the player choose a personality; the selection is saved with `PlayerPrefs`.

- **Local inference with Ollama (no API keys)**
  - The LLM backend is Ollama running on `http://localhost:11434`.
  - Default model: `qwen2.5:3b` (fast enough for casual dialogue on many PCs).

- **Player-friendly fallback when local AI is unavailable**
  - On startup and on each request, the game checks whether Ollama is reachable.
  - If Ollama isn’t installed/running, the game returns a safe fallback JSON reply (with a `confused` animation) that tells the player how to start Ollama, so gameplay doesn’t freeze.

## Project structure (high level)

- `Assets/Scripts/`
  - `GameManager.cs`: player input → send message → receive JSON → update UI + trigger NPC animation.
  - `NPCController.cs`: plays animations / blendshape expressions.
  - `PersonalityDB.cs`: personality definitions (ScriptableObject).
- `Assets/ChatGPT/`
  - `ChatGPTConversation.cs`: conversation state and HTTP calls to the LLM backend (adapted to Ollama).

## How to run (Build / downloaded release)

To run the built game on a Windows PC:

1. Install Ollama (once).
2. Download the model (once): `ollama pull qwen2.5:3b`
3. Before launching the game, start Ollama:

```powershell
ollama serve
```

4. Run the game `.exe`.

If Ollama is not running, the NPC will show a fallback message explaining how to start it.

## Notes / troubleshooting

- **If the NPC keeps saying “Thinking…”**
  - Ensure `ollama serve` is running and that `http://localhost:11434` is reachable.
- **Model name**
  - If your Ollama model is listed with a different name/tag, update the model string in `Assets/ChatGPT/ChatGPTConversation.cs`.

## Credits

- Built in Unity by a student as a prototype exploring NPC dialogue driven by LLMs.
- Local LLM backend powered by Ollama.

