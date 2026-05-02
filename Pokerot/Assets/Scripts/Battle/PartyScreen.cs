using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyScreen : MonoBehaviour
{
    [SerializeField] Text messageText;

    PartyMemberUI[] memberSlots;
    List<Pokemon> pokemons;

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        memberSlots = GetComponentsInChildren<PartyMemberUI>(true);
    }

    public void SetPartyData(List<Pokemon> pokemons)
    {
        if (memberSlots == null || memberSlots.Length == 0)
        {
            Init();
        }

        this.pokemons = pokemons ?? new List<Pokemon>();

        for (int i = 0; i < memberSlots.Length; i++)
        {
            if (i < this.pokemons.Count && this.pokemons[i] != null)
            {
                memberSlots[i].gameObject.SetActive(true);
                memberSlots[i].SetData(this.pokemons[i]);
            }
            else
                memberSlots[i].gameObject.SetActive(false);
        }

        if (messageText != null)
        {
            messageText.text = "Choose a Pokemon";
        }
    }

    public void UpdateMemberSelection(int selectedMember)
    {
        if (pokemons == null || memberSlots == null)
        {
            return;
        }

        for (int i = 0; i < pokemons.Count; i++)
        {
            if (i >= memberSlots.Length || memberSlots[i] == null)
            {
                continue;
            }

            if (i == selectedMember)
                memberSlots[i].SetSelected(true);
            else
                memberSlots[i].SetSelected(false);
        }
    }

    public void SetMessageText(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }
}
