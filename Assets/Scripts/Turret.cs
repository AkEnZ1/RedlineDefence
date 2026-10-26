using System.Collections.Generic;
using UnityEngine;



public class Turret : MonoBehaviour
{
    [Header("Данные турели (ScriptableObject)")]
    public TurretData turretData;

    [Header("Настройки характеристик")]
    public float range = 5f;
    public float fireRate = 1f;
    public int damage = 20;
    public float projectileSpeed = 10f;

    [Header("Ссылки на объекты")]
    public Transform partToRotate;
    public GameObject projectilePrefab;
    public Transform[] firePoints;

    [Header("Способности")]
    public float abilityDuration = 5f;
    public float freezeCooldown = 10f;
    public float fireCooldown = 12f;
    public float poisonCooldown = 15f;

    [Header("Интерфейс турели")]
    public TurretUI turretUI;
    public GameObject uiPrefab;
    public Vector3 uiOffset = new Vector3(0, 2f, 0);

    private Transform target;
    private float fireCountdown = 0f;
    private int currentFirePointIndex = 0;

    private AbilityType currentAbility = AbilityType.None;
    private float abilityTimer = 0f;
    private float freezeTimer = 0f;
    private float fireTimer = 0f;
    private float poisonTimer = 0f;

    private Color normalColor = Color.white;
    private Color freezeColor = Color.cyan;
    private Color fireColor = Color.red;
    private Color poisonColor = Color.green;

    private GameObject myUIInstance;

    void Start()
    {
        // 1. Спавн и привязка World-Space UI над турелью
        if (uiPrefab != null)
        {
            myUIInstance = Instantiate(uiPrefab, transform.position, transform.rotation);

            UIFollower follower = myUIInstance.GetComponent<UIFollower>();
            if (follower != null)
            {
                follower.target = transform;
                follower.offset = this.uiOffset;
            }

            TurretUI uiScript = myUIInstance.GetComponent<TurretUI>();
            if (uiScript != null)
            {
                this.turretUI = uiScript;

                if (uiScript.freezeButton != null)
                    uiScript.freezeButton.onClick.AddListener(ActivateFreeze);

                if (uiScript.fireButton != null)
                    uiScript.fireButton.onClick.AddListener(ActivateFire);

                if (uiScript.poisonButton != null)
                    uiScript.poisonButton.onClick.AddListener(ActivatePoison);
            }
        }

        // 2. Запуск регулярного поиска врагов каждые 0.5 сек (оптимизация производительности)
        InvokeRepeating(nameof(UpdateTarget), 0f, 0.5f);
    }

    void UpdateTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null && shortestDistance <= range)
        {
            target = nearestEnemy.transform;
        }
        else
        {
            target = null;
        }
    }

    void Update()
    {
        UpdateAbilityTimers();

        if (myUIInstance != null)
        {
            UIFollower follower = myUIInstance.GetComponent<UIFollower>();
            if (follower != null)
            {
                follower.offset = this.uiOffset;
            }
        }

        if (target == null) return;

        LockOnTarget();

        if (fireCountdown <= 0f)
        {
            Shoot();
            fireCountdown = 1f / fireRate;
        }
        fireCountdown -= Time.deltaTime;
    }

    void LockOnTarget()
    {
        Vector3 dir = target.position - transform.position;
        Quaternion lookRotation = Quaternion.LookRotation(-dir);
        Vector3 rotation = Quaternion.Lerp(partToRotate.rotation, lookRotation, Time.deltaTime * 10f).eulerAngles;
        partToRotate.rotation = Quaternion.Euler(rotation.x, rotation.y, 0f);
    }

    void Shoot()
    {
        if (firePoints.Length == 0) return;

        GameObject projectileGO = Instantiate(projectilePrefab, firePoints[currentFirePointIndex].position, firePoints[currentFirePointIndex].rotation);
        Projectile projectile = projectileGO.GetComponent<Projectile>();

        if (projectile != null)
        {
            Color projColor = normalColor;
            if (currentAbility == AbilityType.Freeze) projColor = freezeColor;
            else if (currentAbility == AbilityType.Fire) projColor = fireColor;
            else if (currentAbility == AbilityType.Poison) projColor = poisonColor;

            projectile.Seek(target, projectileSpeed, damage, currentAbility, projColor);
        }

        currentFirePointIndex = (currentFirePointIndex + 1) % firePoints.Length;
    }

    private void UpdateAbilityTimers()
    {
        if (currentAbility != AbilityType.None)
        {
            abilityTimer -= Time.deltaTime;
            if (abilityTimer <= 0) currentAbility = AbilityType.None;
        }

        if (freezeTimer > 0) freezeTimer -= Time.deltaTime;
        if (fireTimer > 0) fireTimer -= Time.deltaTime;
        if (poisonTimer > 0) poisonTimer -= Time.deltaTime;

        if (turretUI != null)
        {
            if (turretUI.freezeSlider != null)
                turretUI.freezeSlider.value = currentAbility == AbilityType.Freeze ? abilityTimer / abilityDuration : 1f - (freezeTimer / freezeCooldown);

            if (turretUI.fireSlider != null)
                turretUI.fireSlider.value = currentAbility == AbilityType.Fire ? abilityTimer / abilityDuration : 1f - (fireTimer / fireCooldown);

            if (turretUI.poisonSlider != null)
                turretUI.poisonSlider.value = currentAbility == AbilityType.Poison ? abilityTimer / abilityDuration : 1f - (poisonTimer / poisonCooldown);
        }
    }

    public void ActivateFreeze()
    {
        if (freezeTimer <= 0 && currentAbility == AbilityType.None)
        {
            currentAbility = AbilityType.Freeze;
            abilityTimer = abilityDuration;
            freezeTimer = freezeCooldown;
        }
    }

    public void ActivateFire()
    {
        if (fireTimer <= 0 && currentAbility == AbilityType.None)
        {
            currentAbility = AbilityType.Fire;
            abilityTimer = abilityDuration;
            fireTimer = fireCooldown;
        }
    }

    public void ActivatePoison()
    {
        if (poisonTimer <= 0 && currentAbility == AbilityType.None)
        {
            currentAbility = AbilityType.Poison;
            abilityTimer = abilityDuration;
            poisonTimer = poisonCooldown;
        }
    }

    private void OnDestroy()
    {
        // Очищаем всплывающий интерфейс при уничтожении турели
        if (myUIInstance != null)
        {
            Destroy(myUIInstance);
        }
    }
}