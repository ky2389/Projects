using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] Button restartButton;
    [SerializeField] Button quitButton;

    private void Start()
    {
        restartButton.onClick.AddListener(() => GameOverManager.Instance.RestartGame());
        quitButton.onClick.AddListener(() => Application.Quit());
    }
} 