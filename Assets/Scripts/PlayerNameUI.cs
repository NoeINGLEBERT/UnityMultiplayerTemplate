using UnityEngine;
using TMPro; // For TextMeshPro
using PlayFab;
using PlayFab.ClientModels;
using Photon.Pun; // Photon namespace

public class PlayerNameUI : MonoBehaviourPun
{
    public Camera localPlayerCamera; // Reference to the local player's camera
    public TextMeshProUGUI playerNameText; // Text component for displaying the player's username

    private void Start()
    {
        // Ensure the script is on the same GameObject as the PhotonView
        if (photonView == null)
        {
            Debug.LogError("PhotonView is missing! Ensure it is attached to the same GameObject as this script.");
            return;
        }

        if (photonView.IsMine)
        {
            AssignLocalCamera();
            FetchAndSetPlayFabUsername();
        }
        else
        {
            // Ensure we still have a reference to the local camera, even for other players
            AssignLocalCamera();
        }
    }

    private void LateUpdate()
    {
        if (localPlayerCamera != null)
        {
            // Rotate the name tag to always face the local camera
            transform.rotation = Quaternion.LookRotation(transform.position - localPlayerCamera.transform.position, localPlayerCamera.transform.up);
        }
    }

    private void AssignLocalCamera()
    {
        // Find the active camera tagged as PlayerCamera
        if (localPlayerCamera == null)
        {
            Camera[] allCameras = Camera.allCameras;
            foreach (Camera cam in allCameras)
            {
                if (cam.CompareTag("MainCamera") && cam.isActiveAndEnabled)
                {
                    localPlayerCamera = cam;
                    break;
                }
            }
        }

        if (localPlayerCamera == null)
        {
            Debug.LogWarning("No local player camera found! Ensure your camera is tagged properly.");
        }
    }

    private void FetchAndSetPlayFabUsername()
    {
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), result =>
        {
            string username = result.AccountInfo.TitleInfo.DisplayName; // Or retrieve original casing from custom data
            photonView.RPC("UpdatePlayerName", RpcTarget.AllBuffered, username); // Sync username across all clients
        },
        error =>
        {
            Debug.LogError("Failed to fetch PlayFab username: " + error.ErrorMessage);
        });
    }

    [PunRPC]
    private void UpdatePlayerName(string username)
    {
        // Update the UI with the player's username
        if (playerNameText != null)
        {
            playerNameText.text = username;
        }
        else
        {
            Debug.LogWarning("PlayerNameText is not assigned.");
        }
    }
}
