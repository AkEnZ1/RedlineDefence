
using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class Wave
{
    public GameObject[] enemyPrefabs;
    [Min(0)] public int count = 5;
    [Min(0.01f)] public float spawnRate = 1f;
    [Min(0f)] public float healthMultiplier = 1f;
    [Min(0f)] public float damageMultiplier = 1f;
}

public class WaveManager : MonoBehaviour
{
    [Header("Wave Configuration")]
    public Wave[] waves;

    [Header("Spawn")]
    public Transform spawnPoint;
    public Transform[] pathWaypoints;
    public float spawnWidth = 4f;
    public float spawnHeight = 4f;

    [Header("Optional HUD")]
    public CombatHUDView uiManager;

    private int currentWaveIndex;
    private int enemiesAlive;
    private int enemiesSpawned;

    private Coroutine waveRoutine;
    private bool isRunning;

    public bool IsRunning => isRunning;
    public int TotalWaves => waves != null ? waves.Length : 0;
    public int CurrentWaveNumber => currentWaveIndex + 1;

    // Событие вызывается после завершения всех волн
    // и устранения всех врагов.
    public event Action AllWavesCompleted;

    // Событие сообщает о внутренней ошибке запуска волн.
    public event Action<string> WaveRunFailed;

    /// <summary>
    /// Проверяет, можно ли запустить бой.
    /// Не меняет состояние игры.
    /// </summary>
    public bool CanStart(out string reason)
    {
        reason = string.Empty;

        if (isRunning)
        {
            reason = "Волны уже запущены.";
            return false;
        }

        if (waves == null || waves.Length == 0)
        {
            reason = "В WaveManager не настроены волны.";
            return false;
        }

        if (spawnPoint == null)
        {
            reason = "Не назначен Spawn Point.";
            return false;
        }

        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            reason = "Не назначены точки маршрута врагов.";
            return false;
        }

        foreach (Transform waypoint in pathWaypoints)
        {
            if (waypoint == null)
            {
                reason = "В маршруте врагов есть пустая точка.";
                return false;
            }
        }

        for (int i = 0; i < waves.Length; i++)
        {
            Wave wave = waves[i];

            if (wave == null)
            {
                reason = $"Волна {i + 1} не настроена.";
                return false;
            }

            if (wave.count < 0)
            {
                reason = $"У волны {i + 1} отрицательное количество врагов.";
                return false;
            }

            // Пустая волна разрешена.
            if (wave.count == 0)
                continue;

            if (wave.spawnRate <= 0f)
            {
                reason = $"У волны {i + 1} некорректный Spawn Rate.";
                return false;
            }

            if (!HasValidEnemyPrefab(wave))
            {
                reason =
                    $"В волне {i + 1} нет префаба с компонентом Enemy.";
                return false;
            }
        }

        return true;
    }

    private bool HasValidEnemyPrefab(Wave wave)
    {
        if (wave.enemyPrefabs == null)
            return false;

        foreach (GameObject prefab in wave.enemyPrefabs)
        {
            if (prefab != null &&
                prefab.GetComponentInChildren<Enemy>(true) != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Сбрасывает счётчики к началу цикла.
    /// Не запускает волны автоматически.
    /// </summary>
    public void PrepareForNewRun()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        isRunning = false;
        currentWaveIndex = 0;
        enemiesAlive = 0;
        enemiesSpawned = 0;

        if (uiManager != null)
        {
            if (TotalWaves > 0)
                uiManager.UpdateWaveText(1, TotalWaves);

            uiManager.UpdateProgressBar(0f);
        }
    }

    /// <summary>
    /// Запускает последовательное прохождение всех волн.
    /// </summary>
    public bool BeginWaves()
    {
        if (isRunning)
            return false;

        if (!CanStart(out string reason))
        {
            Debug.LogError(
                $"[WaveManager] Невозможно начать бой: {reason}"
            );

            return false;
        }

        isRunning = true;
        waveRoutine = StartCoroutine(RunAllWaves());

        return true;
    }

    private IEnumerator RunAllWaves()
    {
        for (currentWaveIndex = 0;
             currentWaveIndex < waves.Length;
             currentWaveIndex++)
        {
            Wave currentWave = waves[currentWaveIndex];

            enemiesSpawned = 0;
            enemiesAlive = 0;

            if (uiManager != null)
            {
                uiManager.UpdateWaveText(
                    currentWaveIndex + 1,
                    waves.Length
                );

                uiManager.UpdateProgressBar(0f);
            }

            // Пустая волна пропускается.
            if (currentWave.count == 0)
                continue;

            while (enemiesSpawned < currentWave.count)
            {
                if (!SpawnEnemy(currentWave))
                {
                    isRunning = false;
                    waveRoutine = null;

                    WaveRunFailed?.Invoke(
                        $"Не удалось создать врага в волне " +
                        $"{currentWaveIndex + 1}."
                    );

                    yield break;
                }

                enemiesSpawned++;

                if (uiManager != null)
                {
                    float progress =
                        (float)enemiesSpawned / currentWave.count;

                    uiManager.UpdateProgressBar(progress);
                }

                // Интервал между появлениями врагов.
                if (enemiesSpawned < currentWave.count)
                {
                    yield return new WaitForSeconds(
                        1f / currentWave.spawnRate
                    );
                }
            }

            // Все враги волны появились.
            // Ждём, пока они будут уничтожены или достигнут конца маршрута.
            while (enemiesAlive > 0)
            {
                yield return null;
            }

            // Только после этого цикл переходит к следующей волне.
        }

        isRunning = false;
        waveRoutine = null;

        Debug.Log("[WaveManager] Все волны завершены.");

        AllWavesCompleted?.Invoke();
    }

    private bool SpawnEnemy(Wave wave)
    {
        GameObject prefab = GetRandomValidEnemyPrefab(wave);

        if (prefab == null)
        {
            Debug.LogError(
                "[WaveManager] Не удалось найти подходящий префаб врага."
            );

            return false;
        }

        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(
                -Mathf.Max(0f, spawnWidth) / 2f,
                Mathf.Max(0f, spawnWidth) / 2f
            ),
            0f,
            UnityEngine.Random.Range(
                -Mathf.Max(0f, spawnHeight) / 2f,
                Mathf.Max(0f, spawnHeight) / 2f
            )
        );

        GameObject enemyObject = Instantiate(
            prefab,
            spawnPoint.position + randomOffset,
            spawnPoint.rotation
        );

        enemyObject.transform.Rotate(0f, 180f, 0f);

        Enemy enemy = enemyObject.GetComponentInChildren<Enemy>();

        if (enemy == null)
        {
            Destroy(enemyObject);
            return false;
        }

        enemy.waypoints = pathWaypoints;
        enemy.SetOffset(randomOffset);

        enemy.maxHealth *= wave.healthMultiplier;
        enemy.damageToCore = Mathf.RoundToInt(
            enemy.damageToCore * wave.damageMultiplier
        );

        enemiesAlive++;

        return true;
    }

    private GameObject GetRandomValidEnemyPrefab(Wave wave)
    {
        int validCount = 0;

        foreach (GameObject prefab in wave.enemyPrefabs)
        {
            if (prefab != null &&
                prefab.GetComponentInChildren<Enemy>(true) != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
            return null;

        int selectedIndex = UnityEngine.Random.Range(0, validCount);

        foreach (GameObject prefab in wave.enemyPrefabs)
        {
            if (prefab == null ||
                prefab.GetComponentInChildren<Enemy>(true) == null)
            {
                continue;
            }

            if (selectedIndex == 0)
                return prefab;

            selectedIndex--;
        }

        return null;
    }

    /// <summary>
    /// Вызывается Enemy при уничтожении или достижении конца маршрута.
    /// </summary>
    public void EnemyDefeated()
    {
        if (!isRunning)
            return;

        if (enemiesAlive <= 0)
        {
            Debug.LogWarning(
                "[WaveManager] Получен лишний вызов EnemyDefeated."
            );

            return;
        }

        enemiesAlive--;
    }

    private void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawWireCube(
                spawnPoint.position,
                new Vector3(
                    spawnWidth,
                    1f,
                    spawnHeight
                )
            );
        }

        if (pathWaypoints != null && pathWaypoints.Length > 1)
        {
            Gizmos.color = Color.yellow;

            for (int i = 0; i < pathWaypoints.Length - 1; i++)
            {
                if (pathWaypoints[i] != null &&
                    pathWaypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(
                        pathWaypoints[i].position,
                        pathWaypoints[i + 1].position
                    );
                }
            }
        }
    }
}
