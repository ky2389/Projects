using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ViewManager : MonoBehaviour
{
    [System.Serializable]
    public class RoomView
    {
        public string viewName;
        public GameObject viewObject;
        public bool isCloseUp = false;
        public string parentView = ""; // For close-ups, which wall they belong to
        public List<NavArrow> navigationArrows = new List<NavArrow>();
    }
    
    [System.Serializable]
    public class NavArrow
    {
        public string targetView;
        public ArrowPosition position;
    }
    
    public enum ArrowPosition
    {
        Left,
        Right, 
        Up,
        Down
    }
    
    public List<RoomView> roomViews = new List<RoomView>();
    public GameObject arrowPrefab;
    public Transform arrowsPanel;

    public Image transitionImage;
    public float transitionDuration = 0.5f;
    public float blackScreenDuration = 0.2f;
    
    private string currentViewName = "";
    private Dictionary<string, RoomView> viewsDict = new Dictionary<string, RoomView>();
    private List<GameObject> currentArrows = new List<GameObject>();
    private bool isTransitioning = false;

    void Start()
    {
        // Create dictionary for quick access
        foreach (RoomView view in roomViews)
        {
            viewsDict[view.viewName] = view;
            if (view.viewObject != null)
                view.viewObject.SetActive(false);
        }
        if (viewsDict.ContainsKey("SouthWall"))
            ChangeView("SouthWall");
        // else if (roomViews.Count > 0)
        //     SwitchToView(roomViews[0].viewName);
    }
    public void ChangeView(string viewName)
    {
        if (!viewsDict.ContainsKey(viewName) || isTransitioning)
            return;
            
        isTransitioning = true;
        StartCoroutine(TransitionToView(viewName));
    }

    private IEnumerator TransitionToView(string viewName)
    {
        // Fade to black
        yield return StartCoroutine(FadeToBlack());
        
        // Change the view while the screen is black
        SwitchToView(viewName);
        
        // Pause briefly while fully black
        yield return new WaitForSeconds(blackScreenDuration);
        
        // Fade back to transparent
        yield return StartCoroutine(FadeToTransparent());
        
        isTransitioning = false;
    }

        private IEnumerator FadeToBlack()
    {
        if (transitionImage == null)
            yield break;
            
        // Show transition image
        transitionImage.gameObject.SetActive(true);
        
        // Fade to black
        float elapsedTime = 0;
        while (elapsedTime < transitionDuration)
        {
            float alpha = Mathf.Lerp(0, 1, elapsedTime / transitionDuration);
            transitionImage.color = new Color(0, 0, 0, alpha);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we reach full black
        transitionImage.color = new Color(0, 0, 0, 1);
    }
    
    private IEnumerator FadeToTransparent()
    {
        if (transitionImage == null)
            yield break;
            
        // Fade back to transparent
        float elapsedTime = 0;
        while (elapsedTime < transitionDuration)
        {
            float alpha = Mathf.Lerp(1, 0, elapsedTime / transitionDuration);
            transitionImage.color = new Color(0, 0, 0, alpha);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we reach full transparent
        transitionImage.color = new Color(0, 0, 0, 0);
        transitionImage.gameObject.SetActive(false);
    }

    public void SwitchToView(string viewName)
    {
        if (!viewsDict.ContainsKey(viewName))
        {
            Debug.LogWarning("View name not found in dictionary: " + viewName); 
            return;
        }
            
        // Disable current view
        if (!string.IsNullOrEmpty(currentViewName) && viewsDict.ContainsKey(currentViewName))
            viewsDict[currentViewName].viewObject.SetActive(false);
            
        // Clear current arrows
        foreach (GameObject arrow in currentArrows)
            Destroy(arrow);
        currentArrows.Clear();
        
        // Enable new view
        RoomView newView = viewsDict[viewName];
        newView.viewObject.SetActive(true);
        currentViewName = viewName;
        
        // Create navigation arrows
        foreach (NavArrow navArrow in newView.navigationArrows)
        {
            if (viewsDict.ContainsKey(navArrow.targetView))
            {
                GameObject arrowObj = Instantiate(arrowPrefab, arrowsPanel);
                ArrowButton arrowButton = arrowObj.GetComponent<ArrowButton>();
                if (arrowButton != null)
                {
                    arrowButton.Initialize(navArrow.targetView, this);
                    currentArrows.Add(arrowObj);
                    
                    // Position arrows based on specified position
                    Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                    Vector2 arrowPos = Vector2.zero;
                    
                    switch (navArrow.position)
                    {
                        case ArrowPosition.Left:
                            arrowPos = new Vector2(50, screenSize.y / 2);
                            arrowButton.SetDirection(Vector3.left);
                            break;
                        case ArrowPosition.Right:
                            arrowPos = new Vector2(screenSize.x - 450, screenSize.y / 2);
                            arrowButton.SetDirection(Vector3.right);
                            break;
                        case ArrowPosition.Up:
                            arrowPos = new Vector2(screenSize.x / 2-200, screenSize.y - 60);
                            arrowButton.SetDirection(Vector3.up);
                            break;
                        case ArrowPosition.Down:
                            arrowPos = new Vector2(screenSize.x / 2-200, 60);
                            arrowButton.SetDirection(Vector3.down);
                            break;
                    }
                    
                    arrowObj.GetComponent<RectTransform>().position = arrowPos;
                }
            }
        }
    }
    
    // Method to handle going from a close-up back to its parent wall
    public void ReturnFromCloseUp()
    {
        if (string.IsNullOrEmpty(currentViewName) || !viewsDict.ContainsKey(currentViewName))
            return;
            
        RoomView currentView = viewsDict[currentViewName];
        if (currentView.isCloseUp && !string.IsNullOrEmpty(currentView.parentView))
            ChangeView(currentView.parentView);
    }
    // void Update()
    // {
    //     //check if vaild current view
    //     if (string.IsNullOrEmpty(currentViewName) || !viewsDict.ContainsKey(currentViewName))
    //         Debug.LogWarning("Current view name is invalid or empty.");
    // }
}