using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using ChatGPTWrapper;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PokemonCustomizationUI : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] TMP_InputField pokemonNameInput;
    [SerializeField] TMP_Dropdown type1Dropdown;
    [SerializeField] TMP_Dropdown type2Dropdown;
    [SerializeField] TMP_InputField animalNameInput;

    [Header("Preview")]
    [SerializeField] Image pokemonSprite;
    [SerializeField] TextMeshProUGUI previewText;

    [Header("Navigation")]
    [SerializeField] Button confirmButton;
    [SerializeField] Button backButton;
    [SerializeField] Button generateButton;
    [SerializeField] GameObject loadingIndicator;

    private PokemonType selectedType1;
    private PokemonType selectedType2;
    private string defaultSpritePathBack = "Pokemon/Sprites/sprite_-1547647098";
    private string defaultSpritePathFront = "Pokemon/Sprites/sprite_989815560";
    private const string CUSTOM_POKEMON_PATH = "Assets/Resources/CustomPokemon";
    private const string CUSTOM_SPRITES_PATH = "Assets/Resources/CustomSprites";
    [SerializeField] private ChatGPTConversation chatGPT;
    [SerializeField] private ImageGenerator imageGen;
    // [SerializeField] private ImageGenerator imageGenerator; // Add this component manually and assign in Inspector
    private PokemonData currentPokemonData;
    private Sprite generatedFrontSprite;
    private Sprite generatedBackSprite;

    private void Start()
    {
        InitializeTypeDropdowns();
        SetupButtons();
        UpdatePreview();
        loadingIndicator.SetActive(false);

        // Create custom sprites directory if it doesn't exist
        #if UNITY_EDITOR
        if (!Directory.Exists(CUSTOM_SPRITES_PATH))
        {
            Directory.CreateDirectory(CUSTOM_SPRITES_PATH);
        }
        #endif

        // Initialize ChatGPT
        if (chatGPT != null)
        {
            chatGPT.Init();
            chatGPT.chatGPTResponse.AddListener(OnChatGPTResponse);
        }
        else
        {
            Debug.LogError("ChatGPTConversation component is not assigned!");
        }
    }

    private void OnDestroy()
    {
        // Clean up event listener
        if (chatGPT != null)
        {
            chatGPT.chatGPTResponse.RemoveListener(OnChatGPTResponse);
        }
    }

    private void InitializeTypeDropdowns()
    {
        // Get all Pokemon types except None
        var types = System.Enum.GetValues(typeof(PokemonType))
            .Cast<PokemonType>()
            .Where(t => t != PokemonType.None)
            .ToList();

        // Clear and populate dropdowns
        type1Dropdown.ClearOptions();
        type2Dropdown.ClearOptions();

        var typeOptions = types.Select(t => t.ToString()).ToList();
        type1Dropdown.AddOptions(new List<string> { "None" }.Concat(typeOptions).ToList());
        type2Dropdown.AddOptions(new List<string> { "None" }.Concat(typeOptions).ToList());

        // Add listeners
        type1Dropdown.onValueChanged.AddListener((value) => {
            selectedType1 = value == 0 ? PokemonType.None : types[value - 1];
            UpdatePreview();
        });

        type2Dropdown.onValueChanged.AddListener((value) => {
            selectedType2 = value == 0 ? PokemonType.None : types[value - 1];
            UpdatePreview();
        });
    }

    private void SetupButtons()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
        backButton.onClick.AddListener(OnBackClicked);
        generateButton.onClick.AddListener(OnGenerateClicked);
    }

    private void OnGenerateClicked()
    {
        if (string.IsNullOrWhiteSpace(animalNameInput.text))
        {
            // Show error message
            return;
        }

        if (chatGPT == null)
        {
            Debug.LogError("ChatGPTConversation component is not assigned!");
            return;
        }

        loadingIndicator.SetActive(true);
        generateButton.interactable = false;

        // Updated prompt to request image generation as well
        string prompt = $"Create a Pokemon based on this animal: {animalNameInput.text}. " +
                      "First, provide a JSON object with the Pokemon data, then I need you to generate a sprite image for this Pokemon. " +
                      "After the JSON, please generate a Pokemon sprite image in a simple, colorful cartoon style similar to classic Pokemon sprites. " +
                      "The image should be suitable for a game, showing the Pokemon from a front view against a transparent or white background.";

        chatGPT.SendToChatGPT(prompt);
    }

    public void OnChatGPTResponse(string response)
    {
        try
        {
            // Extract JSON from response (look for content between JSON: and any image-related text)
            string jsonContent = ExtractJsonFromResponse(response);
            
            if (string.IsNullOrEmpty(jsonContent))
            {
                Debug.LogError("Could not extract JSON from ChatGPT response");
                return;
            }

            currentPokemonData = JsonUtility.FromJson<PokemonData>(jsonContent);

            // Update UI with generated data
            pokemonNameInput.text = currentPokemonData.name;
            selectedType1 = (PokemonType)System.Enum.Parse(typeof(PokemonType), currentPokemonData.type1);
            selectedType2 = (PokemonType)System.Enum.Parse(typeof(PokemonType), currentPokemonData.type2);

            // Update dropdowns to match selected types
            type1Dropdown.value = type1Dropdown.options.FindIndex(option => option.text == currentPokemonData.type1);
            type2Dropdown.value = type2Dropdown.options.FindIndex(option => option.text == currentPokemonData.type2);

            // Create and save new moves
            List<LearnableMove> learnableMoves = CreateNewMoves(currentPokemonData.moves);

            // Start image generation process
            StartCoroutine(GeneratePokemonSprites());

            UpdatePreview();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating Pokemon: {e.Message}");
            // Show error message to user
        }
        finally
        {
            loadingIndicator.SetActive(false);
            generateButton.interactable = true;
            // Unsubscribe from the response event
            chatGPT.chatGPTResponse.RemoveListener(OnChatGPTResponse);
        }
    }

    private string ExtractJsonFromResponse(string response)
    {
        // Look for JSON content between "JSON:" and any following text
        int jsonStart = response.IndexOf("{");
        int jsonEnd = response.LastIndexOf("}");
        
        if (jsonStart != -1 && jsonEnd != -1 && jsonEnd > jsonStart)
        {
            return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }
        
        // Fallback: try to parse the entire response as JSON
        return response.Trim();
    }

    private IEnumerator GeneratePokemonSprites()
    {
        // Note: To use DALL-E image generation, add the ImageGenerator component to this GameObject
        // and assign it in the Inspector. For now, using placeholder sprites.
        
        
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
            // Using placeholder sprites
            yield return CreatePlaceholderSprite(currentPokemonData?.name ?? "UnknownPokemon");
        }
    }

    private void OnSpritesGenerated(Texture2D frontTexture, Texture2D backTexture, bool success, string error = "")
    {
        if (success && frontTexture != null && backTexture != null)
        {
            // Convert textures to sprites
            generatedFrontSprite = Sprite.Create(frontTexture, new Rect(0, 0, frontTexture.width, frontTexture.height), new Vector2(0.5f, 0.5f));
            generatedBackSprite = Sprite.Create(backTexture, new Rect(0, 0, backTexture.width, backTexture.height), new Vector2(0.5f, 0.5f));

            // Save sprites to files
            SaveSpritesToFiles(frontTexture, backTexture, currentPokemonData.name);

            // Update the preview
            UpdatePreview();

            Debug.Log($"Successfully generated sprites for {currentPokemonData.name}");
        }
        else
        {
            Debug.LogWarning($"Image generation failed: {error}. Using placeholder sprites.");
            // Fallback to placeholder sprites
            StartCoroutine(CreatePlaceholderSprite(currentPokemonData?.name ?? "UnknownPokemon"));
        }
    }

    private void SaveSpritesToFiles(Texture2D frontTexture, Texture2D backTexture, string pokemonName)
    {
        #if UNITY_EDITOR
        byte[] frontPngData = frontTexture.EncodeToPNG();
        byte[] backPngData = backTexture.EncodeToPNG();
        
        string safeName = string.Join("_", pokemonName.Split(Path.GetInvalidFileNameChars()));
        string frontPath = $"{CUSTOM_SPRITES_PATH}/{safeName}_front.png";
        string backPath = $"{CUSTOM_SPRITES_PATH}/{safeName}_back.png";
        
        File.WriteAllBytes(frontPath, frontPngData);
        File.WriteAllBytes(backPath, backPngData);
        
        AssetDatabase.Refresh();
        Debug.Log($"Saved sprites to {frontPath} and {backPath}");
        #endif
    }

    private IEnumerator GenerateSpriteWithDALLE(string pokemonName, string description)
    {
        // This method is now replaced by the ImageGenerator class
        // Keeping it for backward compatibility, but it just calls the placeholder
        yield return CreatePlaceholderSprite(pokemonName);
    }

    private IEnumerator CreatePlaceholderSprite(string pokemonName)
    {
        // Create a simple 96x96 colored texture as placeholder
        Texture2D frontTexture = new Texture2D(96, 96);
        Texture2D backTexture = new Texture2D(96, 96);
        
        // Create a simple gradient based on the Pokemon's types
        Color primaryColor = GetTypeColor(selectedType1);
        Color secondaryColor = selectedType2 != PokemonType.None ? GetTypeColor(selectedType2) : primaryColor;
        
        // Fill the textures with a simple pattern
        for (int x = 0; x < 96; x++)
        {
            for (int y = 0; y < 96; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(48, 48));
                if (distance < 40)
                {
                    Color pixelColor = Color.Lerp(primaryColor, secondaryColor, distance / 40f);
                    frontTexture.SetPixel(x, y, pixelColor);
                    backTexture.SetPixel(x, y, pixelColor * 0.8f); // Slightly darker for back sprite
                }
                else
                {
                    frontTexture.SetPixel(x, y, Color.clear);
                    backTexture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        frontTexture.Apply();
        backTexture.Apply();
        
        // Convert to sprites
        generatedFrontSprite = Sprite.Create(frontTexture, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f));
        generatedBackSprite = Sprite.Create(backTexture, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f));
        
        // Save the textures as PNG files
        #if UNITY_EDITOR
        byte[] frontPngData = frontTexture.EncodeToPNG();
        byte[] backPngData = backTexture.EncodeToPNG();
        
        string safeName = string.Join("_", pokemonName.Split(Path.GetInvalidFileNameChars()));
        string frontPath = $"{CUSTOM_SPRITES_PATH}/{safeName}_front.png";
        string backPath = $"{CUSTOM_SPRITES_PATH}/{safeName}_back.png";
        
        File.WriteAllBytes(frontPath, frontPngData);
        File.WriteAllBytes(backPath, backPngData);
        
        AssetDatabase.Refresh();
        #endif
        
        yield return null;
    }

    private Color GetTypeColor(PokemonType type)
    {
        switch (type)
        {
            case PokemonType.Fire: return Color.red;
            case PokemonType.Water: return Color.blue;
            case PokemonType.Grass: return Color.green;
            case PokemonType.Electric: return Color.yellow;
            case PokemonType.Ice: return Color.cyan;
            case PokemonType.Fighting: return new Color(0.8f, 0.2f, 0.2f);
            case PokemonType.Poison: return Color.magenta;
            case PokemonType.Ground: return new Color(0.8f, 0.6f, 0.2f);
            case PokemonType.Flying: return new Color(0.6f, 0.8f, 1f);
            case PokemonType.Psychic: return new Color(1f, 0.4f, 0.8f);
            case PokemonType.Bug: return Color.green;
            case PokemonType.Rock: return new Color(0.6f, 0.4f, 0.2f);
            case PokemonType.Ghost: return new Color(0.4f, 0.2f, 0.6f);
            case PokemonType.Dragon: return new Color(0.4f, 0.2f, 0.8f);
            case PokemonType.Dark: return new Color(0.2f, 0.2f, 0.2f);
            case PokemonType.Steel: return Color.gray;
            case PokemonType.Fairy: return new Color(1f, 0.6f, 0.8f);
            default: return Color.white;
        }
    }

    private void UpdatePreview()
    {
        // Update sprite - use generated sprite if available, otherwise use default
        Sprite sprite = generatedFrontSprite != null ? generatedFrontSprite : Resources.Load<Sprite>(defaultSpritePathFront);
        if (sprite != null)
            pokemonSprite.sprite = sprite;

        // Update preview text
        string type2Text = selectedType2 == PokemonType.None ? "" : $" / {selectedType2}";
        string pokemonName = string.IsNullOrWhiteSpace(pokemonNameInput.text) ? "Who am I ?" : pokemonNameInput.text;
        previewText.text = $"Preview:\n{pokemonName}\n{selectedType1}{type2Text}";
    }

    private void OnConfirmClicked()
    {
        if (string.IsNullOrWhiteSpace(pokemonNameInput.text))
        {
            // Show error message
            return;
        }

        // Create custom Pokemon
        PokemonBase customPokemon = CreateCustomPokemon();
        
        // Save to PlayerPartyInitializer
        var initializer = FindFirstObjectByType<PlayerPartyInitializer>();
        if (initializer != null)
        {
            initializer.SetInitialPokemon(customPokemon);
        }

        // Load next scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("Battle");
    }

    private void OnBackClicked()
    {
        // Load previous scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private PokemonBase CreateCustomPokemon()
    {
        PokemonBase pokemon = ScriptableObject.CreateInstance<PokemonBase>();
        
        // Set basic info
        pokemon.Name = pokemonNameInput.text;
        pokemon.Type1 = selectedType1;
        pokemon.Type2 = selectedType2;

        // Use generated sprites if available, otherwise use default sprites
        if (generatedFrontSprite != null && generatedBackSprite != null)
        {
            pokemon.FrontSprite = generatedFrontSprite;
            pokemon.BackSprite = generatedBackSprite;
        }
        else
        {
            // Fallback to default sprites
            pokemon.FrontSprite = Resources.Load<Sprite>(defaultSpritePathFront);
            pokemon.BackSprite = Resources.Load<Sprite>(defaultSpritePathBack);
        }

        // Set default stats
        pokemon.MaxHp = 120;
        pokemon.Attack = 120;
        pokemon.Defense = 120;
        pokemon.SpAttack = 120;
        pokemon.SpDefense = 120;
        pokemon.Speed = 120;

        // Get moves from GPT response if available
        if (currentPokemonData != null && currentPokemonData.moves != null)
        {
            List<LearnableMove> learnableMoves = CreateNewMoves(currentPokemonData.moves);
            pokemon.LearnableMoves = learnableMoves;
        }
        else
        {
            // Create empty move list as fallback
            pokemon.LearnableMoves = new List<LearnableMove>();
        }

        // Save the custom Pokemon as an asset
        SaveCustomPokemon(pokemon);

        return pokemon;
    }

    private void SaveCustomPokemon(PokemonBase pokemon)
    {
        #if UNITY_EDITOR
        // Create directory if it doesn't exist
        if (!Directory.Exists(CUSTOM_POKEMON_PATH))
        {
            Directory.CreateDirectory(CUSTOM_POKEMON_PATH);
        }

        // Create a unique filename based on the Pokemon's name
        string safeName = string.Join("_", pokemon.Name.Split(Path.GetInvalidFileNameChars()));
        string assetPath = $"{CUSTOM_POKEMON_PATH}/{safeName}.asset";

        // Save the asset
        AssetDatabase.CreateAsset(pokemon, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Saved custom Pokemon to: {assetPath}");
        #endif
    }

    private List<LearnableMove> CreateNewMoves(MoveData[] movesData)
    {
        List<LearnableMove> learnableMoves = new List<LearnableMove>();

        #if UNITY_EDITOR
        // Create directory if it doesn't exist
        string movesPath = "Assets/Resources/CustomMoves";
        if (!Directory.Exists(movesPath))
        {
            Directory.CreateDirectory(movesPath);
        }

        foreach (var moveData in movesData)
        {
            // Create new move asset
            MoveBase newMove = ScriptableObject.CreateInstance<MoveBase>();
            newMove.Name = moveData.name;
            newMove.Type = (PokemonType)System.Enum.Parse(typeof(PokemonType), moveData.type);
            newMove.Power = moveData.power;
            newMove.Accuracy = moveData.accuracy;
            newMove.Description = moveData.description;
            newMove.AlwaysHits = moveData.alwaysHits;
            newMove.PP = moveData.pp;
            newMove.Priority = moveData.priority;
            newMove.Category = (MoveCategory)System.Enum.Parse(typeof(MoveCategory), moveData.category);
            newMove.Target = (MoveTarget)System.Enum.Parse(typeof(MoveTarget), moveData.target);

            // Initialize empty effects
            newMove.Effects = new MoveEffects();
            newMove.Secondaries = new List<SecondaryEffects>();

            // Save move asset
            string safeName = string.Join("_", moveData.name.Split(Path.GetInvalidFileNameChars()));
            string assetPath = $"{movesPath}/{safeName}.asset";
            AssetDatabase.CreateAsset(newMove, assetPath);

            // Create learnable move
            LearnableMove learnableMove = new LearnableMove
            {
                Base = newMove,
                Level = 1
            };
            learnableMoves.Add(learnableMove);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        #endif

        return learnableMoves;
    }

    void Update()
    {
        UpdatePreview();
    }

    // // this is for testing moves
    // private List<LearnableMove> GetRandomMovesForTypes(PokemonType type1, PokemonType type2)
    // {
    //     List<LearnableMove> moves = new List<LearnableMove>();
        
    //     // Load all moves from Resources
    //     MoveBase[] allMoves = Resources.LoadAll<MoveBase>("Moves");
        
    //     // Filter moves by type
    //     var typeMoves = allMoves.Where(m => m.Type == type1 || m.Type == type2).ToList();
        
    //     // Randomly select 4 moves
    //     int moveCount = Mathf.Min(4, typeMoves.Count);
    //     for (int i = 0; i < moveCount; i++)
    //     {
    //         int randomIndex = Random.Range(0, typeMoves.Count);
    //         LearnableMove move = new LearnableMove
    //         {
    //             Base = typeMoves[randomIndex],
    //             Level = 1
    //         };
    //         moves.Add(move);
    //         typeMoves.RemoveAt(randomIndex);
    //     }

    //     return moves;
    // }
}

[System.Serializable]
public class PokemonData
{
    public string name;
    public string type1;
    public string type2;
    public string description;
    public MoveData[] moves;
}

[System.Serializable]
public class MoveData
{
    public string name;
    public string type;
    public int power;
    public int accuracy;
    public string description;
    public bool alwaysHits;
    public int pp;
    public int priority;
    public string category; // "Physical", "Special", or "Status"
    public string target; // "Foe" or "Self"
} 