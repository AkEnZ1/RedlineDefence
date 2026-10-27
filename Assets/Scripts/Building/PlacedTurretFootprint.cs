
using UnityEngine;

public sealed class PlacedTurretFootprint : MonoBehaviour
{
    private BuildingManager owner;
    private BuildPlatform platform;
    private Vector2Int[] occupiedCells;
    private bool initialized;

    public void Initialize(
        BuildingManager buildingManager,
        BuildPlatform buildPlatform,
        Vector2Int[] cells)
    {
        owner = buildingManager;
        platform = buildPlatform;
        occupiedCells = cells;
        initialized = true;
    }

    private void OnDestroy()
    {
        if (!initialized || owner == null)
            return;

        owner.ReleaseCells(platform, occupiedCells);
    }
}
