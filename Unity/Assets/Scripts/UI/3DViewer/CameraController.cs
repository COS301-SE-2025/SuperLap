using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSensitivity = 2f;
    public float sprintMultiplier = 2f;

    float rotationX = 0f;
    float rotationY = 0f;

    void Start()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        rotationX = euler.y;
        rotationY = euler.x;
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
    }

    void HandleMovement()
    {
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        // WASD (Horizontal/Vertical) + Space (Up) + Ctrl (Down)
        Vector3 direction = new Vector3(
            Input.GetAxis("Horizontal"),
            (Input.GetKey(KeyCode.Space) ? 1 : 0) - (Input.GetKey(KeyCode.LeftControl) ? 1 : 0),
            Input.GetAxis("Vertical")
        );

        // Move relative to camera rotation
        transform.Translate(direction * speed * Time.deltaTime, Space.Self);
    }

    void HandleLook()
    {
        if (Input.GetMouseButton(1)) // right mouse button to look
        {
            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            rotationX += mouseX;
            rotationY -= mouseY;
            rotationY = Mathf.Clamp(rotationY, -89f, 89f);

            transform.rotation = Quaternion.Euler(rotationY, rotationX, 0);
        }
    }
}
