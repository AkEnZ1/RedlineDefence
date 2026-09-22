using System.Collections;
using UnityEngine;

[System.Serializable]
public class Wave
{
    public GameObject[] enemyPrefabs;
    public int count;
    public float spawnRate;
    public float healthMultiplier = 1f;
    public float damageMultiplier = 1f;
}

public class WaveManager : MonoBehaviour
{
    public Wave[] waves;
    public Transform spawnPoint;
    public Transform[] pathWaypoints;
    public UIManager uiManager;
    public float spawnWidth = 4.0f;
    public float spawnHeight = 4.0f;

    private int currentWaveIndex = 0;
    private int enemiesAlive = 0;
    private int enemiesSpawned = 0;

    void Start()
    {
        if (waves.Length > 0)
        {
            StartCoroutine(SpawnWave());
        }
    }

    private IEnumerator SpawnWave()
    {
        Wave currentWave = waves[currentWaveIndex];
        enemiesSpawned = 0;

        uiManager.UpdateWaveText(currentWaveIndex + 1, waves.Length);
        uiManager.UpdateProgressBar(0f);

        while (enemiesSpawned < currentWave.count)
        {
            SpawnEnemy(currentWave);
            enemiesSpawned++;
            uiManager.UpdateProgressBar((float)enemiesSpawned / currentWave.count);

            yield return new WaitForSeconds(1f / currentWave.spawnRate);
        }
    }

    private void SpawnEnemy(Wave wave)
    {
        // Используем новые переменные для расчета смещения
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnWidth / 2, spawnWidth / 2),
            0,
            Random.Range(-spawnHeight / 2, spawnHeight / 2)
        );

        GameObject randomPrefab = wave.enemyPrefabs[Random.Range(0, wave.enemyPrefabs.Length)];
        GameObject enemyObj = Instantiate(randomPrefab, spawnPoint.position + randomOffset, spawnPoint.rotation);

        enemyObj.transform.Rotate(0, 180, 0); // Разворот «лицом» вперед

        Enemy enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.waypoints = pathWaypoints;
            enemyScript.SetOffset(randomOffset);
            enemyScript.maxHealth *= wave.healthMultiplier;
            enemyScript.damageToCore = Mathf.RoundToInt(enemyScript.damageToCore * wave.damageMultiplier);
        }

        enemiesAlive++;
    }

    void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            // Рисуем рамку с учетом ширины и высоты
            Gizmos.DrawWireCube(spawnPoint.position, new Vector3(spawnWidth, 1, spawnHeight));
        }
    }

    public void EnemyDefeated()
    {
        enemiesAlive--;

        if (enemiesAlive <= 0 && enemiesSpawned == waves[currentWaveIndex].count)
        {
            currentWaveIndex++;
            if (currentWaveIndex < waves.Length)
            {
                StartCoroutine(SpawnWave());
            }
            else
            {
                Debug.Log("уровень пройден");
            }
        }
    }
}