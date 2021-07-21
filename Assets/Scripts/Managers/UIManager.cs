using System;
using Creatures;
using Foods;
using General;
using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;
        [SerializeField] private Slider timeFactorSlider;
        [SerializeField] private Text timeFactorText;
        [SerializeField] private Text populationNumberText;
        [SerializeField] private Button pauseOrPlayButton;
        [SerializeField] private Text pauseOrPlayButtonText;

        // The "St" addition in variable names means that these variable are used to display statistics
        [Header("Food Statistics"), SerializeField]
        private GameObject foodStatsWindow;

        [SerializeField] private Button foodStatsWindowCloseButtonSt;
        [SerializeField] private Text foodIDTextSt;
        [SerializeField] private Text foodEnergyTextSt;

        [Header("Creature Statistics"), SerializeField]
        private GameObject creatureStatsWindow;

        [SerializeField] private Button creatureStatsWindowCloseButtonSt;
        [SerializeField] private Text creatureIDTextSt;
        [SerializeField] private Text creatureMovementSpeedTextSt;
        [SerializeField] private Text creatureSizeTextSt;
        [SerializeField] private Text creatureEnergyTextSt;
        [SerializeField] private Text creatureEnergyAmountForReproductionTextSt;
        [SerializeField] private Text creatureDieChanceTextSt;

        [Header("Simulation time"), SerializeField]
        private Text simulationTimeText;

        private const string PauseSimulationText = "S";
        private const string PlaySimulationText = "P";
        private const string SimulationTimeFormat = "Food Manager: {0} ms; Creature Manager: {1} ms; Total: {2} ms";
        private static readonly Vector2 PlaySimulationTextOffset = new Vector2(5, 0);
        private static readonly Vector2 PauseSimulationTextOffset = new Vector2(0.8f, 0);

        private void Awake()
        {
            Instance = this;

            timeFactorSlider.onValueChanged.AddListener(SetTimeFactor);
            pauseOrPlayButton.onClick.AddListener(PauseOrPlaySimulation);
            foodStatsWindowCloseButtonSt.onClick.AddListener(() => foodStatsWindow.SetActive(false));
            creatureStatsWindowCloseButtonSt.onClick.AddListener(() => creatureStatsWindow.SetActive(false));
        }

        /// <summary>
        /// Starts or stops the simulation
        /// </summary>
        private void PauseOrPlaySimulation()
        {
            // If time factor is not equal to zero the simulation is playing
            bool isSimulationPlaying = timeFactorSlider.value != 0.0f;
            // If simulation is playing we need to pause it and vise versa
            SetTimeFactor(isSimulationPlaying ? 0.0f : 1.0f);
            timeFactorSlider.value = isSimulationPlaying ? 0.0f : 1.0f;
        }

        /// <summary>
        /// Updates the PauseOrPlay button text depending on the actual simulation state
        /// </summary>
        private void SetPauseOrPlayButtonText()
        {
            // If time factor is not equal to zero the simulation is playing
            bool isSimulationPlaying = timeFactorSlider.value != 0.0f;
            // Set button text and offset depending on isSimulationPlaying boolean
            pauseOrPlayButtonText.text = isSimulationPlaying ? PlaySimulationText : PauseSimulationText;
            Vector2 anchoredPosition = isSimulationPlaying ? PlaySimulationTextOffset : PauseSimulationTextOffset;
            pauseOrPlayButtonText.rectTransform.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// Sets the specified value as the actual time factor of simulation and UI
        /// </summary>
        private void SetTimeFactor(float value)
        {
            World.Instance.SetSimulationTimeFactor((int) value);
            SetPauseOrPlayButtonText();

            timeFactorText.text = $"Time factor - {value}x";
        }

        /// <summary>
        /// Updates the population UI text with the specified populationNumber
        /// </summary>
        public void SetPopulationNumberText(int populationNumber)
        {
            populationNumberText.text = populationNumber.ToString();
        }

        /// <summary>
        /// Updates the text object that stores an information about execution time of food and creature managers
        /// </summary>
        /// <param name="foodManagerTime">Total execution time of the food manager</param>
        /// <param name="creatureManagerTime">Total execution time of the creature manager</param>
        /// <param name="totalSimulationTime">Total execution time of all managers</param>
        public void SetSimulationExecutionTimeForUI(float foodManagerTime, float creatureManagerTime,
            float totalSimulationTime)
        {
            const float conversionMultiplier = 1000.0f;
            const int roundToDigits = 3;
            // Multiply numbers by 1000f to convert them from seconds to ms and round them to only 3 digits
            foodManagerTime = (float) Math.Round(foodManagerTime * conversionMultiplier, roundToDigits);
            creatureManagerTime = (float) Math.Round(creatureManagerTime * conversionMultiplier, roundToDigits);
            totalSimulationTime = (float) Math.Round(totalSimulationTime * conversionMultiplier, roundToDigits);

            simulationTimeText.text = string.Format(SimulationTimeFormat, foodManagerTime, creatureManagerTime,
                totalSimulationTime);
        }

        /// <summary>
        /// Hides all the statistics windows
        /// </summary>
        public void HideStatisticsWindows()
        {
            foodStatsWindow.SetActive(false);
            creatureStatsWindow.SetActive(false);
        }

        /// <summary>
        /// Display statistic of the specified creature 
        /// </summary>
        public void DisplayCreatureStats(CreatureComponent creature)
        {
            HideStatisticsWindows();

            const string twoDigitsFormat = "0.00";

            creatureIDTextSt.text = creature.ID.ToString();
            creatureMovementSpeedTextSt.text = creature.MovementSpeed.ToString(twoDigitsFormat);
            creatureSizeTextSt.text = creature.Size.ToString(twoDigitsFormat);
            creatureEnergyTextSt.text = ((int) creature.Energy).ToString();
            creatureEnergyAmountForReproductionTextSt.text = ((int) creature.EnergyAmountForReproduction).ToString();
            creatureDieChanceTextSt.text = creature.DieChance.ToString(twoDigitsFormat);

            creatureStatsWindow.SetActive(true);
        }

        /// <summary>
        /// Display statistics of the specified food
        /// </summary>
        public void DisplayFoodStats(FoodTracker food)
        {
            HideStatisticsWindows();

            foodIDTextSt.text = food.ID.ToString();
            foodEnergyTextSt.text = ((int) food.Energy).ToString();

            foodStatsWindow.SetActive(true);
        }
    }
}