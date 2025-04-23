using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InteractionManager : MonoBehaviour
{
    [Header("UI References")]
    public Image cursorNormal;
    public Image cursorHover;
    public Transform inventoryPanel;
    public GameObject inventoryItemPrefab;
    
    private List<InventoryItem> inventoryItems = new List<InventoryItem>();
    private InventoryItem selectedItem;
    
    void Start()
    {
        // Initialize cursor
        Cursor.visible = false;
        if (cursorNormal != null)
            cursorNormal.gameObject.SetActive(true);
        if (cursorHover != null)
            cursorHover.gameObject.SetActive(false);
    }
    
    void Update()
    {
        // Update cursor position
        Vector2 cursorPos = Input.mousePosition;
        if (cursorNormal != null)
            cursorNormal.rectTransform.position = cursorPos;
        if (cursorHover != null)
            cursorHover.rectTransform.position = cursorPos;
        
        // Check for interactable UI elements
        if (EventSystem.current.IsPointerOverGameObject())
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            bool foundInteractable = false;
            
            foreach (RaycastResult result in results)
            {
                InteractableObject interactable = result.gameObject.GetComponent<InteractableObject>();
                InventoryItem inventoryItem = result.gameObject.GetComponent<InventoryItem>();
                if(inventoryItem != null)
                {
                    //show hover cursor
                    if(cursorNormal != null) cursorNormal.gameObject.SetActive(false);
                    if(cursorHover != null) cursorHover.gameObject.SetActive(true);
                    foundInteractable = true;
                }
                if (interactable != null)
                {
                    // Show hover cursor
                    if (cursorNormal != null) cursorNormal.gameObject.SetActive(false);
                    if (cursorHover != null) cursorHover.gameObject.SetActive(true);
                    
                    foundInteractable = true;
                    
                    // Process click
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (selectedItem == null)
                        {
                            // Direct interaction
                            interactable.Interact();
                            ResetCursor();
                        }
                        else
                        {
                            // Use item on target
                            bool used = interactable.UseItemOn(selectedItem.itemID);
                            if (used)
                            {
                                if(selectedItem.itemID!="matches")
                                {
                                    RemoveItemFromInventory(selectedItem);
                                    selectedItem = null;
                                }
                            }
                        }
                    }
                    
                    break;
                }
            }
            
            if (!foundInteractable)
                ResetCursor();
        }
        else
        {
            ResetCursor();
        }
        
        
        // Debug.Log("Selected Item: " + (selectedItem != null ? selectedItem.itemID : "None"));
    }
    
    private void ResetCursor()
    {
        if (cursorNormal != null) cursorNormal.gameObject.SetActive(true);
        if (cursorHover != null) cursorHover.gameObject.SetActive(false);
    }
    
    public void AddItemToInventory(string itemID, Sprite itemSprite)
    {
        if (inventoryPanel == null || inventoryItemPrefab == null)
            return;
            
        GameObject newItemObj = Instantiate(inventoryItemPrefab, inventoryPanel);
        InventoryItem newItem = newItemObj.GetComponent<InventoryItem>();
        
        if (newItem != null)
        {
            newItem.Initialize(itemID, itemSprite, this);
            inventoryItems.Add(newItem);
        }
    }
    
    public void RemoveItemFromInventory(InventoryItem item)
    {
        if (inventoryItems.Contains(item))
        {
            inventoryItems.Remove(item);
            Destroy(item.gameObject);
        }
    }
    
    public void SelectItem(InventoryItem item)
    {
        // Deselect previous item
        if (selectedItem != null)
            selectedItem.Select(false);
            
        // Select new item
        selectedItem = item;
        item.Select(true);
    }
    public void DeselectItem()
{
    if (selectedItem != null)
    {
        selectedItem.Select(false);
        selectedItem = null;
    }
}
}