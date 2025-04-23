using UnityEngine;
using UnityEngine.UI;

public class ImageChange : MonoBehaviour
{
    public Image oldImage;
    public Sprite newImage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ChangeImage()
    {
        oldImage.sprite = newImage;
    }
}
