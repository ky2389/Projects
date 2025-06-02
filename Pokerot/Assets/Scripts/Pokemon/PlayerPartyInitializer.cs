using UnityEngine;

public class PlayerPartyInitializer : MonoBehaviour
{
    [SerializeField] private PokemonParty playerParty;
    private int initialPokemonId = 1; // Default to Bulbasaur
    [SerializeField] private int initialPokemonLevel = 10;

    private void Awake()
    {
        // Ensure playerParty is assigned
        if (playerParty == null)
        {
            playerParty = GetComponent<PokemonParty>();
            if (playerParty == null)
            {
                Debug.LogError("PokemonParty component not found!");
            }
        }

        // Make this object persist between scenes
        DontDestroyOnLoad(gameObject);
        
        // Ensure we don't have multiple instances
        if (FindObjectsOfType<PlayerPartyInitializer>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }

    // private void Start()
    // {
    //     // If no custom Pokemon is set, use the default one
    //     if (playerParty.Pokemons.Count == 0)
    //     {
    //         InitializeDefaultPokemon();
    //     }
    // }

    // private async void InitializeDefaultPokemon()
    // {
    //     PokemonBase pokemonBase = await PokemonDatabase.Instance.GetPokemon(initialPokemonId);
    //     if (pokemonBase != null)
    //     {
    //         Pokemon pokemon = new Pokemon(pokemonBase, 5); // Start at level 5
    //         playerParty.AddPokemon(pokemon);
    //         Debug.Log($"Added default Pokemon {pokemon.Base.Name} to party");
    //     }
    //     else
    //     {
    //         Debug.LogError("Failed to load default Pokemon!");
    //     }
    // }

    public void SetInitialPokemon(PokemonBase customPokemon)
    {
        if (customPokemon == null)
        {
            Debug.LogError("Custom Pokemon is null!");
            return;
        }

        // Clear existing Pokemon
        playerParty.Pokemons.Clear();
        
        // Add the custom Pokemon
        Pokemon pokemon = new Pokemon(customPokemon, initialPokemonLevel); // Start at level
        playerParty.AddPokemon(pokemon);
        Debug.Log($"Added custom Pokemon {pokemon.Base.Name} to party");
    }
} 