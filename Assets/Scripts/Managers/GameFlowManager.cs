
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GamePhase
{
    Preparation,
    Combat,
    Result
}

public sealed class GameFlowManager : MonoBehaviour
{
    [Header("Gameplay Systems")]
    [SerializeField] private BuildingManager buildingManager;
    [SerializeField] private WaveManager waveManager;

    [Header("Phase UI")]
    [SerializeField] private GameObject preparationRoot;
    [SerializeField] private GameObject resultRoot;

    [SerializeField] private Button beginCombatButton;
    [SerializeField] private Button returnToPreparationButton;

    [Header("Optional Text")]
    [SerializeField] private TMP_Text phaseStatusText;
    [SerializeField] private TMP_Text resultMessageText;

    public GamePhase CurrentPhase { get; private set; }
        = GamePhase.Preparation;

    public event Action<GamePhase> PhaseChanged;

    private void Awake()
    {
        if (buildingManager == null)
            buildingManager = FindObjectOfType<BuildingManager>();

        if (waveManager == null)
            waveManager = FindObjectOfType<WaveManager>();
    }

    private void OnEnable()
    {
        if (waveManager != null)
        {
            waveManager.AllWavesCompleted += HandleWavesCompleted;
            waveManager.WaveRunFailed += HandleWaveRunFailed;
        }

        if (beginCombatButton != null)
            beginCombatButton.onClick.AddListener(BeginCombat);

        if (returnToPreparationButton != null)
        {
            returnToPreparationButton.onClick.AddListener(
                ReturnToPreparation
            );
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (waveManager == null)
        {
            Debug.LogError(
                "[GameFlowManager] WaveManager не назначен. " +
                "Бой не сможет запуститься."
            );
        }
        else
        {
            waveManager.PrepareForNewRun();
        }

        ApplyPhase(GamePhase.Preparation);
    }

    private void OnDisable()
    {
        if (waveManager != null)
        {
            waveManager.AllWavesCompleted -= HandleWavesCompleted;
            waveManager.WaveRunFailed -= HandleWaveRunFailed;
        }

        if (beginCombatButton != null)
            beginCombatButton.onClick.RemoveListener(BeginCombat);

        if (returnToPreparationButton != null)
        {
            returnToPreparationButton.onClick.RemoveListener(
                ReturnToPreparation
            );
        }
    }

    /// <summary>
    /// Начинает бой, если текущая фаза — подготовка
    /// и WaveManager настроен корректно.
    /// </summary>
    public void BeginCombat()
    {
        if (CurrentPhase != GamePhase.Preparation)
            return;

        if (waveManager == null)
        {
            Debug.LogError(
                "[GameFlowManager] Нельзя начать бой: " +
                "WaveManager отсутствует."
            );

            return;
        }

        if (!waveManager.CanStart(out string reason))
        {
            Debug.LogWarning(
                $"[GameFlowManager] Нельзя начать бой: {reason}"
            );

            return;
        }

        // Сначала закрываем возможность строительства.
        ApplyPhase(GamePhase.Combat);

        if (!waveManager.BeginWaves())
        {
            Debug.LogError(
                "[GameFlowManager] Волны не запустились. " +
                "Возвращаемся к подготовке."
            );

            ApplyPhase(GamePhase.Preparation);
        }
    }

    /// <summary>
    /// Возвращает игру к фазе подготовки.
    /// В текущей тестовой версии башни и баланс сохраняются.
    /// </summary>
    public void ReturnToPreparation()
    {
        if (CurrentPhase != GamePhase.Result)
            return;

        if (waveManager != null)
            waveManager.PrepareForNewRun();

        ApplyPhase(GamePhase.Preparation);
    }

    private void HandleWavesCompleted()
    {
        if (CurrentPhase != GamePhase.Combat)
            return;

        if (resultMessageText != null)
        {
            resultMessageText.text =
                "ВСЕ ВОЛНЫ ОТРАЖЕНЫ\n\n" +
                "Оборона завершила испытание.";
        }

        ApplyPhase(GamePhase.Result);
    }

    private void HandleWaveRunFailed(string reason)
    {
        if (CurrentPhase != GamePhase.Combat)
            return;

        if (resultMessageText != null)
        {
            resultMessageText.text =
                "ОШИБКА ВОЛНЫ\n\n" + reason;
        }

        ApplyPhase(GamePhase.Result);
    }

    private void ApplyPhase(GamePhase newPhase)
    {
        CurrentPhase = newPhase;

        bool isPreparation = newPhase == GamePhase.Preparation;
        bool isResult = newPhase == GamePhase.Result;

        // Отменяем призрак и блокируем ввод строительства
        // за пределами фазы подготовки.
        if (buildingManager != null)
        {
            buildingManager.CancelPlacement();
            buildingManager.enabled = isPreparation;
        }

        if (preparationRoot != null)
            preparationRoot.SetActive(isPreparation);

        if (resultRoot != null)
            resultRoot.SetActive(isResult);

        if (beginCombatButton != null)
        {
            beginCombatButton.gameObject.SetActive(isPreparation);
        }

        if (returnToPreparationButton != null)
        {
            returnToPreparationButton.gameObject.SetActive(isResult);
        }

        if (phaseStatusText != null)
        {
            switch (newPhase)
            {
                case GamePhase.Preparation:
                    phaseStatusText.text = "ПОДГОТОВКА";
                    break;

                case GamePhase.Combat:
                    phaseStatusText.text = "БОЙ";
                    break;

                case GamePhase.Result:
                    phaseStatusText.text = "РЕЗУЛЬТАТ";
                    break;
            }
        }

        Debug.Log($"[GameFlowManager] Фаза: {newPhase}");

        PhaseChanged?.Invoke(newPhase);
    }
}

