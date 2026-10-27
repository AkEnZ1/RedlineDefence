
using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScrapResourceManager : MonoBehaviour
{
    public static ScrapResourceManager Instance { get; private set; }

    [Header("Initial Resources")]
    [SerializeField, Min(0)]
    private int startingScrap = 250;

    private int currentScrap;

    public int CurrentScrap => currentScrap;

    // UI подписывается на событие и узнаёт об изменении баланса.
    public event Action<int> BalanceChanged;

    private void Awake()
    {
        // Не допускаем существование нескольких менеджеров.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[ScrapResourceManager] Обнаружен дубликат менеджера. " +
                "Дубликат будет удалён."
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Объект должен находиться в корне иерархии.
        // Он останется при переходах между сценами.
        DontDestroyOnLoad(gameObject);

        // Начальный баланс задаётся только у первого экземпляра.
        currentScrap = Mathf.Max(0, startingScrap);

        Debug.Log(
            $"[ScrapResourceManager] Менеджер создан. " +
            $"Начальный баланс: {currentScrap}."
        );
    }

    public bool CanAfford(int cost)
    {
        return cost >= 0 && currentScrap >= cost;
    }

    public bool TrySpend(int cost)
    {
        if (cost < 0)
        {
            Debug.LogError(
                "[ScrapResourceManager] Нельзя списать " +
                "отрицательное количество ресурсов."
            );

            return false;
        }

        if (!CanAfford(cost))
            return false;

        if (cost == 0)
            return true;

        currentScrap -= cost;

        NotifyBalanceChanged();

        Debug.Log(
            $"[ScrapResourceManager] Потрачено: {cost}. " +
            $"Остаток: {currentScrap}."
        );

        return true;
    }

    public void AddScrap(int amount)
    {
        if (amount < 0)
        {
            Debug.LogError(
                "[ScrapResourceManager] Для начисления " +
                "используй неотрицательное количество."
            );

            return;
        }

        if (amount == 0)
            return;

        // Защита от переполнения int.
        if (amount > int.MaxValue - currentScrap)
            currentScrap = int.MaxValue;
        else
            currentScrap += amount;

        NotifyBalanceChanged();

        Debug.Log(
            $"[ScrapResourceManager] Получено: {amount}. " +
            $"Баланс: {currentScrap}."
        );
    }

    private void NotifyBalanceChanged()
    {
        BalanceChanged?.Invoke(currentScrap);
    }

    private void OnDestroy()
    {
        // Не сбрасываем ссылку, если уничтожен дубликат,
        // а не тот экземпляр, который владеет Singleton.
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
