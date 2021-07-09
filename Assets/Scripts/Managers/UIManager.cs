using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	public static UIManager Manager;
	[SerializeField] private Slider timeFactorSlider;
	[SerializeField] private Text timeFactorText;
	[SerializeField] private Text populationNumberText;
	[SerializeField] private Button pauseOrPlayButton;
	[SerializeField] private Text pauseOrPlayButtonText;

	[Header("Statistics")]
	[SerializeField] private Text averageSpeedText;
	[SerializeField] private Text averageSizeText;
	[SerializeField] private Text averageEnergyText;
	[SerializeField] private Text averageEnergyToRPText;
	[SerializeField] private Text averageEnergyPBText;
	[SerializeField] private Text averageDieChanceText;
	[SerializeField] private Text averageViewRadiusText;

	private const string PauseSimulationText = "S";
	private const string PlaySimulationText = "P";
	private static readonly Vector2 PlaySimulationTextOffset = new Vector2(5, 0);
	private static readonly Vector2 PauseSimulationTextOffset = new Vector2(0.8f, 0);
	
	private void Awake()
	{
		Manager = this;

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

	private void SetPauseOrPlayButtonText(float timeFactor)
	{
		// If time factor is not equal to zero the simulation is playing
		bool isSimulationPlaying = timeFactorSlider.value != 0.0f;
		// Set button text and offset depending on isSimulationPlaying boolean
		pauseOrPlayButtonText.text = isSimulationPlaying ? PlaySimulationText : PauseSimulationText;
		Vector2 anchoredPosition = isSimulationPlaying ? PlaySimulationTextOffset : PauseSimulationTextOffset;
		pauseOrPlayButtonText.rectTransform.anchoredPosition = anchoredPosition;
	}
	
	/// <summary>
	/// Sets the disired time factor for settings and UI
	/// </summary>
	/// <param name="value"></param>
	private void SetTimeFactor(float value)
	{
		Time.timeScale = math.max(1.0f, value);
		World.Instance.SetIsSimulationPlaying(value != 0.0f);
		SetPauseOrPlayButtonText(value);

		timeFactorText.text = $"Time factor - {value}x";
	}
	
	/// <summary>
	/// Updates the population UI text with the specified number
	/// </summary>
	/// <param name="number">Population number</param>
	public void SetPopulationNumberText(int number)
	{
		populationNumberText.text = number.ToString();
	}
}
