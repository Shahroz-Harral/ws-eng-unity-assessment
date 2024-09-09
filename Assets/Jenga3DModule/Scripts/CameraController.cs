using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform[] stacks; // Assign the stack objects in the Inspector
    public float rotationSpeed = 100.0f; // Speed for rotating the camera
    public float zoomSpeed = 10.0f; // Speed for zooming in and out
    public float minDistance = 2.0f; // Minimum zoom distance
    public float maxDistance = 20.0f; // Maximum zoom distance

    private int currentStackIndex = 0; // Current stack index
    private Transform currentTarget; // Current target to focus on
    private float distanceToTarget; // Current distance from camera to target

    void Start()
    {
        // Initialize the first target stack
        if (stacks.Length > 0)
        {
            currentStackIndex = 0;
            SetTarget(stacks[currentStackIndex]);
        }
    }

    void Update()
    {
        if (currentTarget != null)
        {
            HandleCameraRotation();
            HandleCameraZoom();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchToNextStack();
        }
    }

    private void SetTarget(Transform target)
    {
        currentTarget = target;
        distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
    }

    private void HandleCameraRotation()
    {
        if (Input.GetMouseButton(1)) // Right mouse button for rotation
        {
            float horizontalInput = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            float verticalInput = -Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

            // Rotate around the target based on mouse movement
            transform.RotateAround(currentTarget.position, Vector3.up, horizontalInput);
            transform.RotateAround(currentTarget.position, transform.right, verticalInput);
        }
    }

    private void HandleCameraZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed * Time.deltaTime;
        distanceToTarget -= scrollInput;
        distanceToTarget = Mathf.Clamp(distanceToTarget, minDistance, maxDistance);

        transform.position = currentTarget.position - transform.forward * distanceToTarget;
    }

    private void SwitchToNextStack()
    {
        currentStackIndex = (currentStackIndex + 1) % stacks.Length;
        SetTarget(stacks[currentStackIndex]);
    }
}
