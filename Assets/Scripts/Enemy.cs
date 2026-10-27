
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Основные характеристики")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(0f)] public float speed = 2f;
    [Min(0)] public int damageToCore = 10;
    public Transform[] waypoints;

    [Header("Награда")]
    [Min(0)] public int scrapReward = 10;

    private float currentHealth;
    private int targetWaypointIndex = 0;
    private Color[] originalColors;
    private Vector3 pathOffset;
    private float originalSpeed;
    private bool isFrozen = false;
    private bool isOnFire = false;
    private bool isPoisoned = false;

    // Защищает от повторной смерти и повторного начисления награды.
    private bool isResolved = false;

    private Material[] materials;
    private Color[] originalEmissions;

    public void SetOffset(Vector3 offset)
    {
        pathOffset = offset;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        originalSpeed = speed;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        materials = new Material[renderers.Length];
        originalColors = new Color[renderers.Length];
        originalEmissions = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            materials[i] = renderers[i].material;
            originalColors[i] = materials[i].color;

            if (materials[i].HasProperty("_EmissionColor"))
            {
                originalEmissions[i] =
                    materials[i].GetColor("_EmissionColor");
            }
        }
    }

    private void Update()
    {
        if (isResolved)
            return;

        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (waypoints == null ||
            waypoints.Length == 0 ||
            targetWaypointIndex >= waypoints.Length)
        {
            return;
        }

        Transform targetWaypoint = waypoints[targetWaypointIndex];

        if (targetWaypoint == null)
            return;

        Vector3 actualTargetPosition =
            targetWaypoint.position + pathOffset;

        actualTargetPosition.y = transform.position.y;

        Vector3 direction = actualTargetPosition - transform.position;

        Vector3 separation = Vector3.zero;
        float separationRadius = 1.5f;
        float separationForce = 2.0f;

        Collider[] nearbyEnemies =
            Physics.OverlapSphere(transform.position, separationRadius);

        foreach (Collider col in nearbyEnemies)
        {
            if (col.gameObject != gameObject &&
                col.CompareTag("Enemy"))
            {
                Vector3 pushDirection =
                    transform.position - col.transform.position;

                pushDirection.y = 0f;

                float distance = pushDirection.magnitude;

                if (distance > 0.01f)
                {
                    separation += pushDirection.normalized / distance;
                }
            }
        }

        Vector3 moveDirection =
            direction.normalized + separation * separationForce;

        transform.Translate(
            moveDirection.normalized * speed * Time.deltaTime,
            Space.World
        );

        if (Vector3.Distance(
                transform.position,
                actualTargetPosition) <= 0.2f)
        {
            targetWaypointIndex++;

            if (targetWaypointIndex >= waypoints.Length)
            {
                ReachCore();
            }
            else
            {
                Transform nextWaypoint = waypoints[targetWaypointIndex];

                if (nextWaypoint == null)
                    return;

                Vector3 nextTarget =
                    nextWaypoint.position + pathOffset;

                nextTarget.y = transform.position.y;

                Vector3 lookDirection = nextTarget - transform.position;

                if (lookDirection != Vector3.zero)
                {
                    transform.rotation =
                        Quaternion.LookRotation(-lookDirection);
                }
            }
        }
    }

    public void TakeDamage(int amount)
    {
        // Не принимаем урон после завершения врага
        // и не допускаем отрицательный урон.
        if (isResolved || amount <= 0)
            return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        StartCoroutine(FlashRed());
    }

    private IEnumerator FlashRed()
    {
        SetStatusColor(Color.red, 3f);

        yield return new WaitForSeconds(0.1f);

        if (isResolved)
            yield break;

        if (isFrozen)
        {
            SetStatusColor(Color.cyan, 2f);
        }
        else if (isPoisoned)
        {
            SetStatusColor(Color.green, 1.5f);
        }
        else if (isOnFire)
        {
            SetStatusColor(Color.red, 3f);
        }
        else
        {
            ResetStatusColor();
        }
    }

    private void ReachCore()
    {
        if (isResolved)
            return;

        Debug.Log("Урон ядру: " + damageToCore);

        // Враг достиг ядра — награда за убийство не выдаётся.
        ResolveEnemy(false);
    }

    private void Die()
    {
        if (isResolved)
            return;

        // Враг уничтожен — выдаём награду один раз.
        ResolveEnemy(true);
    }

    private void ResolveEnemy(bool rewardForKill)
    {
        if (isResolved)
            return;

        // Сначала фиксируем исход, чтобы повторный урон
        // не привёл к повторной награде.
        isResolved = true;

        if (rewardForKill && scrapReward > 0)
        {
            ScrapResourceManager resourceManager =
                ScrapResourceManager.Instance;

            if (resourceManager != null)
            {
                resourceManager.AddScrap(scrapReward);

                Debug.Log(
                    $"[Enemy] Враг уничтожен. " +
                    $"Награда: {scrapReward} лома."
                );
            }
            else
            {
                Debug.LogWarning(
                    "[Enemy] ScrapResourceManager не найден. " +
                    "Награда за убийство не начислена."
                );
            }
        }

        // Сообщаем менеджеру волн, что этот враг выбыл.
        WaveManager waveManager = FindObjectOfType<WaveManager>();

        if (waveManager != null)
        {
            waveManager.EnemyDefeated();
        }

        Destroy(gameObject);
    }

    public void ApplyAbility(AbilityType type)
    {
        if (isResolved)
            return;

        if (type == AbilityType.Freeze && !isFrozen)
            StartCoroutine(FreezeRoutine());

        if (type == AbilityType.Fire && !isOnFire)
            StartCoroutine(FireRoutine());

        if (type == AbilityType.Poison && !isPoisoned)
            StartCoroutine(PoisonRoutine());
    }

    private IEnumerator FreezeRoutine()
    {
        isFrozen = true;

        float previousSpeed = speed;
        speed = originalSpeed * 0.4f;

        SetStatusColor(Color.cyan, 2f);

        yield return new WaitForSeconds(3f);

        if (isResolved)
            yield break;

        speed = previousSpeed;
        isFrozen = false;

        ResetStatusColor();
    }

    private IEnumerator FireRoutine()
    {
        isOnFire = true;

        for (int i = 0; i < 3; i++)
        {
            if (isResolved)
                yield break;

            SetStatusColor(Color.red, 3f);
            TakeDamage(10);

            if (isResolved)
                yield break;

            yield return new WaitForSeconds(1f);
        }

        isOnFire = false;
        ResetStatusColor();
    }

    private IEnumerator PoisonRoutine()
    {
        isPoisoned = true;

        for (int i = 0; i < 5; i++)
        {
            if (isResolved)
                yield break;

            SetStatusColor(Color.green, 1.5f);
            TakeDamage(4);

            if (isResolved)
                yield break;

            yield return new WaitForSeconds(1f);
        }

        isPoisoned = false;
        ResetStatusColor();
    }

    private void SetStatusColor(
        Color statusColor,
        float emissionIntensity)
    {
        if (materials == null)
            return;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null)
                continue;

            materials[i].color = statusColor;

            if (materials[i].HasProperty("_EmissionColor"))
            {
                materials[i].SetColor(
                    "_EmissionColor",
                    statusColor * emissionIntensity
                );
            }
        }
    }

    private void ResetStatusColor()
    {
        if (materials == null || originalColors == null)
            return;

        if (isFrozen || isOnFire || isPoisoned)
            return;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null)
                continue;

            materials[i].color = originalColors[i];

            if (materials[i].HasProperty("_EmissionColor") &&
                originalEmissions != null)
            {
                materials[i].SetColor(
                    "_EmissionColor",
                    originalEmissions[i]
                );
            }
        }
    }
}
