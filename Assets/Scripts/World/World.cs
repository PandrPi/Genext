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
    
    private static Rect _worldEdgeArea;

    private void Start()
    {
        Instance = this;
        myTransform = transform;
        worldArea = myTransform.Find("World Area");
        worldArea.localScale = new Vector3(worldSize.x, worldSize.y, 1.0f);

        _worldEdgeArea = new Rect(-worldSize * 0.5f, worldSize);

        foodManager.Initialize(worldFoodCount, worldSize, GameObject.Find("Free Food Container").transform);
        creatureManager.Initialize(GameObject.Find("Free Creatures Container").transform);

        Creature creature = creatureManager.GetCreature(null);
        creature.InitializeCreature();
    }

    private void FixedUpdate()
    {
        if (isSimulationPlaying == true)
        {
            creatureManager.UpdateManager();
            foodManager.UpdateManager();
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

    private void OnDestroy()
    {
        foodManager.Dispose();
        creatureManager.Dispose();
    }
}