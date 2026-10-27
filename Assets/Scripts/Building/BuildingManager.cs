
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("Placement Raycast")]
    [SerializeField] private Camera placementCamera;
    [SerializeField] private LayerMask placementLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField, Min(1f)] private float rayDistance = 500f;

    [Header("Obstacle Check")]
    [SerializeField, Min(0.1f)]
    private float obstacleCheckHeight = 4f;

    [SerializeField, Min(0f)]
    private float obstacleMargin = 0.03f;

    [Header("Preview Colors")]
    [SerializeField]
    private Color normalColor = new Color(0f, 1f, 0f, 0.6f);

    [SerializeField]
    private Color blockedColor = new Color(1f, 0f, 0f, 0.6f);

    [Header("Footprint Indicator")]
    [SerializeField, Min(0.01f)]
    private float footprintLineThickness = 0.04f;

    [SerializeField, Min(0.01f)]
    private float footprintLineHeight = 0.025f;

    [SerializeField, Min(0.001f)]
    private float footprintSurfaceOffset = 0.02f;

    private GameObject footprintIndicator;
    private Transform[] footprintEdges = new Transform[4];

    private TurretData currentSelectedTurret;
    private GameObject currentGhostInstance;
    private BuildPlatform currentPlatform;

    private Vector2Int currentStartCell;
    private Vector3 currentGhostPosition;

    private bool currentHasCandidate;
    private bool currentGhostIsValid;
    private string currentBlockReason;

    private float selectionTime;

    // Ключ — платформа, значение — занятые на ней клетки.
    private readonly Dictionary<BuildPlatform, HashSet<Vector2Int>>
        occupiedCellsByPlatform =
            new Dictionary<BuildPlatform, HashSet<Vector2Int>>();

    // Повторно используем буфер, чтобы не создавать массив
    // коллайдеров каждый кадр при проверке препятствий.
    private readonly Collider[] overlapBuffer = new Collider[32];

    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (currentSelectedTurret == null)
            return;

        if (Mouse.current == null)
            return;

        // ПКМ отменяет размещение даже над интерфейсом.
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelPlacement();
            return;
        }

        // Не разрешаем установку по клику на UI.
        if (IsPointerOverUI())
        {
            currentHasCandidate = false;
            currentGhostIsValid = false;

            if (currentGhostInstance != null)
                currentGhostInstance.SetActive(false);
            if (footprintIndicator != null)
                footprintIndicator.SetActive(false);

            return;
        }

        RefreshGhostPosition();

        // Не ставим башню в тот же момент,
        // когда игрок нажал кнопку выбора турели.
        if (Time.time - selectionTime < 0.2f)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryPlaceTurret();
    }

    public void SelectTurret(TurretData turretData)
    {
        if (turretData == null)
        {
            Debug.LogWarning(
                "[BuildingManager] TurretData не назначен."
            );
            return;
        }

        CancelPlacement();

        GameObject prefabToSpawn =
            turretData.previewPrefab != null
                ? turretData.previewPrefab
                : turretData.prefab;

        if (prefabToSpawn == null)
        {
            Debug.LogError(
                $"[BuildingManager] У турели '{turretData.turretName}' " +
                "не назначены Prefab и Preview Prefab."
            );
            return;
        }

        currentSelectedTurret = turretData;
        selectionTime = Time.time;

        currentGhostInstance = Instantiate(prefabToSpawn);

        // У призрака остаётся только визуальное представление.
        foreach (MonoBehaviour script in
                 currentGhostInstance.GetComponentsInChildren
                     <MonoBehaviour>(true))
        {
            script.enabled = false;
        }

        foreach (Collider col in
                 currentGhostInstance.GetComponentsInChildren
                     <Collider>(true))
        {
            col.enabled = false;
        }

        SetGhostColor(currentGhostInstance, normalColor);
        CreateFootprintIndicator();
        Debug.Log(
            $"[BuildingManager] Выбрана турель: " +
            $"{turretData.turretName}, " +
            $"размер: {turretData.footprintSize.x}x" +
            $"{turretData.footprintSize.y}"
        );
    }
    private void CreateFootprintIndicator()
    {
        // Если старый контур уже существует, удаляем его.
        if (footprintIndicator != null)
            Destroy(footprintIndicator);

        footprintIndicator = new GameObject("FootprintIndicator");
        footprintEdges = new Transform[4];

        string[] edgeNames =
        {
        "Edge_Left",
        "Edge_Right",
        "Edge_Front",
        "Edge_Back"
    };

        for (int i = 0; i < footprintEdges.Length; i++)
        {
            // Создаём тонкую полосу на основе куба.
            GameObject edge =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            edge.name = edgeNames[i];
            edge.transform.SetParent(
                footprintIndicator.transform,
                false
            );

            // Контур нужен только для изображения.
            // Удаляем коллайдер, чтобы он ничего не блокировал.
            Collider edgeCollider = edge.GetComponent<Collider>();

            if (edgeCollider != null)
                Destroy(edgeCollider);

            footprintEdges[i] = edge.transform;
        }

        // До попадания курсора на платформу контур скрыт.
        footprintIndicator.SetActive(false);
    }

    private void UpdateFootprintIndicator(
        BuildPlatform platform,
        Vector2Int footprint)
    {
        if (footprintIndicator == null || platform == null)
            return;

        float cellSize = platform.CellSize;

        float width = footprint.x * cellSize;
        float depth = footprint.y * cellSize;

        float lineThickness = Mathf.Min(
            footprintLineThickness,
            cellSize * 0.08f
        );

        float lineHeight = Mathf.Min(
            footprintLineHeight,
            cellSize * 0.05f
        );

        // Контур повторяет ориентацию платформы.
        footprintIndicator.transform.SetPositionAndRotation(
            currentGhostPosition +
            platform.GridOrigin.up * footprintSurfaceOffset,
            platform.GridOrigin.rotation
        );

        // Слева и справа: длинные полосы вдоль глубины.
        footprintEdges[0].localPosition =
            new Vector3(-width * 0.5f, 0f, 0f);

        footprintEdges[0].localScale =
            new Vector3(lineThickness, lineHeight,
                        depth + lineThickness);

        footprintEdges[1].localPosition =
            new Vector3(width * 0.5f, 0f, 0f);

        footprintEdges[1].localScale =
            new Vector3(lineThickness, lineHeight,
                        depth + lineThickness);

        // Спереди и сзади: полосы вдоль ширины.
        footprintEdges[2].localPosition =
            new Vector3(0f, 0f, -depth * 0.5f);

        footprintEdges[2].localScale =
            new Vector3(width + lineThickness, lineHeight,
                        lineThickness);

        footprintEdges[3].localPosition =
            new Vector3(0f, 0f, depth * 0.5f);

        footprintEdges[3].localScale =
            new Vector3(width + lineThickness, lineHeight,
                        lineThickness);

        footprintIndicator.SetActive(true);
    }
    private void RefreshGhostPosition()
    {
        currentHasCandidate = false;
        currentGhostIsValid = false;
        currentPlatform = null;

        if (currentGhostInstance == null ||
            currentSelectedTurret == null)
            return;

        Camera cam = placementCamera != null
            ? placementCamera
            : Camera.main;

        if (cam == null)
        {
            HideGhost();
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance,
                placementLayer,
                QueryTriggerInteraction.Ignore))
        {
            HideGhost();
            return;
        }

        BuildPlatform platform =
            hit.collider.GetComponentInParent<BuildPlatform>();

        if (platform == null || !platform.IsConfigured)
        {
            HideGhost();
            return;
        }

        Vector2Int footprint = currentSelectedTurret.footprintSize;
        footprint.x = Mathf.Max(1, footprint.x);
        footprint.y = Mathf.Max(1, footprint.y);

        // Переводим мировую точку в координаты конкретной платформы.
        Vector3 localHit =
            platform.GridOrigin.InverseTransformPoint(hit.point);

        float cellSize = platform.CellSize;

        // Для нечётных размеров центр окажется в центре клетки.
        // Для чётных — на пересечении линий сетки.
        int startX = Mathf.FloorToInt(
            localHit.x / cellSize - footprint.x * 0.5f + 0.5f
        );

        int startZ = Mathf.FloorToInt(
            localHit.z / cellSize - footprint.y * 0.5f + 0.5f
        );

        currentStartCell = new Vector2Int(startX, startZ);
        currentPlatform = platform;

        Vector3 localCenter = new Vector3(
            (startX + footprint.x * 0.5f) * cellSize,
            0f,
            (startZ + footprint.y * 0.5f) * cellSize
        );

        currentGhostPosition =
            platform.GridOrigin.TransformPoint(localCenter);

        currentGhostInstance.transform.position =
            currentGhostPosition;
        currentGhostInstance.transform.rotation =
    platform.GridOrigin.rotation;
        UpdateFootprintIndicator(platform, footprint);
        currentGhostInstance.SetActive(true);
        currentHasCandidate = true;

        currentGhostIsValid = CanPlaceFootprint(
            platform,
            currentStartCell,
            footprint,
            currentGhostPosition,
            out currentBlockReason
        );
        if (currentGhostIsValid)
        {
            ScrapResourceManager resources =
                ScrapResourceManager.Instance;

            if (resources == null)
            {
                currentGhostIsValid = false;
                currentBlockReason =
                    "Менеджер ресурсов отсутствует.";
            }
            else if (!resources.CanAfford(currentSelectedTurret.cost))
            {
                currentGhostIsValid = false;
                currentBlockReason =
                    "Недостаточно металлолома.";
            }
        }
        SetGhostColor(
            currentGhostInstance,
            currentGhostIsValid ? normalColor : blockedColor
        );
        SetGhostColor(
    footprintIndicator,
    currentGhostIsValid ? normalColor : blockedColor
);
    }

    private bool CanPlaceFootprint(
        BuildPlatform platform,
        Vector2Int startCell,
        Vector2Int footprint,
        Vector3 worldCenter,
        out string reason)
    {
        reason = string.Empty;

        // Проверяем всю площадь, а не только одну клетку.
        if (startCell.x < 0 ||
            startCell.y < 0 ||
            startCell.x + footprint.x > platform.Columns ||
            startCell.y + footprint.y > platform.Rows)
        {
            reason = "Вся турель должна находиться внутри платформы.";
            return false;
        }

        // Проверяем занятость каждой клетки.
        if (occupiedCellsByPlatform.TryGetValue(
                platform, out HashSet<Vector2Int> occupied))
        {
            for (int x = 0; x < footprint.x; x++)
            {
                for (int z = 0; z < footprint.y; z++)
                {
                    Vector2Int cell = new Vector2Int(
                        startCell.x + x,
                        startCell.y + z
                    );

                    if (occupied.Contains(cell))
                    {
                        reason = "Одна или несколько клеток заняты.";
                        return false;
                    }
                }
            }
        }

        // Проверяем препятствия только тогда,
        // когда в Inspector указан соответствующий слой.
        if (obstacleLayer.value != 0)
        {
            float cellSize = platform.CellSize;

            Vector3 halfExtents = new Vector3(
                Mathf.Max(
                    0.01f,
                    footprint.x * cellSize * 0.5f - obstacleMargin
                ),
                obstacleCheckHeight * 0.5f + 0.025f,
                Mathf.Max(
                    0.01f,
                    footprint.y * cellSize * 0.5f - obstacleMargin
                )
            );

            Vector3 boxCenter =
                worldCenter +
                platform.GridOrigin.up *
                (obstacleCheckHeight * 0.5f - 0.025f);

            int hitCount = Physics.OverlapBoxNonAlloc(
                boxCenter,
                halfExtents,
                overlapBuffer,
                platform.GridOrigin.rotation,
                obstacleLayer,
                QueryTriggerInteraction.Ignore
            );

            // Любой коллайдер в запрещённом слое блокирует установку.
            // При заполнении буфера также действуем безопасно.
            if (hitCount > 0)
            {
                reason = "На площади турели находится препятствие.";
                return false;
            }
        }

        return true;
    }

    private void TryPlaceTurret()
    {
        // Проверяем позицию заново непосредственно перед установкой.
        RefreshGhostPosition();

        if (!currentHasCandidate || !currentGhostIsValid)
        {
            Debug.Log(
                "[BuildingManager] Размещение невозможно. " +
                currentBlockReason
            );
            return;
        }

        TurretData selectedData = currentSelectedTurret;
        BuildPlatform platform = currentPlatform;
        Vector2Int startCell = currentStartCell;
        Vector2Int footprint = selectedData.footprintSize;
        ScrapResourceManager resources =
    ScrapResourceManager.Instance;

        if (resources == null)
        {
            Debug.LogError(
                "[BuildingManager] ScrapResourceManager не найден."
            );

            return;
        }

        if (!resources.CanAfford(selectedData.cost))
        {
            Debug.Log(
                "[BuildingManager] Недостаточно металлолома."
            );

            RefreshGhostPosition();
            return;
        }
        if (selectedData.prefab == null)
        {
            Debug.LogError(
                "[BuildingManager] Не назначен основной Prefab турели."
            );
            return;
        }

        GameObject placedTurret = Instantiate(
    selectedData.prefab,
    currentGhostPosition,
    platform.GridOrigin.rotation
);
        if (!resources.TrySpend(selectedData.cost))
        {
            Destroy(placedTurret);

            Debug.LogWarning(
                "[BuildingManager] Покупка не выполнена: " +
                "не удалось списать стоимость турели."
            );

            RefreshGhostPosition();
            return;
        }
        Turret turretScript =
            placedTurret.GetComponentInChildren<Turret>();

        if (turretScript != null)
            turretScript.turretData = selectedData;

        Vector2Int[] cells = new Vector2Int[
            footprint.x * footprint.y
        ];

        int index = 0;

        if (!occupiedCellsByPlatform.TryGetValue(
                platform, out HashSet<Vector2Int> occupied))
        {
            occupied = new HashSet<Vector2Int>();
            occupiedCellsByPlatform.Add(platform, occupied);
        }

        for (int x = 0; x < footprint.x; x++)
        {
            for (int z = 0; z < footprint.y; z++)
            {
                Vector2Int cell = new Vector2Int(
                    startCell.x + x,
                    startCell.y + z
                );

                cells[index++] = cell;
                occupied.Add(cell);
            }
        }

        PlacedTurretFootprint tracker =
            placedTurret.GetComponent<PlacedTurretFootprint>();

        if (tracker == null)
        {
            tracker = placedTurret.AddComponent
                <PlacedTurretFootprint>();
        }

        tracker.Initialize(this, platform, cells);

        Debug.Log(
            $"[BuildingManager] Турель '{selectedData.turretName}' " +
            $"установлена. Занято клеток: {cells.Length}."
        );

        CancelPlacement();
    }

    public void ReleaseCells(
        BuildPlatform platform,
        Vector2Int[] cells)
    {
        if (platform == null || cells == null)
            return;

        if (!occupiedCellsByPlatform.TryGetValue(
                platform, out HashSet<Vector2Int> occupied))
            return;

        foreach (Vector2Int cell in cells)
            occupied.Remove(cell);

        if (occupied.Count == 0)
            occupiedCellsByPlatform.Remove(platform);
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }

    private void HideGhost()
    {
        currentHasCandidate = false;
        currentGhostIsValid = false;
        currentPlatform = null;

        if (currentGhostInstance != null)
            currentGhostInstance.SetActive(false);
        if (footprintIndicator != null)
            footprintIndicator.SetActive(false);
    }

    private void SetGhostColor(GameObject ghost, Color color)
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        Renderer[] renderers =
            ghost.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer rend in renderers)
        {
            rend.GetPropertyBlock(propertyBlock);

            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);

            rend.SetPropertyBlock(propertyBlock);
        }
    }

    public void CancelPlacement()
    {
        if (currentGhostInstance != null)
            Destroy(currentGhostInstance);
        if (footprintIndicator != null)
            Destroy(footprintIndicator);

        footprintIndicator = null;
        footprintEdges = new Transform[4];
        currentGhostInstance = null;
        currentSelectedTurret = null;
        currentPlatform = null;
        currentHasCandidate = false;
        currentGhostIsValid = false;
        currentBlockReason = string.Empty;
    }
}
