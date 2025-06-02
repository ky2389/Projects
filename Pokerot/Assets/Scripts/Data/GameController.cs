using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Battle, Dialog, Paused }

public class GameController : MonoBehaviour
{
    [SerializeField] BattleSystem battleSystem;
    public AudioSource BattleMusic;

    GameState state;
    GameState stateBeforePause;

    public static GameController Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
        ConditionsDB.Init();
        BattleMusic = GetComponent<AudioSource>();
    }

    private void Start()
    {
        battleSystem.OnBattleOver += EndBattle;
        state = GameState.Battle;
        StartBattle();
    }

    public void PauseGame(bool pause)
    {
        if (pause)
        {
            stateBeforePause = state;
            state = GameState.Paused;
        }
        else
        {
            state = stateBeforePause;
        }
    }

    public async void StartBattle()
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);

        var playerParty = FindFirstObjectByType<PokemonParty>();
        if (playerParty == null)
        {
            Debug.LogError("No PokemonParty found in scene!");
            return;
        }

        var wildPokemonSettings = FindFirstObjectByType<WildPokemonSettings>();
        if (wildPokemonSettings == null)
        {
            Debug.LogError("No WildPokemonSettings found in scene!");
            return;
        }

        var wildPokemon = await wildPokemonSettings.GetComponent<WildPokemonSettings>().GetRandomWildPokemon();
        if (wildPokemon == null)
        {
            Debug.LogError("Failed to get wild Pokemon!");
            return;
        }

        var wildPokemonCopy = new Pokemon(wildPokemon.Base, wildPokemon.Level);

        BattleMusic.Play();
        battleSystem.StartBattle(playerParty, wildPokemonCopy);
    }

    void EndBattle(bool won)
    {
        BattleMusic.Stop();
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(false);
        
        if (!won)
        {
            // Clean up battle system before scene transition
            battleSystem.gameObject.SetActive(false);
            StartCoroutine(LoadGameOverScene());
        }
        else
        {
            StartBattle();
        }
    }

    IEnumerator LoadGameOverScene()
    {
        // Wait for one frame to ensure all cleanup is complete
        yield return null;
        SceneManager.LoadScene("GameOver");
    }

    private void Update()
    {
        if (state == GameState.Battle)
        {
            battleSystem.HandleUpdate();
        }
        else if (state == GameState.Dialog)
        {
            DialogManager.Instance.HandleUpdate();
        }
    }
}
