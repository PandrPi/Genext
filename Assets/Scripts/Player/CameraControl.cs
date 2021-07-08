using UnityEngine;

public class CameraControl : MonoBehaviour
{
	[SerializeField]				private World world;
	[SerializeField, Range(0, 5)]	private float movementSpeed = 0.5f;
	[SerializeField, Range(0, 1)]	private float movementSpeedLerp = 0.05f;
	[SerializeField, Range(0, 1)]	private float zoomSpeed = 1.5f;
	[SerializeField, Range(0, 1)]	private float zoomSpeedLerp = 0.05f;

	private Transform myTransform;
	private Camera myCamera;
	private Vector2 zoomLimit;

	private float deltaTime;
	private Vector3 newPosition;
	private float newZoom;
	private Vector3 Origin;
	private Vector3 Diference;
	private bool Drag = false;

	private const float CameraZPosition = -10f;

	void Start()
    {
		myTransform = transform;
		myCamera = GetComponent<Camera>();

		deltaTime = Time.fixedDeltaTime;
		newPosition = myTransform.position;
		newZoom = myCamera.orthographicSize;
		zoomLimit = new Vector2(1, world.worldSize.y / 2);
	}

    void FixedUpdate()
    {
		if (Input.GetMouseButton(1))
		{
			Vector3 tempOrigin = myCamera.ScreenToWorldPoint(Input.mousePosition);
			Diference = (tempOrigin) - myTransform.position;
			if (Drag == false)
			{
				Drag = true;
				Origin = tempOrigin;
			}
		}
		else
		{
			Drag = false;
		}
		if (Drag == true)
		{
			newPosition = Origin - Diference;
		}


		float zoom = Input.GetAxis("Mouse ScrollWheel") / Time.timeScale;
		newZoom -= zoom * newZoom * zoomSpeed;
		newZoom = Mathf.Clamp(newZoom, zoomLimit.x, zoomLimit.y);
		myCamera.orthographicSize = Mathf.Lerp(myCamera.orthographicSize, newZoom, zoomSpeedLerp);

		if(zoom > 0)
		{
			newPosition = Vector3.Lerp(newPosition, myCamera.ScreenToWorldPoint(Input.mousePosition), System.Math.Abs(zoom));
			newPosition.z = CameraZPosition;
		}

		myTransform.position = Vector3.Lerp(myTransform.position, newPosition, movementSpeedLerp);
	}
}
