using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static bool isGameOver = false;

    void Start()
    {
        isGameOver = false;
        Time.timeScale = 1f;
    }

    public void TriggerGameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        Debug.Log("проигрыш зафиксирован, интерфейс заблокирован");
    }
}