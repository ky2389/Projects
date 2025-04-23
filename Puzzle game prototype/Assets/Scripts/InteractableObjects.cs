using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class InteractableObject : MonoBehaviour
{
    [Header("Interaction Settings")]
    public string objectName;
    public string objectDescription;
    public bool canPickup = false;
    public string itemID = "";
    public Sprite itemSprite;
    
    [Header("Events")]
    public UnityEvent onInteract;
    public UnityEvent onPickup;
    
    [Header("Item Combinations")]
    public List<ItemCombination> itemCombinations = new List<ItemCombination>();
    
    [System.Serializable]
    public class ItemCombination
    {
        public string requiredItemID;
        public UnityEvent onCombine;
    }
    
    public void Interact()
    {
        onInteract.Invoke();
        
        if (canPickup)
        {
            InteractionManager InteractionManager = FindFirstObjectByType<InteractionManager>();
            if (InteractionManager != null)
            {
                InteractionManager.AddItemToInventory(itemID, itemSprite);
                onPickup.Invoke();
                gameObject.SetActive(false);
            }
        }
    }
    
    public bool UseItemOn(string usedItemID)
    {
        foreach (ItemCombination combination in itemCombinations)
        {
            if (combination.requiredItemID == usedItemID)
            {
                combination.onCombine.Invoke();
                return true;
            }
        }
        
        return false;
    }
}