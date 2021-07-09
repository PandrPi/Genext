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
    private bool isSimulationPlaying = true;

    // edge area is area where a creature should check whether they are trying to move outside the world and
    // if it's true we should change or reflect their movement direction
    private const string WorldEdgeTag = "WorldEdge";
    private const int EdgeAreaWidth = 4;
    private static Rect _worldEdgeArea;

    private void Start()
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
        if (isSimulationPlaying == true)
        {
            creatureManager.UpdateManager();
        }
    }

    public void SetIsSimulationPlaying(bool value)
    {
        isSimulationPlaying = value;
    }

    /// <summary>
    /// Checks whether the specified point is inside the world edge area and returns the result. 
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <returns></returns>
    public static bool IsInsideEdgeArea(Vector2 position)
    {
        return _worldEdgeArea.Contains(position);
    }


    /// <summary>
    /// Initializes the world edge colliders
    /// </summary>
    private void CreateWorldEdgeColliders()
    {
        Vector2 temp = worldSize / 2; // stores half world size vector
        CreateUnitBoxColliderObject(new Vector2(0, temp.y + 0.5f), new Vector2(worldSize.x + 1, 1));
        CreateUnitBoxColliderObject(new Vector2(0, -temp.y - 0.5f), new Vector2(worldSize.x + 1, 1));
        CreateUnitBoxColliderObject(new Vector2(temp.x + 0.5f, 0), new Vector2(1, worldSize.y + 1));
        CreateUnitBoxColliderObject(new Vector2(-temp.x - 0.5f, 0), new Vector2(1, worldSize.y + 1));
    }

    /// <summary>
    /// Creates a single world edge collider at the specified position and with the specified size
    /// </summary>
    /// <param name="position"></param>
    /// <param name="size"></param>
    private void CreateUnitBoxColliderObject(Vector2 position, Vector2 size)
    {
        Transform trs = new GameObject("World Edge Collider").transform;
        trs.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;
        trs.tag = WorldEdgeTag;
        trs.position = position;
        trs.localScale = size;
        trs.parent = worldArea;
    }

    private void OnDestroy()
    {
        foodManager.Dispose();
        creatureManager.Dispose();
    }
}