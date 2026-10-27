using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Global UI Elements")]
    [SerializeField] private Image brightnessOverlay; // Ссылка на глобальную панель яркости

    private Dictionary<string, UIWindow> windowDictionary = new Dictionary<string, UIWindow>();
    private UIWindow currentFullScreenWindow; // Текущее полноэкранное окно

    private void Awake()
    {
        // Паттерн Singleton с защитой от дублирования и переносом между сценами
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Динамическая регистрация окна при запуске сцены
    public void RegisterWindow(string windowID, UIWindow window)
    {
        if (string.IsNullOrEmpty(windowID))
        {
            Debug.LogWarning("[UIManager] Попытка зарегистрировать окно с пустым ID!");
            return;
        }

        if (!windowDictionary.ContainsKey(windowID))
        {
            windowDictionary.Add(windowID, window);
            window.Hide(); // Прячем окно сразу после регистрации
            Debug.Log($"[UIManager] Окно '{windowID}' успешно зарегистрировано.");
        }
        else
        {
            // Обновляем ссылку, если окно перезагрузилось или пересоздалось
            windowDictionary[windowID] = window;
            window.Hide();
        }
    }

    // Удаление окна из словаря при выгрузке сцены или уничтожении объекта
    public void UnregisterWindow(string windowID)
    {
        if (windowDictionary.ContainsKey(windowID))
        {
            windowDictionary.Remove(windowID);
        }
    }

    public void OpenWindow(string windowID)
    {
        if (!windowDictionary.ContainsKey(windowID))
        {
            Debug.LogWarning($"[UIManager] Окно с ID '{windowID}' не найдено в словаре менеджера!");
            return;
        }

        UIWindow targetWindow = windowDictionary[windowID];

        // Если окно было уничтожено или выключено некорректно
        if (targetWindow == null)
        {
            windowDictionary.Remove(windowID);
            Debug.LogWarning($"[UIManager] Окно с ID '{windowID}' ссылается на уничтоженный объект!");
            return;
        }

        // Если это полноэкранное меню, закрываем предыдущее полноэкранное
        if (targetWindow.Type == UIWindow.WindowType.FullScreen)
        {
            if (currentFullScreenWindow != null && currentFullScreenWindow != targetWindow)
            {
                currentFullScreenWindow.Hide();
            }
            currentFullScreenWindow = targetWindow;
        }

        // Включаем целевое окно
        targetWindow.Show();
    }

    public void CloseWindow(string windowID)
    {
        if (windowDictionary.ContainsKey(windowID) && windowDictionary[windowID] != null)
        {
            windowDictionary[windowID].Hide();
        }
    }

    // Глобальное управление яркостью
    public void SetBrightness(float value)
    {
        if (brightnessOverlay != null)
        {
            Color color = brightnessOverlay.color;
            color.a = Mathf.Lerp(0.6f, 0f, value);
            brightnessOverlay.color = color;
        }
    }
}