using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DriveUI : ModuleUI
{
	private Drive drive;

	public GameObject SpeedDistanceLayout;
	public InputField DistanceInputField;
	public Text DistanceUnitText;
	public InputField SpeedInputField;
	public Text SpeedUnitText;
	public Button GoButton;

	private InputField distanceInputField;
	private InputField speedInputField;
	private Button goButton;

	protected override void Awake()
	{
		base.Awake();

		GameObject speedDistanceLayout = SafeInstantiateGameObject(SpeedDistanceLayout, transform);
		distanceInputField = SafeInstantiate<InputField> (DistanceInputField, speedDistanceLayout.transform);
		SafeInstantiateText (DistanceUnitText, speedDistanceLayout.transform, "cm");
		speedInputField = SafeInstantiate<InputField> (SpeedInputField, speedDistanceLayout.transform);
		SafeInstantiateText (SpeedUnitText, speedDistanceLayout.transform, "cm/s");
		goButton = SafeInstantiate<Button> (GoButton, transform);
		goButton.onClick.AddListener (OnGoButtonClicked);
	}

	public void OnGoButtonClicked()
	{
		float distance_cm=float.Parse (distanceInputField.text);
		float speed_cm_per_s=float.Parse (speedInputField.text);
		drive.DriveAhead (distance_cm, speed_cm_per_s);
	}

	protected override void Start ()
	{
		base.Start();
		drive = module as Drive;
	}

	protected override void Update ()
	{
		base.Update();
	}
}
