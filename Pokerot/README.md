# Pokerot

Pokerot is a small Unity monster-battling game inspired by classic Pokemon. The twist is that the player can create their own starter Pokemon by typing in an animal or idea, then the game uses AI to generate the Pokemon's name, typing, moves, description, and sprites.

This is a learning project and prototype, not a commercial Pokemon game.

## What You Can Do

- Create a custom starter Pokemon from a text idea.
- Pick one or two types yourself, or let the AI choose.
- Generate front and back pixel-style battle sprites.
- Battle with your custom Pokemon in a simple turn-based battle system.
- Encounter normal Pokemon and previously generated custom Pokemon as wild battles.

## Tech Notes

The project is built in Unity `6000.0.34f1`.

Main systems:

- `Assets/Scripts/UI/PokemonCustomizationUI.cs` handles the custom Pokemon creation flow.
- `Assets/Scripts/Data/ImageGenerator.cs` calls Poe for image generation and processes the returned sprites.
- `Assets/Scripts/ChatGPT/` contains both OpenAI-compatible chat support and a local Ollama option.
- `Assets/Scripts/Battle/BattleSystem.cs` runs the turn-based battle logic.
- `Assets/Scripts/Pokemon/PokemonDatabase.cs` loads Pokemon data from local assets or PokeAPI.
- `Assets/Resources/AIPromptConfig.json` stores the editable AI prompts.

## Running It

1. Open the project in Unity Hub.
2. Use Unity `6000.0.34f1` or a compatible Unity 6 version.
3. Open `Assets/Scenes/MainMenu.unity` or `Assets/Scenes/Customize.unity`.
4. Press Play.

The AI features need API keys. You can either set environment variables:

- `POE_API_KEY` for sprite generation.
- `OPENAI_API_KEY` if you want to use the backup OpenAI chat path.

Or create a local file named `LocalApiSecrets.json` in the project root:

```json
{
    "poeApiKey": "your-poe-key",
    "openAIApiKey": "your-openai-key"
}
```

That file is ignored by Git. `LocalApiSecrets.example.json` is included as a safe template.

## Current State

This is still a prototype. The core loop works: generate a Pokemon, save it locally, and battle with it. Some generated content can be weird because it depends on the AI response, but the game has fallback handling for failed sprite generation and incomplete move data.

Generated custom Pokemon, sprites, and moves are saved locally under `Assets/Resources/CustomPokemon`, `Assets/Resources/CustomSprites`, and `Assets/Resources/CustomMoves` while working in the editor.

## Credits

This project builds on [WaterHusky's Pokemon Game Project](https://github.com/WaterHusky/LeBryan_P03A), which provided the base Pokemon-style battle system. I extended it with AI Pokemon creation, custom sprite generation, runtime persistence, and extra tooling around generated content.
