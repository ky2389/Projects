using UnityEngine;
using UnityEngine.UI;

public class ArrowButton : MonoBehaviour
{
    public string targetView;
    public Image arrowImage;
    
    private ViewManager viewManager;
    
    public void Initialize(string target, ViewManager manager)
    {
        targetView = target;
        viewManager = manager;
    }
    
    public void SetDirection(Vector3 direction)
    {
        // Set direction of arrow based on input direction
        if (direction == Vector3.up)
            transform.eulerAngles = new Vector3(0, 0, 0); // Up
        else if (direction == Vector3.right)
            transform.eulerAngles = new Vector3(0, 0, 270); // Right
        else if (direction == Vector3.down)
            transform.eulerAngles = new Vector3(0, 0, 180); // Down
        else if (direction == Vector3.left)
            transform.eulerAngles = new Vector3(0, 0, 90); // Left
    }
    
    public void OnClick()
    {
        if (viewManager != null)
            viewManager.ChangeView(targetView);
    }
}