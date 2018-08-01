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

	public float TouchRotationMinMagSquared = 1; 
	public float TouchRotationMinAngle = 0.1f;

	private bool touchRotating = false;
	private Vector2 touchRotationStart = Vector2.zero;

	private float yrotation;

	public void Start()
	{
		yrotation = transform.eulerAngles.y;
	}

	// Update is called once per frame
	void Update()
	{
		Vector3 translation = Vector3.zero;
	
		// Zoom in or out
		float zoomDelta = CameraUpDown ();

		if (zoomDelta!=0.0f)
			translation -= Vector3.up * ZoomSpeed * zoomDelta;
		
		float xrotation = transform.eulerAngles.x - zoomDelta * PanSpeed;
		xrotation = Mathf.Clamp(xrotation, PanAngleMin, PanAngleMax);

		yrotation += CameraRotation();

		transform.eulerAngles = new Vector3(0, yrotation, 0);

		translation += CameraMovement();

		// Keep camera within level and zoom area
		Vector3 desiredPosition = transform.position + transform.TransformDirection(translation);

		desiredPosition.x = Mathf.Clamp(desiredPosition.x, -LevelArea, LevelArea);
		desiredPosition.y = Mathf.Clamp(desiredPosition.y, ZoomMin, ZoomMax);
		desiredPosition.z = Mathf.Clamp(desiredPosition.z, -LevelArea, LevelArea);

		// Finally move camera parallel to world axis
		transform.position = desiredPosition;
			
		transform.eulerAngles = new Vector3(xrotation, yrotation, 0);
		
	}

	private float CameraUpDown()
	{
		float delta=0f;

		#if UNITY_STANDALONE
		delta= Input.GetAxis("Mouse ScrollWheel")*ZoomSpeed*Time.deltaTime;
		#else
		if (Input.touchCount == 2)
		{
			Touch touchZero = Input.GetTouch(0);
			Touch touchOne = Input.GetTouch(1);

			Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
			Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

			float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
			float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

			float deltaMagnitudeDiff = (prevTouchDeltaMag - touchDeltaMag)/Mathf.Sqrt(Screen.width*Screen.width + Screen.height*Screen.height);

			delta = deltaMagnitudeDiff;
		}
		#endif

		return delta;
	}

	private float CameraRotation()
	{
		float yrotation = 0.0f;
		#if UNITY_STANDALONE
		if (Input.GetMouseButton(1)) // RMB
			yrotation = Input.GetAxis("Mouse X") * RotationSpeed * Time.deltaTime;
		#else
		if (Input.touchCount == 2)
		{
			if (!touchRotating)
			{
				touchRotationStart = Input.touches [1].position - Input.touches [0].position;
				touchRotating = touchRotationStart.sqrMagnitude > TouchRotationMinMagSquared;
			} 
			else
			{
				Vector2 currVector = Input.touches [1].position - Input.touches [0].position;
				float angleOffset = Vector2.Angle(touchRotationStart, currVector);

				if (angleOffset > TouchRotationMinAngle)
				{
					Vector3 LR = Vector3.Cross(touchRotationStart, currVector);
					// z > 0 left rotation, z < 0 right rotation
					yrotation -= Mathf.Sign(LR.z) * angleOffset;

					touchRotationStart = currVector;
				}
			}
		}
		else
			touchRotating = false;	
		#endif

		return yrotation;
	}

	private Vector3 CameraMovement()
	{
		Vector3 move = Vector3.zero;
		#if UNITY_STANDALONE
		if (Input.GetMouseButton(0)) // LMB
		{
			move += new Vector3(Input.GetAxis("Mouse X") * DragSpeed * Time.deltaTime, 0, 
			Input.GetAxis("Mouse Y") * DragSpeed * Time.deltaTime);
		}
		#else
		if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
		{
			Touch touch = Input.GetTouch (0);

			move += new Vector3(touch.deltaPosition.x / Screen.width * DragSpeed, 0, 
				touch.deltaPosition.y / Screen.height * DragSpeed);				
		}
		#endif

		return move;
	}
}

} //namespace