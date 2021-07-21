using Creatures;
using Foods;
using Managers;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace General
{
    [System.Serializable]
    public class DefaultCreatureParameters
    {
        [SerializeField] public float movementSpeed = 1.0f;
        [SerializeField] public float size = 1.0f;
        [SerializeField] public float energy = 2500.0f;
        [SerializeField] public float energyToReproduce = 3000.0f;
        [SerializeField] public float dieChance = 0.2f;
        [SerializeField] public float viewRadius = 3.0f;
    }

    public class World : MonoBehaviour
    {
        public static World Instance;
        public Vector2 worldSize = new Vector2(128, 64);

        [Header("Food Parameters")] [SerializeField, Range(1, 16384)]
        private int foodsNumber;

        [SerializeField] private Mesh foodMesh;
        [SerializeField] private Material foodMaterial;

        [Header("Creature Parameters")] [SerializeField, Range(1, 16384)]
        private int creaturesNumber;

        [SerializeField] private Mesh creatureMesh;
        [SerializeField] private Material creatureMaterial;
        [SerializeField] public DefaultCreatureParameters defaultCreatureParameters;
        [SerializeField, Range(5, 100)] public int quadrantCellSize = 5;

        private Camera mainCamera;
        private Transform myTransform;
        private Transform worldArea;
        private Material worldAreaMaterial;
        private int simulationTimeFactor = 1;

        public static float4 WorldAreaRect;
        private const string WorldAreaTransformName = "World Area";
        private const string CameraSizeShaderVariableName = "_CameraSize";
        private static readonly int CameraSize = Shader.PropertyToID(CameraSizeShaderVariableName);

        private void Awake()
        {
            Instance = this;
            mainCamera = Camera.main;
            myTransform = transform;
        }

        private void Start()
        {
            worldArea = myTransform.Find(WorldAreaTransformName);
            worldAreaMaterial = worldArea.GetComponent<Renderer>().sharedMaterial;

            worldArea.localScale = new Vector3(worldSize.x, worldSize.y, 1.0f);
            WorldAreaRect = new float4(-worldSize.x, -worldSize.y, worldSize.x, worldSize.y) * 0.5f;
            // worldAreaMaterial.mainTextureScale = new Vector2(worldSize.x, worldSize.y);

            FoodManagementSystem.Instance.Initialize(foodsNumber, worldSize, foodMesh, foodMaterial, quadrantCellSize);
            CreatureManagementSystem.Instance.Initialize(creaturesNumber, creatureMesh, creatureMaterial);

            var creatureEntity = CreatureManagementSystem.Instance.GetCreature();
            CreatureManagementSystem.Instance.InitializeCreature(creatureEntity);
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            float foodManagerTime = 0;
            float creatureManagerTime = 0;
            var totalTimeStart = Time.realtimeSinceStartup;

            // Process our simulation simulationTimeFactor times! Such a way of game speed up is much faster than
            // changing Time.timeScale property but gives us almost the same result
            for (var i = 0; i < simulationTimeFactor; i++)
            {
                var start1 = Time.realtimeSinceStartup;
                FoodManagementSystem.Instance.CustomUpdate(deltaTime);
                foodManagerTime += Time.realtimeSinceStartup - start1;

                start1 = Time.realtimeSinceStartup;
                CreatureManagementSystem.Instance.CustomUpdate(deltaTime);
                creatureManagerTime += Time.realtimeSinceStartup - start1;
            }

            var totalSimulationTime = Time.realtimeSinceStartup - totalTimeStart;
            UIManager.Instance.SetSimulationExecutionTimeForUI(foodManagerTime, creatureManagerTime,
                totalSimulationTime);

            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current.IsPointerOverGameObject() == false)
                {
                    DisplayClickedFoodOrCreature();
                }
            }

            // Send orthographicSize to the shader of our worldAreaMaterial
            worldAreaMaterial.SetFloat(CameraSize, mainCamera.orthographicSize);
        }

        private void OnDestroy()
        {
            const float defaultCameraSize = 5.0f;
            worldAreaMaterial.SetFloat(CameraSize, defaultCameraSize);
        }

        /// <summary>
        /// This method is looking for the closest object (creature of food) and if it is found the UIManager displays
        /// the statistics of the closest object
        /// </summary>
        private void DisplayClickedFoodOrCreature()
        {
            const float closestDistance = 0.5f;
            var cellSize = quadrantCellSize;
            var foodMultiHashMap = FoodManagementSystem.FoodQuadrantMultiHashMap;
            var creatureMultiHashMap = CreatureManagementSystem.CreatureQuadrantMultiHashMap;

            var closestFoodDistance = closestDistance;
            var closestCreatureDistance = closestDistance;
            FoodTracker closestFood = default;
            CreatureComponent closestCreature = default;

            var mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var clickPosition = new float2(mousePosition.x, mousePosition.y);

            // Check the quadrant where the cursor is placed and all its neighbours to find the closest food and
            // the closest creature
            for (var x = mousePosition.x - cellSize; x <= mousePosition.x + cellSize; x += cellSize)
            {
                for (var y = mousePosition.y - cellSize; y <= mousePosition.y + cellSize; y += cellSize)
                {
                    var quadrantPosition = new float2(x, y);
                    FindClosestObject(clickPosition, quadrantPosition, ref closestFoodDistance, ref closestFood,
                        foodMultiHashMap);
                    FindClosestObject(clickPosition, quadrantPosition, ref closestCreatureDistance,
                        ref closestCreature, creatureMultiHashMap);
                }
            }

            // We have to display the closest object. Initially closestCreatureDistance and closestFoodDistance equals
            // to the value of closestDistance variable. So if both distances are the same then the else branch will
            // be executed (for example, 1.0 is never smaller than 1.0). However if the food object is closer than the
            // creature object the else branch will be executed too, so we have to check whether the closestFood.ID
            // equals to zero and if it is true we just close all statistics windows because we have not found any
            // object which is close enough to the cursor position. Inside the else branch if closestFood.ID is not
            // equal to zero we have to display the closestFood stats. In the case if distance to the creature is
            // smaller than distance to the food we display the closestCreature stats.

            if (closestCreatureDistance < closestFoodDistance)
            {
                UIManager.Instance.DisplayCreatureStats(closestCreature);
            }
            else
            {
                if (closestFood.ID != 0)
                {
                    UIManager.Instance.DisplayFoodStats(closestFood);
                }
                else
                {
                    UIManager.Instance.HideStatisticsWindows();
                }
            }
        }

        /// <summary>
        /// This method is looking for the closest instance of type T which is placed nearby the cursor position.
        /// </summary>
        /// <param name="clickPosition">The cursor position in world space</param>
        /// <param name="quadrantPosition">Position of the quadrant where the closest object will be searched</param>
        /// <param name="closestDistance">Current closest distance</param>
        /// <param name="closestUnitData">Current closest object</param>
        /// <param name="multiHashMap">A NativeMultiHashMap which will be used</param>
        /// <typeparam name="T">An struct which implements methods of the IUnitDataWithPosition interface</typeparam>
        private void FindClosestObject<T>(float2 clickPosition, float2 quadrantPosition, ref float closestDistance,
            ref T closestUnitData, NativeMultiHashMap<int, T> multiHashMap) where T : struct, IUnitDataWithPosition
        {
            var hashKey = GetHashKeyByPoint(quadrantPosition);
            if (multiHashMap.TryGetFirstValue(hashKey, out var unitData, out var iterator))
            {
                do
                {
                    var currentDistance = math.distance(unitData.GetPosition(), clickPosition);
                    if (currentDistance < closestDistance)
                    {
                        closestDistance = currentDistance;
                        closestUnitData = unitData;
                    }
                } while (multiHashMap.TryGetNextValue(out unitData, ref iterator));
            }
        }

        /// <summary>
        /// Returns the hash number calculated for the specified point
        /// </summary>
        private int GetHashKeyByPoint(float2 point)
        {
            var cellSize = quadrantCellSize;
            const int cellYMultiplier = FoodProcessingJob.CellYMultiplier;
            return (int) (math.floor(point.x / cellSize) + (cellYMultiplier * math.floor(point.y / cellSize)));
        }

        /// <summary>
        /// Returns a new Unity.Mathematics.Random instance with a random seed
        /// </summary>
        public static Unity.Mathematics.Random GetRandom()
        {
            const int randomRangeMin = 1;
            const int randomRangeMax = 2000000000;
            return new Unity.Mathematics.Random((uint) Random.Range(randomRangeMin, randomRangeMax));
        }

        /// <summary>
        /// Sets a new time factor for the simulation
        /// </summary>
        /// <param name="timeFactor">New time factor</param>
        public void SetSimulationTimeFactor(int timeFactor)
        {
            simulationTimeFactor = timeFactor;
        }
    }
}