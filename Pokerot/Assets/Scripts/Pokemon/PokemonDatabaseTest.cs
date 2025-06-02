using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class PokemonDatabaseTest : MonoBehaviour
{
    [SerializeField] private Image frontSprite;
    [SerializeField] private Image backSprite;
    [SerializeField] private TextMeshProUGUI pokemonName;
    [SerializeField] private TextMeshProUGUI pokemonStats;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI pokemonIdText;
    [SerializeField] PokemonDatabase pokemonDatabase;
    
    private int currentPokemonId = 1;
    private const int MAX_POKEMON_ID = 151; // Generation 1 Pokemon
    
    private async void Start()
    {
        // Set up button listeners
        if (previousButton != null)
            previousButton.onClick.AddListener(() => LoadPreviousPokemon());
        if (nextButton != null)
            nextButton.onClick.AddListener(() => LoadNextPokemon());
            
        // Load initial Pokemon
        await LoadPokemon(currentPokemonId);
    }

    private async void LoadPreviousPokemon()
    {
        currentPokemonId--;
        if (currentPokemonId < 1)
            currentPokemonId = MAX_POKEMON_ID;
        await LoadPokemon(currentPokemonId);
    }

    private async void LoadNextPokemon()
    {
        currentPokemonId++;
        if (currentPokemonId > MAX_POKEMON_ID)
            currentPokemonId = 1;
        await LoadPokemon(currentPokemonId);
    }

    private async Task LoadPokemon(int id)
    {
        PokemonBase pokemon = await PokemonDatabase.Instance.GetPokemon(id);
        
        if (pokemon != null)
        {
            // Update UI
            pokemonName.text = pokemon.Name;
            if (pokemonIdText != null)
                pokemonIdText.text = $"#{id:D3}";
            
            if (frontSprite != null && pokemon.FrontSprite != null)
                frontSprite.sprite = pokemon.FrontSprite;
                
            if (backSprite != null && pokemon.BackSprite != null)
                backSprite.sprite = pokemon.BackSprite;
                
            if (pokemonStats != null)
            {
                pokemonStats.text = $"HP: {pokemon.MaxHp}\n" +
                                  $"Attack: {pokemon.Attack}\n" +
                                  $"Defense: {pokemon.Defense}\n" +
                                  $"Sp. Attack: {pokemon.SpAttack}\n" +
                                  $"Sp. Defense: {pokemon.SpDefense}\n" +
                                  $"Speed: {pokemon.Speed}\n" +
                                  $"Type 1: {pokemon.Type1}\n" +
                                  $"Type 2: {pokemon.Type2}";
            }
        }
    }
} 