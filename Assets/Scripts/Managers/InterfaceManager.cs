using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InterfaceManager : MonoBehaviour
{
	public static InterfaceManager manager;
	[SerializeField] private Slider timeFactorSlider;
	[SerializeField] private Text timeFactorText;
	[SerializeField] private Text populationNumberText;
	[Header("Statistics")]
	[SerializeField] private Text averageSpeedText;
	[SerializeField] private Text averageSizeText;
	[SerializeField] private Text averageEnergyText;
	[SerializeField] private Text averageEnergyToRPText;
	[SerializeField] private Text averageEnergyPBText;
	[SerializeField] private Text averageDieChanceText;
	[SerializeField] private Text averageViewRadiusText;

	private void Awake()
	{
		manager = this;

		timeFactorSlider.onValueChanged.AddListener((value) =>
		{
			Time.timeScale = value;
			timeFactorText.text = $"Time factor - {value}x";
		});

	}

	public void SetPopulationNumberText(int number)
	{
		populationNumberText.text = number.ToString();
	}
}
