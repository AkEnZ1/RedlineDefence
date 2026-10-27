
using TMPro;
using UnityEngine;

public sealed class ScrapCounterView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text balanceText;

    [SerializeField]
    private string labelPrefix = "МЕТАЛЛОЛОМ";

    private ScrapResourceManager resourceManager;

    private void Start()
    {
        resourceManager = ScrapResourceManager.Instance;

        if (resourceManager == null)
        {
            Debug.LogError(
                "[ScrapCounterView] ScrapResourceManager " +
                "не найден в сцене."
            );

            enabled = false;
            return;
        }

        resourceManager.BalanceChanged += RefreshBalance;

        // Сначала показываем стартовый баланс.
        RefreshBalance(resourceManager.CurrentScrap);
    }

    private void OnDestroy()
    {
        if (resourceManager != null)
        {
            resourceManager.BalanceChanged -= RefreshBalance;
        }
    }

    private void RefreshBalance(int amount)
    {
        if (balanceText == null)
        {
            Debug.LogError(
                "[ScrapCounterView] Не назначен TMP_Text."
            );

            return;
        }

        balanceText.text = $"{labelPrefix}: {amount}";
    }
}
