
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurretSelectButton : MonoBehaviour
{
    [Header("Turret")]
    [SerializeField] private TurretData turretData;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text costLabel;

    private Button button;
    private ScrapResourceManager resourceManager;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError(
                $"[TurretSelectButton] На объекте '{name}' " +
                "отсутствует компонент Button."
            );

            enabled = false;
            return;
        }

        button.onClick.AddListener(OnButtonClicked);
    }

    private void Start()
    {
        resourceManager = ScrapResourceManager.Instance;

        if (resourceManager != null)
            resourceManager.BalanceChanged += OnBalanceChanged;
        else
            Debug.LogError(
                "[TurretSelectButton] ScrapResourceManager " +
                "не найден в сцене."
            );

        RefreshButtonState();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnButtonClicked);

        if (resourceManager != null)
            resourceManager.BalanceChanged -= OnBalanceChanged;
    }

    private void OnButtonClicked()
    {
        if (turretData == null)
            return;

        BuildingManager manager = BuildingManager.Instance;

        if (manager == null || !manager.isActiveAndEnabled)
        {
            Debug.Log(
                "[Store] Строительство сейчас недоступно."
            );

            return;
        }

        if (resourceManager == null ||
            !resourceManager.CanAfford(turretData.cost))
        {
            Debug.Log(
                "[Store] Недостаточно металлолома."
            );

            return;
        }

        manager.SelectTurret(turretData);

        Debug.Log(
            $"[Store] Выбрана турель: {turretData.turretName}"
        );
    }

    private void OnBalanceChanged(int newBalance)
    {
        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        if (turretData == null)
            return;

        if (costLabel != null)
            costLabel.text = turretData.cost.ToString();

        if (button != null && resourceManager != null)
        {
            button.interactable =
                resourceManager.CanAfford(turretData.cost);
        }
    }
}
