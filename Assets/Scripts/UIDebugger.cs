using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class UIDebugger : MonoBehaviour
{
    void Update()
    {
        if (EventSystem.current == null || Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = Mouse.current.position.ReadValue();

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);

            if (results.Count > 0)
            {
                Debug.Log("перехват клика объектом: " + results[0].gameObject.name);
            }
            else
            {
                Debug.Log("луч не зафиксировал UI");
            }
        }
    }
}