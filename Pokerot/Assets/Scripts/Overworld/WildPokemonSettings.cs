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
    [SerializeField] List<WildPokemonEncounter> possibleEncounters;

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
    }

    public async Task<Pokemon> GetRandomWildPokemon()
    {
        if (possibleEncounters == null || possibleEncounters.Count == 0)
        {
            Debug.LogError("No possible Pokemon encounters defined!");
            return null;
        }

        // Calculate total encounter rate
        float totalRate = 0f;
        foreach (var encounter in possibleEncounters)
        {
            if (encounter.encounterRate <= 0)
            {
                Debug.LogWarning($"Encounter rate for Pokemon ID {encounter.pokemonId} is 0 or negative. Skipping.");
                continue;
            }
            totalRate += encounter.encounterRate;
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
} 