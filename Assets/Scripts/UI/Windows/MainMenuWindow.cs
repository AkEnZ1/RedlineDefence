using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Нужно для загрузки сцены боя

public class MainMenuWindow : UIWindow
{
    [Header("Menu Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button guideButton;
    [SerializeField] private Button exitButton;

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene"; // Имя твоей боевой сцены

    private void OnEnable()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (guideButton != null)
            guideButton.onClick.AddListener(OnGuideClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnDisable()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsClicked);

        if (guideButton != null)
            guideButton.onClick.RemoveListener(OnGuideClicked);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExitClicked);
    }

    private void OnPlayClicked()
    {
        // Загружаем сцену боя. Глобальный UIManager и настройки при этом сохранятся!
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnSettingsClicked()
    {
        // Переключаемся на окно настроек через наш UIManager
        UIManager.Instance.OpenWindow("Settings");
    }

    private void OnGuideClicked()
    {
        // Переключаемся на окно справки
        UIManager.Instance.OpenWindow("Guide");
    }

    private void OnExitClicked()
    {
        Debug.Log("[MainMenu] Выход из игры...");
        Application.Quit();
    }
}