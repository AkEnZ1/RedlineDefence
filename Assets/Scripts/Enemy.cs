using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float maxHealth = 100f;
    public float speed = 2f;
    public int damageToCore = 10;
    public Transform[] waypoints;

    private float currentHealth;
    private int targetWaypointIndex = 0;
    private Color[] originalColors;
    private Vector3 pathOffset;
    private float originalSpeed;
    private bool isFrozen = false;
    private bool isOnFire = false;
    private bool isPoisoned = false;
    private Material[] materials;
    private Color[] originalEmissions;

    public void SetOffset(Vector3 offset)
    {
        pathOffset = offset;
    }

    void Start()
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
                originalEmissions[i] = materials[i].GetColor("_EmissionColor");
            }
        }
    }

    void Update()
    {
        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (waypoints == null || waypoints.Length == 0 || targetWaypointIndex >= waypoints.Length)
            return;

        Vector3 actualTargetPosition = waypoints[targetWaypointIndex].position + pathOffset;
        actualTargetPosition.y = transform.position.y;

        Vector3 direction = actualTargetPosition - transform.position;

        Vector3 separation = Vector3.zero;
        float separationRadius = 1.5f;
        float separationForce = 2.0f;

        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, separationRadius);
        foreach (Collider col in nearbyEnemies)
        {
            if (col.gameObject != gameObject && col.CompareTag("Enemy"))
            {
                Vector3 pushDirection = transform.position - col.transform.position;
                pushDirection.y = 0;
                float distance = pushDirection.magnitude;

                if (distance > 0.01f)
                {
                    separation += pushDirection.normalized / distance;
                }
            }
        }

        Vector3 moveDirection = direction.normalized + (separation * separationForce);

        transform.Translate(moveDirection.normalized * speed * Time.deltaTime, Space.World);

        if (Vector3.Distance(transform.position, actualTargetPosition) <= 0.2f)
        {
            targetWaypointIndex++;
            if (targetWaypointIndex >= waypoints.Length)
            {
                ReachCore();
            }
            else
            {
                Vector3 nextTarget = waypoints[targetWaypointIndex].position + pathOffset;
                nextTarget.y = transform.position.y;

                Vector3 lookDirection = nextTarget - transform.position;
                if (lookDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(-lookDirection);
                }
            }
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        StartCoroutine(FlashRed());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashRed()
    {
        // Окрашиваем в ярко-красный при попадании
        SetStatusColor(Color.red, 3f);

        yield return new WaitForSeconds(0.1f);

        // Проверяем, какой статус должен вернуться вместо оригинального цвета
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
        Debug.Log("урон ядру - " + damageToCore);
        FindObjectOfType<WaveManager>().EnemyDefeated();
        Destroy(gameObject);
    }

    private void Die()
    {
        FindObjectOfType<WaveManager>().EnemyDefeated();
        Destroy(gameObject);
    }

    public void ApplyAbility(AbilityType type)
    {
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
        speed = originalSpeed * 0.4f; // Замедление на 60%

        // Визуальное окрашивание в синий цвет заморозки
        SetStatusColor(Color.cyan, 2f);

        yield return new WaitForSeconds(3f);

        speed = previousSpeed;
        isFrozen = false;
        ResetStatusColor();
    }

    private IEnumerator FireRoutine()
    {
        isOnFire = true;

        // В течение 3 секунд каждую секунду наносим урон
        for (int i = 0; i < 3; i++)
        {
            SetStatusColor(Color.red, 3f); // Вспышка пламени
            TakeDamage(10);
            yield return new WaitForSeconds(1f);
        }

        isOnFire = false;
        ResetStatusColor();
    }

    private IEnumerator PoisonRoutine()
    {
        isPoisoned = true;

        // В течение 5 секунд каждую секунду наносим легкий урон
        for (int i = 0; i < 5; i++)
        {
            SetStatusColor(Color.green, 1.5f); // Зеленый оттенок яда
            TakeDamage(4);
            yield return new WaitForSeconds(1f);
        }

        isPoisoned = false;
        ResetStatusColor();
    }

    // Вспомогательный метод для окрашивания под эффекты
    private void SetStatusColor(Color statusColor, float emissionIntensity)
    {
        if (materials == null) return;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null) continue;
            materials[i].color = statusColor;
            if (materials[i].HasProperty("_EmissionColor"))
            {
                materials[i].SetColor("_EmissionColor", statusColor * emissionIntensity);
            }
        }
    }

    // Вспомогательный метод для возврата к нормальному виду
    private void ResetStatusColor()
    {
        if (materials == null || originalColors == null) return;

        // Если на врага действуют другие эффекты, не сбрасываем цвет раньше времени
        if (isFrozen || isOnFire || isPoisoned) return;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null) continue;
            materials[i].color = originalColors[i];
            if (materials[i].HasProperty("_EmissionColor") && originalEmissions != null)
            {
                materials[i].SetColor("_EmissionColor", originalEmissions[i]);
            }
        }
    }
}