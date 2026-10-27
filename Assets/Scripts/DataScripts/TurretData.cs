
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewTurretData",
    menuName = "RedlineDefense/Turret Data"
)]
public class TurretData : ScriptableObject
{
    [Header("Info")]
    public string turretName = "Турель";
    public Sprite icon;
    [Min(0)] public int cost = 100;

    [Header("Placement")]
    [Min(1)]
    public Vector2Int footprintSize = Vector2Int.one;

    [Header("Prefabs")]
    public GameObject prefab;
    public GameObject previewPrefab;

#if UNITY_EDITOR
    private void OnValidate()
    {
        footprintSize.x = Mathf.Max(1, footprintSize.x);
        footprintSize.y = Mathf.Max(1, footprintSize.y);
        cost = Mathf.Max(0, cost);
    }
#endif
}
