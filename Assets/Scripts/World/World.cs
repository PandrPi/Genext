using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public static World Instance;
    public Vector2 worldSize = new Vector2(128, 64);

    [SerializeField, Range(1, 4096)] private int worldFoodCount;
    [SerializeField, Range(0, 1)] private float foodRandomOffset;
    [SerializeField] private FoodManager foodManager;
    [SerializeField] private CreatureManager creatureManager;

    private Transform myTransform;
    private Transform worldArea;

    // edge area is area where entity should check whether they are trying to move outside the world and
    // if it's true - reflect their movement direction
    public const string WorldEdgeTag = "WorldEdge";
    private const int EdgeAreaWidth = 4;
    private static Rect _worldEdgeArea;

    void Start()
    {
        Instance = this;
        myTransform = transform;
        worldArea = myTransform.Find("World Area");
        Vector3 scale = worldSize;
        scale.z = 1;
        worldArea.localScale = scale;
        CreateWorldEdgeColliders();

        Vector2 halfWorldSize = worldSize * 0.5f;
        Vector2 edgeWidthVector = Vector2.one * EdgeAreaWidth;
        _worldEdgeArea = new Rect(-halfWorldSize, worldSize);

        foodManager.Initialize(worldFoodCount, halfWorldSize, GameObject.Find("Free Food Container").transform);
        creatureManager.Initialize(GameObject.Find("Free Creatures Container").transform);

        Creature creature = creatureManager.GetCreature(null);
        creature.InitializeCreature();
    }

    private void FixedUpdate()
    {
        creatureManager.UpdateManager();
    }

    public static bool IsInsideEdgeArea(Vector2 position)
    {
        return _worldEdgeArea.Contains(position);
    }

    private void CreateWorldEdgeColliders()
    {
        Vector2 temp = worldSize / 2; // stores half world size vector
        CreateUnitBoxColliderObject(new Vector2(0, temp.y + 0.5f), new Vector2(worldSize.x + 1, 1));
        CreateUnitBoxColliderObject(new Vector2(0, -temp.y - 0.5f), new Vector2(worldSize.x + 1, 1));
        CreateUnitBoxColliderObject(new Vector2(temp.x + 0.5f, 0), new Vector2(1, worldSize.y + 1));
        CreateUnitBoxColliderObject(new Vector2(-temp.x - 0.5f, 0), new Vector2(1, worldSize.y + 1));
    }

    private void CreateUnitBoxColliderObject(Vector2 position, Vector2 scale)
    {
        Transform trs = new GameObject("World Edge Collider").transform;
        trs.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;
        trs.tag = WorldEdgeTag;
        trs.position = position;
        trs.localScale = scale;
        trs.parent = worldArea;
    }

    private void OnDestroy()
    {
        foodManager.Dispose();
        creatureManager.Dispose();
    }
}