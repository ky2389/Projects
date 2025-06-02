# Pokemon Sprite Image Generation System

This system extends your ChatGPT-powered Pokemon creation to also generate custom sprites using OpenAI's DALL-E API.

## Features

1. **JSON Pokemon Data Generation**: ChatGPT creates Pokemon data with name, types, moves, and descriptions
2. **Sprite Image Generation**: DALL-E generates front and back sprite images based on the Pokemon description
3. **Automatic Integration**: Generated sprites are automatically saved and integrated into your Pokemon assets
4. **Fallback System**: If image generation fails, colorful placeholder sprites are created based on Pokemon types

## Setup Instructions

### 1. Add the ImageGenerator Component

1. In your `Customize` scene, find the GameObject that has the `PokemonCustomizationUI` component
2. Add the `ImageGenerator` component to the same GameObject:
   - Select the GameObject
   - In the Inspector, click "Add Component"
   - Search for "Image Generator" and add it

### 2. Configure API Keys

1. **ChatGPT API Key**: Already configured in your `ChatGPTConversation` component
2. **DALL-E API Key**: Set in the `ImageGenerator` component
   - In the Inspector, find the `ImageGenerator` component
   - Set the `Open AI Api Key` field to your OpenAI API key (same as ChatGPT)

### 3. Enable Image Generation in Code

In `PokemonCustomizationUI.cs`, uncomment the image generation code:

```csharp
private IEnumerator GeneratePokemonSprites()
{
    ImageGenerator imageGen = GetComponent<ImageGenerator>();
    if (imageGen != null && currentPokemonData != null)
    {
        imageGen.GeneratePokemonSprites(
            currentPokemonData.name, 
            currentPokemonData.description, 
            OnSpritesGenerated
        );
    }
    else
    {
        // Fallback to placeholder sprites
        yield return CreatePlaceholderSprite(currentPokemonData?.name ?? "UnknownPokemon");
    }
}
```

### 4. Update ChatGPT Prompt (Already Done)

The system already includes an updated prompt that asks ChatGPT to provide both JSON data and indicate that sprite generation will follow.

## How It Works

### 1. User Input
- Player enters an animal name (e.g., "Lion", "Dragon", "Coffee Cup")

### 2. ChatGPT Response
- ChatGPT generates Pokemon data including:
  - Name (e.g., "Lionite")
  - Types (e.g., Fire/Normal)
  - Description
  - 4 custom moves with full data

### 3. Sprite Generation
- DALL-E generates front and back view sprites based on:
  - Pokemon name
  - Pokemon description
  - Specific art style instructions for pixel art Pokemon sprites

### 4. Integration
- Sprites are saved as PNG files in `Assets/Resources/CustomSprites/`
- Pokemon data is saved as ScriptableObject in `Assets/Resources/CustomPokemon/`
- Everything is automatically integrated for battle use

## Generated Files

### Sprites
- `Assets/Resources/CustomSprites/{PokemonName}_front.png`
- `Assets/Resources/CustomSprites/{PokemonName}_back.png`

### Pokemon Data
- `Assets/Resources/CustomPokemon/{PokemonName}.asset`

### Moves
- `Assets/Resources/CustomMoves/{MoveName}.asset` (for each custom move)

## Customization Options

### Sprite Style
You can modify the sprite generation prompts in `ImageGenerator.cs`:

```csharp
private string CreatePokemonSpritePrompt(string pokemonName, string description, string viewType)
{
    return $"Create a pixel art style Pokemon sprite of {pokemonName} in {viewType}. " +
           $"Description: {description}. " +
           "Style: 16-bit pixel art, clean lines, bright colors, transparent background, " +
           "similar to classic Pokemon game sprites...";
}
```

### Fallback Sprites
The system creates type-based colored circular sprites as fallbacks. You can modify the colors in `GetTypeColor()` method.

## Troubleshooting

### Image Generation Not Working
1. Verify OpenAI API key is set correctly
2. Check Unity Console for error messages
3. Ensure you have sufficient OpenAI API credits
4. The system will automatically fall back to placeholder sprites if DALL-E fails

### Sprites Not Appearing
1. Check that files are being saved to `Assets/Resources/CustomSprites/`
2. Ensure `AssetDatabase.Refresh()` is being called (should be automatic)
3. Verify the sprites are being assigned to the Pokemon asset

### API Rate Limits
- DALL-E has rate limits; if you hit them, the system will use placeholder sprites
- Consider adding delays between requests for bulk generation

## Cost Considerations

- Each Pokemon generation creates 2 DALL-E images (front and back)
- DALL-E 3 costs approximately $0.04 per 1024x1024 image
- Budget approximately $0.08 per Pokemon for sprite generation
- Placeholder sprites are generated locally for free as fallback

## Future Enhancements

1. **Sprite Caching**: Save generated sprites permanently to avoid regenerating
2. **Style Variations**: Allow users to choose different art styles
3. **Batch Generation**: Generate multiple Pokemon sprites efficiently
4. **Animation**: Generate multiple frames for animated sprites
5. **Alternative AI Models**: Support for other image generation APIs

## Example Workflow

1. Player types "Dragon"
2. ChatGPT creates "Dracoflame" with Fire/Dragon types
3. DALL-E generates front sprite of a dragon-like Pokemon
4. DALL-E generates back sprite (darker, rear view)
5. Both sprites saved and integrated into Pokemon asset
6. Player can immediately use custom Pokemon in battle

This system provides a complete end-to-end Pokemon creation experience with both data and visual generation! 