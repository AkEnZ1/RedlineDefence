
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class CameraPanController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera controlledCamera;

    [Header("Middle Mouse Drag")]
    [SerializeField, Min(0.001f)]
    private float dragSpeed = 0.015f;

    [Header("World Movement Limits")]
    [SerializeField]
    private Vector2 xBounds = new Vector2(-15f, 15f);

    [SerializeField]
    private Vector2 zBounds = new Vector2(-15f, 15f);

    [Header("Perspective Zoom")]
    [SerializeField, Min(0.01f)]
    private float perspectiveZoomSpeed = 2f;

    [SerializeField, Min(0.1f)]
    private float minPerspectiveDistance = 5f;

    [SerializeField, Min(0.1f)]
    private float maxPerspectiveDistance = 40f;

    [SerializeField]
    private float zoomPlaneY = 0f;

    [Header("Orthographic Zoom")]
    [SerializeField, Min(0.01f)]
    private float orthographicZoomSpeed = 1.5f;

    [SerializeField, Min(0.1f)]
    private float minOrthographicSize = 3f;

    [SerializeField, Min(0.1f)]
    private float maxOrthographicSize = 15f;

    private bool isDragging;
    private Vector2 previousMousePosition;

    private void Awake()
    {
        if (controlledCamera == null)
            controlledCamera = GetComponent<Camera>();

        if (controlledCamera == null)
            controlledCamera = Camera.main;

        if (controlledCamera == null)
        {
            Debug.LogError(
                "[CameraPanController] Камера не назначена."
            );

            enabled = false;
        }

        // Защищаем настройки от случайного перепутывания границ.
        if (minPerspectiveDistance > maxPerspectiveDistance)
        {
            float temp = minPerspectiveDistance;
            minPerspectiveDistance = maxPerspectiveDistance;
            maxPerspectiveDistance = temp;
        }

        if (minOrthographicSize > maxOrthographicSize)
        {
            float temp = minOrthographicSize;
            minOrthographicSize = maxOrthographicSize;
            maxOrthographicSize = temp;
        }
    }

    private void Update()
    {
        if (Mouse.current == null || controlledCamera == null)
            return;

        HandleZoom();

        // Начало перетаскивания средней кнопкой.
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            if (!IsPointerOverUI())
            {
                isDragging = true;
                previousMousePosition =
                    Mouse.current.position.ReadValue();
            }
        }

        if (!isDragging)
            return;

        if (Mouse.current.middleButton.wasReleasedThisFrame ||
            !Mouse.current.middleButton.isPressed)
        {
            isDragging = false;
            return;
        }

        Vector2 currentMousePosition =
            Mouse.current.position.ReadValue();

        Vector2 mouseDelta =
            currentMousePosition - previousMousePosition;

        previousMousePosition = currentMousePosition;

        MoveCamera(mouseDelta);
    }

    private void HandleZoom()
    {
        float rawScroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(rawScroll) < 0.01f)
            return;

        // Не зумим игровое поле, когда игрок прокручивает UI.
        if (IsPointerOverUI())
            return;

        // На разных устройствах Input System может возвращать
        // величины прокрутки разного масштаба.
        float scrollUnits = Mathf.Abs(rawScroll) > 10f
            ? rawScroll / 120f
            : Mathf.Clamp(rawScroll, -1f, 1f);

        if (controlledCamera.orthographic)
        {
            // Положительная прокрутка уменьшает область обзора.
            float newSize =
                controlledCamera.orthographicSize -
                scrollUnits * orthographicZoomSpeed;

            controlledCamera.orthographicSize = Mathf.Clamp(
                newSize,
                minOrthographicSize,
                maxOrthographicSize
            );

            return;
        }

        // Для перспективной камеры приближаемся к точке,
        // где центральный луч камеры пересекает плоскость карты.
        Ray centerRay = controlledCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        Plane groundPlane = new Plane(
            Vector3.up,
            new Vector3(0f, zoomPlaneY, 0f)
        );

        if (!groundPlane.Raycast(centerRay, out float distance) ||
            distance <= 0f)
        {
            return;
        }

        Vector3 zoomPivot = centerRay.GetPoint(distance);

        Vector3 offset =
            controlledCamera.transform.position - zoomPivot;

        float currentDistance = offset.magnitude;

        if (currentDistance < 0.001f)
            return;

        // Колесо вверх приближает, вниз отдаляет.
        float newDistance = Mathf.Clamp(
            currentDistance - scrollUnits * perspectiveZoomSpeed,
            minPerspectiveDistance,
            maxPerspectiveDistance
        );

        Vector3 newPosition =
            zoomPivot + offset.normalized * newDistance;

        controlledCamera.transform.position =
            ClampToBounds(newPosition);
    }

    private void MoveCamera(Vector2 mouseDelta)
    {
        float yaw = controlledCamera.transform.eulerAngles.y;

        Vector3 right =
            Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        Vector3 forward =
            Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

        // Перетаскиваем карту, а не сам указатель.
        Vector3 movement =
            (-right * mouseDelta.x -
             forward * mouseDelta.y) * dragSpeed;

        controlledCamera.transform.position = ClampToBounds(
            controlledCamera.transform.position + movement
        );
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        position.x = Mathf.Clamp(
            position.x, xBounds.x, xBounds.y
        );

        position.z = Mathf.Clamp(
            position.z, zBounds.x, zBounds.y
        );

        return position;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }

    private void OnDisable()
    {
        isDragging = false;
    }
}
