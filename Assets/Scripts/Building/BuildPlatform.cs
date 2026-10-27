
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BuildPlatform : MonoBehaviour
{
    [Header("Grid Origin")]
    [SerializeField] private Transform gridOrigin;

    [Header("Grid Dimensions")]
    [SerializeField, Min(1)] private int columns = 10;
    [SerializeField, Min(1)] private int rows = 10;
    [SerializeField, Min(0.01f)] private float cellSize = 1f;

    public Transform GridOrigin => gridOrigin;
    public int Columns => columns;
    public int Rows => rows;
    public float CellSize => cellSize;

    public bool IsConfigured =>
        gridOrigin != null &&
        columns > 0 &&
        rows > 0 &&
        cellSize > 0f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        cellSize = Mathf.Max(0.01f, cellSize);
    }
#endif
}
