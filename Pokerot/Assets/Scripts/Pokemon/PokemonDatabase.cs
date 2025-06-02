using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.IO;

public class PokemonDatabase : MonoBehaviour
{
    [SerializeField] private string POKE_API_BASE_URL = "https://pokeapi.co/api/v2/pokemon/";
    private const string POKEMON_ASSETS_PATH = "Pokemon";
    private const int MAX_GEN1_POKEMON_ID = 999; 

    public static PokemonDatabase Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async Task<PokemonBase> GetPokemon(int id)
    {
        // Validate that we're only requesting Generation 1 Pokemon
        if (id < 1 || id > MAX_GEN1_POKEMON_ID)
        {
            Debug.LogError($"Invalid Pokemon ID: {id}. Only Generation 1 Pokemon (IDs 1-{MAX_GEN1_POKEMON_ID}) are supported.");
            return null;
        }

        // Try to load from Resources
        string resourcePath = $"{POKEMON_ASSETS_PATH}/pokemon_{id:D3}";
        PokemonBase pokemon = Resources.Load<PokemonBase>(resourcePath);
        
        if (pokemon != null)
        {
            return pokemon;
        }

        // If not in Resources, download from API and save as asset
        using (UnityWebRequest request = UnityWebRequest.Get($"{POKE_API_BASE_URL}{id}"))
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                PokeAPIPokemon apiPokemon = JsonUtility.FromJson<PokeAPIPokemon>(json);
                pokemon = await ConvertToPokemonBase(apiPokemon);
                
                // Save as asset
                SavePokemonAsAsset(pokemon, id);
                
                return pokemon;
            }
            else
            {
                Debug.LogError($"Failed to load Pokemon {id}: {request.error}");
                return null;
            }
        }
    }

    private async Task<PokemonBase> ConvertToPokemonBase(PokeAPIPokemon apiPokemon)
    {
        PokemonBase pokemon = ScriptableObject.CreateInstance<PokemonBase>();
        
        // Set basic info
        pokemon.name = $"pokemon_{apiPokemon.id:D3}"; // This sets the asset name
        pokemon.Name = apiPokemon.name; // This sets the Pokemon's display name
        
        // Set types
        if (apiPokemon.types.Count > 0)
        {
            pokemon.Type1 = ParsePokemonType(apiPokemon.types[0].type.name);
            pokemon.Type2 = apiPokemon.types.Count > 1 ? ParsePokemonType(apiPokemon.types[1].type.name) : PokemonType.None;
        }

        // Set stats
        foreach (var stat in apiPokemon.stats)
        {
            switch (stat.stat.name)
            {
                case "hp":
                    pokemon.MaxHp = stat.base_stat;
                    break;
                case "attack":
                    pokemon.Attack = stat.base_stat;
                    break;
                case "defense":
                    pokemon.Defense = stat.base_stat;
                    break;
                case "special-attack":
                    pokemon.SpAttack = stat.base_stat;
                    break;
                case "special-defense":
                    pokemon.SpDefense = stat.base_stat;
                    break;
                case "speed":
                    pokemon.Speed = stat.base_stat;
                    break;
            }
        }

        // Load sprites first
        var frontSpriteTask = LoadSprite(apiPokemon.sprites.front_default);
        var backSpriteTask = LoadSprite(apiPokemon.sprites.back_default);
        
        // Wait for both sprites to load
        await Task.WhenAll(frontSpriteTask, backSpriteTask);
        
        // Assign sprites
        pokemon.FrontSprite = await frontSpriteTask;
        pokemon.BackSprite = await backSpriteTask;

        // Set other properties
        pokemon.ExpYield = apiPokemon.base_experience;
        pokemon.GrowthRate = GrowthRate.MediumFast; // Default to MediumFast
        pokemon.CatchRate = 45; // Default catch rate

        // Convert moves
        List<LearnableMove> learnableMoves = new List<LearnableMove>();
        foreach (var move in apiPokemon.moves)
        {
            foreach (var detail in move.version_group_details)
            {
                if (detail.move_learn_method.name == "level-up" && detail.version_group.name == "red-blue")
                {
                    // Create a new MoveBase for this move
                    MoveBase moveBase = await CreateMoveBase(move.move.name);
                    if (moveBase != null)
                    {
                        LearnableMove learnableMove = new LearnableMove();
                        learnableMove.Base = moveBase;
                        learnableMove.Level = detail.level_learned_at;
                        learnableMoves.Add(learnableMove);
                    }
                }
            }
        }
        pokemon.LearnableMoves = learnableMoves;

        return pokemon;
    }

    private async Task<MoveBase> CreateMoveBase(string moveName)
    {
        // Try to load existing move from Resources
        string resourcePath = $"Moves/{moveName.ToLower()}";
        MoveBase moveBase = Resources.Load<MoveBase>(resourcePath);
        
        if (moveBase != null)
        {
            return moveBase;
        }

        // If not found, create a new one
        moveBase = ScriptableObject.CreateInstance<MoveBase>();
        moveBase.name = moveName.ToLower();

        // Load move data from PokeAPI
        using (UnityWebRequest request = UnityWebRequest.Get($"https://pokeapi.co/api/v2/move/{moveName.ToLower()}"))
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                PokeAPIMoveData moveData = JsonUtility.FromJson<PokeAPIMoveData>(json);

                // Set move properties
                moveBase.Name = moveData.name;
                moveBase.Description = moveData.effect_entries[0].effect;
                moveBase.Type = ParsePokemonType(moveData.type.name);
                moveBase.Power = moveData.power;
                moveBase.Accuracy = moveData.accuracy;
                moveBase.PP = moveData.pp;
                moveBase.Priority = moveData.priority;
                moveBase.Category = moveData.damage_class.name == "special" ? MoveCategory.Special : MoveCategory.Physical;
                moveBase.Target = MoveTarget.Foe;

                // Save the move as an asset
                SaveMoveAsAsset(moveBase);

                return moveBase;
            }
            else
            {
                Debug.LogError($"Failed to load move {moveName}: {request.error}");
                return null;
            }
        }
    }

    private void SaveMoveAsAsset(MoveBase move)
    {
        #if UNITY_EDITOR
        // Create the directory if it doesn't exist
        string directory = Path.Combine(Application.dataPath, "Resources", "Moves");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Save the asset
        string assetPath = $"Assets/Resources/Moves/{move.name.ToLower()}.asset";
        UnityEditor.AssetDatabase.CreateAsset(move, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        #endif
    }

    [System.Serializable]
    private class PokeAPIMoveData
    {
        public string name;
        public int power;
        public int accuracy;
        public int pp;
        public int priority;
        public PokeAPITypeInfo type;
        public PokeAPIDamageClass damage_class;
        public PokeAPIEffectEntry[] effect_entries;
    }

    [System.Serializable]
    private class PokeAPIDamageClass
    {
        public string name;
    }

    [System.Serializable]
    private class PokeAPIEffectEntry
    {
        public string effect;
    }

    private async Task<Sprite> LoadSprite(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        // Try to load from Resources first
        string spriteName = $"sprite_{url.GetHashCode()}";
        string spritePath = $"{POKEMON_ASSETS_PATH}/Sprites/{spriteName}";
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        
        if (sprite != null)
        {
            Debug.Log($"Loaded existing sprite: {spritePath}");
            return sprite;
        }

        // If not in Resources, download and save
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                
                // Save sprite as PNG
                SaveSpriteAsAsset(sprite, spriteName);
                
                // Wait longer for the asset to be imported
                await Task.Delay(500);
                
                // Try multiple times to load the sprite
                for (int i = 0; i < 3; i++)
                {
                    sprite = Resources.Load<Sprite>(spritePath);
                    if (sprite != null)
                    {
                        Debug.Log($"Successfully loaded new sprite: {spritePath}");
                        return sprite;
                    }
                    await Task.Delay(200);
                }
                
                Debug.LogError($"Failed to load sprite after saving: {spritePath}");
                return null;
            }
            else
            {
                Debug.LogError($"Failed to load sprite from {url}: {request.error}");
                return null;
            }
        }
    }

    private void SavePokemonAsAsset(PokemonBase pokemon, int id)
    {
        #if UNITY_EDITOR
        // Create the directory if it doesn't exist
        string directory = Path.Combine(Application.dataPath, "Resources", POKEMON_ASSETS_PATH);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Save the asset
        string assetPath = $"Assets/Resources/{POKEMON_ASSETS_PATH}/pokemon_{id:D3}.asset";
        UnityEditor.AssetDatabase.CreateAsset(pokemon, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        #endif
    }

    private void SaveSpriteAsAsset(Sprite sprite, string spriteName)
    {
        #if UNITY_EDITOR
        // Create the directory if it doesn't exist
        string directory = Path.Combine(Application.dataPath, "Resources", POKEMON_ASSETS_PATH, "Sprites");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Save the sprite as a texture asset
        string texturePath = $"Assets/Resources/{POKEMON_ASSETS_PATH}/Sprites/{spriteName}.png";
        File.WriteAllBytes(texturePath, sprite.texture.EncodeToPNG());
        UnityEditor.AssetDatabase.Refresh();

        // Configure the texture import settings
        UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(texturePath) as UnityEditor.TextureImporter;
        if (importer != null)
        {
            importer.textureType = UnityEditor.TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
            importer.SaveAndReimport();

            // Force Unity to process the import
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
        #endif
    }

    private PokemonType ParsePokemonType(string typeName)
    {
        return typeName.ToLower() switch
        {
            "normal" => PokemonType.Normal,
            "fire" => PokemonType.Fire,
            "water" => PokemonType.Water,
            "electric" => PokemonType.Electric,
            "grass" => PokemonType.Grass,
            "ice" => PokemonType.Ice,
            "fighting" => PokemonType.Fighting,
            "poison" => PokemonType.Poison,
            "ground" => PokemonType.Ground,
            "flying" => PokemonType.Flying,
            "psychic" => PokemonType.Psychic,
            "bug" => PokemonType.Bug,
            "rock" => PokemonType.Rock,
            "ghost" => PokemonType.Ghost,
            "dragon" => PokemonType.Dragon,
            "dark" => PokemonType.Dark,
            "steel" => PokemonType.Steel,
            "fairy" => PokemonType.Fairy,
            _ => PokemonType.None
        };
    }
} 