# Pokerot

A Unity-based Pokemon game that allows players to create and customize their own Pokemon using AI-generated content. The game combines ChatGPT for Pokemon data generation and DALL-E for sprite creation. 
**Remark:** Image generation part is yet to be debugged if possible with the availability of AI image api keys. 

## Credits

This project is an extension of [WaterHusky's Pokemon Game Project](https://github.com/WaterHusky/LeBryan_P03A). The original project provided the core Pokemon battle system and game mechanics, which have been enhanced with AI-powered customization features and loading pokemons online more conveniently.

**Important Note:** This project is for educational purposes only and is not intended for commercial use.

## Features

- Create custom Pokemon based on any animal or concept
- AI-generated Pokemon data including:
  - Custom name
  - Type combinations
  - Unique moves and abilities
  - Detailed descriptions
- AI-generated Pokemon sprites (front and back views)
- Battle system with custom Pokemon
- Fallback sprite generation based on Pokemon types

## Prerequisites

- Unity 2022.3 or later
- OpenAI API key (for both ChatGPT and DALL-E)

## Setup Instructions

1. Clone this repository
2. Open the project in Unity
3. In the Unity Editor, open the `Customize` scene
4. Configure API Keys:
   - In the Inspector, find the `ChatGPTConversation` component
   - Set your OpenAI API key in the `API Key` field
   - In the `ImageGenerator` component
   - Set the same OpenAI API key in the `Open AI Api Key` field

## Project Structure

- `Assets/Scripts/UI/PokemonCustomizationUI.cs` - Main UI controller for Pokemon customization
- `Assets/Scripts/Data/ImageGenerator.cs` - Handles sprite generation using DALL-E
- `Assets/Resources/CustomPokemon` - Stores generated Pokemon data
- `Assets/Resources/CustomSprites` - Stores generated Pokemon sprites
- `Assets/Resources/CustomMoves` - Stores generated Pokemon moves

## How to Use

1. Enter an animal name or concept in the input field
2. Click "Generate" to create a custom Pokemon
3. The system will:
   - Generate Pokemon data using ChatGPT
   - Create custom sprites using DALL-E
   - Save all assets automatically
4. Click "Confirm" to use your custom Pokemon in battle

## Notes

- The system requires an active internet connection for AI generation
- If image generation fails, the system will create colorful placeholder sprites based on the Pokemon's types
- Generated assets are saved locally and can be reused in future sessions

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- OpenAI for providing the ChatGPT and DALL-E APIs
- Unity Technologies for the game engine
