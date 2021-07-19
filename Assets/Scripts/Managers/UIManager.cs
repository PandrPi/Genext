using System;
using General;
using Unity.Mathematics;
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

        [Header("Statistics"), SerializeField] private Text averageSpeedText;
        [SerializeField] private Text averageSizeText;
        [SerializeField] private Text averageEnergyText;
        [SerializeField] private Text averageEnergyToRPText;
        [SerializeField] private Text averageEnergyPBText;
        [SerializeField] private Text averageDieChanceText;
        [SerializeField] private Text averageViewRadiusText;

        [Header("Simulation time"), SerializeField]
        private Text simulationTimeText;

        private const string PauseSimulationText = "S";
        private const string PlaySimulationText = "P";
        private const string SimulationTimeFormat = "Food Instance: {0} ms; Creature Instance: {1} ms";
        private static readonly Vector2 PlaySimulationTextOffset = new Vector2(5, 0);
        private static readonly Vector2 PauseSimulationTextOffset = new Vector2(0.8f, 0);

        private void Awake()
        {
            Instance = this;

            timeFactorSlider.onValueChanged.AddListener(SetTimeFactor);
            pauseOrPlayButton.onClick.AddListener(PauseOrPlaySimulation);
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

        public void SetSimulationManagersTime(float foodManagerTime, float creatureManagerTime)
        {
            const float conversionMultiplier = 1000.0f;
            const int roundToDigits = 3;
            // Multiply numbers by 1000f to convert them from seconds to ms and round them to only 3 digits
            foodManagerTime = (float) Math.Round(foodManagerTime * conversionMultiplier, roundToDigits);
            creatureManagerTime = (float) Math.Round(creatureManagerTime * conversionMultiplier, roundToDigits);

            simulationTimeText.text = string.Format(SimulationTimeFormat, foodManagerTime, creatureManagerTime);
        }
    }
}