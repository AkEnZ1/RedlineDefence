using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIWindow : MonoBehaviour
{
    public enum WindowType
    {
        FullScreen, // Полноэкранное меню (закрывает другие полноэкранные)
        Overlay     // Окно поверх игры (HUD, Пауза, диалоги)
    }

    [Header("Window Settings")]
    [SerializeField] private string windowID;
    [SerializeField] private WindowType windowType = WindowType.FullScreen;

    protected CanvasGroup canvasGroup;

    public string WindowID => windowID;
    public WindowType Type => windowType;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    protected virtual void Start()
    {
        // Автоматически регистрируем окно в глобальном менеджере при появлении на сцене
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterWindow(windowID, this);
        }
    }

    protected virtual void OnDestroy()
    {
        // Безопасно отписываемся при уничтожении сцены/объекта
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterWindow(windowID);
        }
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    public virtual void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        gameObject.SetActive(false);
    }
}