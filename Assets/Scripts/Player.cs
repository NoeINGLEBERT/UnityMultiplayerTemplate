using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;

public class Player : MonoBehaviour
{
    private Rigidbody _rb;

    [SerializeField]
    private float _moveSpeed;
    private Vector2 _moveDir;
    [SerializeField]
    private InputActionReference _moveInput;

    [SerializeField] 
    private GameObject _cameraAnchor;
    [SerializeField]
    private float _lookSpeed;
    private Vector2 _lookDir;
    [SerializeField]
    private float _cameraArmLength;
    private RaycastHit _cameraHit;
    [SerializeField]
    private InputActionReference _lookInput;

    [SerializeField]
    private float _jumpForce;
    private bool _desiredJump;
    private RaycastHit _groundHit;
    [SerializeField]
    private InputActionReference _jumpInput;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        _lookDir = _lookSpeed * _lookInput.action.ReadValue<Vector2>();
        _cameraAnchor.transform.rotation = Quaternion.Euler(Mathf.Clamp((_cameraAnchor.transform.rotation.eulerAngles.x - _lookDir.y + 180)%360, 100, 260) - 180, _cameraAnchor.transform.rotation.eulerAngles.y + _lookDir.x, _cameraAnchor.transform.rotation.eulerAngles.z);
        if (Physics.Raycast(_cameraAnchor.transform.position, -_cameraAnchor.transform.forward, out _cameraHit, _cameraArmLength, LayerMask.GetMask("Ground")))
        {
            _cameraAnchor.transform.Find("Main Camera").transform.localPosition = new Vector3(0, 0, 1f-_cameraHit.distance);
        }
        else
        {
            _cameraAnchor.transform.Find("Main Camera").transform.localPosition = new Vector3(0, 0, -_cameraArmLength);
        }
            

        _moveDir = _moveInput.action.ReadValue<Vector2>();


        _desiredJump |= _jumpInput.action.ReadValue<float>() > 0 & Physics.Raycast(transform.position, -Vector3.up, out _groundHit, 1.1f, LayerMask.GetMask("Ground"));

        Debug.Log(_desiredJump);

    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = Vector3.ProjectOnPlane(_cameraAnchor.transform.forward, Vector3.up).normalized * _moveSpeed * _moveDir.y + _cameraAnchor.transform.right * _moveSpeed * _moveDir.x +  _rb.linearVelocity.y * Vector3.up;

        if (_desiredJump)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _jumpForce, _rb.linearVelocity.z);
            _desiredJump = false;
        }
    }
}
