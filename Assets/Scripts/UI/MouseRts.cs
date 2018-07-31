// A modified version of script from:
// http://www.andrejeworutzki.de/game-developement/unity-realtime-strategy-camera/

using UnityEngine;

namespace Ev3devMapping
{

public class MouseRts : MonoBehaviour
{
	public int LevelArea = 100;

	public int ScrollArea = 25;
	public int ScrollSpeed = 25;
	public int DragSpeed = 100;

	public int ZoomSpeed = 25;
	public float ZoomMin = 0.5f;
	public int ZoomMax = 100;

	public int PanSpeed = 50;
	public int PanAngleMin = 25;
	public int PanAngleMax = 80;

	public int RotationSpeed=100;

	private float yrotation;

	public void Start()
	{
		yrotation = transform.eulerAngles.y;
	}

	// Update is called once per frame
	void Update()
	{
		// Init camera translation for this frame.
		var translation = Vector3.zero;
		Camera camera = GetComponent<Camera>();
	
		// Zoom in or out
		var zoomDelta = Input.GetAxis("Mouse ScrollWheel")*ZoomSpeed*Time.deltaTime;
		if (zoomDelta!=0)
			translation -= Vector3.up * ZoomSpeed * zoomDelta;
		
		var pan = camera.transform.eulerAngles.x - zoomDelta * PanSpeed;
		pan = Mathf.Clamp(pan, PanAngleMin, PanAngleMax);

		//Rotate around Y axis
		if (Input.GetMouseButton(0)) // LMB
			yrotation -=  Input.GetAxis("Mouse X") * RotationSpeed * Time.deltaTime;

		// Start panning camera if zooming in close to the ground or if just zooming out.
//		Vector3 forward = GetComponent<Camera>().transform.forward;
//		Vector3 right = GetComponent<Camera> ().transform.right;
			
		camera.transform.eulerAngles = new Vector3(0, yrotation, 0);
		// Move camera with arrow keys
	//	translation += new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * Time.deltaTime * ScrollSpeed;

		// Move camera with mouse
		if (Input.GetMouseButton(1)) // RMB
		{
			// Hold button and drag camera around
			translation -= new Vector3(Input.GetAxis("Mouse X") * DragSpeed * Time.deltaTime, 0, 
				Input.GetAxis("Mouse Y") * DragSpeed * Time.deltaTime);
		}
		else
		{
			/*
			// Move camera if mouse pointer reaches screen borders
			if (Input.mousePosition.x < ScrollArea)
			{
				translation = right * -ScrollSpeed * Time.deltaTime;
			}

			if (Input.mousePosition.x >= Screen.width - ScrollArea)
			{
				translation += right * ScrollSpeed * Time.deltaTime;
			}

			if (Input.mousePosition.y < ScrollArea)
			{
				translation += forward * -ScrollSpeed * Time.deltaTime;
			}

			if (Input.mousePosition.y > Screen.height - ScrollArea)
			{
				translation += forward * ScrollSpeed * Time.deltaTime;
			}
			*/
		}

		// Keep camera within level and zoom area
		var desiredPosition = camera.transform.position + camera.transform.TransformDirection(translation);

		desiredPosition.x = Mathf.Clamp(desiredPosition.x, -LevelArea, LevelArea);
		desiredPosition.y = Mathf.Clamp(desiredPosition.y, ZoomMin, ZoomMax);
		desiredPosition.z = Mathf.Clamp(desiredPosition.z, -LevelArea, LevelArea);

		// Finally move camera parallel to world axis
		camera.transform.position = desiredPosition;
			
		camera.transform.eulerAngles = new Vector3(pan, yrotation, 0);
		
	}
}

} //namespace