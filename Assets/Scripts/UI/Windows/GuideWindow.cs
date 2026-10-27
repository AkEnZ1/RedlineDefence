using UnityEngine;
using UnityEngine.UI;

public class GuideWindow : UIWindow
{
    [Header("Navigation")]
    [SerializeField] private Button backButton;

    private void OnEnable()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDisable()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);
    }

    private void OnBackClicked()
    {
        // Возвращаемся в Главное меню
        UIManager.Instance.OpenWindow("MainMenu");
    }
}