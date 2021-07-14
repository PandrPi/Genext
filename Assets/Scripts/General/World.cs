using Creatures;
using Managers;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
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

        [Header("Food Parameters")] [SerializeField, Range(1, 4096)]
        private int foodsNumber;

        [SerializeField] private Mesh foodMesh;
        [SerializeField] private Material foodMaterial;

        [Header("Creature Parameters")] [SerializeField, Range(1, 4096)]
        private int creaturesNumber;

        [SerializeField] private Mesh creatureMesh;
        [SerializeField] private Material creatureMaterial;
        [SerializeField] public DefaultCreatureParameters defaultCreatureParameters;
        [SerializeField, Range(5, 100)] public int quadrantCellSize = 5;

        private Transform myTransform;
        private Transform worldArea;
        private Material worldAreaMaterial;
        private int simulationTimeFactor = 1;

        public static float4 WorldAreaRect;
        private const string WorldAreaTransformName = "World Area";

        private void Start()
        {
            Instance = this;
            myTransform = transform;
            worldArea = myTransform.Find(WorldAreaTransformName);
            worldAreaMaterial = worldArea.GetComponent<Renderer>().sharedMaterial;

            worldArea.localScale = new Vector3(worldSize.x, worldSize.y, 1.0f);
            WorldAreaRect = new float4(-worldSize.x, -worldSize.y, worldSize.x, worldSize.y) * 0.5f;
            worldAreaMaterial.mainTextureScale = new Vector2(worldSize.x, worldSize.y);

            FoodManagementSystem.Instance.Initialize(foodsNumber, worldSize, foodMesh, foodMaterial, quadrantCellSize);
            CreatureManagementSystem.Instance.Initialize(creaturesNumber, creatureMesh, creatureMaterial);

            var creatureEntity = CreatureManagementSystem.Instance.GetCreature();
            CreatureManagementSystem.Instance.InitializeCreature(creatureEntity);
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            float foodManagerTime = 0;
            float creatureManagerTime = 0;

            // Process our simulation simulationTimeFactor times! Such a way of game speed up is much faster than
            // changing Time.timeScale property but gives us almost (not sure, we need to test it) the same result
            for (int i = 0; i < simulationTimeFactor; i++)
            {
                float start1 = Time.realtimeSinceStartup;
                FoodManagementSystem.Instance.CustomUpdate(deltaTime);
                foodManagerTime += Time.realtimeSinceStartup - start1;

                start1 = Time.realtimeSinceStartup;
                CreatureManagementSystem.Instance.CustomUpdate(deltaTime);
                creatureManagerTime += Time.realtimeSinceStartup - start1;
            }
            
            UIManager.Instance.SetSimulationManagersTime(foodManagerTime, creatureManagerTime);
        }

        public static Unity.Mathematics.Random GetRandom()
        {
            return new Unity.Mathematics.Random((uint) Random.Range(1, 1000000));
        }
        
        public void SetSimulationTimeFactor(int timeFactor)
        {
            simulationTimeFactor = timeFactor;
        }
    }
}