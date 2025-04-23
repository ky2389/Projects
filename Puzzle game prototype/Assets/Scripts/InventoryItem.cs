using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    public string itemID;
    public Image itemImage;
    public Color32 normalColor = new Color32(255, 255, 255, 255);
    public Color32 selectedColor = new Color32(0, 0, 0, 255);
    
    private InteractionManager interactionManager;
    private bool isSelected = false;
    
    public void Initialize(string id, Sprite sprite, InteractionManager system)
    {
        itemID = id;
        if (itemImage != null)
        {
            itemImage.sprite = sprite;
            itemImage.color = normalColor;
        }
        interactionManager = system;
    }
    
    public void OnItemClicked()
    {
        if (interactionManager != null)
        {
            if (isSelected)
            {
                // If already selected, deselect it
                interactionManager.DeselectItem();
            }
            else
            {
                // If not selected, select it
                interactionManager.SelectItem(this);
            }
        }
    }
    
    public void Select(bool selected)
    {
        isSelected = selected;
        if (itemImage != null)
        {
            itemImage.color = selected ? selectedColor : normalColor;
        }
    }
}