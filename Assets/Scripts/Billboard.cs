using UnityEngine;

public class Billboard : MonoBehaviour
{
    public float baseScale = 0.01f; // базовый масштаб Canvas в World Space
    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        // наведение на камеру
        transform.LookAt(transform.position + cam.forward);

        // расчет расстояния между камерой и интерфейсом турели
        float distance = Vector3.Distance(transform.position, cam.position);

        // динамическое масштабирование: чем дальше камера, тем больше локальный масштаб
        transform.localScale = Vector3.one * (distance * baseScale);
    }
}