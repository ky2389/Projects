using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
    private const string CUSTOM_POKEMON_ASSET_PATH = "Assets/Resources/CustomPokemon";
    private const string CUSTOM_MOVES_ASSET_PATH = "Assets/Resources/CustomMoves";
    private const string CUSTOM_SPRITES_ASSET_PATH = "Assets/Resources/CustomSprites";
    private const string CUSTOM_CONTENT_FOLDER = "CustomPokemon";
    [SerializeField] private LLMConversationBase chatGPT;
    [SerializeField] private ImageGenerator imageGen;
    // [SerializeField] private ImageGenerator imageGenerator; // Add this component manually and assign in Inspector
    private PokemonData currentPokemonData;
    private Sprite generatedFrontSprite;
    private Sprite generatedBackSprite;
    private bool allowGeneratedTypesForCurrentRequest;

    private void Start()
    {
        InitializeTypeDropdowns();
        SetupButtons();
        UpdatePreview();
        loadingIndicator.SetActive(false);

        EnsureRuntimeContentDirectory();

        if (imageGen == null)
        {
            imageGen = FindFirstObjectByType<ImageGenerator>();
            if (imageGen != null)
            {
                Debug.Log("ImageGenerator was not assigned in the inspector, found one in the scene automatically.");
            }
        }

        // Initialize ChatGPT
        if (chatGPT != null)
        {
            chatGPT.Init();
            chatGPT.chatGPTResponse.AddListener(OnChatGPTResponse);
        }
        else
        {
            Debug.LogError("LLM conversation component is not assigned!");
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
            NormalizeSelectedTypes();
            UpdatePreview();
        });

        type2Dropdown.onValueChanged.AddListener((value) => {
            selectedType2 = value == 0 ? PokemonType.None : types[value - 1];
            NormalizeSelectedTypes();
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
            Debug.LogError("LLM conversation component is not assigned!");
            return;
        }

        loadingIndicator.SetActive(true);
        generateButton.interactable = false;

        NormalizeSelectedTypes();
        allowGeneratedTypesForCurrentRequest = !HasSelectedType();

        string prompt = BuildPokemonGenerationPrompt();

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
                CompleteGenerationRequest();
                return;
            }

            currentPokemonData = JsonUtility.FromJson<PokemonData>(jsonContent);

            // Update UI with generated data
            pokemonNameInput.text = currentPokemonData.name;
            if (allowGeneratedTypesForCurrentRequest && !HasSelectedType())
            {
                selectedType1 = ParsePokemonTypeOrNone(currentPokemonData.type1);
                selectedType2 = ParsePokemonTypeOrNone(currentPokemonData.type2);
                NormalizeSelectedTypes();
                SetDropdownToType(type1Dropdown, selectedType1);
                SetDropdownToType(type2Dropdown, selectedType2);
            }
            else
            {
                NormalizeSelectedTypes();
                currentPokemonData.type1 = selectedType1.ToString();
                currentPokemonData.type2 = selectedType2.ToString();
            }

            // Start image generation process
            StartCoroutine(GeneratePokemonSprites());

            UpdatePreview();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating Pokemon: {e.Message}");
            // Show error message to user
            CompleteGenerationRequest();
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

    private string BuildPokemonGenerationPrompt()
    {
        AIPromptConfig promptConfig = AIPromptConfig.Load();
        string typeInstruction = allowGeneratedTypesForCurrentRequest
            ? promptConfig.pokemonTypeAutoInstruction
            : AIPromptConfig.FillTemplate(
                promptConfig.pokemonTypeLockedInstructionTemplate,
                ("selectedTypeDescription", GetSelectedTypeDescription()),
                ("type1", selectedType1.ToString()),
                ("type2", selectedType2.ToString()));

        return AIPromptConfig.FillTemplate(
            promptConfig.pokemonDataPromptTemplate,
            ("animalName", animalNameInput.text),
            ("typeInstruction", typeInstruction),
            ("moveDesignInstruction", promptConfig.moveDesignInstruction));
    }

    private bool HasSelectedType()
    {
        return selectedType1 != PokemonType.None || selectedType2 != PokemonType.None;
    }

    private void NormalizeSelectedTypes()
    {
        if (selectedType1 == PokemonType.None && selectedType2 != PokemonType.None)
        {
            selectedType1 = selectedType2;
            selectedType2 = PokemonType.None;
            SetDropdownToType(type1Dropdown, selectedType1);
            SetDropdownToType(type2Dropdown, selectedType2);
        }

        if (selectedType1 == selectedType2)
        {
            selectedType2 = PokemonType.None;
            SetDropdownToType(type2Dropdown, selectedType2);
        }
    }

    private string GetSelectedTypeDescription()
    {
        if (selectedType2 == PokemonType.None)
        {
            return $"a single {selectedType1}-type";
        }

        return $"a dual {selectedType1}/{selectedType2}-type";
    }

    private PokemonType ParsePokemonTypeOrNone(string typeName)
    {
        if (System.Enum.TryParse(typeName, true, out PokemonType parsedType))
        {
            return parsedType;
        }

        return PokemonType.None;
    }

    private void SetDropdownToType(TMP_Dropdown dropdown, PokemonType type)
    {
        int optionIndex = dropdown.options.FindIndex(option => option.text == type.ToString());
        dropdown.value = optionIndex >= 0 ? optionIndex : 0;
    }

    private IEnumerator GeneratePokemonSprites()
    {
        if (imageGen != null && currentPokemonData != null)
        {
            imageGen.GeneratePokemonSprites(
                currentPokemonData.name, 
                currentPokemonData.description, 
                animalNameInput.text,
                selectedType1,
                selectedType2,
                OnSpritesGenerated
            );
        }
        else
        {
            Debug.LogWarning($"Using placeholder sprites because ImageGenerator is {(imageGen == null ? "not assigned/found" : "available")} and Pokemon data is {(currentPokemonData == null ? "missing" : "available")}.");
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
            pokemonSprite.color = Color.white;

            // Save sprites to files
            SaveSpritesToFiles(frontTexture, backTexture, currentPokemonData.name);

            // Update the preview
            UpdatePreview();

            Debug.Log($"Successfully generated sprites for {currentPokemonData.name}");
            CompleteGenerationRequest();
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
        byte[] frontPngData = frontTexture.EncodeToPNG();
        byte[] backPngData = backTexture.EncodeToPNG();
        
        string safeName = string.Join("_", pokemonName.Split(Path.GetInvalidFileNameChars()));
        string frontPath = Path.Combine(RuntimeContentDirectory, $"{safeName}_front.png");
        string backPath = Path.Combine(RuntimeContentDirectory, $"{safeName}_back.png");
        
        File.WriteAllBytes(frontPath, frontPngData);
        File.WriteAllBytes(backPath, backPngData);
        
        #if UNITY_EDITOR
        SaveSpriteAssetCopy(frontPngData, $"{safeName}_front");
        SaveSpriteAssetCopy(backPngData, $"{safeName}_back");
        AssetDatabase.Refresh();
        #endif

        Debug.Log($"Saved sprites to {frontPath} and {backPath}");
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
        
        SaveSpritesToFiles(frontTexture, backTexture, pokemonName);
        
        CompleteGenerationRequest();
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
        {
            pokemonSprite.color = Color.white;
            pokemonSprite.sprite = sprite;
        }

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
        NormalizeSelectedTypes();

        if (currentPokemonData != null)
        {
            currentPokemonData.type1 = selectedType1.ToString();
            currentPokemonData.type2 = selectedType2.ToString();
        }

        PokemonBase pokemon = ScriptableObject.CreateInstance<PokemonBase>();
        
        // Set basic info
        pokemon.name = pokemonNameInput.text;
        pokemon.Name = pokemonNameInput.text;
        pokemon.Description = currentPokemonData?.description ?? "";
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

        // Set balanced starter-style defaults so generated Pokemon work when reloaded later.
        pokemon.MaxHp = 80;
        pokemon.Attack = 80;
        pokemon.Defense = 80;
        pokemon.SpAttack = 80;
        pokemon.SpDefense = 80;
        pokemon.Speed = 80;
        pokemon.ExpYield = 64;
        pokemon.GrowthRate = GrowthRate.MediumFast;
        pokemon.CatchRate = 45;

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
        string safeName = string.Join("_", pokemon.Name.Split(Path.GetInvalidFileNameChars()));
        string jsonPath = Path.Combine(RuntimeContentDirectory, $"{safeName}.json");
        if (currentPokemonData != null)
        {
            File.WriteAllText(jsonPath, JsonUtility.ToJson(currentPokemonData, true));
            Debug.Log($"Saved custom Pokemon data to: {jsonPath}");
        }

        #if UNITY_EDITOR
        // Create directory if it doesn't exist
        if (!Directory.Exists(CUSTOM_POKEMON_ASSET_PATH))
        {
            Directory.CreateDirectory(CUSTOM_POKEMON_ASSET_PATH);
        }

        // Create a unique filename based on the Pokemon's name
        string assetPath = $"{CUSTOM_POKEMON_ASSET_PATH}/{safeName}.asset";
        LinkSavedSpriteAssets(pokemon, safeName);

        // Save the asset
        if (AssetDatabase.LoadAssetAtPath<PokemonBase>(assetPath) == null)
        {
            AssetDatabase.CreateAsset(pokemon, assetPath);
        }
        else
        {
            Debug.LogWarning($"Custom Pokemon asset already exists, keeping runtime Pokemon and skipping asset overwrite: {assetPath}");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Saved custom Pokemon to: {assetPath}");
        #endif
    }

    private List<LearnableMove> CreateNewMoves(MoveData[] movesData)
    {
        List<LearnableMove> learnableMoves = new List<LearnableMove>();

        if (movesData == null)
        {
            return learnableMoves;
        }

        #if UNITY_EDITOR
        // Create directory if it doesn't exist
        if (!Directory.Exists(CUSTOM_MOVES_ASSET_PATH))
        {
            Directory.CreateDirectory(CUSTOM_MOVES_ASSET_PATH);
        }
        #endif

        foreach (var moveData in movesData)
        {
            MoveBase moveBase = FindExistingMove(moveData.name);
            if (moveBase != null)
            {
                learnableMoves.Add(new LearnableMove
                {
                    Base = moveBase,
                    Level = 1
                });
                continue;
            }

            // Create new custom move asset
            MoveBase newMove = ScriptableObject.CreateInstance<MoveBase>();
            newMove.name = moveData.name;
            newMove.Name = moveData.name;
            newMove.Type = ParsePokemonTypeOrNone(moveData.type);
            newMove.Power = moveData.power;
            newMove.Accuracy = moveData.accuracy;
            newMove.Description = moveData.description;
            newMove.AlwaysHits = moveData.alwaysHits;
            newMove.PP = moveData.pp;
            newMove.Priority = moveData.priority;
            newMove.Category = ParseMoveCategoryOrDefault(moveData.category);
            newMove.Target = ParseMoveTargetOrDefault(moveData.target);
            newMove.Effects = new MoveEffects();
            newMove.Secondaries = new List<SecondaryEffects>();

            // Save move asset
            #if UNITY_EDITOR
            string safeName = string.Join("_", moveData.name.Split(Path.GetInvalidFileNameChars()));
            string assetPath = $"{CUSTOM_MOVES_ASSET_PATH}/{safeName}.asset";
            if (AssetDatabase.LoadAssetAtPath<MoveBase>(assetPath) == null)
            {
                AssetDatabase.CreateAsset(newMove, assetPath);
            }
            else
            {
                Debug.LogWarning($"Custom move asset already exists, keeping runtime move and skipping asset overwrite: {assetPath}");
            }
            #endif

            // Create learnable move
            LearnableMove learnableMove = new LearnableMove
            {
                Base = newMove,
                Level = 1
            };
            learnableMoves.Add(learnableMove);
        }

        #if UNITY_EDITOR
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        #endif

        return learnableMoves;
    }

    private MoveBase FindExistingMove(string moveName)
    {
        if (string.IsNullOrWhiteSpace(moveName))
        {
            return null;
        }

        string normalizedRequestedName = NormalizeMoveName(moveName);
        return Resources.LoadAll<MoveBase>("Moves")
            .FirstOrDefault(move =>
                NormalizeMoveName(move.Name) == normalizedRequestedName ||
                NormalizeMoveName(move.name) == normalizedRequestedName);
    }

    private string NormalizeMoveName(string moveName)
    {
        if (string.IsNullOrWhiteSpace(moveName))
        {
            return "";
        }

        return new string(moveName
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private MoveCategory ParseMoveCategoryOrDefault(string category)
    {
        if (System.Enum.TryParse(category, true, out MoveCategory parsedCategory))
        {
            return parsedCategory;
        }

        return MoveCategory.Physical;
    }

    private MoveTarget ParseMoveTargetOrDefault(string target)
    {
        if (System.Enum.TryParse(target, true, out MoveTarget parsedTarget))
        {
            return parsedTarget;
        }

        return MoveTarget.Foe;
    }

    private string RuntimeContentDirectory => Path.Combine(Application.persistentDataPath, CUSTOM_CONTENT_FOLDER);

    private void EnsureRuntimeContentDirectory()
    {
        if (!Directory.Exists(RuntimeContentDirectory))
        {
            Directory.CreateDirectory(RuntimeContentDirectory);
        }
    }

    private void CompleteGenerationRequest()
    {
        loadingIndicator.SetActive(false);
        generateButton.interactable = true;
    }

    #if UNITY_EDITOR
    private void SaveSpriteAssetCopy(byte[] pngData, string spriteName)
    {
        if (!Directory.Exists(CUSTOM_SPRITES_ASSET_PATH))
        {
            Directory.CreateDirectory(CUSTOM_SPRITES_ASSET_PATH);
        }

        string spritePath = $"{CUSTOM_SPRITES_ASSET_PATH}/{spriteName}.png";
        File.WriteAllBytes(spritePath, pngData);
        AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    private void LinkSavedSpriteAssets(PokemonBase pokemon, string safeName)
    {
        Sprite frontSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{CUSTOM_SPRITES_ASSET_PATH}/{safeName}_front.png");
        Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{CUSTOM_SPRITES_ASSET_PATH}/{safeName}_back.png");

        if (frontSprite != null)
        {
            pokemon.FrontSprite = frontSprite;
        }

        if (backSprite != null)
        {
            pokemon.BackSprite = backSprite;
        }
    }
    #endif

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