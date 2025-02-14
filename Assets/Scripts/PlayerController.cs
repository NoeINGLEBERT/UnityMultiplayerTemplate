using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody _rb;
    private PhotonView _photonView;

    [SerializeField] private float _moveSpeed;
    private Vector2 _moveDir;
    [SerializeField] private InputActionReference _moveInput;

    [SerializeField] private GameObject _cameraAnchor;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private float _lookSpeed;
    private Vector2 _lookDir;
    [SerializeField] private float _cameraArmLength;
    private RaycastHit _cameraHit;
    [SerializeField] private InputActionReference _lookInput;

    [SerializeField] private float _jumpForce;
    private bool _desiredJump;
    private RaycastHit _groundHit;
    [SerializeField] private InputActionReference _jumpInput;

    private ChatManager _chatManager; // Reference to ChatManager

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _photonView = GetComponent<PhotonView>();

        // Find ChatManager in the scene
        _chatManager = FindFirstObjectByType<ChatManager>();

        if (_photonView.IsMine)
        {
            Cursor.lockState = CursorLockMode.Locked;
            _playerCamera.gameObject.SetActive(true);
        }
        else
        {
            _playerCamera.gameObject.SetActive(false);
            GetComponent<Rigidbody>().isKinematic = true;
            enabled = false;
        }
    }

    private void Update()
    {
        if (!_photonView.IsMine) return;

        // Prevent player movement and jumping if the chat is open
        if (_chatManager != null && _chatManager.isChatOpen)
        {
            _moveDir = Vector2.zero;
            _desiredJump = false;
            return;
        }

        _lookDir = _lookSpeed * _lookInput.action.ReadValue<Vector2>();
        _cameraAnchor.transform.rotation = Quaternion.Euler(
            Mathf.Clamp((_cameraAnchor.transform.rotation.eulerAngles.x - _lookDir.y + 180) % 360, 100, 260) - 180,
            _cameraAnchor.transform.rotation.eulerAngles.y + _lookDir.x,
            _cameraAnchor.transform.rotation.eulerAngles.z
        );

        if (Physics.Raycast(_cameraAnchor.transform.position, -_cameraAnchor.transform.forward, out _cameraHit, _cameraArmLength, LayerMask.GetMask("Ground")))
        {
            _playerCamera.transform.localPosition = new Vector3(0, 0, 1f - _cameraHit.distance);
        }
        else
        {
            _playerCamera.transform.localPosition = new Vector3(0, 0, -_cameraArmLength);
        }

        _moveDir = _moveInput.action.ReadValue<Vector2>();
        _desiredJump |= _jumpInput.action.ReadValue<float>() > 0 &&
                        Physics.Raycast(transform.position, -Vector3.up, out _groundHit, 1.1f, LayerMask.GetMask("Ground"));
    }

    private void FixedUpdate()
    {
        if (!_photonView.IsMine) return;

        if (_chatManager != null && _chatManager.isChatOpen) return; // Prevent movement while typing

        Vector3 movement = Vector3.ProjectOnPlane(_cameraAnchor.transform.forward, Vector3.up).normalized * _moveSpeed * _moveDir.y +
                           _cameraAnchor.transform.right * _moveSpeed * _moveDir.x;
        _rb.linearVelocity = new Vector3(movement.x, _rb.linearVelocity.y, movement.z);

        if (_desiredJump)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _jumpForce, _rb.linearVelocity.z);
            _desiredJump = false;
        }
    }
}
