using General;
using UnityEngine;

namespace Player
{
    public class CameraControl : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private float movementSpeedLerp = 0.05f;
        [SerializeField, Range(0, 1)] private float zoomSpeed = 1.5f;
        [SerializeField, Range(0, 1)] private float zoomSpeedLerp = 0.05f;

        private Transform myTransform;
        private Camera myCamera;
        private Vector2 zoomLimit;

        private Vector3 newPosition;
        private float newZoom;
        private Vector3 origin;
        private Vector3 difference;
        private bool drag;

        private const float CameraZPosition = -10f;

        private void Start()
        {
            myTransform = transform;
            myCamera = GetComponent<Camera>();

            newPosition = myTransform.position;
            newZoom = myCamera.orthographicSize;
            zoomLimit = new Vector2(1, World.Instance.worldSize.y / 2);
        }

        private void FixedUpdate()
        {
            if (Input.GetMouseButton(1))
            {
                var tempOrigin = myCamera.ScreenToWorldPoint(Input.mousePosition);
                difference = tempOrigin - myTransform.position;
                if (drag == false)
                {
                    drag = true;
                    origin = tempOrigin;
                }
            }
            else
            {
                drag = false;
            }

            if (drag)
            {
                newPosition = origin - difference;
            }


            var zoom = Input.GetAxis("Mouse ScrollWheel");
            newZoom -= zoom * newZoom * zoomSpeed;
            newZoom = Mathf.Clamp(newZoom, zoomLimit.x, zoomLimit.y);
            myCamera.orthographicSize = Mathf.Lerp(myCamera.orthographicSize, newZoom, zoomSpeedLerp);

            if (zoom > 0)
            {
                newPosition = Vector3.Lerp(newPosition, myCamera.ScreenToWorldPoint(Input.mousePosition),
                    System.Math.Abs(zoom));
                newPosition.z = CameraZPosition;
            }

            myTransform.position = Vector3.Lerp(myTransform.position, newPosition, movementSpeedLerp);
        }
    }
}