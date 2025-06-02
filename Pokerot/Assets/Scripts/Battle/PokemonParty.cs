using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PokemonParty : MonoBehaviour
{
    [SerializeField] List<Pokemon> pokemons;

    public List<Pokemon> Pokemons {
        get {
            return pokemons;
        }
    }

    private void Awake()
    {
        // Make this object persist between scenes
        DontDestroyOnLoad(gameObject);
        
        // Ensure we don't have multiple instances
        if (FindObjectsOfType<PokemonParty>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        // Initialize pokemons list if it's null
        if (pokemons == null)
        {
            pokemons = new List<Pokemon>();
        }
    }

    private void Start()
    {
        foreach (var pokemon in pokemons)
        {
            if (pokemon != null)
        {
            pokemon.Init();
            }
        }
    }

    public Pokemon GetHealthyPokemon()
    {
        return pokemons.Where(x => x.HP > 0).FirstOrDefault();
    }

    public void AddPokemon(Pokemon newPokemon)
    {
        if (pokemons.Count < 6)
        {
            pokemons.Add(newPokemon);
            Debug.Log($"Added Pokemon {newPokemon.Base.Name} to party. Total: {pokemons.Count}");
        }
        else
        {
            Debug.LogWarning("Party is full! Cannot add more Pokemon.");
        }
    }
}
