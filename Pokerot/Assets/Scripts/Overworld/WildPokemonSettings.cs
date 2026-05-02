using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[System.Serializable]
public class WildPokemonEncounter
{
    public int pokemonId;
    public float encounterRate;
    public int minLevel;
    public int maxLevel;
}

public class WildPokemonSettings : MonoBehaviour
{
    private const int DefaultCustomBaseStat = 80;
    private const int DefaultCustomExpYield = 64;
    private const int DefaultCustomCatchRate = 45;

    [SerializeField] List<WildPokemonEncounter> possibleEncounters;
    [Header("Custom Pokemon Encounters")]
    [SerializeField] bool includeCustomPokemon = true;
    [SerializeField] float customPokemonEncounterRate = 5f;
    [SerializeField] int customPokemonMinLevel = 2;
    [SerializeField] int customPokemonMaxLevel = 5;

    private void OnValidate()
    {
        if (possibleEncounters != null)
        {
            foreach (var encounter in possibleEncounters)
            {
                // Ensure encounter rate is positive
                if (encounter.encounterRate < 0)
                {
                    encounter.encounterRate = 0;
                }

                // Ensure min level is not greater than max level
                if (encounter.minLevel > encounter.maxLevel)
                {
                    encounter.maxLevel = encounter.minLevel;
                }

                // Ensure levels are within valid range (1-100)
                encounter.minLevel = Mathf.Clamp(encounter.minLevel, 1, 100);
                encounter.maxLevel = Mathf.Clamp(encounter.maxLevel, 1, 100);
            }
        }

        customPokemonEncounterRate = Mathf.Max(0f, customPokemonEncounterRate);
        customPokemonMinLevel = Mathf.Clamp(customPokemonMinLevel, 1, 100);
        customPokemonMaxLevel = Mathf.Clamp(customPokemonMaxLevel, 1, 100);
        if (customPokemonMinLevel > customPokemonMaxLevel)
        {
            customPokemonMaxLevel = customPokemonMinLevel;
        }
    }

    public async Task<Pokemon> GetRandomWildPokemon(PokemonParty playerParty = null)
    {
        PokemonBase[] customPokemon = GetCustomPokemonEncounters(playerParty);
        bool hasConfiguredEncounters = possibleEncounters != null && possibleEncounters.Count > 0;
        bool hasCustomEncounters = customPokemon.Length > 0 && customPokemonEncounterRate > 0f;

        if (!hasConfiguredEncounters && !hasCustomEncounters)
        {
            Debug.LogError("No possible Pokemon encounters defined!");
            return null;
        }

        // Calculate total encounter rate
        float totalRate = 0f;
        if (hasConfiguredEncounters)
        {
            foreach (var encounter in possibleEncounters)
            {
                if (encounter.encounterRate <= 0)
                {
                    Debug.LogWarning($"Encounter rate for Pokemon ID {encounter.pokemonId} is 0 or negative. Skipping.");
                    continue;
                }
                totalRate += encounter.encounterRate;
            }
        }

        if (hasCustomEncounters)
        {
            totalRate += customPokemon.Length * customPokemonEncounterRate;
        }

        if (totalRate <= 0)
        {
            Debug.LogError("Total encounter rate is 0 or negative. No Pokemon can be encountered.");
            return null;
        }

        // Get random value between 0 and total rate
        float randomValue = Random.Range(0f, totalRate);
        float currentSum = 0f;

        // Find the selected Pokemon based on encounter rates
        WildPokemonEncounter selectedEncounter = null;
        if (hasConfiguredEncounters)
        {
            foreach (var encounter in possibleEncounters)
            {
                if (encounter.encounterRate <= 0) continue;
                
                currentSum += encounter.encounterRate;
                if (randomValue <= currentSum)
                {
                    selectedEncounter = encounter;
                    break;
                }
            }
        }

        if (selectedEncounter == null && hasCustomEncounters)
        {
            PokemonBase customPokemonBase = customPokemon[Random.Range(0, customPokemon.Length)];
            int customLevel = Random.Range(customPokemonMinLevel, customPokemonMaxLevel + 1);
            Pokemon pokemon = new Pokemon(customPokemonBase, customLevel);
            pokemon.Init();
            Debug.Log($"Wild custom Pokemon appeared: {customPokemonBase.Name} Lv.{customLevel}");
            return pokemon;
        }

        if (selectedEncounter == null)
        {
            Debug.LogError("Failed to select a Pokemon encounter!");
            return null;
        }

        // Get the Pokemon base from the database
        PokemonBase pokemonBase = await PokemonDatabase.Instance.GetPokemon(selectedEncounter.pokemonId);
        
        if (pokemonBase != null)
        {
            // Create a new Pokemon instance with random level within the defined range
            int level = Random.Range(selectedEncounter.minLevel, selectedEncounter.maxLevel + 1);
            Pokemon pokemon = new Pokemon(pokemonBase, level);
            pokemon.Init();
            return pokemon;
        }

        Debug.LogError($"Failed to load Pokemon with ID {selectedEncounter.pokemonId}");
        return null;
    }

    private PokemonBase[] GetCustomPokemonEncounters(PokemonParty playerParty)
    {
        if (!includeCustomPokemon)
        {
            return new PokemonBase[0];
        }

        PokemonBase[] customPokemon = Resources.LoadAll<PokemonBase>("CustomPokemon");
        if (customPokemon == null || customPokemon.Length == 0)
        {
            return new PokemonBase[0];
        }

        List<PokemonBase> validPokemon = new List<PokemonBase>();
        foreach (PokemonBase pokemonBase in customPokemon)
        {
            if (pokemonBase != null && !IsInPlayerParty(pokemonBase, playerParty))
            {
                EnsureCustomPokemonReadyForBattle(pokemonBase);
                validPokemon.Add(pokemonBase);
            }
        }

        return validPokemon.ToArray();
    }

    private bool IsInPlayerParty(PokemonBase pokemonBase, PokemonParty playerParty)
    {
        if (pokemonBase == null || playerParty == null || playerParty.Pokemons == null)
        {
            return false;
        }

        foreach (Pokemon partyPokemon in playerParty.Pokemons)
        {
            if (partyPokemon?.Base == null)
            {
                continue;
            }

            if (partyPokemon.Base == pokemonBase)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(partyPokemon.Base.Name) &&
                string.Equals(partyPokemon.Base.Name, pokemonBase.Name, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureCustomPokemonReadyForBattle(PokemonBase pokemonBase)
    {
        string safeName = GetSafeResourceName(pokemonBase.Name);

        if (pokemonBase.FrontSprite == null)
        {
            pokemonBase.FrontSprite = LoadCustomSprite($"{safeName}_front");
        }

        if (pokemonBase.BackSprite == null)
        {
            pokemonBase.BackSprite = LoadCustomSprite($"{safeName}_back");
        }

        if (pokemonBase.MaxHp <= 0) pokemonBase.MaxHp = DefaultCustomBaseStat;
        if (pokemonBase.Attack <= 0) pokemonBase.Attack = DefaultCustomBaseStat;
        if (pokemonBase.Defense <= 0) pokemonBase.Defense = DefaultCustomBaseStat;
        if (pokemonBase.SpAttack <= 0) pokemonBase.SpAttack = DefaultCustomBaseStat;
        if (pokemonBase.SpDefense <= 0) pokemonBase.SpDefense = DefaultCustomBaseStat;
        if (pokemonBase.Speed <= 0) pokemonBase.Speed = DefaultCustomBaseStat;
        if (pokemonBase.ExpYield <= 0) pokemonBase.ExpYield = DefaultCustomExpYield;
        if (pokemonBase.CatchRate <= 0) pokemonBase.CatchRate = DefaultCustomCatchRate;

        pokemonBase.GrowthRate = GrowthRate.MediumFast;
        pokemonBase.LearnableMoves = GetValidLearnableMoves(pokemonBase.LearnableMoves);
    }

    private Sprite LoadCustomSprite(string spriteName)
    {
        string resourcePath = $"CustomSprites/{spriteName}";
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Debug.LogWarning($"Could not load custom Pokemon sprite at Resources/{resourcePath}");
            return null;
        }

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private List<LearnableMove> GetValidLearnableMoves(List<LearnableMove> learnableMoves)
    {
        List<LearnableMove> validMoves = new List<LearnableMove>();
        if (learnableMoves == null)
        {
            return validMoves;
        }

        foreach (LearnableMove learnableMove in learnableMoves)
        {
            if (learnableMove != null && learnableMove.Base != null)
            {
                validMoves.Add(learnableMove);
            }
        }

        return validMoves;
    }

    private string GetSafeResourceName(string pokemonName)
    {
        if (string.IsNullOrWhiteSpace(pokemonName))
        {
            return "";
        }

        foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
        {
            pokemonName = pokemonName.Replace(invalidChar, '_');
        }

        return pokemonName;
    }
} 