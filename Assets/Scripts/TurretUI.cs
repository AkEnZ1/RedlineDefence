using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TurretUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Button freezeButton;
    public Button fireButton;
    public Button poisonButton;

    public Slider freezeSlider;
    public Slider fireSlider;
    public Slider poisonSlider;

    public CanvasGroup buttonsGroup;
    public RectTransform slidersPanel;

    public float slideOffset = 60f;
    public float moveSpeed = 10f;

    private Vector2 targetSlidersPosition;
    private Vector2 slidersUpPosition;
    private Vector2 slidersDownPosition;

    private bool isHovered = false;

    void Start()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            if (Camera.main != null) canvas.worldCamera = Camera.main;
            else canvas.worldCamera = FindFirstObjectByType<Camera>();
        }

        if (slidersPanel != null && buttonsGroup != null)
        {
            RectTransform buttonsRect = buttonsGroup.GetComponent<RectTransform>();

            slidersUpPosition = buttonsRect.anchoredPosition;
            slidersDownPosition = slidersUpPosition - new Vector2(0, slideOffset);

            slidersPanel.anchoredPosition = slidersUpPosition;
            targetSlidersPosition = slidersUpPosition;
        }

        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }
    }

    void Update()
    {
        if (slidersPanel != null)
        {
            slidersPanel.anchoredPosition = Vector2.Lerp(slidersPanel.anchoredPosition, targetSlidersPosition, Time.deltaTime * moveSpeed);
        }

        if (buttonsGroup != null)
        {
            float targetAlpha = isHovered ? 1f : 0f;
            buttonsGroup.alpha = Mathf.Lerp(buttonsGroup.alpha, targetAlpha, Time.deltaTime * moveSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GameManager.isGameOver) return;

        isHovered = true;
        targetSlidersPosition = slidersDownPosition;
        if (buttonsGroup != null)
        {
            buttonsGroup.interactable = true;
            buttonsGroup.blocksRaycasts = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetSlidersPosition = slidersUpPosition;
        if (buttonsGroup != null)
        {
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }
    }
}