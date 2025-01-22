using Photon.Pun;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject roomPanel;

    public override void OnConnectedToMaster()
    {
        loginPanel.SetActive(false);
        roomPanel.SetActive(true);
        Debug.Log("Connected to Photon Master Server!");
        // You can now join or create a room
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.Log($"Disconnected from Photon: {cause}");
    }

    public void CreateRoom()
    {
        PhotonNetwork.CreateRoom("TestRoom", new Photon.Realtime.RoomOptions { MaxPlayers = 4 });
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No rooms available, creating a new one.");
        CreateRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room successfully!");
        PhotonNetwork.Instantiate("PlayerPrefab", Vector3.zero, Quaternion.identity);
        roomPanel.SetActive(false);
    }
}