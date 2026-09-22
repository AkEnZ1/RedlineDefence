using UnityEngine;
using UnityEngine.UI;

public class OverlayUI : MonoBehaviour
{
    void Start()
    {
        // Находим все графические компоненты интерфейса (картинки, текст)
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

        foreach (Graphic g in graphics)
        {
            if (g.material != null)
            {
                // Создаем копию текущего UI материала
                Material overrideMaterial = new Material(g.material);

                // Переключаем ZTest в состояние "Always" (всегда рисовать поверх)
                overrideMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

                // Отключаем запись в буфер глубины для прозрачных объектов
                overrideMaterial.SetInt("_ZWrite", 0);

                // Применяем измененный материал к элементу
                g.material = overrideMaterial;
            }
        }
    }
}